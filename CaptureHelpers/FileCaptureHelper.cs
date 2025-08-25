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
    public TimeSpan TotalTime => WaveStream != null ? WaveStream.TotalTime : TimeSpan.Zero;

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

    public void SeekTo(TimeSpan time) {
        if(WaveStream is not WaveFileReader) {
            // TODO not sure about MP3 seek accuracy
            throw new NotSupportedException();
        }
        WaveStream.CurrentTime = time;
    }

    WaveStream CreateWaveStream() {
        var ext = Path.GetExtension(FilePath).ToLower();
        return ext switch {
            ".mp3" => new Mp3FileReaderBase(FilePath, fmt => new Mp3FrameDecompressor(fmt)),
            ".wav" => new WaveFileReader(FilePath),
            _ => throw new NotSupportedException($"Extension '{ext}' not supported"),
        };
    }

}
