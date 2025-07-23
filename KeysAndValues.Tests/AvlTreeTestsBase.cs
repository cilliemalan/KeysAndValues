using System.Collections.Immutable;
using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Text;

#pragma warning disable xUnit2013 // Do not use equality check to check for collection size.

namespace KeysAndValues.Tests;

public abstract class AvlTreeTestsBase
{
    /// <summary>
    /// Gets the number of operations to perform in randomized tests.
    /// </summary>
    protected int RandomOperationsCount
    {
        get { return 100; }
    }

    internal static void AssertAreSame<T>(T expected, T actual)
    {
        if (typeof(T).GetTypeInfo().IsValueType)
        {
            Assert.Equal(expected, actual); //, message, formattingArgs);
        }
        else
        {
#pragma warning disable xUnit2005 // Do not use Assert.Same() on value type 'T'. Value types do not have identity. Use Assert.Equal instead.
            Assert.Same((object)expected!, (object)actual!); //, message, formattingArgs);
#pragma warning restore xUnit2005
        }
    }

    internal static void CollectionAssertAreEquivalent<T>(ICollection<T> expected, ICollection<T> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        foreach (T value in expected)
        {
            Assert.Contains(value, actual);
        }
    }

    protected static string ToString(System.Collections.IEnumerable sequence)
    {
        var sb = new StringBuilder();
        sb.Append('{');
        int count = 0;
        foreach (object item in sequence)
        {
            if (count > 0)
            {
                sb.Append(',');
            }

            if (count == 10)
            {
                sb.Append("...");
                break;
            }

            sb.Append(item);
            count++;
        }

        sb.Append('}');
        return sb.ToString();
    }

    protected static object ToStringDeferred(System.Collections.IEnumerable sequence)
    {
        return new DeferredToString(() => ToString(sequence));
    }

    protected static void ManuallyEnumerateTest<T>(IList<T> expectedResults, IEnumerator<T> enumerator)
    {
        T[] manualArray = new T[expectedResults.Count];
        int i = 0;

        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        while (enumerator.MoveNext())
        {
            manualArray[i++] = enumerator.Current;
        }

        enumerator.MoveNext();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        enumerator.MoveNext();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);

        Assert.Equal(expectedResults.Count, i); //, "Enumeration did not produce enough elements.");
        Assert.Equal<T>(expectedResults, manualArray);
    }

    /// <summary>
    /// Generates an array of unique values.
    /// </summary>
    /// <param name="length">The desired length of the array.</param>
    /// <returns>An array of doubles.</returns>
    protected double[] GenerateDummyFillData(int length = 1000)
    {
        Assert.InRange(length, 0, int.MaxValue);

        int seed = unchecked((int)DateTime.Now.Ticks);

        Debug.WriteLine("Random seed {0}", seed);

        var random = new Random(seed);
        var inputs = new double[length];
        var ensureUniqueness = new HashSet<double>();
        for (int i = 0; i < inputs.Length; i++)
        {
            double input;
            do
            {
                input = random.NextDouble();
            }
            while (!ensureUniqueness.Add(input));
            inputs[i] = input;
        }

        Assert.NotNull(inputs);
        Assert.Equal(length, inputs.Length);

        return inputs;
    }

    private class DeferredToString
    {
        private readonly Func<string> _generator;

        internal DeferredToString(Func<string> generator)
        {
            Debug.Assert(generator != null);
            _generator = generator;
        }

        public override string ToString()
        {
            return _generator();
        }
    }








    [Fact]
    public virtual void EmptyTest()
    {
        this.EmptyTestHelper(Empty<int, bool>(), 5);
    }

    [Fact]
    public void ContainsTest()
    {
        this.ContainsTestHelper(Empty<int, string>(), 5, "foo");
    }

    [Fact]
    public void RemoveTest()
    {
        this.RemoveTestHelper(Empty<int, GenericParameterHelper>(), 5);
    }

    [Fact]
    public void SetItemTest()
    {
        ImmutableAvlTree<string, int> map = this.Empty<string, int>()
            .SetItem("Microsoft", 100)
            .SetItem("Corporation", 50);
        Assert.Equal(2, map.Count);

        map = map.SetItem("Microsoft", 200);
        Assert.Equal(2, map.Count);
        Assert.Equal(200, map["Microsoft"]);

        // Set it to the same thing again and make sure it's all good.
        ImmutableAvlTree<string, int> sameMap = map.SetItem("Microsoft", 200);
        Assert.Same(map, sameMap);
    }

    [Fact]
    public void SetItemsTest()
    {
        var template = new Dictionary<string, int>
            {
                { "Microsoft", 100 },
                { "Corporation", 50 },
            };
        ImmutableAvlTree<string, int> map = this.Empty<string, int>().SetItems(template);
        Assert.Equal(2, map.Count);

        var changes = new Dictionary<string, int>
            {
                { "Microsoft", 150 },
                { "Dogs", 90 },
            };
        map = map.SetItems(changes);
        Assert.Equal(3, map.Count);
        Assert.Equal(150, map["Microsoft"]);
        Assert.Equal(50, map["Corporation"]);
        Assert.Equal(90, map["Dogs"]);

        map = map.SetItems(
        [
            new KeyValuePair<string, int>("Microsoft", 80),
            new KeyValuePair<string, int>("Microsoft", 70),
        ]);
        Assert.Equal(3, map.Count);
        Assert.Equal(70, map["Microsoft"]);
        Assert.Equal(50, map["Corporation"]);
        Assert.Equal(90, map["Dogs"]);

        map = this.Empty<string, int>().SetItems(
        [
            new KeyValuePair<string, int>("a", 1), 
            new KeyValuePair<string, int>("b", 2),
            new KeyValuePair<string, int>("a", 3),
        ]);
        Assert.Equal(2, map.Count);
        Assert.Equal(3, map["a"]);
        Assert.Equal(2, map["b"]);
    }

    [Fact]
    public void ContainsKeyTest()
    {
        this.ContainsKeyTestHelper(Empty<int, GenericParameterHelper>(), 1, new GenericParameterHelper());
    }

    [Fact]
    public void IndexGetNonExistingKeyThrowsTest()
    {
        Assert.Throws<KeyNotFoundException>(() => this.Empty<int, int>()[3]);
    }

    [Fact]
    public void IndexGetTest()
    {
        ImmutableAvlTree<int, int> map = this.Empty<int, int>().Add(3, 5);
        Assert.Equal(5, map[3]);
    }

    /// <summary>
    /// Verifies that the GetHashCode method returns the standard one.
    /// </summary>
    [Fact]
    public void GetHashCodeTest()
    {
        ImmutableAvlTree<string, int> dictionary = Empty<string, int>();
        Assert.Equal(EqualityComparer<object>.Default.GetHashCode(dictionary), dictionary.GetHashCode());
    }

    [Fact]
    public void ICollectionOfKVMembers()
    {
        var dictionary = (ICollection<KeyValuePair<string, int>>)Empty<string, int>();
        Assert.Throws<NotSupportedException>(() => dictionary.Add(new KeyValuePair<string, int>()));
        Assert.Throws<NotSupportedException>(() => dictionary.Remove(new KeyValuePair<string, int>()));
        Assert.Throws<NotSupportedException>(() => dictionary.Clear());
        Assert.True(dictionary.IsReadOnly);
    }

    [Fact]
    public void ICollectionMembers()
    {
        ((ICollection)Empty<string, int>()).CopyTo(new object[0], 0);

        var dictionary = (ICollection)Empty<string, int>().Add("a", 1);
        Assert.True(dictionary.IsSynchronized);
        Assert.NotNull(dictionary.SyncRoot);
        Assert.Same(dictionary.SyncRoot, dictionary.SyncRoot);

        var array = new object[2];
        dictionary.CopyTo(array, 1);
        Assert.Null(array[0]);
        Assert.Equal(new DictionaryEntry("a", 1), (DictionaryEntry)array[1]);
    }

    [Fact]
    public void IDictionaryOfKVMembers()
    {
        var dictionary = (IDictionary<string, int>)Empty<string, int>().Add("c", 3);
        Assert.Throws<NotSupportedException>(() => dictionary.Add("a", 1));
        Assert.Throws<NotSupportedException>(() => dictionary.Remove("a"));
        Assert.Throws<NotSupportedException>(() => dictionary["a"] = 2);
        Assert.Throws<KeyNotFoundException>(() => dictionary["a"]);
        Assert.Equal(3, dictionary["c"]);
    }

    [Fact]
    public void IDictionaryMembers()
    {
        var dictionary = (IDictionary)Empty<string, int>().Add("c", 3);
        Assert.Throws<NotSupportedException>(() => dictionary.Add("a", 1));
        Assert.Throws<NotSupportedException>(() => dictionary.Remove("a"));
        Assert.Throws<NotSupportedException>(() => dictionary["a"] = 2);
        Assert.Throws<NotSupportedException>(() => dictionary.Clear());
        Assert.False(dictionary.Contains("a"));
        Assert.True(dictionary.Contains("c"));
        Assert.Throws<KeyNotFoundException>(() => dictionary["a"]);
        Assert.Equal(3, dictionary["c"]);
        Assert.True(dictionary.IsFixedSize);
        Assert.True(dictionary.IsReadOnly);
        Assert.Equal(new[] { "c" }, dictionary.Keys.Cast<string>().ToArray());
        Assert.Equal(new[] { 3 }, dictionary.Values.Cast<int>().ToArray());
    }

    [Fact]
    public void IDictionaryEnumerator()
    {
        var dictionary = (IDictionary)Empty<string, int>().Add("a", 1);
        IDictionaryEnumerator enumerator = dictionary.GetEnumerator();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.Key);
        Assert.Throws<InvalidOperationException>(() => enumerator.Value);
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(enumerator.Entry, enumerator.Current);
        Assert.Equal(enumerator.Key, enumerator.Entry.Key);
        Assert.Equal(enumerator.Value, enumerator.Entry.Value);
        Assert.Equal("a", enumerator.Key);
        Assert.Equal(1, enumerator.Value);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.Key);
        Assert.Throws<InvalidOperationException>(() => enumerator.Value);
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
        Assert.False(enumerator.MoveNext());

        enumerator.Reset();
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.Key);
        Assert.Throws<InvalidOperationException>(() => enumerator.Value);
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
        Assert.True(enumerator.MoveNext());
        Assert.Equal(enumerator.Key, ((DictionaryEntry)enumerator.Current).Key);
        Assert.Equal(enumerator.Value, ((DictionaryEntry)enumerator.Current).Value);
        Assert.Equal("a", enumerator.Key);
        Assert.Equal(1, enumerator.Value);
        Assert.False(enumerator.MoveNext());
        Assert.Throws<InvalidOperationException>(() => enumerator.Current);
        Assert.Throws<InvalidOperationException>(() => enumerator.Key);
        Assert.Throws<InvalidOperationException>(() => enumerator.Value);
        Assert.Throws<InvalidOperationException>(() => enumerator.Entry);
        Assert.False(enumerator.MoveNext());
    }

    [Fact]
    public void TryGetKey()
    {
        var alpha = "a";
        var Alpha = new string("a");
        var beta = "b";

        Assert.NotSame(alpha, Alpha);

        ImmutableAvlTree<string, int> dictionary = Empty<int>()
            .Add(alpha, 1);

        string actualKey;
        Assert.True(dictionary.TryGetKey(alpha, out actualKey));
        Assert.Same(alpha, actualKey);

        Assert.True(dictionary.TryGetKey(Alpha, out actualKey));
        Assert.Same(alpha, actualKey);
        Assert.NotSame(Alpha, actualKey);

        Assert.False(dictionary.TryGetKey(beta, out actualKey));
        Assert.Same(beta, actualKey);
    }

    protected void EmptyTestHelper<K, V>(ImmutableAvlTree<K, V> empty, K someKey)
        where V : IComparable<V>
        where K : IComparable<K>
    {
        Assert.Same(empty, empty.Clear());
        Assert.Equal(0, empty.Count);
        Assert.Equal(0, empty.Count());
        Assert.Equal(0, empty.Keys.Count());
        Assert.Equal(0, empty.Values.Count());
        Assert.Same(EqualityComparer<V>.Default, GetValueComparer(empty));
        Assert.False(empty.ContainsKey(someKey));
        Assert.DoesNotContain(new KeyValuePair<K, V>(someKey, default(V)!), empty);
        Assert.Equal(default(V), empty.GetValueOrDefault(someKey));

        V value;
        Assert.False(empty.TryGetValue(someKey, out value!));
        Assert.Equal(default(V), value);
    }

    protected void AddExistingKeySameValueTestHelper<TKey, TValue>(ImmutableAvlTree<TKey, TValue> map, TKey key, TValue value1, TValue value2)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        Assert.NotNull(map);
        Assert.NotNull(key);
        Assert.True(GetValueComparer(map).Equals(value1, value2));

        map = map.Add(key, value1);
        Assert.Same(map, map.Add(key, value2));
        Assert.Same(map, map.AddRange(new[] { new KeyValuePair<TKey, TValue>(key, value2) }));
    }

    /// <summary>
    /// Verifies that adding a key-value pair where the key already is in the map but with a different value throws.
    /// </summary>
    /// <typeparam name="TKey">The type of key in the map.</typeparam>
    /// <typeparam name="TValue">The type of value in the map.</typeparam>
    /// <param name="map">The map to manipulate.</param>
    /// <param name="key">The key to add.</param>
    /// <param name="value1">The first value to add.</param>
    /// <param name="value2">The second value to add.</param>
    /// <remarks>
    /// Adding a key-value pair to a map where that key already exists, but with a different value, cannot fit the
    /// semantic of "adding", either by just returning or mutating the value on the existing key.  Throwing is the only reasonable response.
    /// </remarks>
    protected void AddExistingKeyDifferentValueTestHelper<TKey, TValue>(ImmutableAvlTree<TKey, TValue> map, TKey key, TValue value1, TValue value2)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        Assert.NotNull(map);
        Assert.NotNull(key);
        Assert.False(GetValueComparer(map).Equals(value1, value2));

        ImmutableAvlTree<TKey, TValue> map1 = map.Add(key, value1);
        ImmutableAvlTree<TKey, TValue> map2 = map.Add(key, value2);
        Assert.Throws<ArgumentException>(null, () => map1.Add(key, value2));
        Assert.Throws<ArgumentException>(null, () => map2.Add(key, value1));
    }

    protected void ContainsKeyTestHelper<TKey, TValue>(ImmutableAvlTree<TKey, TValue> map, TKey key, TValue value)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        Assert.False(map.ContainsKey(key));
        Assert.True(map.Add(key, value).ContainsKey(key));
    }

    protected void ContainsTestHelper<TKey, TValue>(ImmutableAvlTree<TKey, TValue> map, TKey key, TValue value)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        Assert.DoesNotContain(new KeyValuePair<TKey, TValue>(key, value), map);
        Assert.False(map.Contains(key, value));
        Assert.Contains(new KeyValuePair<TKey, TValue>(key, value), map.Add(key, value));
        Assert.True(map.Add(key, value).Contains(key, value));
    }

    protected void RemoveTestHelper<TKey, TValue>(ImmutableAvlTree<TKey, TValue> map, TKey key)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        // no-op remove
        Assert.Same(map, map.Remove(key));
        Assert.Same(map, map.RemoveRange(Enumerable.Empty<TKey>()));

        // substantial remove
        ImmutableAvlTree<TKey, TValue> addedMap = map.Add(key, default(TValue)!);
        ImmutableAvlTree<TKey, TValue> removedMap = addedMap.Remove(key);
        Assert.NotSame(addedMap, removedMap);
        Assert.False(removedMap.ContainsKey(key));
    }

    protected abstract ImmutableAvlTree<TKey, TValue> Empty<TKey, TValue>()
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>;

    protected abstract ImmutableAvlTree<string, TValue> Empty<TValue>()
        where TValue : IComparable<TValue>;

    protected abstract IEqualityComparer<TValue> GetValueComparer<TKey, TValue>(ImmutableAvlTree<TKey, TValue> dictionary)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>;
}
