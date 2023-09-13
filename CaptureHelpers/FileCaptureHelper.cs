using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using NLayer.NAudioSupport;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace Project;

class FileCaptureHelper : ICaptureHelper {
    readonly string FilePath;
    readonly TimeSpan StartTime;

    WaveStream WaveStream;

    public FileCaptureHelper(string filePath, TimeSpan startTime = default) {
        FilePath = filePath;
        StartTime = startTime;
    }

    public void Dispose() {
        WaveStream?.Dispose();
    }

    public TimeSpan CurrentTime => WaveStream != null ? WaveStream.CurrentTime : TimeSpan.Zero;

    public bool Live => false;
    public ISampleProvider SampleProvider { get; private set; }
    public Exception Exception => null;

    public void Start() {
        WaveStream = CreateWaveStream();
        WaveStream.CurrentTime = StartTime;

        SampleProvider = WaveStream.ToSampleProvider();

        if(SampleProvider.WaveFormat.Channels > 1)
            SampleProvider = new StereoToMonoSampleProvider(SampleProvider);

        if(SampleProvider.WaveFormat.SampleRate != Analysis.SAMPLE_RATE)
            SampleProvider = new WdlResamplingSampleProvider(SampleProvider, Analysis.SAMPLE_RATE);
    }

    public void SkipTo(TimeSpan time) {
        var len = Analysis.SAMPLE_RATE / 2;
        var buf = ArrayPool<float>.Shared.Rent(len);

        try {
            while(WaveStream.CurrentTime < time) {
                if(SampleProvider.Read(buf, 0, len) < len)
                    break;
            }
        } finally {
            ArrayPool<float>.Shared.Return(buf);
        }
    }

    WaveStream CreateWaveStream() {
        var ext = Path.GetExtension(FilePath).ToLower();
        return ext switch {
            ".mp3" => new Mp3FileReaderBase(FilePath, CreateMp3FrameDecompressor),
            ".wav" => new WaveFileReader(FilePath),
            _ => throw new NotSupportedException($"Extension '{ext}' not supported"),
        };
    }

    static IMp3FrameDecompressor CreateMp3FrameDecompressor(WaveFormat mp3Format) {
        var sr = mp3Format.SampleRate;

        if(sr > 12000 && sr < 32000)
            throw new Exception($"{sr} Hz mp3 decoder is broken, https://github.com/naudio/NLayer/issues/19");

        return new Mp3FrameDecompressor(mp3Format);
    }
}
