namespace KeysAndValues.Internal;
using System.Security.Cryptography;
using System.Reflection;
using System.Linq.Expressions;

/// <summary>
/// Provides access to the underlying methods
/// of the sha256 implementation so that
/// hashing may be performed on spans directly.
/// </summary>
public readonly struct Sha256Hasher : IDisposable
{
    static readonly Action<SHA256, ReadOnlySpan<byte>> ingest;
    static readonly Action<SHA256, Span<byte>> compute;

    static Sha256Hasher()
    {
        var htt = typeof(HashAlgorithm);
        var hashCore = htt.GetMethod("HashCore",
            BindingFlags.NonPublic | BindingFlags.Instance,
            [typeof(ReadOnlySpan<byte>)])!;
        var tryHashFinal = htt.GetMethod("TryHashFinal",
            BindingFlags.NonPublic | BindingFlags.Instance)!;

        var pSha = Expression.Parameter(typeof(SHA256), "sha");
        var pData = Expression.Parameter(typeof(ReadOnlySpan<byte>), "data");
        var callHashCore = Expression.Call(
                instance: pSha,
                method: hashCore,
                [pData]);
        ingest = Expression.Lambda<Action<SHA256, ReadOnlySpan<byte>>>(
            callHashCore,
            [pSha, pData])
            .Compile();

        var pBytesWritten = Expression.Variable(typeof(int), "bytesWritten");
        var pDest = Expression.Parameter(typeof(Span<byte>), "destination");
        var pCallTryHashFinal = Expression.Call(
                    instance: pSha,
                    method: tryHashFinal,
                    [pDest, pBytesWritten]);
        var pTryFinalBlock = Expression.Block(
                [pBytesWritten],
                pCallTryHashFinal);
        compute = Expression.Lambda<Action<SHA256, Span<byte>>>(
            pTryFinalBlock,
            [pSha, pDest])
            .Compile();
    }

    readonly SHA256 sha = SHA256.Create();

    public Sha256Hasher() { }

    public void Ingest(ReadOnlySpan<byte> data)
    {
        ingest(sha, data);
    }

    public void Compute(Span<byte> destination)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(destination.Length, 32, nameof(destination));
        compute(sha, destination);
    }

    public void Dispose()
    {
        sha.Dispose();
    }
}
