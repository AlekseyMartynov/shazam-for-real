using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Project.Test;

public class CaptureBufferTests {
    const int TestSampleRate = 123;
    const short PositiveHalfShort = short.MaxValue / 2;
    const float ReadSentinel = 0xdeadbeef;

    [Fact]
    public void ZeroPadAfterMaxDuration() {
        var captureBuf = CreateCaptureBuffer(1);

        var firstBatchLen = TestSampleRate - 1;
        Add16BitSamples(captureBuf, PositiveHalfShort, firstBatchLen);
        MustReadExactly(captureBuf, firstBatchLen, 0.5f);

        Add16BitSamples(captureBuf, PositiveHalfShort, TestSampleRate - firstBatchLen + 1);
        MustReadZeroPadded(captureBuf, 1, 0.5f);
    }

    [Fact]
    public void Stop() {
        var captureBuf = CreateCaptureBuffer(123);

        Add16BitSamples(captureBuf, PositiveHalfShort, 1);
        captureBuf.Stop();
        Add16BitSamples(captureBuf, PositiveHalfShort, 1);

        MustReadZeroPadded(captureBuf, 1, 0.5f);
    }

    [Fact]
    public async Task ConsumeStreamAsync() {
        using var sourceStream = new MemoryStream();
        Add16BitSamples(sourceStream, PositiveHalfShort, 3 * TestSampleRate);
        sourceStream.Position = 0;

        var captureBuf = CreateCaptureBuffer(2);
        await captureBuf.ConsumeStreamAsync(sourceStream);

        Assert.True(sourceStream.Position < sourceStream.Length);

        MustReadZeroPadded(captureBuf, 2 * TestSampleRate, 0.5f);
    }

    [Fact]
    public async Task ConsumeStreamAsync_Short() {
        using var sourceStream = new MemoryStream();
        Add16BitSamples(sourceStream, PositiveHalfShort, TestSampleRate);
        sourceStream.Position = 0;

        var captureBuf = CreateCaptureBuffer(2);
        await captureBuf.ConsumeStreamAsync(sourceStream);

        Assert.Equal(sourceStream.Length, sourceStream.Position);

        MustReadZeroPadded(captureBuf, TestSampleRate, 0.5f);
    }

    [Fact]
    public async Task ConsumeStreamAsync_Disposed() {
        var sourceStream = new MemoryStream();
        sourceStream.Dispose();

        var captureBuf = CreateCaptureBuffer(123);

        var exception = await Record.ExceptionAsync(async delegate {
            await captureBuf.ConsumeStreamAsync(sourceStream);
        });

        Assert.Null(exception);
        MustReadZeroPadded(captureBuf, 0);
    }

    [Fact]
    public void Unaligned() {
        var captureBuf = CreateCaptureBuffer(1);
        var bytes = MemoryMarshal.AsBytes([
            PositiveHalfShort,
            PositiveHalfShort,
            PositiveHalfShort,
            PositiveHalfShort,
            PositiveHalfShort,
            PositiveHalfShort,
        ]);

        // !pending, !even
        captureBuf.AddRange(bytes[..3]);
        MustReadExactly(captureBuf, 1, 0.5f);

        // pending, even
        captureBuf.AddRange(bytes[3..7]);
        MustReadExactly(captureBuf, 2, 0.5f);

        // pending, !even
        captureBuf.AddRange(bytes[7..10]);
        MustReadExactly(captureBuf, 2, 0.5f);

        // !pending, even
        captureBuf.AddRange(bytes[10..12]);
        MustReadExactly(captureBuf, 1, 0.5f);
    }

    static CaptureBuffer CreateCaptureBuffer(int maxDurationSeconds) {
        return new CaptureBuffer(
            new(TestSampleRate, 1),
            TimeSpan.FromSeconds(maxDurationSeconds)
        );
    }

    static void Add16BitSamples(CaptureBuffer captureBuf, short sampleValue, int sampleCount) {
        var samples = new short[sampleCount];
        Array.Fill(samples, sampleValue);
        captureBuf.AddRange(MemoryMarshal.Cast<short, byte>(samples));
    }

    static void Add16BitSamples(Stream stream, short sampleValue, int sampleCount) {
        using var writer = new BinaryWriter(stream, Encoding.Default, true);
        for(var i = 0; i < sampleCount; i++) {
            writer.Write(sampleValue);
        }
    }

    static void MustReadExactly(CaptureBuffer captureBuf, int expectedSampleCount, float expectedLastSample = default) {
        MustReadCore(false, captureBuf, expectedSampleCount, expectedLastSample);
    }

    static void MustReadZeroPadded(CaptureBuffer captureBuf, int expectedSampleCount, float expectedLastSample = default) {
        MustReadCore(true, captureBuf, expectedSampleCount, expectedLastSample);
    }

    static void MustReadCore(bool zeroPadded, CaptureBuffer captureBuf, int expectedSampleCount, float expectedLastSample) {
        var readBuf = new float[expectedSampleCount + 1];
        Array.Fill(readBuf, ReadSentinel);
        Assert.Equal(
            zeroPadded ? readBuf.Length : expectedSampleCount,
            captureBuf.SampleProvider.Read(readBuf)
        );
        if(expectedSampleCount > 0) {
            Assert.Equal(expectedLastSample, readBuf[expectedSampleCount - 1], precision: 3);
        }
        Assert.Equal(
            zeroPadded ? 0 : ReadSentinel,
            readBuf[expectedSampleCount]
        );

    }
}
