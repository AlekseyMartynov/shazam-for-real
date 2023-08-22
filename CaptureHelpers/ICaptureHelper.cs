using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

interface ICaptureHelper : IDisposable {
    bool Live { get; }
    ISampleProvider SampleProvider { get; }
    void Start();
}
