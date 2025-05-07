using KeysAndValues.Internal;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace KeysAndValues;

public sealed partial class KeyValueStore : IEnumerable<KeyValuePair<Mem, Mem>>, IDisposable
{
    private int disposed;
    private long sequence;
    private readonly SpinLock spinLock = new();
    private readonly UnsafeMemoryPool pool = new();
    private ImmutableSortedDictionary<Mem, Mem> store = ImmutableSortedDictionary<Mem, Mem>.Empty;

    private KeyValueStore()
    {
    }

    public static KeyValueStore CreateEmpty() => new();

    public static KeyValueStore CreateNewFrom(IDictionary<Mem, Mem> source)
    {
        var m = new KeyValueStore();

        var b = m.store.ToBuilder();

        foreach (var kvp in source)
        {
            b.Add(m.pool.Allocate(kvp.Key), m.pool.Allocate(kvp.Value));
        }

        m.store = b.ToImmutable();
        m.sequence = 1;
        return m;
    }

    public unsafe bool TryGet(ReadOnlySpan<byte> key, out Mem value)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);

        fixed (byte* k = key)
        {
            Mem kmem = new(k, key.Length);
            return store.TryGetValue(kmem, out value);
        }
    }

    public unsafe long Apply(ChangeOperation[] operations)
    {
        Span<Mem> newValues;
        Span<Mem> newKeys;
        if (operations.Length > 1024)
        {
            newValues = new Mem[operations.Length];
            newKeys = new Mem[operations.Length];
        }
        else
        {
            // A result of a stackalloc expression of this type in this context may be exposed outside of the containing method.
            // These are never returned. Why is it complaining?
#pragma warning disable CS9081 
            newValues = stackalloc Mem[operations.Length];
            newKeys = stackalloc Mem[operations.Length];
#pragma warning restore CS9081 
        }

        for (int i = 0; i < operations.Length; i++)
        {
            if (operations[i].Type != ChangeOperationType.Set)
            {
                continue;
            }

            newValues[i] = pool.Allocate(operations[i].Value.Span);
        }

        for (; ; )
        {
            ObjectDisposedException.ThrowIf(disposed != 0, this);

            var s = store;
            var b = s.ToBuilder();
            for (int i = 0; i < operations.Length; i++)
            {
                var operation = operations[i];

                using var kmh = operation.Key.Pin();
                var kmem = new Mem(kmh, operation.Key.Length);

                switch (operation.Type)
                {
                    case ChangeOperationType.Set:
                        {
                            if (b.TryGetKey(kmem, out var existingKey))
                            {
                                b[existingKey] = newValues[i];
                            }
                            else
                            {
                                var nkey = newKeys[i];
                                if (nkey.IsNull)
                                {
                                    nkey = newKeys[i] = pool.Allocate(kmem);
                                }
                                b[nkey] = newValues[i];
                            }
                        }
                        break;
                    case ChangeOperationType.Delete:
                        b.Remove(kmem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(operations));
                }
            }

            var newSequence = TryMerge(s, b.ToImmutable());
            if (newSequence != 0)
            {
                //OnChange?.Invoke((newSequence, operations));
                return newSequence;
            }
        }
    }

    private long TryMerge(ImmutableSortedDictionary<Mem, Mem> oldStore, ImmutableSortedDictionary<Mem, Mem> newStore)
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

    internal (long sequence, ImmutableSortedDictionary<Mem, Mem> store) Snapshot()
    {
        ForceTakeSpinlock();

        var _store = store;
        var _sequence = sequence;
        spinLock.Exit();

        return (_sequence, _store);
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

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            pool.Dispose();
        }
    }
}
