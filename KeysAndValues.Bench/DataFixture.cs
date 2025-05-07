namespace KeysAndValues.Bench;

public abstract class DataFixture : IDisposable
{
    protected Mem[] keys = null!;
    protected Mem[] values = null!;
    IDisposable pool = null!;
    bool disposed;

    protected void Clear()
    {
        keys = null!;
        values = null!;
        pool?.Dispose();
        pool = null!;
    }

    protected void Initialize()
    {
        Clear();
        var (pool, data) = Corpus.Generate(10000, 8, 32, 16, 1024, 123123123);
        keys = [.. data.Keys];
        var r = new Random();
        values = [.. data.Values.OrderBy(_ => r.Next())];
        this.pool = pool;
        Console.WriteLine($"Corups size is {((Internal.UnsafeMemoryPool)pool).TotalAllocated} bytes");
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        Clear();
    }

    ~DataFixture()
    {
        Dispose(disposing: false);
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
