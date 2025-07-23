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

#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

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
        AddExistingKeyDifferentValueTestHelper(NewEmpty<string, string>(), "Company", "Microsoft", "MICROSOFT");
    }

    [Fact]
    public void ToUnorderedTest()
    {
        ImmutableAvlTree<int, GenericParameterHelper> sortedMap = Empty<int, GenericParameterHelper>().AddRange(Enumerable.Range(1, 100).Select(n => new KeyValuePair<int, GenericParameterHelper>(n, new GenericParameterHelper(n))));
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

        ImmutableAvlTree<string, string> map = NewEmpty<string, string>();
        ImmutableAvlTree<string, string> actual = map.AddRange(uniqueEntries);
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

        ImmutableAvlTree<string, string> map = NewEmpty<string, string>();
        ImmutableAvlTree<string, string> actual = map.AddRange(uniqueEntries);
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

        ImmutableAvlTree<string, string> map = NewEmpty<string, string>();
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

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    public void EnumerationTests(int numEntries)
    {
        var s = Empty<string, string>()
            .AddRange(Enumerable.Range(0, numEntries)
            .Select(x => new KeyValuePair<string, string>(
                x.ToString("0000"),
                x.ToString())));

        Assert.Equal(numEntries, s.Count());
        Assert.Equal(numEntries, s.DistinctBy(x => x.Key).Count());

        using var en = s.GetEnumerator();
        int cnt = 0;
        string? prev = null;
        while (en.MoveNext())
        {
            Assert.True(prev is null || en.Current.Key.CompareTo(prev) >= 0, "Keys are not sorted");

            cnt++;
            prev = en.Current.Key;
        }
        Assert.Equal(numEntries, cnt);

        en.Reset();
        cnt = 0;
        prev = null;
        while (en.MoveNext())
        {
            Assert.True(prev is null || en.Current.Key.CompareTo(prev) >= 0, "Keys are not sorted");
            cnt++;
            prev = en.Current.Key;
        }
        Assert.Equal(numEntries, cnt);

        var d = (System.Collections.IDictionary)s;
        Assert.IsAssignableFrom<IDictionaryEnumerator>(d.GetEnumerator());
        Assert.Equal(numEntries, d.Cast<object>().Count());
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(10)]
    [InlineData(15)]
    [InlineData(16)]
    [InlineData(17)]
    [InlineData(31)]
    [InlineData(32)]
    [InlineData(33)]
    [InlineData(127)]
    [InlineData(128)]
    [InlineData(129)]
    [InlineData(1023)]
    [InlineData(1024)]
    [InlineData(1025)]
    public void ReverseEnumerationTests(int numEntries)
    {
        var s = ImmutableAvlTree<string, string>.Empty
            .AddRange(Enumerable.Range(0, numEntries)
            .Select(x => new KeyValuePair<string, string>(
                x.ToString("0000"),
                x.ToString())));

        Assert.Equal(numEntries, s.Count());
        Assert.Equal(numEntries, s.DistinctBy(x => x.Key).Count());

        using var en = s.Reversed().GetEnumerator();
        int cnt = 0;
        string? prev = null;
        while (en.MoveNext())
        {
            Assert.True(prev is null || en.Current.Key.CompareTo(prev) <= 0, "Keys are not sorted");

            cnt++;
            prev = en.Current.Key;
        }
        Assert.Equal(numEntries, cnt);

        en.Reset();
        cnt = 0;
        prev = null;
        while (en.MoveNext())
        {
            Assert.True(prev is null || en.Current.Key.CompareTo(prev) <= 0, "Keys are not sorted");
            cnt++;
            prev = en.Current.Key;
        }
        Assert.Equal(numEntries, cnt);

        var d = (IDictionary)s;
        Assert.Equal(numEntries, d.Cast<object>().Count());
    }

    [Fact]
    public void RangeEnumeration()
    {
        var dictionary = new Dictionary<string, int>()
            {
                { "a", 1 },
                { "b", 2 },
                { "c", 3 },
                { "d", 4 },
                { "e", 5 },
                { "f", 6 },
                { "g", 7 },
                { "h", 8 },
                { "i", 9 }
            }.ToImmutableAvlTree();

        Assert.Equal(["a", "b", "c", "d", "e", "f", "g", "h", "i"], dictionary.Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["a", "b", "c", "d", "e", "f", "g", "h", "i"], dictionary.Range("a", "j").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["c", "d", "e", "f", "g"], dictionary.Range("c", "h").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["a", "b"], dictionary.Range("a", "c").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["a"], dictionary.Range("a", "b").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["f", "g", "h", "i"], dictionary.Range("f", "j").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["d", "e", "f"], dictionary.Range("d", "g").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["e", "f"], dictionary.Range("dd", "g").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["d", "e", "f"], dictionary.Range("d", "ff").Select(x => x.Key).ToArray().AsSpan());
    }

    [Fact]
    public void InexactRangeEnumeration()
    {
        var dictionary = new Dictionary<string, int>()
            {
                { "aa", 1 },
                { "bb", 2 },
                { "cc", 3 },
                { "dd", 4 },
                { "ee", 5 },
                { "ff", 6 },
                { "gg", 7 },
                { "hh", 8 },
                { "ii", 9 }
            }.ToImmutableAvlTree();

        Assert.Equal(["aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii"], dictionary.Range("aa", "iz").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii"], dictionary.Range("a ", "iz").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["aa", "bb", "cc", "dd", "ee", "ff", "gg", "hh", "ii"], dictionary.Range("aa", "iz").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["cc", "dd", "ee", "ff", "gg"], dictionary.Range("cc", "gz").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["cc", "dd", "ee", "ff", "gg"], dictionary.Range("c", "gz").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["aa"], dictionary.Range("aa", "az").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["aa"], dictionary.Range("", "az").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["aa"], dictionary.Range("", "aaa").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["aa"], dictionary.Range("aa", "aaa").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal(["aa"], dictionary.Range("a", "aaa").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal([], dictionary.Range("", "a").Select(x => x.Key).ToArray().AsSpan());
        Assert.Equal([], dictionary.Range("", "aa").Select(x => x.Key).ToArray().AsSpan());
    }

    [Fact]
    public void ExhaustiveRangeEnumeration()
    {
        var items = Enumerable.Range(0, 1000).Select(i => new KeyValuePair<string, int>($"{i:00000}5", i)).ToList();
        var dictionary = items.OrderBy(_ => Random.Shared.Next()).ToImmutableAvlTree();

        for (int i = 0; i < items.Count; i++)
        {
            var start = items[i].Key;
            var expectedCount = items.Count - i;
            Assert.Equal(expectedCount, dictionary.Range(start).Count());

            start = $"{start[..5]}4";
            Assert.Equal(expectedCount, dictionary.Range(start).Count());

            if (expectedCount - 1 >= 0)
            {
                start = $"{start[..5]}6";
                Assert.Equal(expectedCount - 1, dictionary.Range(start).Count());
            }
        }

        for (int i = 0; i < items.Count; i++)
        {
            var end = items[i].Key;
            var expectedCount = i;
            Assert.Equal(expectedCount, dictionary.Range("", end).Count());

            end = $"{end[..5]}4";
            Assert.Equal(expectedCount, dictionary.Range("", end).Count());

            end = $"{end[..5]}6";
            Assert.Equal(expectedCount + 1, dictionary.Range("", end).Count());
        }
    }

    protected override ImmutableAvlTree<TKey, TValue> Empty<TKey, TValue>()
    {
        return NewEmpty<TKey, TValue>();
    }

    static ImmutableAvlTree<TKey, TValue> NewEmpty<TKey, TValue>()
        where TKey : IComparable<TKey>
        where TValue : IComparable<TValue>
    {
        return ImmutableAvlTree<TKey, TValue>.Empty;
    }

    protected override ImmutableAvlTree<string, TValue> Empty<TValue>()
    {
        return ImmutableAvlTree.Create<string, TValue>();
    }

    protected override IEqualityComparer<TValue> GetValueComparer<TKey, TValue>(ImmutableAvlTree<TKey, TValue> dictionary)
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