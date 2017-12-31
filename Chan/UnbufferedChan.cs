using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

namespace Chan
{
    public class UnbufferedChan<T> : IChan<T>
    {
        private readonly ConcurrentQueue<UnbufferedChanReceiver<T>> _receivers;

        public UnbufferedChan()
        {
            _receivers = new ConcurrentQueue<UnbufferedChanReceiver<T>>();
        }

        public bool IsClosed { get; private set; }

        public void Close()
        {
            IsClosed = true;
        }

        public void Send(T item)
        {
            Send(item, CancellationToken.None);
        }

        public void Send(T item, CancellationToken cancellationToken)
        {
            AssertNotClosed();
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
            UnbufferedChanReceiver<T> receiver;
            if (_receivers.Count == 0 || !_receivers.TryDequeue(out receiver))
                throw new InvalidOperationException("No alive receiver for this no buffered chan.");
            receiver.WakeUp(() => item);
        }

        public T Receive()
        {
            return Receive(CancellationToken.None);
        }

        public T Receive(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException(cancellationToken);
            AssertNotClosed();

            using (var receiver = new UnbufferedChanReceiver<T>())
            {
                _receivers.Enqueue(receiver);
                return receiver.WaitForValue(cancellationToken);
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

        private void AssertNotClosed()
        {
            if (IsClosed) throw new InvalidOperationException("The chan has been closed");
        }
    }
}