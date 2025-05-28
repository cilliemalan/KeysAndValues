using System.ComponentModel;

namespace KeysAndValues;

public sealed partial class KeyValueStore
{
    public delegate void ChangeHandler(
        KeyValueStore keyValueStore, 
        in ReadOnlySpan<ChangeOperation> operations,
        long newSequence,
        ImmutableAvlTree<Mem, Mem> store);

    private SpinLock spinLock = new();
    private long sequence;
    private ImmutableAvlTree<Mem, Mem> store = ImmutableAvlTree<Mem, Mem>.Empty;

    public int Count => store.Count;

    /// <summary>
    /// Called whenever the store changes. Handlers
    /// must not block, as it is called while
    /// a spinlock is held.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Advanced)]
    public event ChangeHandler? Changed;

    private KeyValueStore()
    {
    }

    public static KeyValueStore CreateEmpty()
        => new();

    public static KeyValueStore CreateNewFrom(IEnumerable<KeyValuePair<Mem, Mem>> source)
    {
        return new()
        {
            store = ImmutableAvlTree<Mem, Mem>.Empty.AddRange(source, true, false),
            sequence = 1 // shrug
        };
    }

    public bool TryGet(Mem key, out Mem value)
    {
        return store.TryGetValue(key, out value);
    }

    public long Apply(ReadOnlySpan<ChangeOperation> operations)
    {
        for (; ; )
        {
            var s = store;

            bool spinLockTaken = false;
            spinLock.TryEnter(ref spinLockTaken);
            if (!spinLockTaken)
            {
                Thread.Yield();
                continue;
            }

            var newStore = s.Apply(operations);
            bool exchanged = Interlocked.CompareExchange(ref store, newStore, s) == s;
            if (!exchanged)
            {
                spinLock.Exit();
                continue;
            }

            // NOTE: Using interlocked because I don't believe the spinlock
            // will do a memory barrier.
            var newSequence = Interlocked.Increment(ref sequence);
            
            try
            {
                Changed?.Invoke(this, operations, newSequence, newStore);
            }
            finally
            {
                spinLock.Exit();
            }
            
            return newSequence;
        }
    }

    public long Sequence => sequence;

    public ImmutableAvlTree<Mem, Mem> Snapshot() => store;

    public ImmutableAvlTree<Mem, Mem> Snapshot(out long sequence)
    {
        bool taken = false;
        spinLock.Enter(ref taken);
        Debug.Assert(taken);
        sequence = this.sequence;
        var store = this.store;
        spinLock.Exit();

        return store;
    }
}
