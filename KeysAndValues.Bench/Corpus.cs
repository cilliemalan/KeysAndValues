using KeysAndValues.Internal;

namespace KeysAndValues.Bench;

public static class Corpus
{
    public static (IDisposable MemoryPool, Dictionary<Mem, Mem> Data) Generate(int numKeys, int keyLenMin, int keyLenMax, int valueLenMin, int valueLenMax, int seed = 0)
    {
        var pool = new UnsafeMemoryPool();
        var rnd = new Random(seed);

        var dic = new Dictionary<Mem, Mem>(numKeys);
        for (int i = 0; i < numKeys; i++)
        {
            var kl = rnd.Next(keyLenMin, keyLenMax);
            var vl = rnd.Next(valueLenMin, valueLenMax);
            var k = pool.Allocate(kl, rnd.NextBytes);
            var v = pool.Allocate(vl, rnd.NextBytes);

            dic.Add(k, v);
        }

        return (pool, dic);
    }
}
