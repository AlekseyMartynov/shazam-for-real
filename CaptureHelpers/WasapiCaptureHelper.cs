using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;

namespace Project;

[SupportedOSPlatform("windows")]
sealed class WasapiCaptureHelper : CaptureHelper {
    readonly WasapiRecorder Recorder;
    readonly CaptureBuffer CaptureBuffer;

    public WasapiCaptureHelper() {
        var builder = new WasapiRecorderBuilder().WithFormat(WAVE_FORMAT);
        if(WasapiLoopbackHelper.Loopback) {
            builder = builder.WithLoopbackCapture();
        }
        Recorder = builder.Build();
        CaptureBuffer = new();
        SampleProvider = CaptureBuffer.SampleProvider;
        Start();
    }

    public override void Dispose() {
        CaptureBuffer.Stop();
        Recorder.Dispose();
    }

    public override bool Live => true;

    void Start() {
        Recorder.DataAvailable += (buffer, _, _, _) => {
            CaptureBuffer.AddRange(buffer);
        };

        Recorder.RecordingStopped += (s, e) => {
            Exception = e.Exception;
        };

        Recorder.StartRecording();
    }
}
