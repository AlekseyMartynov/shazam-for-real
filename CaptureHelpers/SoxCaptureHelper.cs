using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Project;

class SoxCaptureHelper : ICaptureHelper {
    Process Sox;
    WaveStream WaveStream;

    public void Dispose() {
        WaveStream?.Dispose();

        if(Sox != null) {
            Sox.Kill();
            Sox.Dispose();
        }
    }

    public bool Live => true;
    public ISampleProvider SampleProvider { get; private set; }
    public Exception Exception { get; private set; }


    public void Start() {
        var fmt = ICaptureHelper.WAVE_FORMAT;

        var pendingSox = new Process {
            StartInfo = new ProcessStartInfo {
                FileName = "sox",
                Arguments = $"-q -d -r {fmt.SampleRate} -c {fmt.Channels} -b {fmt.BitsPerSample} -e signed-integer -t raw -",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            },
            EnableRaisingEvents = true
        };

        pendingSox.Exited += Sox_Exited;
        pendingSox.ErrorDataReceived += Sox_ErrorDataReceived;

        try {
            pendingSox.Start();
            pendingSox.BeginErrorReadLine();
        } catch {
            throw new Exception("Failed to start sox (https://en.wikipedia.org/wiki/SoX)");
        }

        if(!pendingSox.HasExited) {
            Sox = pendingSox;
            WaveStream = new RawSourceWaveStream(Sox.StandardOutput.BaseStream, fmt);
            SampleProvider = WaveStream.ToSampleProvider();
        }
    }

    void Sox_Exited(object s, EventArgs e) {
        SampleProvider = EternalSilence.AppendTo(SampleProvider);

        var proc = (Process)s;

        var code = proc.ExitCode;
        if(code != 0)
            Exception = new Exception("sox exited with code " + code);
    }

    void Sox_ErrorDataReceived(object s, DataReceivedEventArgs e) {
        var text = e.Data;

        if(String.IsNullOrEmpty(text))
            return;

        if(text.Contains("can't encode 0-bit"))
            return;

        Console.Error.WriteLine(text);
    }
}
