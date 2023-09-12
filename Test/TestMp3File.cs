#if DEBUG
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Project.Test;

class TestMp3File : IDisposable {
    readonly WaveStream Mp3Stream;

    public TestMp3File(string fileName) {
        Mp3Stream = new Mp3FileReaderBase(
            GetFullPath(fileName),
            fmt => new Mp3FrameDecompressor(fmt)
        );

        SampleProvider = Mp3Stream.ToSampleProvider();

        if(SampleProvider.WaveFormat.Channels != 1)
            throw new NotSupportedException();

        // NOTE NLayer has issues with 16000 sample rate (MPEG-2 Layer 3)
        // https://github.com/naudio/NLayer/issues/19
        if(SampleProvider.WaveFormat.SampleRate != Analysis.SAMPLE_RATE)
            SampleProvider = new WdlResamplingSampleProvider(SampleProvider, Analysis.SAMPLE_RATE);
    }

    public ISampleProvider SampleProvider { get; private set; }

    public void SaveRaw(string fileName) {
        using var rawFile = File.OpenWrite(GetFullPath(fileName));

        var wave = new SampleToWaveProvider16(SampleProvider);
        var bufLen = 4096;
        var buf = new byte[bufLen];

        while(true) {
            var readLen = wave.Read(buf, 0, bufLen);
            rawFile.Write(buf, 0, readLen);
            if(readLen < bufLen)
                break;
        }
    }

    public void Dispose() {
        Mp3Stream.Dispose();
    }

    static string GetFullPath(string fileName) {
        return Path.Combine(TestHelper.DATA_DIR, fileName);
    }
}
#endif
