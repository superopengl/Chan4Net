using Chan.BufferedChan;
using Chan.UnbufferedChan;
using System.Collections.Generic;
using System.Threading;

namespace Chan
{
    /// <summary>
    ///     Golang chan like implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Chan<T> : IChan<T>
    {
        private readonly IChan<T> _chan;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="size">null or an integer no less than 1. if null or not specified, an unbuffered chan will be created.</param>
        public Chan(int? size = null)
        {
            if (size.HasValue)
            {
                _chan = new BufferedChan<T>(size.Value);
            }
            else
            {
                _chan = new UnbufferedChan<T>();
            }
        }
        /// <summary>
        ///     Returns if the channel has been closed. You cannot continue adding item into a closed channel.
        ///     However, items remaining in the channel can be taken and yielded until the channel is empty.
        /// </summary>
        public bool IsClosed
        {
            get { return _chan.IsClosed; }
        }
        /// <summary>
        ///     Closes the channel. No more item can be sent into channel after this is called.
        /// </summary>
        public void Close()
        {
            _chan.Close();
        }
        /// <summary>
        ///     Sends an item into channel. 
        ///     For a buffered channel, If the channel is full, this method will block the current thread,
        ///     until some item is taken.
        /// </summary>
        public void Send(T item)
        {
            _chan.Send(item);
        }
        /// <summary>
        ///     Sends an item into channel. 
        ///     For a buffered channel, If the channel is full, this method will block the current thread,
        ///     until some item is taken.
        ///     If the cancel source is fired, this method will throw an OperationCanceledException.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        public void Send(T item, CancellationToken cancellationToken)
        {
            _chan.Send(item, cancellationToken);
        }
        /// <summary>
        ///     Removes and returns an item from channel. If the channel is empty, this will block the current thread,
        ///     until an item is sent into the channel.
        /// </summary>
        /// <returns></returns>
        public T Receive()
        {
            return _chan.Receive();
        }
        /// <summary>
        ///     Removes and returns an item from channel. If the channel is empty, this will block the current thread,
        ///     until an item is sent into the channel.
        ///     If the cancel source is fired, this method will throw an OperationCanceledException.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public T Receive(CancellationToken cancellationToken)
        {
            return _chan.Receive(cancellationToken);
        }
        /// <summary>
        ///     Removes and returns elements from the channel.
        ///     Empty channel will block the current thread.
        ///     Unlike Receive(), cancelling token and closing channel won't throw exception but just break the loop.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<T> Yield()
        {
            return _chan.Yield();
        }
        /// <summary>
        ///     Removes and returns elements from the channel.
        ///     Empty channel will block the current thread.
        ///     Unlike Receive(), cancelling token and closing channel won't throw exception but just break the loop.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public IEnumerable<T> Yield(CancellationToken cancellationToken)
        {
            return _chan.Yield(cancellationToken);
        }
    }
}