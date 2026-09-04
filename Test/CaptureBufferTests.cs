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

    readonly float[] ReadBuf = new float[10 * TestSampleRate];

    [Fact]
    public void ZeroPadAfterMaxDuration() {
        var captureBuf = CreateCaptureBuffer(1);
        var sp = captureBuf.SampleProvider;

        var firstBatchLen = TestSampleRate - 1;
        Add16BitSamples(captureBuf, PositiveHalfShort, firstBatchLen);
        Assert.Equal(firstBatchLen, sp.Read(ReadBuf));

        Add16BitSamples(captureBuf, PositiveHalfShort, TestSampleRate - firstBatchLen + 1);
        Assert.Equal(ReadBuf.Length, sp.Read(ReadBuf));

        Assert.Equal(0.5, ReadBuf[0], precision: 3);
        Assert.Equal(0, ReadBuf[1], precision: 3);
    }

    [Fact]
    public void Stop() {
        var captureBuf = CreateCaptureBuffer(123);

        Add16BitSamples(captureBuf, PositiveHalfShort, 1);
        captureBuf.Stop();
        Add16BitSamples(captureBuf, PositiveHalfShort, 1);

        Assert.Equal(ReadBuf.Length, captureBuf.SampleProvider.Read(ReadBuf));
        Assert.Equal(0.5, ReadBuf[0], precision: 3);
        Assert.Equal(0, ReadBuf[1], precision: 3);
    }

    [Fact]
    public async Task ConsumeStreamAsync() {
        using var sourceStream = new MemoryStream();
        Add16BitSamples(sourceStream, PositiveHalfShort, 3 * TestSampleRate);
        sourceStream.Position = 0;

        var captureBuf = CreateCaptureBuffer(2);
        await captureBuf.ConsumeStreamAsync(sourceStream);

        Assert.True(sourceStream.Position < sourceStream.Length);

        captureBuf.SampleProvider.Read(ReadBuf);
        Assert.Equal(0.5, ReadBuf[2 * TestSampleRate - 1], precision: 3);
        Assert.Equal(0, ReadBuf[2 * TestSampleRate], precision: 3);
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
        Assert.Equal(ReadBuf.Length, captureBuf.SampleProvider.Read(ReadBuf));
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
}
