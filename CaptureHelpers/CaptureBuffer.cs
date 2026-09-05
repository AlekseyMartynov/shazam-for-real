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

    int RemainingTrimBytes;

    int RemainingBytes {
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
        RemainingTrimBytes = TimeToBlockAlignedBytes(waveFormat, maxTrim);
        RemainingBytes = WaveBuffer.BufferLength;
        SampleProvider = WaveBuffer.ToSampleProvider();
    }

    static int TimeToBlockAlignedBytes(WaveFormat waveFormat, TimeSpan time) {
        var byteCount = (int)(time.TotalSeconds * waveFormat.AverageBytesPerSecond);
        return byteCount - (byteCount % waveFormat.BlockAlign);
    }

    public async Task ConsumeStreamAsync(Stream stream) {
        using var memOwner = MemoryPool<byte>.Shared.Rent(WaveBuffer.WaveFormat.SampleRate / 2);
        var mem = memOwner.Memory;
        try {
            while(RemainingBytes > 0) {
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
        if(bytes.IsEmpty || RemainingBytes < 1) {
            return;
        }
        if(PendingByte > -1) {
            AddAligned([(byte)PendingByte, bytes[0]]);
            bytes = bytes[1..];
            PendingByte = -1;
            if(bytes.IsEmpty) {
                return;
            }
        }
        if(int.IsEvenInteger(bytes.Length)) {
            AddAligned(bytes);
        } else {
            AddAligned(bytes[..^1]);
            PendingByte = bytes[^1];
        }
    }

    void AddAligned(ReadOnlySpan<byte> bytes) {
        bytes = Trim(bytes);
        if(RemainingBytes < bytes.Length) {
            bytes = bytes[..RemainingBytes];
        }
        if(bytes.Length > 0) {
            WaveBuffer.AddSamples(bytes);
            RemainingBytes -= bytes.Length;
        }
    }

    ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> bytes) {
        // Rationale: mic recording starts with ~350ms of digital silence
        // which is better to exclude from signature
        if(RemainingTrimBytes < 1) {
            return bytes;
        }
        var nonSilentPcmIndex = MemoryMarshal.Cast<byte, short>(bytes).IndexOfAnyExceptInRange((short)-1, (short)1);
        var trimCount = Math.Min(RemainingTrimBytes, nonSilentPcmIndex < 0 ? bytes.Length : 2 * nonSilentPcmIndex);
        if(nonSilentPcmIndex < 0) {
            RemainingTrimBytes -= trimCount;
        } else {
            RemainingTrimBytes = 0;
        }
        return bytes[trimCount..];
    }

    public void Stop() {
        RemainingBytes = 0;
    }
}
