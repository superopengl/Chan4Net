using System.Collections.Generic;
using System.Threading;

namespace Chan
{
    public interface IChan<T>
    {
        bool IsClosed { get; }
        void Close();
        void Send(T item);
        void Send(T item, CancellationToken cancellationToken);
        T Receive();
        T Receive(CancellationToken cancellationToken);
        IEnumerable<T> Yield();
        IEnumerable<T> Yield(CancellationToken cancellationToken);
    }
}