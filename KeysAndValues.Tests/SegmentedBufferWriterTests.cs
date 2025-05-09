using KeysAndValues.Internal;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace KeysAndValues.Tests;

public class SegmentedBufferWriterTests
{
    [Fact]
    public void BasicTest()
    {
        string[] thing = ["hello", "world"];
        Assert.Equal(thing, Churn(thing));
    }

    [Fact]
    public void EmptyTest()
    {
        SegmentedBufferWriter<byte> writer = new();
        var seg = writer.WrittenSequence;
        Assert.Equal(0, seg.Length);
        Assert.True(seg.IsEmpty);
    }

    [Fact]
    public void SingleByteTest()
    {
        SegmentedBufferWriter<byte> writer = new();
        writer.GetSpan(1)[0] = 0x01;
        writer.Advance(1);
        var s = writer.WrittenSequence;
        Assert.Equal(1, s.Length);
        Assert.Equal([0x01], s.FirstSpan.ToArray());
    }

    [Fact]
    public void NoAdvanceTest()
    {
        SegmentedBufferWriter<byte> writer = new();
        var s1 = writer.GetSpan();
        var s2 = writer.GetSpan();
        s1[0] = 1;
        Assert.Equal(1, s2[0]);
    }

    [Theory]
    [InlineData(100, 33, 1)]
    [InlineData(100, 33, 2)]
    [InlineData(100, 33, 3)]
    [InlineData(100, 33, 4)]
    [InlineData(100, 33, 10)]
    [InlineData(1000, 100, 9)]
    [InlineData(1000, 100, 10)]
    [InlineData(1000, 100, 11)]
    [InlineData(1000, 1000, 3)]
    [InlineData(100, 31, 3)]
    [InlineData(100, 31, 4)]
    [InlineData(100, 31, 5)]
    public void VariousSizesTest(int chunkSize, int writeSize, int numWrites)
    {
        var allData = RandomNumberGenerator.GetBytes(writeSize * numWrites);
        SegmentedBufferWriter<byte> writer = new(chunkSize);
        for (int i = 0; i < numWrites; i++)
        {
            var s = writer.GetSpan(writeSize);
            allData.AsSpan(i * writeSize, writeSize).CopyTo(s);
            writer.Advance(writeSize);
        }

        using var ms = new MemoryStream(writer.Length);
        foreach(var d in writer.WrittenSequence)
        {
            ms.Write(d.Span);
        }
        var _a = ms.ToArray().AsSpan();
        Assert.Equal(allData.AsSpan(), _a);
    }

    private static T Churn<T>(T thing)
    {
        SegmentedBufferWriter<byte> writer = new();
        System.Text.Json.Utf8JsonWriter ujw = new(writer);
        System.Text.Json.JsonSerializer.Serialize(ujw, thing);
        using var ms = new MemoryStream(writer.Length);
        foreach (var s in writer.WrittenSequence)
        {
            ms.Write(s.Span);
        }
        ms.Position = 0;
        return System.Text.Json.JsonSerializer.Deserialize<T>(ms)!;
    }
}
