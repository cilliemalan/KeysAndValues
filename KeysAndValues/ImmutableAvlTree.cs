namespace KeysAndValues;

using System.Collections;
using System.Collections.Immutable;
using System.ComponentModel;


#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

// based on immutable sorted dictionary from the .net runtime
#pragma warning disable IDE0301 // Simplify collection initialization
#pragma warning disable IDE0028 // Simplify collection initialization

[DebuggerDisplay("Count = {Count}")]
[DebuggerTypeProxy(typeof(ImmutableDictionaryDebuggerProxy<,>))]
public static class ImmutableAvlTree
{
    public static ImmutableAvlTree<TKey, TValue> Create<TKey, TValue>()
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        return ImmutableAvlTree<TKey, TValue>.Empty;
    }

    public static ImmutableAvlTree<TKey, TValue> CreateRange<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> items)
        where TValue : IComparable<TValue>
        where TKey : IComparable<TKey>
    {
        return ImmutableAvlTree<TKey, TValue>.Empty.AddRange(items, true, false);
    }
}

public class ImmutableAvlTree<TKey, TValue> : IImmutableDictionary<TKey, TValue>, IDictionary<TKey, TValue>, IDictionary
    where TValue : IComparable<TValue>
    where TKey : IComparable<TKey>
{
    public static readonly ImmutableAvlTree<TKey, TValue> Empty = new();

    readonly Node root;
    readonly int count;

    [EditorBrowsable(EditorBrowsableState.Never)]
    public ImmutableAvlTree()
    {
        root = Node.EmptyNode;
        count = 0;
    }

    private ImmutableAvlTree(Node root, int count)
    {
        Debug.Assert(root is not null);
        Debug.Assert(count >= 0);
        root.Freeze();
        this.root = root;
        this.count = count;
    }

    public bool IsEmpty => root.IsEmpty;
    public int Count => count;
    public IEnumerable<TKey> Keys => root.Keys;
    public IEnumerable<TValue> Values => root.Values;

    ImmutableAvlTree<TKey, TValue> Clear() => root.IsEmpty ? this : Empty;

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear() => Clear();

    ICollection<TKey> IDictionary<TKey, TValue>.Keys => new KeysCollectionAccessor(this);
    ICollection<TValue> IDictionary<TKey, TValue>.Values => new ValuesCollectionAccessor(this);
    bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => true;

    public TValue this[TKey key]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));
            if (!TryGetValue(key, out var value))
            {
                throw new KeyNotFoundException($"The key '{key}' was not found");
            }

            return value;
        }
    }

    public ref readonly TValue ValueRef(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        return ref root.ValueRef(key);
    }

    TValue IDictionary<TKey, TValue>.this[TKey key]
    {
        get { return this[key]; }
        set { throw new NotSupportedException(); }
    }

    public Builder ToBuilder() => new(this);

    public ImmutableAvlTree<TKey, TValue> Add(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        Node result = root.Add(key, value, out _);
        return Wrap(result, count + 1);
    }

    public ImmutableAvlTree<TKey, TValue> SetItem(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        Node result = root.SetItem(key, value, out var rev, out bool _);
        return Wrap(result, rev ? count : count + 1);
    }

    public ImmutableAvlTree<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));
        return AddRange(items, overwriteOnCollision: true, avoidToSortedMap: false);
    }

    public ImmutableAvlTree<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));
        return AddRange(items, overwriteOnCollision: false, avoidToSortedMap: false);
    }

    public ImmutableAvlTree<TKey, TValue> Remove(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        Node result = root.Remove(key, out var mutated);
        return Wrap(result, mutated ? count - 1 : count);
    }

    public ImmutableAvlTree<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
    {
        ArgumentNullException.ThrowIfNull(keys, nameof(keys));
        int c = count;
        Node result = root;
        foreach (var key in keys)
        {
            var newResult = result.Remove(key, out var mutated);
            if (mutated)
            {
                result = newResult;
                c -= 1;
            }
        }

        return Wrap(result, c);
    }

    public ImmutableAvlTree<TKey, TValue> WithComparers(IComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
    {
        if (keyComparer == Comparer<TKey>.Default && valueComparer == EqualityComparer<TValue>.Default)
        {
            return this;
        }

        throw new NotSupportedException();
    }

    public ImmutableAvlTree<TKey, TValue> WithComparers(IComparer<TKey>? keyComparer)
    {
        if (keyComparer == Comparer<TKey>.Default)
        {
            return this;
        }

        throw new NotSupportedException();
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value) => this.Add(key, value);
    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value) => this.SetItem(key, value);
    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items) => this.SetItems(items);
    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs) => this.AddRange(pairs);
    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys) => this.RemoveRange(keys);
    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key) => this.Remove(key);

    public bool ContainsKey(TKey key)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        return root.ContainsKey(key);
    }

    public bool ContainsValue(TValue value)
    {
        ArgumentNullException.ThrowIfNull(value, nameof(value));
        return root.ContainsValue(value);
    }

    public bool Contains(KeyValuePair<TKey, TValue> pair)
    {
        return root.Contains(pair);
    }

    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        return root.TryGetValue(key, out value);
    }

    public bool TryGetKey(TKey key, out TKey actualKey)
    {
        ArgumentNullException.ThrowIfNull(key, nameof(key));
        return root.TryGetKey(key, out actualKey);
    }

    void IDictionary<TKey, TValue>.Add(TKey key, TValue value) => throw new NotSupportedException();
    bool IDictionary<TKey, TValue>.Remove(TKey key) => throw new NotSupportedException();


    void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();
    void ICollection<KeyValuePair<TKey, TValue>>.Clear() => throw new NotSupportedException();
    bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item) => throw new NotSupportedException();

    void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array, nameof(array));
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex, nameof(arrayIndex));
        ArgumentOutOfRangeException.ThrowIfGreaterThan(array.Length, arrayIndex + count, nameof(arrayIndex));

        foreach (KeyValuePair<TKey, TValue> item in this)
        {
            array[arrayIndex++] = item;
        }
    }

    bool IDictionary.IsFixedSize => true;
    bool IDictionary.IsReadOnly => true;
    ICollection IDictionary.Keys => new KeysCollectionAccessor(this);
    ICollection IDictionary.Values => new ValuesCollectionAccessor(this);

    void IDictionary.Add(object key, object? value) => throw new NotSupportedException();
    bool IDictionary.Contains(object key) => this.ContainsKey((TKey)key);
    IDictionaryEnumerator IDictionary.GetEnumerator()
    {
        return new DictionaryEnumerator(GetEnumerator());
    }

    public Enumerable Reversed() => new(this, true);
    public Enumerable Reversed(TKey start) => new(this, true, start);
    public Enumerable Reversed(TKey start, TKey end)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(end, start);
        return new(this, true, start, end);
    }
    public Enumerable Range(TKey start) => new(this, false, start);
    public Enumerable Range(TKey start, TKey end)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(start, end);
        return new(this, false, start, end);
    }

    void IDictionary.Remove(object key) => throw new NotSupportedException();
    object? IDictionary.this[object key]
    {
        get { return this[(TKey)key]; }
        set { throw new NotSupportedException(); }
    }
    void IDictionary.Clear() => throw new NotSupportedException();

    void ICollection.CopyTo(Array array, int index)
    {
        root.CopyTo(array, index, this.Count);
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    object ICollection.SyncRoot => this;

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    bool ICollection.IsSynchronized => true;

    IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
    {
        return IsEmpty ? System.Linq.Enumerable.Empty<KeyValuePair<TKey, TValue>>().GetEnumerator() : this.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return this.GetEnumerator();
    }

    public Enumerator GetEnumerator()
    {
        return root.GetEnumerator();
    }

    public Enumerator GetReverseEnumerator() => new(root, reverse: true);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private ImmutableAvlTree<TKey, TValue> Wrap(Node root, int count)
    {
        if (root == this.root)
        {
            return this;
        }

        if (root.IsEmpty)
        {
            return Empty;
        }

        return new(root, count);
    }

    internal ImmutableAvlTree<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items, bool overwriteOnCollision, bool avoidToSortedMap)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));

        if (IsEmpty && !avoidToSortedMap)
        {
            return FillFromEmpty(items, overwriteOnCollision);
        }

        Node result = root;
        int count = this.count;
        foreach (var item in items)
        {
            bool replacedExistingValue = false;
            Node newResult = overwriteOnCollision
                ? result.SetItem(item.Key, item.Value, out replacedExistingValue, out bool mutated)
                : result.Add(item.Key, item.Value, out mutated);

            if (mutated)
            {
                result = newResult;
                if (!replacedExistingValue)
                {
                    count++;
                }
            }
        }

        return Wrap(result, count);
    }

    private ImmutableAvlTree<TKey, TValue> FillFromEmpty(IEnumerable<KeyValuePair<TKey, TValue>> items, bool overwriteOnCollision)
    {
        ArgumentNullException.ThrowIfNull(items, nameof(items));
        Debug.Assert(IsEmpty);

        if (items is ImmutableAvlTree<TKey, TValue> itree)
        {
            return itree;
        }

        SortedDictionary<TKey, TValue> dictionary;
        if (items is IDictionary<TKey, TValue> itemsAsDictionary)
        {
            dictionary = new SortedDictionary<TKey, TValue>(itemsAsDictionary);
        }
        else
        {
            dictionary = new SortedDictionary<TKey, TValue>();
            foreach (KeyValuePair<TKey, TValue> item in items)
            {
                if (overwriteOnCollision)
                {
                    dictionary[item.Key] = item.Value;
                }
                else
                {
                    TValue value;
                    if (dictionary.TryGetValue(item.Key, out value!))
                    {
                        if (value.CompareTo(item.Value) != 0)
                        {
                            throw new ArgumentException($"Duplicate key: '{item.Key}'");
                        }
                    }
                    else
                    {
                        dictionary.Add(item.Key, item.Value);
                    }
                }
            }
        }

        if (dictionary.Count == 0)
        {
            return this;
        }

        var root = Node.NodeTreeFromSortedDictionary(dictionary);
        return new ImmutableAvlTree<TKey, TValue>(root, dictionary.Count);
    }

    public sealed class Node : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public static readonly Node EmptyNode = new();

        private readonly TKey key = default!;
        private readonly TValue value = default!;
        private bool frozen;
        private byte height;
        private Node? left;
        private Node? right;

        public bool IsEmpty => left is null;

        private Node()
        {
            frozen = true;
        }

        private Node(TKey key, TValue value, Node left, Node right, bool frozen)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));
            ArgumentNullException.ThrowIfNull(left, nameof(left));
            ArgumentNullException.ThrowIfNull(right, nameof(right));
            Debug.Assert(!frozen || (left.frozen && right.frozen));

            this.key = key;
            this.value = value;
            this.left = left;
            this.right = right;
            this.frozen = frozen;
            this.height = checked((byte)(1 + Math.Max(left.height, right.height)));
        }

        public int Height => height;
        public Node? Left => left;
        public Node? Right => right;
        public KeyValuePair<TKey, TValue> Value => new(key, value);
        public IEnumerable<TKey> Keys => this.Select(x => x.Key);
        public IEnumerable<TValue> Values => this.Select(x => x.Value);

        public Enumerator GetEnumerator() => new(this);
        public Enumerator GetEnumerator(Builder builder) => new(this, builder);
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => new Enumerator(this);
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => new Enumerator(this);
        //public Enumerator GetEnumerator(Builder) => new(this);

        internal void CopyTo(Array dest, int index, int dictionarySize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dest.Length, index + dictionarySize, nameof(index));

            foreach (var item in this)
            {
                dest.SetValue(new DictionaryEntry(item.Key, item.Value), index++);
            }
        }

        internal void CopyTo(KeyValuePair<TKey, TValue>[] dest, int index, int dictionarySize)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index, nameof(index));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(dest.Length, index + dictionarySize, nameof(index));

            foreach (var item in this)
            {
                dest[index++] = item;
            }
        }

        internal void CopyTo(Span<KeyValuePair<TKey, TValue>> dest)
        {
            int index = 0;
            foreach (var item in this)
            {
                dest[index++] = item;
            }
        }

        internal static Node FromSortedDictionary(SortedDictionary<TKey, TValue> dictionary)
        {
            var list = new KeyValuePair<TKey, TValue>[dictionary.Count];
            int count = 0;

            foreach (var item in dictionary)
            {
                list[count++] = item;
            }

            return NodeTreeFromList(list, 0, list.Length);
        }

        internal Node Add(TKey key, TValue value, out bool mutated)
        {
            return SetOrAdd(key, value, false, out _, out mutated);
        }

        internal Node SetItem(TKey key, TValue value, out bool replacedExistingValue, out bool mutated)
        {
            return SetOrAdd(key, value, true, out replacedExistingValue, out mutated);
        }

        internal Node Remove(TKey key, out bool mutated)
        {
            return RemoveRecursive(key, out mutated);
        }

        internal ref readonly TValue ValueRef(TKey key)
        {
            var match = Search(key);
            if (match.IsEmpty)
            {
                throw new KeyNotFoundException("Key not found");
            }

            return ref match.value;
        }

        internal bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
        {
            var match = Search(key);
            if (match.IsEmpty)
            {
                value = default;
                return false;
            }

            value = match.value;
            return true;
        }

        internal bool TryGetKey(TKey key, out TKey actualKey)
        {
            var match = Search(key);
            if (match.IsEmpty)
            {
                actualKey = key;
                return false;
            }

            actualKey = match.key;
            return true;
        }

        internal bool ContainsKey(TKey key)
        {
            return !Search(key).IsEmpty;
        }

        internal bool ContainsValue(TValue value)
        {
            foreach (var item in this)
            {
                if (value.CompareTo(item.Value) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        internal bool Contains(KeyValuePair<TKey, TValue> pair)
        {
            var match = Search(pair.Key);
            if (match.IsEmpty)
            {
                return false;
            }

            return match.value.CompareTo(pair.Value) == 0;
        }

        internal void Freeze()
        {
            if (!frozen)
            {
                Debug.Assert(left is not null && right is not null);
                left.Freeze();
                right.Freeze();
                frozen = true;
            }
        }

        private static Node RotateLeft(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);

            if (tree.right!.IsEmpty)
            {
                return tree;
            }

            var right = tree.right;
            return right.Mutate(left: tree.Mutate(right: right.left));
        }

        private static Node RotateRight(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);

            if (tree.left!.IsEmpty)
            {
                return tree;
            }

            var left = tree.left;
            return left.Mutate(right: tree.Mutate(left: left.right));
        }

        private static Node DoubleLeft(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);

            if (tree.right!.IsEmpty)
            {
                return tree;
            }

            var rotatedRightChild = tree.Mutate(right: RotateRight(tree.right));
            return RotateLeft(rotatedRightChild);
        }

        private static Node DoubleRight(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);

            if (tree.left!.IsEmpty)
            {
                return tree;
            }

            var rotatedLeftChild = tree.Mutate(left: RotateLeft(tree.left));
            return RotateRight(rotatedLeftChild);
        }

        private static int Balance(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);

            return tree.right!.height - tree.left!.height;
        }

        private static bool IsRightHeavy(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);
            return Balance(tree) >= 2;
        }

        private static bool IsLeftHeavy(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);
            return Balance(tree) <= -2;
        }

        private static Node MakeBalanced(Node tree)
        {
            Debug.Assert(tree is not null);
            Debug.Assert(!tree.IsEmpty);

            if (IsRightHeavy(tree))
            {
                return Balance(tree.right!) < 0 ? DoubleLeft(tree) : RotateLeft(tree);
            }

            if (IsLeftHeavy(tree))
            {
                return Balance(tree.left!) > 0 ? DoubleRight(tree) : RotateRight(tree);
            }

            return tree;
        }

        internal static Node NodeTreeFromList(IReadOnlyList<KeyValuePair<TKey, TValue>> items, int start, int length)
        {
            ArgumentNullException.ThrowIfNull(items);
            if (length == 0)
            {
                return EmptyNode;
            }

            int rightCount = (length - 1) / 2;
            int leftCount = length - rightCount - 1;
            var left = NodeTreeFromList(items, start, leftCount);
            var right = NodeTreeFromList(items, start + leftCount + 1, rightCount);
            var item = items[start + leftCount];
            return new Node(item.Key, item.Value, left, right, frozen: true);
        }

        internal static Node NodeTreeFromSortedDictionary(SortedDictionary<TKey, TValue> dictionary)
        {
            ArgumentNullException.ThrowIfNull(dictionary);

            IReadOnlyList<KeyValuePair<TKey, TValue>> list = [.. dictionary];
            return NodeTreeFromList(list, 0, list.Count);
        }

        private Node SetOrAdd(TKey key, TValue value, bool overwriteExistingValue, out bool replacedExistingValue, out bool mutated)
        {
            replacedExistingValue = false;
            if (IsEmpty)
            {
                mutated = true;
                return new Node(key, value, this, this, false);
            }

            Node result = this;
            int comparison = key.CompareTo(this.key);
            if (comparison > 0)
            {
                Debug.Assert(right is not null);
                Node newRight = right.SetOrAdd(key, value, overwriteExistingValue, out replacedExistingValue, out mutated);
                if (mutated)
                {
                    result = Mutate(right: newRight);
                }
            }
            else if (comparison < 0)
            {
                Debug.Assert(left is not null);
                Node newLeft = left.SetOrAdd(key, value, overwriteExistingValue, out replacedExistingValue, out mutated);
                if (mutated)
                {
                    result = Mutate(left: newLeft);
                }
            }
            else
            {
                if (this.value.CompareTo(value) == 0)
                {
                    mutated = false;
                    return this;
                }

                if (!overwriteExistingValue)
                {
                    throw new ArgumentException($"Duplicate Key {key}");
                }

                mutated = true;
                replacedExistingValue = true;
                result = new(key, value, left!, right!, false);
            }

            if (mutated)
            {
                result = MakeBalanced(result);
            }

            return result;
        }

        private Node RemoveRecursive(TKey key, out bool mutated)
        {
            if (IsEmpty)
            {
                mutated = false;
                return this;
            }

            Debug.Assert(left is not null && right is not null);
            var result = this;
            int comparison = key.CompareTo(this.key);
            if (comparison == 0)
            {
                mutated = true;

                if (right.IsEmpty && left.IsEmpty)
                {
                    result = EmptyNode;
                }
                else if (right.IsEmpty && !left.IsEmpty)
                {
                    result = left;
                }
                else if (!right.IsEmpty && left.IsEmpty)
                {
                    result = right;
                }
                else
                {
                    var succesor = this.right;
                    while (!succesor.left!.IsEmpty)
                    {
                        succesor = succesor.left;
                    }

                    var newRight = right.Remove(succesor.key, out _);
                    result = succesor.Mutate(left, right: newRight);
                }
            }
            else if (comparison < 0)
            {
                var newLeft = this.left.Remove(key, out mutated);
                if (mutated)
                {
                    result = Mutate(left: newLeft);
                }
            }
            else // if comparison > 0
            {
                var newRight = this.right.Remove(key, out mutated);
                if (mutated)
                {
                    result = Mutate(right: newRight);
                }
            }

            return result.IsEmpty ? result : MakeBalanced(result);
        }

        private Node Mutate(Node? left = null, Node? right = null)
        {
            Debug.Assert(this.left is not null && this.right is not null);

            if (frozen)
            {
                return new(key, value, left ?? this.left!, right ?? this.right!, false);
            }

            if (left is not null)
            {
                this.left = left;
            }

            if (right is not null)
            {
                this.right = right;
            }

            height = checked((byte)(1 + Math.Max(this.left.height, this.right.height)));
            return this;
        }

        private Node Search(TKey key)
        {
            if (IsEmpty)
            {
                return this;
            }

            int comparison = key.CompareTo(this.key);
            Debug.Assert(left is not null && right is not null);
            if (comparison == 0)
            {
                return this;
            }

            if (comparison > 0)
            {
                return right.Search(key);
            }
            else
            {
                return left.Search(key);
            }
        }

        public override string ToString()
        {
            var s = $"{key} : {value}";
            if (IsEmpty)
            {
                s += " #";
            }

            return s;
        }
    }

    public struct Enumerator : IEnumerator<KeyValuePair<TKey, TValue>>
    {
        // note System.Collections.Immutable uses an object pool for stacks
        private readonly Builder? builder;
        int version;

        private Node root;
        private Node? current;
        private Node[] stack;
        private int stackIndex;

        private readonly bool reverse;
        private readonly bool hasStart;
        private readonly TKey? start;
        private readonly bool hasEnd;
        private readonly TKey? end;

        internal Enumerator(Node root, Builder? builder = null)
        {
            Debug.Assert(root is not null);

            this.builder = builder;
            this.root = root;
            this.stack = !root.IsEmpty ? ArrayPool<Node>.Shared.Rent(root.Height) : [];

            Reset();
        }

        internal Enumerator(Node root, bool reverse)
        {
            Debug.Assert(root is not null);

            this.builder = null;
            this.root = root;
            this.reverse = reverse;
            this.stack = !root.IsEmpty ? ArrayPool<Node>.Shared.Rent(root.Height) : [];

            Reset();
        }

        internal Enumerator(Node root,
            bool reverse,
            TKey? start)
        {
            Debug.Assert(root is not null);

            this.builder = null;
            this.root = root;
            this.reverse = reverse;
            this.start = start ?? throw new ArgumentNullException(nameof(start));
            this.stack = !root.IsEmpty ? ArrayPool<Node>.Shared.Rent(root.Height) : [];
            this.hasStart = true;
            this.hasEnd = false;

            Reset();
        }

        internal Enumerator(Node root,
            bool reverse,
            TKey? start,
            TKey? end)
        {
            Debug.Assert(root is not null);

            this.builder = null;
            this.root = root;
            this.reverse = reverse;
            this.start = start ?? throw new ArgumentNullException(nameof(start));
            this.end = end ?? throw new ArgumentNullException(nameof(end));
            this.stack = !root.IsEmpty ? ArrayPool<Node>.Shared.Rent(root.Height) : [];
            this.hasStart = true;
            this.hasEnd = true;

            Reset();
        }

        public readonly KeyValuePair<TKey, TValue> Current
        {
            get
            {
                ObjectDisposedException.ThrowIf(root is null, this);
                return (current ?? throw new InvalidOperationException()).Value;
            }
        }

        readonly object IEnumerator.Current => Current;

        public void Dispose()
        {
            root = null!;
            current = null;
            stackIndex = -1;
            if (stack is not null)
            {
                ArrayPool<Node>.Shared.Return(stack);
            }
            stack = null!;
        }

        public bool MoveNext()
        {
            ObjectDisposedException.ThrowIf(root is null, this);
            if (builder is not null && builder.Version != version)
            {
                throw new InvalidOperationException("The collection was changed while enumerating");
            }

            if (stack is null || stackIndex < 0)
            {
                current = null;
                return false;
            }

            var n = stack[stackIndex--];
            current = n;
            if (reverse)
            {
                PushRight(n.Left!);
            }
            else
            {
                PushLeft(n.Right!);
            }

            if (hasEnd)
            {
                var comparison = current.Value.Key.CompareTo(end);
                if ((reverse && comparison <= 0) || (!reverse && comparison >= 0))
                {
                    stackIndex = -1;
                    current = null;
                    return false;
                }
            }

            return true;
        }

        public void Reset()
        {
            ObjectDisposedException.ThrowIf(root is null, this);
            Debug.Assert(stack is not null);

            current = null;
            version = builder?.Version ?? -1;
            stackIndex = -1;

            if (root.IsEmpty)
            {
                return;
            }

            if (!hasStart)
            {
                if (reverse)
                {
                    PushRight(root);
                }
                else
                {
                    PushLeft(root);
                }
            }
            else
            {
                AdvanceTo(start!);
            }
        }

        private void AdvanceTo(TKey key)
        {
            var node = root!;
            for (; ; )
            {
                if (node.IsEmpty)
                {
                    break;
                }

                int comparison = key.CompareTo(node.Value.Key);
                if (reverse) comparison = -comparison;

                if (comparison <= 0)
                {
                    stack[++stackIndex] = node;
                    node = node.Left!;
                    Debug.Assert(node is not null);
                    continue;
                }

                if (node.Right!.IsEmpty)
                {
                    break;
                }

                comparison = key.CompareTo(node.Value.Key);
                if (reverse) comparison = -comparison;

                if (comparison < 0)
                {
                    break;
                }

                node = node.Right!;
            }
        }

        bool IEnumerator.MoveNext() => MoveNext();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushLeft(Node node)
        {
            Debug.Assert(node is not null);
            Debug.Assert(stack is not null);
            Debug.Assert(stackIndex < stack.Length - 1);

            // Push the node and all its lefts onto the stack
            while (!node.IsEmpty)
            {
                stack[++stackIndex] = node;
                node = node.Left!;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void PushRight(Node node)
        {
            Debug.Assert(node is not null);
            Debug.Assert(stack is not null);
            Debug.Assert(stackIndex < stack.Length - 1);

            // Push the node and all its rights onto the stack
            while (!node.IsEmpty)
            {
                stack[++stackIndex] = node;
                node = node.Right!;
                Debug.Assert(node is not null);
            }
        }

        void IEnumerator.Reset() => Reset();
    }

    internal sealed class DictionaryEnumerator : IDictionaryEnumerator
    {
        private readonly IEnumerator<KeyValuePair<TKey, TValue>> inner;

        internal DictionaryEnumerator(IEnumerator<KeyValuePair<TKey, TValue>> inner)
        {
            ArgumentNullException.ThrowIfNull(inner, nameof(inner));
            this.inner = inner;
        }

        public DictionaryEntry Entry => new(inner.Current.Key, inner.Current.Value);
        public object Key => inner.Current.Key;
        public object? Value => inner.Current.Value;
        public object Current => this.Entry;
        public bool MoveNext() => inner.MoveNext();
        public void Reset() => inner.Reset();
    }

    internal abstract class KeysOrValuesCollectionAccessor<T> : ICollection<T>, ICollection
    {
        private readonly IImmutableDictionary<TKey, TValue> _dictionary;

        private readonly IEnumerable<T> _keysOrValues;

        protected KeysOrValuesCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary, IEnumerable<T> keysOrValues)
        {
            ArgumentNullException.ThrowIfNull(dictionary, nameof(dictionary));
            ArgumentNullException.ThrowIfNull(keysOrValues, nameof(keysOrValues));

            _dictionary = dictionary;
            _keysOrValues = keysOrValues;
        }

        public bool IsReadOnly => true;
        public int Count => _dictionary.Count;
        protected IImmutableDictionary<TKey, TValue> Dictionary => _dictionary;
        public void Add(T item) => throw new NotSupportedException();
        public void Clear() => throw new NotSupportedException();
        public abstract bool Contains(T item);

        public void CopyTo(T[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex, nameof(arrayIndex));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(array.Length, arrayIndex + this.Count, nameof(arrayIndex));

            foreach (T item in this)
            {
                array[arrayIndex++] = item;
            }
        }

        public bool Remove(T item) => throw new NotSupportedException();

        public IEnumerator<T> GetEnumerator() => _keysOrValues.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();

        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex, nameof(arrayIndex));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(array.Length, arrayIndex + this.Count, nameof(arrayIndex));

            foreach (T item in this)
            {
                array.SetValue(item, arrayIndex++);
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized => true;

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot => this;
    }

    internal sealed class KeysCollectionAccessor : KeysOrValuesCollectionAccessor<TKey>
    {
        internal KeysCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary)
            : base(dictionary, dictionary.Keys)
        {
        }

        public override bool Contains(TKey item) => this.Dictionary.ContainsKey(item);
    }

    internal sealed class ValuesCollectionAccessor : KeysOrValuesCollectionAccessor<TValue>
    {
        internal ValuesCollectionAccessor(IImmutableDictionary<TKey, TValue> dictionary)
            : base(dictionary, dictionary.Values)
        {
        }

        public override bool Contains(TValue item)
        {
            if (Dictionary is ImmutableSortedDictionary<TKey, TValue> sortedDictionary)
            {
                return sortedDictionary.ContainsValue(item);
            }

            if (Dictionary is ImmutableAvlTree<TKey, TValue> dictionary)
            {
                return dictionary.ContainsValue(item);
            }

            throw new NotSupportedException();
        }
    }


    [DebuggerDisplay("Count = {Count}")]
    [DebuggerTypeProxy("System.Collections.Generic.IDictionaryDebugView<,>")]
    public sealed class Builder : IDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IDictionary
    {
        private Node root = Node.EmptyNode;
        private int count;
        private ImmutableAvlTree<TKey, TValue>? immutable;
        private int version;
        private object? syncRoot;

        internal Builder(ImmutableAvlTree<TKey, TValue> map)
        {
            ArgumentNullException.ThrowIfNull(map, nameof(map));
            root = map.root;
            count = map.count;
            immutable = map;
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys => [.. root.Keys];
        public IEnumerable<TKey> Keys => root.Keys;
        ICollection<TValue> IDictionary<TKey, TValue>.Values => [.. root.Values];
        public IEnumerable<TValue> Values => root.Values;
        public int Count => count;
        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly => false;

        internal int Version => version;

        private Node Root
        {
            get
            {
                return root;
            }

            set
            {
                // We *always* increment the version number because some mutations
                // may not create a new value of root, although the existing root
                // instance may have mutated.
                version++;

                if (root != value)
                {
                    root = value;

                    // Clear any cached value for the immutable view since it is now invalidated.
                    immutable = null;
                }
            }
        }

        public TValue this[TKey key]
        {
            get
            {
                TValue value;
                if (!TryGetValue(key, out value!))
                {
                    throw new KeyNotFoundException($"The key '{key}' was not found");
                }

                return value;
            }

            set
            {
                Root = root.SetItem(key, value, out bool replacedExistingValue, out bool mutated);
                if (mutated && !replacedExistingValue)
                {
                    count++;
                }
            }
        }

        public ref readonly TValue ValueRef(TKey key)
        {
            ArgumentNullException.ThrowIfNull(key);
            return ref root.ValueRef(key);
        }
        bool IDictionary.IsFixedSize => false;
        bool IDictionary.IsReadOnly => false;
        ICollection IDictionary.Keys => Keys.ToArray();
        ICollection IDictionary.Values => Values.ToArray();

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        object ICollection.SyncRoot
        {
            get
            {
                if (syncRoot == null)
                {
                    Interlocked.CompareExchange<object?>(ref syncRoot, new object(), null);
                }

                return syncRoot;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        bool ICollection.IsSynchronized => false;

        public IComparer<TKey> KeyComparer
        {
            get => Comparer<TKey>.Default;
            set => throw new NotSupportedException();
        }

        public IEqualityComparer<TValue> ValueComparer
        {
            get => EqualityComparer<TValue>.Default;
            set => throw new NotSupportedException();
        }

        void IDictionary.Add(object key, object? value) => Add((TKey)key, (TValue)value!);
        bool IDictionary.Contains(object key) => ContainsKey((TKey)key);
        IDictionaryEnumerator IDictionary.GetEnumerator() => new DictionaryEnumerator(GetEnumerator());
        void IDictionary.Remove(object key) => Remove((TKey)key);
        object? IDictionary.this[object key]
        {
            get { return this[(TKey)key]; }
            set { this[(TKey)key] = (TValue)value!; }
        }
        void ICollection.CopyTo(Array array, int index) => Root.CopyTo(array, index, Count);

        public void Add(TKey key, TValue value)
        {
            Root = Root.Add(key, value, out bool mutated);
            if (mutated)
            {
                count++;
            }
        }

        public bool ContainsKey(TKey key) => Root.ContainsKey(key);

        public bool Remove(TKey key)
        {
            Root = Root.Remove(key, out var mutated);
            if (mutated)
            {
                count--;
            }

            return mutated;
        }

        public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Root.TryGetValue(key, out value!);

        public bool TryGetKey(TKey equalKey, out TKey actualKey)
        {
            ArgumentNullException.ThrowIfNull(equalKey, nameof(equalKey));
            return this.Root.TryGetKey(equalKey, out actualKey);
        }

        public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);

        public void Clear()
        {
            Root = Node.EmptyNode;
            count = 0;
        }

        public bool Contains(KeyValuePair<TKey, TValue> item) => Root.Contains(item);

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Root.CopyTo(array, arrayIndex, Count);
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            if (Contains(item))
            {
                return Remove(item.Key);
            }

            return false;
        }

        public Enumerator GetEnumerator() => Root.GetEnumerator(this);

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public bool ContainsValue(TValue value) => root.ContainsValue(value);

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            foreach (KeyValuePair<TKey, TValue> pair in items)
            {
                Add(pair);
            }
        }

        public void RemoveRange(IEnumerable<TKey> keys)
        {
            ArgumentNullException.ThrowIfNull(keys);

            foreach (TKey key in keys)
            {
                Remove(key);
            }
        }
        public TValue? GetValueOrDefault(TKey key)
        {
            return this.GetValueOrDefault(key, default!);
        }

        public TValue GetValueOrDefault(TKey key, TValue defaultValue)
        {
            ArgumentNullException.ThrowIfNull(key, nameof(key));

            TValue value;
            if (TryGetValue(key, out value!))
            {
                return value;
            }

            return defaultValue;
        }

        public ImmutableAvlTree<TKey, TValue> ToImmutable()
        {
            // Creating an instance of ImmutableSortedMap<T> with our root node automatically freezes our tree,
            // ensuring that the returned instance is immutable.  Any further mutations made to this builder
            // will clone (and unfreeze) the spine of modified nodes until the next time this method is invoked.
            return immutable ??= new(root, count);
        }
    }

    public readonly struct Enumerable : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private readonly ImmutableAvlTree<TKey, TValue> instance;
        private readonly bool reversed;
        private readonly TKey? start;
        private readonly TKey? end;
        private readonly bool hasStart;
        private readonly bool hasEnd;

        public Enumerable(ImmutableAvlTree<TKey, TValue> instance, bool reversed)
        {
            this.instance = instance;
            this.reversed = reversed;
        }

        public Enumerable(ImmutableAvlTree<TKey, TValue> instance, bool reversed, TKey start)
        {
            this.instance = instance;
            this.reversed = reversed;
            this.start = start ?? throw new ArgumentNullException(nameof(start));
            this.hasStart = true;
        }

        public Enumerable(ImmutableAvlTree<TKey, TValue> instance, bool reversed, TKey start, TKey end)
        {
            this.instance = instance;
            this.reversed = reversed;
            this.start = start ?? throw new ArgumentNullException(nameof(start));
            this.end = end ?? throw new ArgumentNullException(nameof(end));
            this.hasStart = true;
            this.hasEnd = true;
        }

        public readonly Enumerator GetEnumerator()
        {
            if (hasEnd)
            {
                return new Enumerator(instance.root, reversed, start, end);
            }

            if (hasStart)
            {
                return new Enumerator(instance.root, reversed, start);
            }

            return new Enumerator(instance.root, reversed);
        }

        readonly IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
            => GetEnumerator();

        readonly IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}

internal sealed class ImmutableDictionaryDebuggerProxy<TKey, TValue> where TKey : notnull
{
    /// <summary>
    /// The dictionary to show to the debugger.
    /// </summary>
    private readonly IReadOnlyDictionary<TKey, TValue> _dictionary;

    /// <summary>
    /// The contents of the dictionary, cached into an array.
    /// </summary>
    private DebugViewDictionaryItem<TKey, TValue>[]? _cachedContents;

    /// <summary>
    /// Initializes a new instance of the <see cref="ImmutableDictionaryDebuggerProxy{TKey, TValue}"/> class.
    /// </summary>
    /// <param name="dictionary">The dictionary to show in the debugger.</param>
    public ImmutableDictionaryDebuggerProxy(IReadOnlyDictionary<TKey, TValue> dictionary)
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        _dictionary = dictionary;
    }

    /// <summary>
    /// Gets the contents of the dictionary for display in the debugger.
    /// </summary>
    [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
    public DebugViewDictionaryItem<TKey, TValue>[] Contents
    {
        get
        {
            if (_cachedContents is null)
            {
                _cachedContents = new DebugViewDictionaryItem<TKey, TValue>[_dictionary.Count];
                for (int i = 0; i < _cachedContents.Length; i++)
                {
                    _cachedContents[i] = new(_dictionary.ElementAt(i));
                }
            }

            return _cachedContents;
        }
    }
}

[DebuggerDisplay("{Value}", Name = "[{Key}]")]
internal readonly struct DebugViewDictionaryItem<TKey, TValue>
{
    public DebugViewDictionaryItem(TKey key, TValue value)
    {
        Key = key;
        Value = value;
    }

    public DebugViewDictionaryItem(KeyValuePair<TKey, TValue> keyValue)
    {
        Key = keyValue.Key;
        Value = keyValue.Value;
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public TKey Key { get; }

    [DebuggerBrowsable(DebuggerBrowsableState.Collapsed)]
    public TValue Value { get; }
}

public static class ToImmutableAvlTreeExtensions
{
    public static ImmutableAvlTree<TKey, TValue> ToImmutableAvlTree<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> entries)
        where TKey : IComparable<TKey>
        where TValue : IComparable<TValue>
    {
        ArgumentNullException.ThrowIfNull(entries, nameof(entries));
        return ImmutableAvlTree.CreateRange(entries);
    }
}