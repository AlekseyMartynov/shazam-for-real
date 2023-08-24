using NAudio.Wave.SampleProviders;
using NAudio.Wave;
using NLayer.NAudioSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;

class FileCaptureHelper : ICaptureHelper {
    readonly string FilePath;
    readonly TimeSpan StartTime;

    WaveStream WaveStream;

    public FileCaptureHelper(string filePath, TimeSpan startTime) {
        FilePath = filePath;
        StartTime = startTime;
    }

    public void Dispose() {
        WaveStream?.Dispose();
    }

    public TimeSpan CurrentTime => WaveStream != null ? WaveStream.CurrentTime : TimeSpan.Zero;

    public bool Live => false;
    public ISampleProvider SampleProvider { get; private set; }

    public void Start() {
        WaveStream = CreateWaveStream();
        WaveStream.CurrentTime = StartTime;

        SampleProvider = WaveStream.ToSampleProvider();

        if(SampleProvider.WaveFormat.Channels > 1)
            SampleProvider = new StereoToMonoSampleProvider(SampleProvider);

        if(SampleProvider.WaveFormat.SampleRate != Analysis.SAMPLE_RATE)
            SampleProvider = new WdlResamplingSampleProvider(SampleProvider, Analysis.SAMPLE_RATE);
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
