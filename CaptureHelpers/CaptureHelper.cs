using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Project;

abstract class CaptureHelper : IDisposable {
    public static readonly WaveFormat WAVE_FORMAT = new(Analysis.SAMPLE_RATE, 16, 1);

    public ISampleProvider SampleProvider { get; protected init; }
    public Exception Exception { get; protected set; }

    public abstract bool Live { get; }

    public abstract void Dispose();
}
