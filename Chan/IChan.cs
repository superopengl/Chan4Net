using System.Collections.Generic;
using System.Threading;

namespace Chan4Net
{
    /// <summary>
    ///     Golang chan like implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IChan<T>
    {
        /// <summary>
        ///     Returns if the channel has been closed. You cannot continue adding item into a closed channel.
        ///     However, items remaining in the channel can be taken and yielded until the channel is empty.
        /// </summary>
        bool IsClosed { get; }
        /// <summary>
        ///     Closes the channel. No more item can be sent into channel after this is called.
        /// </summary>
        void Close();
        /// <summary>
        ///     Sends an item into channel. 
        ///     For a buffered channel, If the channel is full, this method will block the current thread,
        ///     until some item is taken.
        /// </summary>
        void Send(T item);
        /// <summary>
        ///     Sends an item into channel. 
        ///     For a buffered channel, If the channel is full, this method will block the current thread,
        ///     until some item is taken.
        ///     If the cancel source is fired, this method will throw an OperationCanceledException.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="cancellationToken"></param>
        void Send(T item, CancellationToken cancellationToken);
        /// <summary>
        ///     Removes and returns an item from channel. If the channel is empty, this will block the current thread,
        ///     until an item is sent into the channel.
        /// </summary>
        /// <returns></returns>
        T Receive();
        /// <summary>
        ///     Removes and returns an item from channel. If the channel is empty, this will block the current thread,
        ///     until an item is sent into the channel.
        ///     If the cancel source is fired, this method will throw an OperationCanceledException.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        T Receive(CancellationToken cancellationToken);
        /// <summary>
        ///     Removes and returns elements from the channel.
        ///     Empty channel will block the current thread.
        ///     Unlike Receive(), cancelling token and closing channel won't throw exception but just break the loop.
        /// </summary>
        /// <returns></returns>
        IEnumerable<T> Yield();
        /// <summary>
        ///     Removes and returns elements from the channel.
        ///     Empty channel will block the current thread.
        ///     Unlike Receive(), cancelling token and closing channel won't throw exception but just break the loop.
        /// </summary>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        IEnumerable<T> Yield(CancellationToken cancellationToken);
    }
}