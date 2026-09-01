using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace Project;

[SupportedOSPlatform("windows")]
class WasapiCaptureHelper : ICaptureHelper {
    readonly WasapiRecorder Recorder;
    readonly CaptureBuffer CaptureBuffer;

    public WasapiCaptureHelper() {
        var builder = new WasapiRecorderBuilder().WithFormat(ICaptureHelper.WAVE_FORMAT);
        if(WasapiLoopbackHelper.Loopback) {
            builder = builder.WithLoopbackCapture();
        }
        Recorder = builder.Build();
        CaptureBuffer = new();
    }

    public void Dispose() {
        CaptureBuffer.Stop();
        Recorder.Dispose();
    }


    public bool Live => true;
    public ISampleProvider SampleProvider => CaptureBuffer.SampleProvider;
    public Exception Exception { get; private set; }

    public void Start() {
        Recorder.DataAvailable += (buffer, _, _, _) => {
            CaptureBuffer.AddRange(buffer);
        };

        Recorder.RecordingStopped += (s, e) => {
            Exception = e.Exception;
        };

        Recorder.StartRecording();
    }
}
