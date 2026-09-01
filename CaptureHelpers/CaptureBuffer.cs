using NAudio.Wave;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace Project;

class CaptureBuffer {
    // Max RetryMs
    static readonly TimeSpan MaxDuration = TimeSpan.FromSeconds(12);

    public readonly ISampleProvider SampleProvider;

    readonly BufferedWaveProvider WaveBuffer;

    int RemainingCount {
        get;
        set {
            if(field != value) {
                Debug.Assert(value >= 0);
                Debug.Assert(field == 0 && value > 0 || value <= field);
                field = value;
                if(field < 1) {
                    WaveBuffer.ReadFully = true;
                }
            }
        }
    }

    public CaptureBuffer() {
        WaveBuffer = new(ICaptureHelper.WAVE_FORMAT, MaxDuration) {
            ReadFully = false
        };
        RemainingCount = WaveBuffer.BufferLength;
        SampleProvider = WaveBuffer.ToSampleProvider();
    }

    public async Task ConsumeStreamAsync(Stream stream) {
        using var memOwner = MemoryPool<byte>.Shared.Rent(Analysis.SAMPLE_RATE / 2);
        var mem = memOwner.Memory;
        try {
            while(RemainingCount > 0) {
                var readLen = await stream.ReadAsync(mem);
                if(readLen == 0) {
                    break; // end of stream
                }
                AddRange(mem[..readLen].Span);
            }
        } catch(Exception x) {
            if(x is not ObjectDisposedException) {
                throw;
            }
        } finally {
            Stop();
        }
    }

    public void AddRange(ReadOnlySpan<byte> bytes) {
        if(RemainingCount < bytes.Length) {
            bytes = bytes[..RemainingCount];
        }
        if(bytes.Length > 0) {
            WaveBuffer.AddSamples(bytes);
            RemainingCount -= bytes.Length;
        }
    }

    public void Stop() {
        RemainingCount = 0;
    }
}
