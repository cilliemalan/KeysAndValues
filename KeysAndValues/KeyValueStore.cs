using System.ComponentModel;

namespace KeysAndValues;

/// <summary>
/// Simple in-memory key-value store.
/// </summary>
public sealed partial class KeyValueStore
{
    /// <summary>
    /// Handler for change events.
    /// </summary>
    /// <param name="keyValueStore">The store that triggered the event.</param>
    /// <param name="operations">Operations for the change</param>
    /// <param name="newVersion">The new version after the change.</param>
    public delegate void ChangeHandler(KeyValueStore keyValueStore, in ReadOnlySpan<ChangeOperation> operations, StoreVersion newVersion);

    private StoreVersion store = StoreVersion.Empty;

    /// <summary>
    /// Gets the number of items in the store.
    /// </summary>
    public int Count => store.Data.Count;

    /// <summary>
    /// Called whenever the store changes. Handlers
    /// must not block, as it is called while
    /// a spinlock is held.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public event ChangeHandler? Changed;

    /// <summary>
    /// Create an empty store.
    /// </summary>
    public KeyValueStore() { }

    /// <summary>
    /// Create a store based on a store version
    /// </summary>
    /// <param name="storeVersion">The store version to create a new store based on.</param>
    /// <exception cref="ArgumentNullException">The store was null</exception>
    public KeyValueStore(StoreVersion storeVersion)
    {
        store = storeVersion ?? throw new ArgumentNullException(nameof(storeVersion));
    }

    /// <summary>
    /// Create a new store based on existing data.
    /// </summary>
    /// <param name="sequence">The sequence number the new store will be created on.</param>
    /// <param name="store">The data for the store.</param>
    /// <exception cref="ArgumentNullException"><paramref name="store"/> was null.</exception>
    public KeyValueStore(long sequence, IEnumerable<KeyValuePair<Mem, Mem>> store)
        : this(new StoreVersion(sequence, ImmutableAvlTree<Mem, Mem>.Empty.AddRange(store ?? throw new ArgumentNullException(nameof(store)))))
    {
    }

    /// <summary>
    /// Try to retrieve an item from the store.
    /// </summary>
    /// <param name="key">The item key.</param>
    /// <param name="value">The item value.</param>
    /// <returns><c>true</c> if the item was found.</returns>
    public bool TryGet(Mem key, out Mem value)
    {
        return store.Data.TryGetValue(key, out value);
    }

    /// <summary>
    /// Apply a set of change operations to the store.
    /// </summary>
    /// <param name="operations">The operations to store</param>
    /// <returns>The version object after the change.</returns>
    public StoreVersion Apply(ReadOnlySpan<ChangeOperation> operations)
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
                Changed?.Invoke(this, operations, newStore);
                return newStore;
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

    /// <summary>
    /// Get the sequence number of the current version of the store.
    /// </summary>
    public long Sequence => store.Sequence;

    /// <summary>
    /// Get the data of the current version of the store.
    /// </summary>
    public ImmutableAvlTree<Mem, Mem> Data => store.Data;

    /// <summary>
    /// Get the current version of the store.
    /// </summary>
    public StoreVersion Snapshot() => store;
}
