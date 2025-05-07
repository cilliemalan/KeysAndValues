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
    ImmutableAvlTree<Mem, Mem> immutableAvlTree = ImmutableAvlTree<Mem, Mem>.Empty;

    Dictionary<Mem, Mem> dictionaryForRemoval = [];
    private SortedDictionary<Mem, Mem> sortedDictionaryForRemoval = [];
    ImmutableDictionary<Mem, Mem> immutableDictionaryForRemoval = ImmutableDictionary.Create<Mem, Mem>();
    ImmutableSortedDictionary<Mem, Mem> immutableSortedDictionaryForRemoval = ImmutableSortedDictionary.Create<Mem, Mem>();
    ImmutableAvlTree<Mem, Mem> immutableAvlTreeForRemoval = ImmutableAvlTree.Create<Mem, Mem>();

    List<ImmutableDictionary<Mem, Mem>> immdics = [];
    List<ImmutableSortedDictionary<Mem, Mem>> immsdics = [];
    List<ImmutableAvlTree<Mem, Mem>> immadics = [];


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
        immutableDictionary = b1.ToImmutable();
        immutableSortedDictionary = b2.ToImmutable();
        immutableAvlTree = ImmutableAvlTree.CreateRange(sortedDictionary);
    }

    [IterationSetup]
    public void ISetup()
    {
        dictionaryForRemoval = new(dictionary);
        sortedDictionaryForRemoval = new(dictionary);
        immutableDictionaryForRemoval = ImmutableDictionary.CreateRange(dictionary);
        immutableSortedDictionaryForRemoval = ImmutableSortedDictionary.CreateRange(dictionary);
        immutableAvlTreeForRemoval = ImmutableAvlTree.CreateRange(sortedDictionaryForRemoval);
    }

    [IterationCleanup]
    public void ICleanup()
    {
        immdics = [];
        immsdics = [];
        immadics = [];
    }

    [GlobalCleanup]
    public void GCleanup()
    {
        dictionary = [];
        sortedDictionary = [];
        immutableDictionary = ImmutableDictionary<Mem, Mem>.Empty;
        immutableSortedDictionary = ImmutableSortedDictionary<Mem, Mem>.Empty;
        immutableAvlTree = ImmutableAvlTree<Mem, Mem>.Empty;

        dictionaryForRemoval = [];
        sortedDictionaryForRemoval = [];
        immutableDictionaryForRemoval = ImmutableDictionary<Mem, Mem>.Empty;
        immutableSortedDictionaryForRemoval = ImmutableSortedDictionary<Mem, Mem>.Empty;
        immutableAvlTreeForRemoval = ImmutableAvlTree<Mem, Mem>.Empty;

        Clear();
    }



    // read operations

    [Benchmark]
    public void TryGetValueImmutableSortedDictionary()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immutableSortedDictionary.TryGetValue(keys[i], out _);
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
    public void TryGetValueImmutableAvlTree()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immutableAvlTree.TryGetValue(keys[i], out _);
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
    public void ContainsKeyImmutableAvlTree()
    {
        for (int i = 0; i < keys.Length; i++)
        {
            immutableAvlTree.ContainsKey(keys[i]);
        }
    }



    // write operations

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
    public void AddImmutableDictionaryBuilder()
    {
        var tempDict = ImmutableDictionary<Mem, Mem>.Empty.ToBuilder();
        for (int i = 0; i < keys.Length; i++)
        {
            tempDict.Add(keys[i], values[i]);
        }
        immdics.Add(tempDict.ToImmutable());
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
    public void AddImmutableSortedDictionaryBuilder()
    {
        var tempDict = ImmutableSortedDictionary<Mem, Mem>.Empty.ToBuilder();
        for (int i = 0; i < keys.Length; i++)
        {
            tempDict.Add(keys[i], values[i]);
        }
        immsdics.Add(tempDict.ToImmutable());
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
    public void UpdateImmutableDictionaryBuilder()
    {
        var tmp = immutableDictionary.ToBuilder();
        for (int i = 0; i < keys.Length; i++)
        {
            tmp[keys[i]] = values[i];
        }
        immdics.Add(tmp.ToImmutable());
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
    public void UpdateImmutableSortedDictionaryBuilder()
    {
        var tmp = immutableSortedDictionary.ToBuilder();
        for (int i = 0; i < keys.Length; i++)
        {
            tmp[keys[i]] = values[i];
        }
        immsdics.Add(tmp.ToImmutable());
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
    public void RemoveImmutableDictionaryBuilder()
    {
        var tmp = immutableDictionaryForRemoval.ToBuilder();
        for (int i = 0; i < keys.Length; i++)
        {
            tmp.Remove(keys[i]);
        }

        immdics.Add(tmp.ToImmutable());
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
    public void RemoveImmutableSortedDictionaryBuilder()
    {
        var tmp = immutableSortedDictionaryForRemoval.ToBuilder();
        for (int i = 0; i < keys.Length; i++)
        {
            tmp.Remove(keys[i]);
        }

        immsdics.Add(tmp.ToImmutable());
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
}
