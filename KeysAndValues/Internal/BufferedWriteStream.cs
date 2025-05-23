namespace KeysAndValues.Internal; 

using System.Collections.Concurrent;
using System.Runtime.ExceptionServices;

public sealed class BufferedWriteStream : Stream
{
    private long writtenLength = 0;
    private long totalLength = 0;
    private int disposed = 0;
    private Exception? pendingException;
    private readonly Thread backgroundThread;
    private readonly BlockingCollection<WriteOperation> writeQueue = [];
    private readonly Stream underlyingStream;
    private readonly bool ownsStream;

    public long WrittenLength => writtenLength;

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => totalLength;
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public BufferedWriteStream(Stream underlyingStream, bool ownsStream)
    {
        ArgumentNullException.ThrowIfNull(underlyingStream);

        this.underlyingStream = underlyingStream;
        this.ownsStream = ownsStream;
        backgroundThread = new Thread(WriteThreadProc);
        backgroundThread.IsBackground = true;
        backgroundThread.Name = "BufferedWriteStream";
        backgroundThread.Start();
    }

    public override void Flush()
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ThrowPendingException();

        writeQueue.Add(WriteOperation.CreateFlush());
    }

    public override Task FlushAsync(CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ThrowPendingException();

        writeQueue.Add(WriteOperation.CreateFlush(out var task, cancellationToken), cancellationToken);
        return task;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotSupportedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotSupportedException();
    }

    public override void SetLength(long value)
    {
        throw new NotSupportedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(buffer.Length, offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(buffer.Length, offset + count);

        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ThrowPendingException();

        writeQueue.Add(new(new ReadOnlyMemory<byte>(buffer, offset, count)));
        Interlocked.Add(ref totalLength, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ThrowPendingException();

        writeQueue.Add(new(new ReadOnlyMemory<byte>(buffer.ToArray())));
        Interlocked.Add(ref totalLength, buffer.Length);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(buffer);
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(buffer.Length, offset);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(buffer.Length, offset + count);
        cancellationToken.ThrowIfCancellationRequested();

        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ThrowPendingException();

        var tcs = new TaskCompletionSource();
        writeQueue.Add(new(new ReadOnlyMemory<byte>(buffer, offset, count), tcs, false, cancellationToken), cancellationToken);
        Interlocked.Add(ref totalLength, count);
        return tcs.Task;
    }

    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ObjectDisposedException.ThrowIf(disposed != 0, this);
        ThrowPendingException();

        var tcs = new TaskCompletionSource();
        writeQueue.Add(new(buffer, tcs, false, cancellationToken), cancellationToken);
        Interlocked.Add(ref totalLength, buffer.Length);
        return new(tcs.Task);
    }

    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        writeQueue.CompleteAdding();
        await Task.Run(backgroundThread.Join).ConfigureAwait(false);
        writeQueue.Dispose();

        if (ownsStream)
        {
            underlyingStream.Dispose();
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (Interlocked.Exchange(ref disposed, 1) == 1)
        {
            return;
        }

        writeQueue.CompleteAdding();
        backgroundThread.Join();
        writeQueue.Dispose();

        if (ownsStream)
        {
            underlyingStream.Dispose();
        }
    }

    private void WriteThreadProc(object? obj)
    {
        try
        {
            foreach (var operation in writeQueue.GetConsumingEnumerable())
            {
                if (operation.CancellationToken.IsCancellationRequested)
                {
                    operation.TaskCompletionSource?.TrySetCanceled(operation.CancellationToken);
                    continue;
                }

                if (!operation.Data.IsEmpty)
                {
                    underlyingStream.Write(operation.Data.Span);
                    Interlocked.Add(ref writtenLength, operation.Data.Length);
                }

                if (operation.Flush)
                {
                    underlyingStream.Flush();
                }

                operation.TaskCompletionSource?.TrySetResult();
            }
        }
        catch (Exception ex)
        {
            pendingException = ex;
        }
    }

    private void ThrowPendingException()
    {
        if (pendingException is null)
        {
            return;
        }

        var exception = pendingException;
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    private record WriteOperation(ReadOnlyMemory<byte> Data,
        TaskCompletionSource? TaskCompletionSource,
        bool Flush,
        CancellationToken CancellationToken)
    {
        public WriteOperation(ReadOnlyMemory<byte> data) : this(data, null, false, default) { }

        public static WriteOperation CreateFlush(out Task task, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource();
            task = tcs.Task;
            return new(default, tcs, true, cancellationToken);
        }

        public static WriteOperation CreateFlush() => new(default, null, true, default);
    }
}
