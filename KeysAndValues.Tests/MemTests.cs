using KeysAndValues.Bench;
using KeysAndValues.Internal;
using System.Text;

namespace KeysAndValues.Tests;

public sealed class MemTests : IDisposable
{
    readonly UnsafeMemoryPool pool = new();

    [Fact]
    public void BasicAllocationTest()
    {
        var src = Encoding.UTF8.GetBytes("Hello, World!");
        var mem = pool.Allocate(src);
        Assert.Equal(src.LongLength, mem.LongLength);
        Assert.True(src.AsSpan().SequenceEqual(mem));
    }

    [Fact]
    public void MultiAllocationTest()
    {
        var src1 = Encoding.UTF8.GetBytes("Hello, World 1!");
        var src2 = Encoding.UTF8.GetBytes("Hello, World 2!");
        var mem1 = pool.Allocate(src1);
        var mem2 = pool.Allocate(src2);
        Assert.Equal(src1.LongLength, mem1.LongLength);
        Assert.True(src1.AsSpan().SequenceEqual(mem1));
        Assert.Equal(src2.LongLength, mem2.LongLength);
        Assert.True(src2.AsSpan().SequenceEqual(mem2));
    }

    [Fact]
    public void ComparisonTest()
    {
        var src1 = Encoding.UTF8.GetBytes("Hello, World 1!");
        var src2 = Encoding.UTF8.GetBytes("Hello, World 2!");
        var mem1 = pool.Allocate(src1);
        var mem2 = pool.Allocate(src2);
        var mem3 = pool.Allocate(src1);
        var mem4 = pool.Allocate(src2);
        Assert.True(mem1 == mem3);
        Assert.True(mem1 != mem2);
        Assert.True(mem2 == mem4);
        Assert.True(mem3 != mem4);
        Assert.True(mem1.CompareTo(mem2) == -1);
        Assert.True(mem2.CompareTo(mem1) == 1);
        Assert.True(mem1 < mem2);
        Assert.True(mem1 <= mem2);
        Assert.True(mem2 > mem1);
        Assert.True(mem2 >= mem1);
        Assert.True(mem2 >= mem4);
        Assert.True(mem2 <= mem4);
    }

    public void Dispose()
    {
        pool.Dispose();
    }
}
