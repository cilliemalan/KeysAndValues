using System.Text;

namespace KeysAndValues.Tests;

public sealed class MemTests
{
    [Fact]
    public void BasicAllocationTest()
    {
        var src = Encoding.UTF8.GetBytes("Hello, World!");
        var mem = new Mem(src);
        Assert.Equal(src.Length, mem.Length);
        Assert.True(src.AsSpan().SequenceEqual(mem));
    }

    [Fact]
    public void MultiAllocationTest()
    {
        var src1 = Encoding.UTF8.GetBytes("Hello, World 1!");
        var src2 = Encoding.UTF8.GetBytes("Hello, World 2!");
        var mem1 = new Mem(src1);
        var mem2 = new Mem(src2);
        Assert.Equal(src1.Length, mem1.Length);
        Assert.True(src1.AsSpan().SequenceEqual(mem1));
        Assert.Equal(src2.Length, mem2.Length);
        Assert.True(src2.AsSpan().SequenceEqual(mem2));
    }

    [Fact]
    public void ComparisonTest()
    {
        var src1 = Encoding.UTF8.GetBytes("Hello, World 1!");
        var src2 = Encoding.UTF8.GetBytes("Hello, World 2!");
        var mem1 = new Mem(src1);
        var mem2 = new Mem(src2);
        var mem3 = new Mem(src1);
        var mem4 = new Mem(src2);
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

    [Fact]
    public void LengthComparisonTest()
    {
        Mem[] strings = 
        [
            new(Encoding.UTF8.GetBytes("itema")),
            new(Encoding.UTF8.GetBytes("itemb")),
            new(Encoding.UTF8.GetBytes("itemb1")),
            new(Encoding.UTF8.GetBytes("itemb2")),
            new(Encoding.UTF8.GetBytes("itemc")),
            new(Encoding.UTF8.GetBytes("itemd")),
        ];

        Assert.True(strings[0] < strings[1]);
        Assert.True(strings[1] < strings[2]);
        Assert.True(strings[2] < strings[3]);
        Assert.True(strings[3] < strings[4]);
        Assert.True(strings[4] < strings[5]);
    }
}
