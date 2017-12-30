using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Chan
{
    /// <summary>
    ///     Golang chan like channel, based on ConcurrentQueue.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Chan<T>
    {
        private readonly ManualResetEventSlim _canAddEvent;
        private readonly ManualResetEventSlim _canTakeEvent;
        private readonly ConcurrentQueue<T> _queue;
        private readonly int _size;

        /// <summary>
        /// </summary>
        /// <param name="size">At least 1.</param>
        public Chan(int size)
        {
            if (size < 1) throw new ArgumentOutOfRangeException("size", size, "Chan size must be at least 1.");
            _size = size;
            _queue = new ConcurrentQueue<T>();
            _canTakeEvent = new ManualResetEventSlim(false);
            _canAddEvent = new ManualResetEventSlim(false);
        }

        /// <summary>
        ///     If the channel is closed. You cannot continue adding item into a closed channel.
        ///     However, items remaining in the channel can be taken and yielded until the channel is empty.
        /// </summary>
        public bool IsClosed { get; private set; }

        /// <summary>
        ///     How many items in channel currently
        /// </summary>
        public int Count
        {
            get { return _queue.Count; }
        }

        /// <summary>
        ///     Add an item into channel. If the channel is full, this method will block the current thread,
        ///     until some item is taken.
        ///     If the cancel source is fired, this method will throw an OperationCanceledException.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        public void Add(T item, CancellationToken cancellationToken)
        {
            while (_queue.Count == _size)
            {
                AssertNotClosed();
                _canAddEvent.Wait(cancellationToken);
            }
            AssertNotClosed();
            _queue.Enqueue(item);
            _canTakeEvent.Set();
        }

        /// <summary>
        ///     Add an item into channel. If the channel is full, this method will block the current thread,
        ///     until some item is taken.
        /// </summary>
        public void Add(T item)
        {
            Add(item, CancellationToken.None);
        }

        /// <summary>
        ///     Close the channel. After this, no more item can be added into channel.
        /// </summary>
        public void Close()
        {
            IsClosed = true;
        }

        private void AssertNotClosed()
        {
            if (IsClosed) throw new InvalidOperationException("The chan has been closed");
        }

        /// <summary>
        ///     Dequeue an item from channel. If the channel is empty, this will block the current thread,
        ///     until an item is added into the channel.
        /// </summary>
        /// <returns></returns>
        public T Take()
        {
            return Take(CancellationToken.None);
        }

        /// <summary>
        ///     Dequeue an item from channel. If the channel is empty, this will block the current thread,
        ///     until an item is added into the channel.
        ///     If the cancel source is fired, this method will throw an OperationCanceledException.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public T Take(CancellationToken cancellationToken)
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
                    AssertNotClosed();
                }
                _canTakeEvent.Wait(cancellationToken);
            }
        }

        /// <summary>
        ///     Remove and return element from the channel.
        ///     Empty channel will block the current thread.
        ///     Unlike Take(), cancelling token and closing channel won't throw exception but just break the loop.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> Yield()
        {
            return Yield(CancellationToken.None);
        }

        /// <summary>
        ///     Remove and return element from the channel.
        ///     Empty channel will block the current thread.
        ///     Unlike Take(), cancelling token and closing channel won't throw exception but just break the loop.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IEnumerable<T> Yield(CancellationToken cancellationToken)
        {
            var enumerator = new ChanYieldEnumerator<T>(this, cancellationToken);
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }

        private class ChanYieldEnumerator<T> : IEnumerator<T>
        {
            private readonly CancellationToken _cancellationToken;
            private readonly Chan<T> _target;

            public ChanYieldEnumerator(Chan<T> target, CancellationToken cancellationToken)
            {
                _target = target;
                _cancellationToken = cancellationToken;
            }

            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                try
                {
                    Current = _target.Take(_cancellationToken);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    return false;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            public T Current { get; private set; }

            object IEnumerator.Current
            {
                get { return Current; }
            }
        }
    }
}