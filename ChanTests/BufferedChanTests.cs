using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Chan.BufferedChan;

namespace ChanTests
{
    [TestClass]
    public class BufferedChanTests
    {
        private readonly TimeSpan _awaitTimeout = TimeSpan.FromMilliseconds(1000);
        private readonly TimeSpan _slowActionLetency = TimeSpan.FromMilliseconds(500);

        [TestMethod]
        [ExpectedException(typeof (ArgumentOutOfRangeException))]
        public void Ctor_ChanSizeZero_ShouldThrow()
        {
            var sut = new BufferedChan<int>(0);
        }

        [TestMethod]
        public void Send_LessThanSize_SendShouldNotBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            var called = false;

            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                called = true;
            });

            producer.Wait();

            Assert.AreEqual(2, sut.Count);
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void Send_AfterClosed_ShouldThrow()
        {
            var sut = new BufferedChan<int>(2);
            sut.Send(1);
            sut.Close();

            Exception exception = null;
            try
            {
                sut.Send(2);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.AreEqual(1, sut.Count);
            Assert.IsTrue(sut.IsClosed);
            Assert.IsInstanceOfType(exception, typeof (InvalidOperationException));
        }

        [TestMethod]
        public void Send_MoreThanSize_SendShouldBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            var called = false;
            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
                called = true;
            });

            producer.Wait(_awaitTimeout);

            Assert.AreEqual(2, sut.Count);
            Assert.IsFalse(called);
        }

        [TestMethod]
        public void SendMany_ReceiveFew_SendShouldBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            bool? called = null;
            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
                sut.Send(4);
                sut.Send(5);
                called = false;
                sut.Send(6);
                called = true;
            });

            var items = new List<int> {sut.Receive(), sut.Receive(), sut.Receive()};

            producer.Wait(_awaitTimeout);

            Assert.AreEqual(2, sut.Count);
            Assert.IsFalse(called != null && called.Value);
            CollectionAssert.AreEquivalent(new[] {1, 2, 3}, items.ToArray());
        }

        [TestMethod]
        public void Send_CancellationToken_ShouldThrow()
        {
            var sut = new BufferedChan<int>(2);
            Exception exception = null;
            var cts = new CancellationTokenSource();
            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                try
                {
                    sut.Send(3, cts.Token);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            producer.Wait(_awaitTimeout);
            cts.Cancel();
            producer.Wait(); // Await the catch block to finish

            Assert.AreEqual(2, sut.Count);
            Assert.IsInstanceOfType(exception, typeof (OperationCanceledException));
        }

        [TestMethod]
        public void SendFew_ReceiveMany_ReceiveShouldBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            sut.Send(1);
            sut.Send(2);

            bool? called = null;
            var items = new List<int>();
            var consumer = Task.Run(() =>
            {
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                called = false;
                items.Add(sut.Receive());
                called = true;
            });

            consumer.Wait(_awaitTimeout);

            Assert.AreEqual(0, sut.Count);
            Assert.IsFalse(called != null && called.Value);
            CollectionAssert.AreEquivalent(new[] {1, 2}, items.ToArray());
        }

        [TestMethod]
        public void Receive_FromEmptyChan_ReceiveShouldBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            var called = false;
            var producer = Task.Run(() =>
            {
                var item = sut.Receive();
                called = true;
            });

            producer.Wait(_awaitTimeout);

            Assert.AreEqual(0, sut.Count);
            Assert.IsFalse(called);
        }

        [TestMethod]
        public void Receive_CancellationToken_ShouldThrow()
        {
            var sut = new BufferedChan<int>(2);
            var cts = new CancellationTokenSource();
            Exception exception = null;
            var consumer = Task.Run(() =>
            {
                try
                {
                    sut.Receive(cts.Token);
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            consumer.Wait(_awaitTimeout);
            cts.Cancel();
            consumer.Wait(); // Await the catch block to finish

            Assert.IsInstanceOfType(exception, typeof (OperationCanceledException));
        }

        [TestMethod]
        public void Receive_FromEmptyChanAfterClosed_ShouldThrow()
        {
            var sut = new BufferedChan<int>(2);
            sut.Close();
            Exception exception = null;
            try
            {
                sut.Receive();
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            Assert.AreEqual(0, sut.Count);
            Assert.IsTrue(sut.IsClosed);
            Assert.IsInstanceOfType(exception, typeof (InvalidOperationException));
        }

        [TestMethod]
        public void Receive_FromNonEmptyChan_ShouldNotBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            sut.Send(1);
            sut.Send(2);

            var item1 = sut.Receive();
            var item2 = sut.Receive();

            Assert.AreEqual(0, sut.Count);
            Assert.AreEqual(1, item1);
            Assert.AreEqual(2, item2);
        }

        [TestMethod]
        public void Yield_NotClosedChan_ShouldBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
            });

            var items = new List<int>();
            var called = false;
            var consumer = Task.Run(() =>
            {
                foreach (var i in sut.Yield())
                {
                    items.Add(i);
                }
                called = true;
            });

            producer.Wait();
            consumer.Wait(_awaitTimeout);

            Assert.AreEqual(3, items.Count);
            Assert.IsFalse(called);
        }

        [TestMethod]
        public void Yield_ClosedChan_ShouldNotBeBlocked()
        {
            var sut = new BufferedChan<int>(2);
            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
            });

            var items = new List<int>();
            var called = false;
            var consumer = Task.Run(() =>
            {
                foreach (var i in sut.Yield())
                {
                    items.Add(i);
                }
                called = true;
            });

            producer.Wait();
            sut.Close();
            consumer.Wait(_awaitTimeout);

            Assert.AreEqual(3, items.Count);
            Assert.IsTrue(called);
        }

        [TestMethod]
        public void ProducerConsumer_MoreThanChanSize()
        {
            var sut = new BufferedChan<int>(2);
            var producerCalled = false;
            var totalItemCount = 100;
            var producer = Task.Run(() =>
            {
                for (var i = 1; i <= totalItemCount; i++)
                {
                    sut.Send(i);
                }
                producerCalled = true;
            });

            var consumerCalled = false;
            var items = new List<int>();
            var consumer = Task.Run(() =>
            {
                for (var i = 1; i <= totalItemCount; i++)
                {
                    items.Add(sut.Receive());
                }
                consumerCalled = true;
            });

            Task.WaitAll(producer, consumer);

            Assert.AreEqual(0, sut.Count);
            Assert.IsTrue(producerCalled);
            Assert.IsTrue(consumerCalled);
            CollectionAssert.AreEquivalent(Enumerable.Range(1, totalItemCount).ToArray(), items.ToArray());
        }

        [TestMethod]
        public void ProducerConsumer_SlowProducer_FastConsumer()
        {
            var sut = new BufferedChan<int>(2);
            var producerCalled = false;
            var producer = Task.Run(async () =>
            {
                await Task.Delay(_slowActionLetency);
                sut.Send(1);
                await Task.Delay(_slowActionLetency);
                sut.Send(2);
                await Task.Delay(_slowActionLetency);
                sut.Send(3);
                producerCalled = true;
            });

            var consumerCalled = false;
            var items = new List<int>();
            var consumer = Task.Run(() =>
            {
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                consumerCalled = true;
            });

            Task.WaitAll(producer, consumer);

            Assert.AreEqual(0, sut.Count);
            Assert.IsTrue(producerCalled);
            Assert.IsTrue(consumerCalled);
            CollectionAssert.AreEquivalent(new[] {1, 2, 3}, items.ToArray());
        }

        [TestMethod]
        public void ProducerConsumer_FastProducer_SlowConsumer()
        {
            var sut = new BufferedChan<int>(2);
            var producerCalled = false;
            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
                producerCalled = true;
            });

            var consumerCalled = false;
            var items = new List<int>();
            var consumer = Task.Run(async () =>
            {
                await Task.Delay(_slowActionLetency);
                items.Add(sut.Receive());
                await Task.Delay(_slowActionLetency);
                items.Add(sut.Receive());
                await Task.Delay(_slowActionLetency);
                items.Add(sut.Receive());
                consumerCalled = true;
            });

            Task.WaitAll(producer, consumer);

            Assert.AreEqual(0, sut.Count);
            Assert.IsTrue(producerCalled);
            Assert.IsTrue(consumerCalled);
            CollectionAssert.AreEquivalent(new[] {1, 2, 3}, items.ToArray());
        }

        [TestMethod]
        public void ProducerConsumer_MultipleProducers_MultipleConsumers()
        {
            var sut = new BufferedChan<int>(2);
            var producer1Called = false;
            var producer1 = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
                producer1Called = true;
            });

            var producer2Called = false;
            var producer2 = Task.Run(() =>
            {
                sut.Send(4);
                sut.Send(5);
                sut.Send(6);
                producer2Called = true;
            });

            var items = new ConcurrentBag<int>();
            var consumer1Called = false;
            var consumer1 = Task.Run(() =>
            {
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                consumer1Called = true;
            });

            var consumer2Called = false;
            var consumer2 = Task.Run(() =>
            {
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                consumer2Called = true;
            });

            Task.WaitAll(producer1, producer2, consumer1, consumer2);

            Assert.AreEqual(0, sut.Count);
            Assert.IsTrue(producer1Called);
            Assert.IsTrue(producer2Called);
            Assert.IsTrue(consumer1Called);
            Assert.IsTrue(consumer2Called);
            CollectionAssert.AreEquivalent(new[] {1, 2, 3, 4, 5, 6}, items.ToArray());
        }

        [TestMethod]
        public void ProducerConsumer_SingleProducer_MultipleConsumers()
        {
            var sut = new BufferedChan<int>(2);
            var producerCalled = false;
            var producer = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
                sut.Send(4);
                sut.Send(5);
                sut.Send(6);
                producerCalled = true;
            });

            var items = new ConcurrentBag<int>();
            var consumer1Called = false;
            var consumer1 = Task.Run(() =>
            {
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                consumer1Called = true;
            });

            var consumer2Called = false;
            var consumer2 = Task.Run(() =>
            {
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                consumer2Called = true;
            });

            Task.WaitAll(producer, consumer1, consumer2);

            Assert.AreEqual(0, sut.Count);
            Assert.IsTrue(producerCalled);
            Assert.IsTrue(consumer1Called);
            Assert.IsTrue(consumer2Called);
            CollectionAssert.AreEquivalent(new[] {1, 2, 3, 4, 5, 6}, items.ToArray());
        }

        [TestMethod]
        public void ProducerConsumer_MultipleProducers_SingleConsumer()
        {
            var sut = new BufferedChan<int>(2);
            var producer1Called = false;
            var producer1 = Task.Run(() =>
            {
                sut.Send(1);
                sut.Send(2);
                sut.Send(3);
                producer1Called = true;
            });

            var producer2Called = false;
            var producer2 = Task.Run(() =>
            {
                sut.Send(4);
                sut.Send(5);
                sut.Send(6);
                producer2Called = true;
            });

            var items = new ConcurrentBag<int>();
            var consumerCalled = false;
            var consumer = Task.Run(() =>
            {
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                items.Add(sut.Receive());
                consumerCalled = true;
            });

            Task.WaitAll(producer1, producer2, consumer);

            Assert.AreEqual(0, sut.Count);
            Assert.IsTrue(producer1Called);
            Assert.IsTrue(producer2Called);
            Assert.IsTrue(consumerCalled);
            CollectionAssert.AreEquivalent(new[] {1, 2, 3, 4, 5, 6}, items.ToArray());
        }
    }
}