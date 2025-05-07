using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace KeysAndValues.Bench;

[MemoryDiagnoser]
public class DictionaryBenchmarks : DataFixture
{
    Dictionary<Mem, Mem> dictionary = [];
    SortedDictionary<Mem, Mem> sortedDictionary = [];
    ImmutableDictionary<Mem, Mem> immutableDictionary = ImmutableDictionary<Mem, Mem>.Empty;
    ImmutableSortedDictionary<Mem, Mem> immutableSortedDictionary = ImmutableSortedDictionary<Mem, Mem>.Empty;

    Dictionary<Mem, Mem> dictionaryForRemoval = [];
    private SortedDictionary<Mem, Mem> sortedDictionaryForRemoval = [];
    ImmutableDictionary<Mem, Mem> immutableDictionaryForRemoval = ImmutableDictionary.Create<Mem, Mem>();
    ImmutableSortedDictionary<Mem, Mem> immutableSortedDictionaryForRemoval = ImmutableSortedDictionary.Create<Mem, Mem>();

    List<ImmutableDictionary<Mem, Mem>> immdics = [];
    List<ImmutableSortedDictionary<Mem, Mem>> immsdics = [];


    [GlobalSetup]
    public void GSetup()
    {
        Initialize();
        var b1 = immutableDictionary.ToBuilder();
        var b2 = immutableSortedDictionary.ToBuilder();
        for (int i = 0; i < keys.Length; i++)
        {
            dictionary[keys[i]] = values[i];
            sortedDictionary[keys[i]] = values[i];
            b1[keys[i]] = values[i];
            b2[keys[i]] = values[i];
        }
    }

    [IterationSetup]
    public void ISetup()
    {
        dictionaryForRemoval = new(dictionary);
        sortedDictionaryForRemoval = new(dictionary);
        immutableDictionaryForRemoval = ImmutableDictionary.CreateRange(dictionary);
        immutableSortedDictionaryForRemoval = ImmutableSortedDictionary.CreateRange(dictionary);
    }

    [IterationCleanup]
    public void ICleanup()
    {
        immdics = [];
        immsdics = [];
    }

    [GlobalCleanup]
    public void GCleanup()
    {
        dictionary = [];
        sortedDictionary = [];
        immutableDictionary = ImmutableDictionary<Mem, Mem>.Empty;
        immutableSortedDictionary = ImmutableSortedDictionary<Mem, Mem>.Empty;

        dictionaryForRemoval = [];
        sortedDictionaryForRemoval = [];
        immutableDictionaryForRemoval = ImmutableDictionary<Mem, Mem>.Empty;
        immutableSortedDictionaryForRemoval = ImmutableSortedDictionary<Mem, Mem>.Empty;

        Clear();
    }






    [Benchmark]
    public void AddDictionary()
    {
        var tempDict = new Dictionary<Mem, Mem>();
        for (int i = 0; i < keys.Length; i++)
        {
            tempDict.Add(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void AddSortedDictionary()
    {
        var tempDict = new SortedDictionary<Mem, Mem>();
        for (int i = 0; i < keys.Length; i++)
        {
            tempDict.Add(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void AddImmutableDictionary()
    {
        var tempDict = ImmutableDictionary<Mem, Mem>.Empty;
        for (int i = 0; i < keys.Length; i++)
        {
            immdics.Add(tempDict);
            tempDict = tempDict.Add(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void AddImmutableSortedDictionary()
    {
        var tempDict = ImmutableSortedDictionary<Mem, Mem>.Empty;
        for (int i = 0; i < keys.Length; i++)
        {
            immsdics.Add(tempDict);
            tempDict = tempDict.Add(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void TryGetValueDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            dictionary.TryGetValue(keys[i], out _);
        }
    }

    [Benchmark]
    public void TryGetValueSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            sortedDictionary.TryGetValue(keys[i], out _);
        }
    }

    [Benchmark]
    public void TryGetValueImmutableDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immutableDictionary.TryGetValue(keys[i], out _);
        }
    }

    [Benchmark]
    public void TryGetValueImmutableSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immutableSortedDictionary.TryGetValue(keys[i], out _);
        }
    }

    [Benchmark]
    public void UpdateDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            dictionary[keys[i]] = values[i];
        }
    }

    [Benchmark]
    public void UpdateSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            sortedDictionary[keys[i]] = values[i];
        }
    }

    [Benchmark]
    public void UpdateImmutableDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immdics.Add(immutableDictionary);
            immutableDictionary = immutableDictionary.SetItem(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void UpdateImmutableSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immsdics.Add(immutableSortedDictionary);
            immutableSortedDictionary = immutableSortedDictionary.SetItem(keys[i], values[i]);
        }
    }

    [Benchmark]
    public void RemoveDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            dictionaryForRemoval.Remove(keys[i]);
        }
    }

    [Benchmark]
    public void RemoveSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            sortedDictionaryForRemoval.Remove(keys[i]);
        }
    }

    [Benchmark]
    public void RemoveImmutableDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immdics.Add(immutableDictionaryForRemoval);
            immutableDictionaryForRemoval = immutableDictionaryForRemoval.Remove(keys[i]);
        }
    }

    [Benchmark]
    public void RemoveImmutableSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immsdics.Add(immutableSortedDictionaryForRemoval);
            immutableSortedDictionaryForRemoval = immutableSortedDictionaryForRemoval.Remove(keys[i]);
        }
    }

    [Benchmark]
    public void ContainsKeyDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            dictionary.ContainsKey(keys[i]);
        }
    }

    [Benchmark]
    public void ContainsKeySortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            sortedDictionary.ContainsKey(keys[i]);
        }
    }

    [Benchmark]
    public void ContainsKeyImmutableDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immutableDictionary.ContainsKey(keys[i]);
        }
    }

    [Benchmark]
    public void ContainsKeyImmutableSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immutableSortedDictionary.ContainsKey(keys[i]);
        }
    }
}
