using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

namespace Chan4Net.Helpers
{
    internal class ChanYieldEnumerator<T> : IEnumerator<T>
    {
        private readonly CancellationToken _cancellationToken;
        private readonly IChan<T> _target;

        public ChanYieldEnumerator(IChan<T> target, CancellationToken cancellationToken)
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
                Current = _target.Receive(_cancellationToken);
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