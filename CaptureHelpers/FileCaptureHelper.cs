using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using NLayer.NAudioSupport;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Project;

sealed class FileCaptureHelper : CaptureHelper {
    readonly WaveStream WaveStream;

    public FileCaptureHelper(string filePath, TimeSpan startTime = default) {
        WaveStream = CreateWaveStream(filePath);
        WaveStream.CurrentTime = startTime;

        SampleProvider = CreateSampleProvider(WaveStream);
    }

    public override void Dispose() {
        WaveStream.Dispose();
    }

    public TimeSpan CurrentTime => WaveStream.CurrentTime;

    public override bool Live => false;

    public void SkipTo(TimeSpan time) {
        var len = Analysis.SAMPLE_RATE / 2;
        var buf = ArrayPool<float>.Shared.Rent(len);

        try {
            var bufSpan = buf.AsSpan(0, len);
            while(WaveStream.CurrentTime < time) {
                if(SampleProvider.Read(bufSpan) < len)
                    break;
            }
        } finally {
            ArrayPool<float>.Shared.Return(buf);
        }
    }

    static WaveStream CreateWaveStream(string filePath) {
        var ext = Path.GetExtension(filePath).ToLower();
        return ext switch {
            ".mp3" => new Mp3FileReaderBase(filePath, fmt => new Mp3FrameDecompressor(fmt)),
            ".wav" => new WaveFileReader(filePath),
            _ => throw new NotSupportedException($"Extension '{ext}' not supported"),
        };
    }

    static ISampleProvider CreateSampleProvider(WaveStream wave) {
        var result = wave.ToSampleProvider().ToMono();
        if(result.WaveFormat.SampleRate != Analysis.SAMPLE_RATE) {
            result = new WdlResamplingSampleProvider(result, Analysis.SAMPLE_RATE);
        }
        return result;
    }
}
