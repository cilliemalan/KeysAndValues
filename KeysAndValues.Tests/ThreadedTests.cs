using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace KeysAndValues.Tests
{
    public class ThreadedTests
    {
        [Fact]
        public void BasicMultiThreadedAddTest()
        {
            var kvs = KeyValueStore.CreateEmpty();
            var data = Corpus.GenerateUnsorted(10000).ToList();
            var cmpkvs = KeyValueStore.CreateNewFrom(data);
            var channel = Channel.CreateUnbounded<KeyValuePair<Mem, Mem>>();
            var threads = Enumerable.Range(0, 16).Select(x => new Thread(() =>
            {
                while (channel.Reader.TryRead(out var item))
                {
                    kvs.Set(item.Key, item.Value);
                }
            })
            {
                IsBackground = true,
                Name = $"Thread-{x}"
            }).ToArray();

            foreach (var kvp in data)
            {
                channel.Writer.TryWrite(kvp);
            }
            channel.Writer.Complete();

            foreach (var thread in threads)
            {
                thread.Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            Assert.Equal(cmpkvs.Count, kvs.Count);
            Assert.Equal(cmpkvs.Snapshot().AsEnumerable(), kvs.Snapshot().AsEnumerable());
        }

        [Fact]
        public void BasicMultiThreadedAddAndReadTest()
        {
            var kvs = KeyValueStore.CreateEmpty();
            var data = Corpus.GenerateUnsorted(10000).ToDictionary();
            var writeChannel = Channel.CreateUnbounded<KeyValuePair<Mem, Mem>>();
            var readChannel = Channel.CreateUnbounded<KeyValuePair<Mem, Mem>>();
            var writeThreads = Enumerable.Range(0, 16).Select(x => new Thread(() =>
            {
                while (writeChannel.Reader.TryRead(out var item))
                {
                    kvs.Set(item.Key, item.Value);
                }
            })
            {
                IsBackground = true,
                Name = $"Thread-{x}"
            }).ToArray();
            long matches = 0;
            long mismatches = 0;
            long hits = 0;
            long misses = 0;
            var readThreads = Enumerable.Range(0, 16).Select(x => new Thread(() =>
            {
                while (readChannel.Reader.TryRead(out var item))
                {
                    bool couldGet = kvs.TryGet(item.Key, out var value);
                    if (couldGet)
                    {
                        Interlocked.Increment(ref hits);
                        if (value != data[item.Key])
                        {
                            Interlocked.Increment(ref mismatches);
                        }
                        else
                        {
                            Interlocked.Increment(ref matches);
                        }
                    }
                    else
                    {
                        Interlocked.Increment(ref misses);
                    }
                }
            })
            {
                IsBackground = true,
                Name = $"Thread-{x}"
            }).ToArray();

            foreach (var kvp in data)
            {
                writeChannel.Writer.TryWrite(kvp);
            }
            writeChannel.Writer.Complete();
            foreach (var kvp in data.OrderBy(x => x.Key))
            {
                readChannel.Writer.TryWrite(kvp);
            }
            readChannel.Writer.Complete();

            foreach (var (wt,rt) in writeThreads.Zip(readThreads))
            {
                wt.Start();
                rt.Start();
            }

            foreach (var thread in writeThreads.Concat(readThreads))
            {
                thread.Join();
            }

            Assert.Equal(0, mismatches);
            Assert.Equal(data.Count, matches + misses);
            Assert.Equal(data.Count, hits + misses);
        }
    }
}
