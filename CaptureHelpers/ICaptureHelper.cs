using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

interface ICaptureHelper : IDisposable {
    ISampleProvider SampleProvider { get; }
    void Start();
}
