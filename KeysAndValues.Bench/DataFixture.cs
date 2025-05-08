namespace KeysAndValues.Bench;

public abstract class DataFixture
{
    protected Mem[] keys = null!;
    protected Mem[] values = null!;
    bool disposed;

    protected void Clear()
    {
        keys = null!;
        values = null!;
    }

    protected void Initialize()
    {
        Clear();
        var data = Corpus.Generate(10000, 16, 32, 16, 1024, 123123123);
        keys = [.. data.Keys];
        var r = new Random();
        values = [.. data.Values.OrderBy(_ => r.Next())];
    }

    protected virtual void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref disposed, true))
        {
            return;
        }

        Clear();
    }
}
