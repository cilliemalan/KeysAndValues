using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;

namespace KeysAndValues;

public static class Corpus
{
    public static SortedDictionary<Mem, Mem> Generate(int numKeys, int keyLenMin = 16, int keyLenMax = 24, int valueLenMin = 8, int valueLenMax = 128, int seed = 0)
    {
        var dic = new SortedDictionary<Mem, Mem>();
        foreach (var item in GenerateUnsorted(numKeys, keyLenMin, keyLenMax, valueLenMin, valueLenMax, seed))
        {
            dic.Add(item.Key, item.Value);
        }

        return dic;
    }

    public static IEnumerable<KeyValuePair<Mem,Mem>> GenerateUnsorted(int numKeys, int keyLenMin = 16, int keyLenMax = 24, int valueLenMin = 8, int valueLenMax = 128, int seed = 0)
    {
        var r = new Random();
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < numKeys; i++)
        {
            var kl = r.Next(keyLenMin, keyLenMax);
            var vl = r.Next(valueLenMin, valueLenMax);

            var km = new byte[kl];
            var vm = new byte[vl];

            rng.GetBytes(km);
            rng.GetBytes(vm);

            var k = new Mem(km);
            var v = new Mem(vm);

            yield return new(k, v);
        }
    }
}
