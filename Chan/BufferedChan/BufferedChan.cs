using Chan4Net.Helpers;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Chan4Net.BufferedChan
{
    internal class BufferedChan<T> : IChan<T>
    {
        private readonly ManualResetEventSlim _canAddEvent;
        private readonly ManualResetEventSlim _canTakeEvent;
        private readonly ConcurrentQueue<T> _queue;
        private readonly int _size;

        public BufferedChan(int size)
        {
            if (size < 1) throw new ArgumentOutOfRangeException("size", size, "The size of a buffered channel must be greater than 1");
            _size = size;
            _queue = new ConcurrentQueue<T>();
            _canTakeEvent = new ManualResetEventSlim(false);
            _canAddEvent = new ManualResetEventSlim(false);
        }

        public int Count
        {
            get { return _queue.Count; }
        }

        public bool IsClosed { get; private set; }
        
        public void Send(T item, CancellationToken cancellationToken)
        {
            while (_queue.Count == _size)
            {
                ChanUtility.AssertChanNotClosed(this);
                _canAddEvent.Wait(cancellationToken);
            }
            ChanUtility.AssertChanNotClosed(this);
            _queue.Enqueue(item);
            _canTakeEvent.Set();
        }
        
        public void Send(T item)
        {
            Send(item, CancellationToken.None);
        }
        
        public void Close()
        {
            IsClosed = true;
        }
        
        public T Receive()
        {
            return Receive(CancellationToken.None);
        }
        
        public T Receive(CancellationToken cancellationToken)
        {
            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }
                T item;
                if (_queue.TryDequeue(out item))
                {
                    _canAddEvent.Set();
                    return item;
                }
                if (_queue.Count == 0)
                {
                    ChanUtility.AssertChanNotClosed(this);
                }
                _canTakeEvent.Wait(cancellationToken);
            }
        }
        
        public IEnumerable<T> Yield()
        {
            return Yield(CancellationToken.None);
        }
        
        public IEnumerable<T> Yield(CancellationToken cancellationToken)
        {
            var enumerator = new ChanYieldEnumerator<T>(this, cancellationToken);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }
}