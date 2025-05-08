using System.Runtime.CompilerServices;

namespace KeysAndValues;

public sealed partial class KeyValueStore : IEnumerable<KeyValuePair<Mem, Mem>>
{
    private readonly SpinLock spinLock = new();
    private long sequence;
    private ImmutableAvlTree<Mem, Mem> store = ImmutableAvlTree<Mem, Mem>.Empty;

    private KeyValueStore()
    {
    }

    private KeyValueStore(IDictionary<Mem, Mem> data, long sequence)
    {
        this.sequence = sequence;
        store = ImmutableAvlTree.CreateRange(data);
    }

    public static KeyValueStore CreateEmpty() => new();

    public static KeyValueStore CreateNewFrom(IDictionary<Mem, Mem> source, long sequence = 1) =>
        new(ImmutableAvlTree.CreateRange(source), sequence);

    public bool TryGet(Mem key, out Mem value)
    {
        return store.TryGetValue(key, out value);
    }

    public unsafe long Apply(ChangeOperation[] operations)
    {
        if (operations.Length == 0)
        {
            return Interlocked.Increment(ref sequence);
        }

        for (; ; )
        {
            var s = store;

            var b = s.ToBuilder();
            for (int i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];

                switch (operation.Type)
                {
                    case ChangeOperationType.Set:
                        b[operation.Key] = operation.Value;
                        break;
                    case ChangeOperationType.Delete:
                        b.Remove(operation.Key);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operations));
                }
            }

            var newSequence = TryMerge(s, b.ToImmutable());
            if (newSequence != 0)
            {
                return newSequence;
            }
        }
    }

    private long TryMerge(ImmutableAvlTree<Mem, Mem> oldStore, ImmutableAvlTree<Mem, Mem> newStore)
    {
        bool taken = false;
        spinLock.Enter(ref taken);
        if (!taken)
        {
            return 0;
        }

        try
        {
            bool exchanged = Interlocked.CompareExchange(ref store, newStore, oldStore) == oldStore;
            if (!exchanged)
            {
                return 0;
            }

            return ++sequence;
        }
        finally
        {
            spinLock.Exit();
        }
    }

    public KeyValueStore Snapshot()
    {
        ForceTakeSpinlock();
        var sequence = this.sequence;
        var store = this.store;
        spinLock.Exit();

        return new(store, sequence);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ForceTakeSpinlock()
    {
        for (; ; )
        {
            bool taken = false;
            spinLock.Enter(ref taken);
            if (taken)
            {
                break;
            }
            Thread.Yield();
        }
    }
}
