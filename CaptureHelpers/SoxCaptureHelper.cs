using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

class SoxCaptureHelper : ICaptureHelper {
    readonly CaptureBuffer CaptureBuffer = new();

    Process Sox;

    public void Dispose() {
        CaptureBuffer.Stop();

        if(Sox != null) {
            Sox.Kill();
            Sox.Dispose();
        }
    }

    public bool Live => true;
    public ISampleProvider SampleProvider => CaptureBuffer.SampleProvider;
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

        if(OperatingSystem.IsWindows() && Environment.GetEnvironmentVariable("AUDIODRIVER") == null) {
            pendingSox.StartInfo.Environment["AUDIODRIVER"] = "waveaudio";
        }

        pendingSox.Exited += Sox_Exited;
        pendingSox.ErrorDataReceived += Sox_ErrorDataReceived;

        try {
            pendingSox.Start();
            pendingSox.BeginErrorReadLine();
        } catch {
            pendingSox.Dispose();
            throw new Exception("Failed to start sox (https://en.wikipedia.org/wiki/SoX)");
        }

        Task.Run(async delegate {
            try {
                await CaptureBuffer.ConsumeStreamAsync(pendingSox.StandardOutput.BaseStream);
            } catch(Exception x) {
                Exception = x;
            }
        });

        Sox = pendingSox;
    }

    void Sox_Exited(object s, EventArgs e) {
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
