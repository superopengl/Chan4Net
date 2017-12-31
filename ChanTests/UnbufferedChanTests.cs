using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Chan;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChanTests
{
    [TestClass]
    public class UnbufferedChanTests
    {
        private readonly TimeSpan _awaitTimeout = TimeSpan.FromMilliseconds(100);
        private readonly TimeSpan _slowActionLetency = TimeSpan.FromMilliseconds(100);

        [TestMethod]
        public void Receive_ShouldBeBlocked_IfNoOneSend()
        {
            var chan = new UnbufferedChan<int>();
            var called = false;
            var task = Task.Run(() =>
            {
                chan.Receive();
                called = true;
            });

            task.Wait(_awaitTimeout);

            Assert.IsFalse(called);
        }

        [TestMethod]
        public void Receive_ShouldGetTheSameObjectThatSenderSent()
        {
            var chan = new UnbufferedChan<object>();
            var item = new object();
            object result = null;
            var receiver = Task.Run(() =>
            {
                Thread.Sleep(_slowActionLetency);
                chan.Send(item);
            });

            result = chan.Receive();
            receiver.Wait();

            Assert.AreSame(item, result);
        }

        [TestMethod]
        public void Receive_ShouldNotBeBlocked_OnceOneSend()
        {
            var chan = new UnbufferedChan<int>();
            var called = false;
            var item = 0;
            var receiver = Task.Run(() =>
            {
                item = chan.Receive();
                called = true;
            });

            Thread.Sleep(_slowActionLetency);
            chan.Send(1); // Make sure Receive() is called before Send()

            receiver.Wait();

            Assert.AreEqual(1, item);
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Receive_RaceCondition_OnlyOneCanGetTheSentItemEachTime()
        {
            var chan = new UnbufferedChan<int>();
            var items = new ConcurrentBag<int>();
            var receiver1 = Task.Run(() => items.Add(chan.Receive()));
            var receiver2 = Task.Run(() => items.Add(chan.Receive()));
            var receiver3 = Task.Run(() => items.Add(chan.Receive()));

            Thread.Sleep(_slowActionLetency);
            chan.Send(1);

            Task.WaitAll(new[] { receiver1, receiver2, receiver3 }, _awaitTimeout);

            CollectionAssert.AreEquivalent(new[] { 1 }, items.ToArray());
        }

        [TestMethod]
        public void Receive_RaceCondition_OneItemIsReceivedOnlyOnce()
        {
            var chan = new UnbufferedChan<int>();
            var items = new ConcurrentBag<int>();
            var samples = Enumerable.Range(0, 3).ToArray();
            var receivers = samples.Select(i => { return Task.Run(() => items.Add(chan.Receive())); }).ToArray();

            Thread.Sleep((int)_slowActionLetency.TotalMilliseconds);
            foreach (var i in samples)
            {
                chan.Send(i);
            }

            Task.WaitAll(receivers);

            CollectionAssert.AreEquivalent(samples, items.ToArray());
        }

        [TestMethod]
        public void Receive_NoSend_NoBufferedChan_ShouldBlock()
        {
            var sut = new UnbufferedChan<int>();
            var called = false;
            var receiver = Task.Run(() =>
            {
                sut.Receive();
                called = true;
            });

            receiver.Wait(_awaitTimeout);

            Assert.IsFalse(called);
        }

        [TestMethod]
        public void Send_NoReceiver_NoBufferedChan_ShouldThrow()
        {
            var sut = new UnbufferedChan<int>();
            Exception exception = null;
            try
            {
                sut.Send(1);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.IsInstanceOfType(exception, typeof(InvalidOperationException));
        }
    }
}