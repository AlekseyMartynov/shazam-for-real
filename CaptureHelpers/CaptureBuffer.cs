using NAudio.Wave;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace Project;

class CaptureBuffer {
    public readonly ISampleProvider SampleProvider;

    readonly BufferedWaveProvider WaveBuffer;

    int PendingByte = -1;

    int RemainingTrim;

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

    public CaptureBuffer()
        : this(CaptureHelper.WAVE_FORMAT, TimeSpan.FromSeconds(12), TimeSpan.FromSeconds(1)) {
        // 12 sec is max 'retryInMilliseconds' returned by Shazam API
    }

    public CaptureBuffer(WaveFormat waveFormat, TimeSpan maxDuration, TimeSpan maxTrim) {
        Debug.Assert(waveFormat.Encoding == WaveFormatEncoding.Pcm);
        Debug.Assert(waveFormat.BitsPerSample == 16);
        Debug.Assert(waveFormat.Channels == 1);
        Debug.Assert(waveFormat.BlockAlign == 2);
        WaveBuffer = new(waveFormat, maxDuration) {
            ReadFully = false
        };
        RemainingTrim = (int)maxTrim.TotalSeconds * waveFormat.AverageBytesPerSecond;
        RemainingCount = WaveBuffer.BufferLength;
        SampleProvider = WaveBuffer.ToSampleProvider();
    }

    public async Task ConsumeStreamAsync(Stream stream) {
        using var memOwner = MemoryPool<byte>.Shared.Rent(WaveBuffer.WaveFormat.SampleRate / 2);
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
        if(bytes.Length < 1 || RemainingCount < 1) {
            return;
        }
        var even = int.IsEvenInteger(bytes.Length);
        if(PendingByte < 0) {
            if(even) {
                AddAligned(bytes);
            } else {
                AddAligned(bytes[..^1]);
                PendingByte = bytes[^1];
            }
        } else {
            AddAligned([(byte)PendingByte, bytes[0]]);
            if(even) {
                AddAligned(bytes[1..^1]);
                PendingByte = bytes[^1];
            } else {
                AddAligned(bytes[1..]);
                PendingByte = -1;
            }
        }
    }

    void AddAligned(ReadOnlySpan<byte> bytes) {
        bytes = Trim(bytes);
        if(RemainingCount < bytes.Length) {
            bytes = bytes[..RemainingCount];
        }
        if(bytes.Length > 0) {
            WaveBuffer.AddSamples(bytes);
            RemainingCount -= bytes.Length;
        }
    }

    ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> bytes) {
        // Rationale: mic recording starts with ~350ms of silence
        // which is better to exclude from signature
        if(RemainingTrim < 1) {
            return bytes;
        }
        var nonSilentPcmIndex = MemoryMarshal.Cast<byte, short>(bytes).IndexOfAnyExceptInRange((short)-1, (short)1);
        var trimCount = Math.Min(RemainingTrim, nonSilentPcmIndex < 0 ? bytes.Length : 2 * nonSilentPcmIndex);
        if(nonSilentPcmIndex < 0) {
            RemainingTrim -= trimCount;
        } else {
            RemainingTrim = 0;
        }
        return bytes[trimCount..];
    }

    public void Stop() {
        RemainingCount = 0;
    }
}
