using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues.Tests
{
    public class BasicOperationTests
    {
        [Fact]
        public void BasicSetTest()
        {
            var kvs = KeyValueStore.CreateEmpty();
            kvs.Set("a", "1");
            kvs.Set("b", "2");
            kvs.Set("c", "3");
            kvs.Set("d", "4");
            var s = kvs.Snapshot();
            Assert.Equal(4, s.Count);
            Assert.Equal("1", s["a"]);
            Assert.Equal("2", s["b"]);
            Assert.Equal("3", s["c"]);
            Assert.Equal("4", s["d"]);
            Assert.Equal(4, kvs.Sequence);
        }

        [Fact]
        public void BasicGetTest()
        {
            var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3",
                ["d"] = "4",
            });
            Assert.Equal(4, kvs.Count);
            Assert.Equal("1", kvs.Get("a"));
            Assert.Equal("2", kvs.Get("b"));
            Assert.Equal("3", kvs.Get("c"));
            Assert.Equal("4", kvs.Get("d"));
        }

        [Fact]
        public void BasicSetOverwriteTest()
        {
            var kvs = KeyValueStore.CreateEmpty();
            kvs.Set("a", "1");
            kvs.Set("b", "2");
            kvs.Set("c", "3");
            kvs.Set("d", "4");
            kvs.Set("c", "5");
            kvs.Set("d", "6");
            var s = kvs.Snapshot();
            Assert.Equal(4, s.Count);
            Assert.Equal("1", s["a"]);
            Assert.Equal("2", s["b"]);
            Assert.Equal("5", s["c"]);
            Assert.Equal("6", s["d"]);
        }

        [Fact]
        public void BasicEnumerationTest()
        {
            var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3",
                ["d"] = "4",
            });
            var s = kvs.Enumerate().ToDictionary();
            Assert.Equal("1", s["a"]);
            Assert.Equal("2", s["b"]);
            Assert.Equal("3", s["c"]);
            Assert.Equal("4", s["d"]);
        }

        [Fact]
        public void BasicRangeEnumerationTest()
        {
            var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3",
                ["d"] = "4",
                ["e"] = "4",
                ["f"] = "4",
            });
            var s = kvs.Enumerate("b", "e").ToDictionary();
            Assert.Equal(3, s.Count);
            Assert.Equal("2", s["b"]);
            Assert.Equal("3", s["c"]);
            Assert.Equal("4", s["d"]);
        }
    }
}
