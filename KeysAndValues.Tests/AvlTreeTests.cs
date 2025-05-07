using Microsoft.Diagnostics.Tracing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace KeysAndValues.Tests;

public class AvlTreeTests : AvlTreeTestsBase
{
    [Fact]
    public void MostBasicTest()
    {
        var empty = ImmutableAvlTree<string, string>.Empty;
        var c1 = empty.Add("Hello", "World");
        var c2 = c1.Add("World", "Hello");

        Assert.Empty(empty);
        Assert.Single(c1);
        Assert.Equal(2, c2.Count);
        Assert.Equal("World", c1["Hello"]);
        Assert.Equal("World", c2["Hello"]);
        Assert.Equal("Hello", c2["World"]);
        Assert.True(c1.ContainsKey("Hello"));
        Assert.False(c1.ContainsKey("World"));
    }

    private enum Operation
    {
        Add,
        Set,
        Remove,
        Last,
    }

    [Fact]
    public void RandomOperationsTest()
    {
        int operationCount = this.RandomOperationsCount;
        var expected = new SortedDictionary<int, bool>();
        ImmutableAvlTree<int, bool> actual = ImmutableAvlTree<int, bool>.Empty;

        int seed = unchecked((int)DateTime.Now.Ticks);
        Debug.WriteLine("Using random seed {0}", seed);
        var random = new Random(seed);

        for (int iOp = 0; iOp < operationCount; iOp++)
        {
            switch ((Operation)random.Next((int)Operation.Last))
            {
                case Operation.Add:
                    int key;
                    do
                    {
                        key = random.Next();
                    }
                    while (expected.ContainsKey(key));
                    bool value = random.Next() % 2 == 0;
                    Debug.WriteLine("Adding \"{0}\"={1} to the set.", key, value);
                    expected.Add(key, value);
                    actual = actual.Add(key, value);
                    break;

                case Operation.Set:
                    bool overwrite = expected.Count > 0 && random.Next() % 2 == 0;
                    if (overwrite)
                    {
                        int position = random.Next(expected.Count);
                        key = expected.Skip(position).First().Key;
                    }
                    else
                    {
                        do
                        {
                            key = random.Next();
                        }
                        while (expected.ContainsKey(key));
                    }

                    value = random.Next() % 2 == 0;
                    Debug.WriteLine("Setting \"{0}\"={1} to the set (overwrite={2}).", key, value, overwrite);
                    expected[key] = value;
                    actual = actual.SetItem(key, value);
                    break;

                case Operation.Remove:
                    if (expected.Count > 0)
                    {
                        int position = random.Next(expected.Count);
                        key = expected.Skip(position).First().Key;
                        Debug.WriteLine("Removing element \"{0}\" from the set.", key);
                        Assert.True(expected.Remove(key));
                        actual = actual.Remove(key);
                    }

                    break;
            }

            Assert.Equal<KeyValuePair<int, bool>>(expected.ToList(), actual.ToList());
        }
    }

    [Fact]
    public void AddExistingKeySameValueTest()
    {
        AddExistingKeySameValueTestHelper(NewEmpty<string, string>(), "Company", "Microsoft", "Microsoft");
    }

    [Fact]
    public void AddExistingKeyDifferentValueTest()
    {
        AddExistingKeyDifferentValueTestHelper(NewEmpty<string,string>(), "Company", "Microsoft", "MICROSOFT");
    }

    [Fact]
    public void ToUnorderedTest()
    {
        IImmutableDictionary<int, GenericParameterHelper> sortedMap = Empty<int, GenericParameterHelper>().AddRange(Enumerable.Range(1, 100).Select(n => new KeyValuePair<int, GenericParameterHelper>(n, new GenericParameterHelper(n))));
        ImmutableDictionary<int, GenericParameterHelper> unsortedMap = sortedMap.ToImmutableDictionary();
        Assert.IsAssignableFrom<ImmutableDictionary<int, GenericParameterHelper>>(unsortedMap);
        Assert.Equal(sortedMap.Count, unsortedMap.Count);
        Assert.Equal<KeyValuePair<int, GenericParameterHelper>>(sortedMap.ToList(), unsortedMap.ToList());
    }

    [Fact]
    public void InitialBulkAddUniqueTest()
    {
        var uniqueEntries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("a", "b"),
                new KeyValuePair<string,string>("c", "d"),
            };

        IImmutableDictionary<string, string> map = NewEmpty<string, string>();
        IImmutableDictionary<string, string> actual = map.AddRange(uniqueEntries);
        Assert.Equal(2, actual.Count);
    }

    [Fact]
    public void InitialBulkAddWithExactDuplicatesTest()
    {
        var uniqueEntries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("a", "b"),
                new KeyValuePair<string,string>("a", "b"),
            };

        IImmutableDictionary<string, string> map = NewEmpty<string, string>();
        IImmutableDictionary<string, string> actual = map.AddRange(uniqueEntries);
        Assert.Equal(1, actual.Count);
    }

    [Fact]
    public void ContainsValueTest()
    {
        this.ContainsValueTestHelper(ImmutableAvlTree<int, GenericParameterHelper>.Empty, 1, new GenericParameterHelper());
    }

    [Fact]
    public void InitialBulkAddWithKeyCollisionTest()
    {
        var uniqueEntries = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("a", "b"),
                new KeyValuePair<string,string>("a", "d"),
            };

        IImmutableDictionary<string, string> map = NewEmpty<string, string>();
        Assert.Throws<ArgumentException>(null, () => map.AddRange(uniqueEntries));
    }

    [Fact]
    public void Create()
    {
        IEnumerable<KeyValuePair<string, string>> pairs = new Dictionary<string, string> { { "a", "b" } };
        StringComparer keyComparer = StringComparer.OrdinalIgnoreCase;
        StringComparer valueComparer = StringComparer.CurrentCulture;

        ImmutableAvlTree<string, string> dictionary = ImmutableAvlTree.Create<string, string>();
        Assert.Equal(0, dictionary.Count);

        dictionary = ImmutableAvlTree.CreateRange(pairs);
        Assert.Equal(1, dictionary.Count);

    }

    [Fact]
    public void ToImmutableAvlTree()
    {
        IEnumerable<KeyValuePair<string, string>> pairs = new Dictionary<string, string> { { "a", "B" } };
        StringComparer keyComparer = StringComparer.OrdinalIgnoreCase;
        StringComparer valueComparer = StringComparer.CurrentCulture;

        ImmutableAvlTree<string, string> dictionary = pairs.ToImmutableAvlTree();
        Assert.Equal(1, dictionary.Count);
    }

    [Fact]
    public void CollisionExceptionMessageContainsKey()
    {
        ImmutableAvlTree<string, string> map = ImmutableAvlTree.Create<string, string>()
            .Add("firstKey", "1").Add("secondKey", "2");
        ArgumentException exception = Assert.Throws<ArgumentException>(null, () => map.Add("firstKey", "3"));
        Assert.Contains("firstKey", exception.Message);
    }

    [Fact]
    public void EnumeratorRecyclingMisuse()
    {
        ImmutableAvlTree<int, int> collection = ImmutableAvlTree.Create<int, int>().Add(3, 5);
        ImmutableAvlTree<int, int>.Enumerator enumerator = collection.GetEnumerator();
        ImmutableAvlTree<int, int>.Enumerator enumeratorCopy = enumerator;
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        enumerator.Dispose();
        Assert.Throws<ObjectDisposedException>(() => enumerator.MoveNext());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Reset());
        Assert.Throws<ObjectDisposedException>(() => enumerator.Current);
        Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.MoveNext());
        Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Reset());
        Assert.Throws<ObjectDisposedException>(() => enumeratorCopy.Current);

        enumerator.Dispose(); // double-disposal should not throw
        enumeratorCopy.Dispose();

        // We expect that acquiring a new enumerator will use the same underlying Stack<T> object,
        // but that it will not throw exceptions for the new enumerator.
        enumerator = collection.GetEnumerator();
        Assert.True(enumerator.MoveNext());
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.Dispose();
    }

    [Fact]
    public void Remove_KeyExists_RemovesKeyValuePair()
    {
        ImmutableAvlTree<int, string> dictionary = new Dictionary<int, string>
            {
                { 1, "a" }
            }.ToImmutableAvlTree();
        Assert.Equal(0, dictionary.Remove(1).Count);
    }

    [Fact]
    public void Remove_FirstKey_RemovesKeyValuePair()
    {
        ImmutableAvlTree<int, string> dictionary = new Dictionary<int, string>
            {
                { 1, "a" },
                { 2, "b" }
            }.ToImmutableAvlTree();
        Assert.Equal(1, dictionary.Remove(1).Count);
    }

    [Fact]
    public void Remove_SecondKey_RemovesKeyValuePair()
    {
        ImmutableAvlTree<int, string> dictionary = new Dictionary<int, string>
            {
                { 1, "a" },
                { 2, "b" }
            }.ToImmutableAvlTree();
        Assert.Equal(1, dictionary.Remove(2).Count);
    }

    [Fact]
    public void Remove_KeyDoesntExist_DoesNothing()
    {
        ImmutableAvlTree<int, string> dictionary = new Dictionary<int, string>
            {
                { 1, "a" }
            }.ToImmutableAvlTree();
        Assert.Equal(1, dictionary.Remove(2).Count);
        Assert.Equal(1, dictionary.Remove(-1).Count);
    }

    [Fact]
    public void Remove_EmptyDictionary_DoesNothing()
    {
        ImmutableAvlTree<int, string> dictionary = ImmutableAvlTree<int, string>.Empty;
        Assert.Equal(0, dictionary.Remove(2).Count);
    }

    [Fact]
    public void ValueRef()
    {
        var dictionary = new Dictionary<string, int>()
            {
                { "a", 1 },
                { "b", 2 }
            }.ToImmutableAvlTree();

        ref readonly int safeRef = ref dictionary.ValueRef("a");
        ref int unsafeRef = ref Unsafe.AsRef(in safeRef);

        Assert.Equal(1, dictionary.ValueRef("a"));

        unsafeRef = 5;

        Assert.Equal(5, dictionary.ValueRef("a"));
    }

    [Fact]
    public void ValueRef_NonExistentKey()
    {
        var dictionary = new Dictionary<string, int>()
            {
                { "a", 1 },
                { "b", 2 }
            }.ToImmutableAvlTree();

        Assert.Throws<KeyNotFoundException>(() => dictionary.ValueRef("c"));
    }

    [Fact]
    public void Indexer_KeyNotFoundException_ContainsKeyInMessage()
    {
        ImmutableAvlTree<string, string> map = ImmutableAvlTree.Create<string, string>()
            .Add("a", "1").Add("b", "2");
        KeyNotFoundException exception = Assert.Throws<KeyNotFoundException>(() => map["c"]);
        Assert.Contains("'c'", exception.Message);
    }


    protected override IImmutableDictionary<TKey, TValue> Empty<TKey, TValue>()
    {
        return NewEmpty<TKey, TValue>();
    }
    static ImmutableAvlTree<TKey, TValue> NewEmpty<TKey, TValue>()
        where TKey : IComparable<TKey>
        where TValue : IComparable<TValue>
    {
        return ImmutableAvlTree<TKey, TValue>.Empty;
    }

    protected override IImmutableDictionary<string, TValue> Empty<TValue>()
    {
        return ImmutableAvlTree.Create<string, TValue>();
    }

    protected override IEqualityComparer<TValue> GetValueComparer<TKey, TValue>(IImmutableDictionary<TKey, TValue> dictionary)
    {
        return EqualityComparer<TValue>.Default;
    }

    protected void ContainsValueTestHelper<TKey, TValue>(ImmutableAvlTree<TKey, TValue> map, TKey key, TValue value)
        where TKey : IComparable<TKey>
        where TValue : IComparable<TValue>

    {
        Assert.False(map.ContainsValue(value));
        Assert.True(map.Add(key, value).ContainsValue(value));
    }
}