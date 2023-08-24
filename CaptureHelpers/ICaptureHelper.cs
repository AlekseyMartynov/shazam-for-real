using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

interface ICaptureHelper : IDisposable {
    static readonly WaveFormat WAVE_FORMAT = new(Analysis.SAMPLE_RATE, 16, 1);

    bool Live { get; }
    ISampleProvider SampleProvider { get; }
    Exception Exception { get; }

    void Start();
}
