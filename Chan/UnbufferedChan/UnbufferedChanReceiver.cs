using System;
using System.Threading;

namespace Chan4Net.UnbufferedChan
{
    internal class UnbufferedChanReceiver<T> : IDisposable
    {
        private readonly ManualResetEventSlim _eventHandler;
        private Func<T> _getValueFunc;

        public UnbufferedChanReceiver()
        {
            _eventHandler = new ManualResetEventSlim(false);
        }

        public void Dispose()
        {
            _eventHandler.Dispose();
        }

        public void WakeUp(Func<T> getValueFunc)
        {
            if (getValueFunc == null) throw new ArgumentNullException("getValueFunc");
            _getValueFunc = getValueFunc;
            _eventHandler.Set();
        }

        public T WaitForValue(CancellationToken cancellationToken)
        {
            while (_getValueFunc == null)
            {
                _eventHandler.Wait(cancellationToken);
            }
            return _getValueFunc();
        }
    }
}