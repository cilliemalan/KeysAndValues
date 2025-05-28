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
            var kvs = KeyValueStore.CreateNewFrom(new Dictionary<Mem, Mem>
            {
                ["a"] = "1",
                ["b"] = "2",
                ["c"] = "3",
                ["d"] = "4",
            });
            using var ms = new MemoryStream();
            kvs.Serialize(ms);
            Debug.Assert(ms.Length > 0);
        }
    }
}
