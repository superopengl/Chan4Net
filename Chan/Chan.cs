using System.Collections.Generic;
using System.Threading;

namespace Chan
{
    public class Chan<T> : IChan<T>
    {
        private readonly IChan<T> _chan;

        public Chan(int size = 0)
        {
            _chan = size == 0 ? (IChan<T>) new UnbufferedChan<T>() : new BufferedChan<T>(size);
        }

        public bool IsClosed
        {
            get { return _chan.IsClosed; }
        }

        public void Close()
        {
            _chan.Close();
        }

        public void Send(T item)
        {
            _chan.Send(item);
        }

        public void Send(T item, CancellationToken cancellationToken)
        {
            _chan.Send(item, cancellationToken);
        }

        public T Receive()
        {
            return _chan.Receive();
        }

        public T Receive(CancellationToken cancellationToken)
        {
            return _chan.Receive(cancellationToken);
        }

        public IEnumerable<T> Yield()
        {
            return _chan.Yield();
        }

        public IEnumerable<T> Yield(CancellationToken cancellationToken)
        {
            return _chan.Yield(cancellationToken);
        }
    }
}