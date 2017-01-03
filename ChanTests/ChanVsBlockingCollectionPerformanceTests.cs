using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Chan;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ChanTests
{
    [TestClass]
    public class ChanVsBlockingCollectionPerformanceTests
    {
        [TestMethod]
        public void OneProducer_OneConsumer_1Buffer_100000Items()
        {
            OneProducer_OneConsumer(1, 100000, 10);
        }

        [TestMethod]
        public void OneProducer_OneConsumer_1000Buffer_100000Items()
        {
            OneProducer_OneConsumer(1000, 100000, 10);
        }

        [TestMethod]
        public void OneProducer_OneConsumer_100000Buffer_100000Items()
        {
            OneProducer_OneConsumer(100000, 100000, 10);
        }

        private void OneProducer_OneConsumer(int capacity, int itemCount, int repeatTimes)
        {
            Console.WriteLine("Buffer size: {0}, Total items: {1}, by running {2} times", capacity, itemCount,
                repeatTimes);
            new PerformanceCaseRepeatedRunner("BufferedChan", repeatTimes,
                () => OneProducer_OneConsumer_Chan(capacity, itemCount)).Run();
            new PerformanceCaseRepeatedRunner("BlockingCollection", repeatTimes,
                () => OneProducer_OneConsumer_BlockingCollection(capacity, itemCount)).Run();
        }

        private void OneProducer_OneConsumer_Chan(int capacity, int itemCount)
        {
            var chan = new Chan<int>(capacity);
            var producer = Task.Run(() =>
            {
                foreach (var i in Enumerable.Range(0, itemCount))
                {
                    chan.Send(i);
                }
                chan.Close();
            });

            var consumer = Task.Run(() =>
            {
                foreach (var i in chan.Yield())
                {
                }
            });

            Task.WaitAll(producer, consumer);
        }

        private void OneProducer_OneConsumer_BlockingCollection(int capacity, int itemCount)
        {
            var blockingCollection = new BlockingCollection<int>(capacity);
            var producer = Task.Run(() =>
            {
                foreach (var i in Enumerable.Range(0, itemCount))
                {
                    blockingCollection.Add(i);
                }
                blockingCollection.CompleteAdding();
            });

            var consumer = Task.Run(() =>
            {
                foreach (var i in blockingCollection.GetConsumingEnumerable())
                {
                }
            });

            Task.WaitAll(producer, consumer);
        }

        private class PerformanceCaseRepeatedRunner
        {
            private readonly Action _action;
            private readonly string _label;
            private readonly int _repeatTimes;

            public PerformanceCaseRepeatedRunner(string label, int repeatTimes, Action action)
            {
                _label = label;
                _repeatTimes = repeatTimes;
                _action = action;
            }

            public void Run()
            {
                var sw = Stopwatch.StartNew();
                foreach (var i in Enumerable.Range(0, _repeatTimes))
                {
                    _action();
                }
                sw.Stop();
                Console.WriteLine(@"{0}: 
    Average time: {1} ms ({2} ticks)", _label, sw.ElapsedMilliseconds/_repeatTimes, sw.ElapsedTicks/_repeatTimes);
            }
        }
    }
}