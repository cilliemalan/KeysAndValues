using System.Diagnostics;

namespace KeysAndValues.Bench;

public static class Corpus
{
    public static SortedDictionary<Mem, Mem> Generate(int numKeys, int keyLenMin, int keyLenMax, int valueLenMin, int valueLenMax, int seed = 0)
    {
        var rnd = new Random(seed);

        var dic = new SortedDictionary<Mem, Mem>();
        for (int i = 0; i < numKeys; i++)
        {
            var kl = rnd.Next(keyLenMin, keyLenMax);
            var vl = rnd.Next(valueLenMin, valueLenMax);

            var km = new byte[kl];
            var vm = new byte[vl];

            rnd.NextBytes(km);
            rnd.NextBytes(vm);

            var k = new Mem(km);
            var v = new Mem(vm);

            dic.Add(k, v);
        }

        return dic;
    }
}
