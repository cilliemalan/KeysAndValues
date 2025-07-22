using System.ComponentModel;

namespace KeysAndValues;

public sealed partial class KeyValueStore
{
    public delegate void ChangeHandler(
        KeyValueStore keyValueStore,
        in ReadOnlySpan<ChangeOperation> operations,
        long newSequence,
        ImmutableAvlTree<Mem, Mem> store);

    private StoreVersion store = StoreVersion.Empty;

    public int Count => store.Data.Count;

    /// <summary>
    /// Called whenever the store changes. Handlers
    /// must not block, as it is called while
    /// a spinlock is held.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public event ChangeHandler? Changed;

    public KeyValueStore() { }

    public KeyValueStore(StoreVersion storeVersion)
    {
        store = storeVersion ?? throw new ArgumentNullException(nameof(storeVersion));
    }

    public KeyValueStore(long sequence, ImmutableAvlTree<Mem, Mem> store)
        : this(new StoreVersion(sequence, store ?? throw new ArgumentNullException(nameof(store))))
    {
    }

    public KeyValueStore(long sequence, IEnumerable<KeyValuePair<Mem, Mem>> store)
        : this(new StoreVersion(sequence, ImmutableAvlTree<Mem, Mem>.Empty.AddRange(store ?? throw new ArgumentNullException(nameof(store)))))
    {
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use constructor instead.")]
    public static KeyValueStore CreateEmpty() => new();

    public static KeyValueStore CreateNewFrom(long sequence, IEnumerable<KeyValuePair<Mem, Mem>> source)
        => new(new StoreVersion(sequence, ImmutableAvlTree<Mem, Mem>.Empty.AddRange(source, true, false)));

    public bool TryGet(Mem key, out Mem value)
    {
        return store.Data.TryGetValue(key, out value);
    }

    public long Apply(ReadOnlySpan<ChangeOperation> operations)
    {
        for (int i = 0; ; i++)
        {
            var s = store;
            var newStore = new StoreVersion(
                checked(s.Sequence + 1),
                s.Data.Apply(operations));

            bool exchanged = Interlocked.CompareExchange(ref store, newStore, s) == s;
            if (exchanged)
            {
                Changed?.Invoke(this, operations, newStore.Sequence, newStore.Data);
                return newStore.Sequence;
            }

            if (i == 0)
            {
                Thread.Yield();
            }
            else
            {
                Thread.Sleep(i * Random.Shared.Next(120));
            }
        }
    }

    public long Sequence => store.Sequence;

    public StoreVersion Snapshot() => store;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use Snapshot instead")]
    public ImmutableAvlTree<Mem, Mem> SnapshotOld() => store.Data;

    [EditorBrowsable(EditorBrowsableState.Never)]
    [Obsolete("Use Snapshot instead")]
    public ImmutableAvlTree<Mem, Mem> SnapshotOld(out long sequence)
    {
        var store = this.store;
        sequence = store.Sequence;
        return store.Data;
    }
}
