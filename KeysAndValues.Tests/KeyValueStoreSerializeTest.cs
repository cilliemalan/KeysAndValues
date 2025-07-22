using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues.Tests
{
    public class KeyValueStoreSerializeTest
    {
        [Fact]
        public void BasicSerializeTest()
        {
            var kvs = new KeyValueStore(1, Corpus.GenerateUnsorted(10));
            using var ms = new MemoryStream();
            kvs.Serialize(ms);
            Debug.Assert(ms.Length > 0);
        }

        [Fact]
        public void BasicDeserializeTest()
        {
            var kvs = new KeyValueStore(1, Corpus.GenerateUnsorted(10));
            using var ms = new MemoryStream();
            kvs.Serialize(ms);
            ms.Position = 0;
            var kvs2 = KeyValueStore.Deserialize(ms);
            Assert.Equal(kvs.Count, kvs2.Count);
            Assert.Equal(kvs.Sequence, kvs2.Sequence);
            Assert.Equal(kvs.Snapshot().Data.AsEnumerable(), kvs2.Snapshot().Data.AsEnumerable());
        }
    }
}
