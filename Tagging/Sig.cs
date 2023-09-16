using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Project;

// Based on https://github.com/marin-m/SongRec/blob/0.1.0/python-version/fingerprinting/signature_format.py

static class Sig {

    public static byte[] Write(int sampleRate, int sampleCount, PeakFinder finder) {
        using(var mem = new MemoryStream())
        using(var writer = new BinaryWriter(mem)) {
            writer.Write(0xCAFE2580); // Dialling "2580" on your phone and holding it up to the music, https://www.shazam.com/company
            writer.Write(-1);
            writer.Write(-1);
            writer.Write(0x94119C00);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(GetSampleRateCode(sampleRate) << 27);
            writer.Write(0);
            writer.Write(0);
            writer.Write(sampleCount);
            writer.Write(0x007C0000);
            writer.Write(0x40000000);
            writer.Write(-1);

            var bandData = GetBandData(finder);
            for(var i = 0; i < bandData.Length; i++) {
                writer.Write(0x60030040 + i);
                writer.Write(bandData[i].Length);
                writer.Write(bandData[i]);
            }

            var totalLen = (int)mem.Length;
            var contentLen = totalLen - 48;

            foreach(var i in new[] { 2, 13 }) {
                mem.Position = i * 4;
                writer.Write(contentLen);
            }

            var crc = Force.Crc32.Crc32Algorithm.Compute(mem.GetBuffer(), 8, totalLen - 8);
            mem.Position = 4;
            writer.Write(crc);

            return mem.ToArray();
        }
    }

    // Alternative (legacy?) format used in ShazamCore10.dll
    // Works with any sample rate
    public static byte[] Write2(int sampleRate, int sampleCount, PeakFinder finder) {
        using(var mem = new MemoryStream())
        using(var writer = new BinaryWriter(mem)) {
            writer.Write(-1);
            writer.Write(0x789ABC05);
            writer.Write(0xFFFFFFFF);
            writer.Write(0x30000002);
            writer.Write(0x10);
            writer.Write(-1);
            writer.Write(-1);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0x40000000);
            writer.Write(-1);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0x50000001);
            writer.Write(0x18);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0);
            writer.Write(0xDEADBEEF);
            writer.Write(Convert.ToInt32(sampleCount * 8000L / sampleRate));
            writer.Write(0);
            writer.Write(0x31100000);
            writer.Write(0x0f);
            writer.Write(0x42700000);

            var bandData = GetBandData(finder);
            for(var i = 0; i < bandData.Length; i++) {
                writer.Write(0);
                writer.Write(0x60030040 + i);
                writer.Write(bandData[i].Length);
                writer.Write(0);
                writer.Write(0);
                writer.Write(0);
                writer.Write(bandData[i]);
            }

            var totalLen = (int)mem.Length;
            var contentLen = totalLen - 32;

            foreach(var i in new[] { 0, 5, 10 }) {
                mem.Position = i * 4;
                writer.Write(contentLen);
            }

            mem.Position = 6 * 4;
            writer.Write(contentLen ^ 0x789ABC13);

            var buffer = mem.GetBuffer();
            var checksum = (uint)0;
            for(var i = 0; i < totalLen / 4; i++)
                checksum += BitConverter.ToUInt32(buffer, 4 * i);

            mem.Position = 7 * 4;
            writer.Write(checksum);

            return mem.ToArray();
        }
    }

    public static void Read(byte[] data, out int sampleRate, out int sampleCount, out IReadOnlyList<IReadOnlyList<PeakInfo>> bands) {
        using var mem = new MemoryStream(data);
        using var reader = new BinaryReader(mem);

        var legacyFormat = false;

        if(reader.ReadUInt32() != 0xcafe2580) {
            if(reader.ReadUInt32() == 0x789abc05) {
                legacyFormat = true;
            } else {
                throw new NotSupportedException();
            }
        }

        if(legacyFormat) {
            sampleRate = 16000;

            mem.Position = 21 * 4;
            sampleCount = 2 * reader.ReadInt32();

            mem.Position = 26 * 4;
        } else {
            mem.Position = 7 * 4;
            sampleRate = SampleRateFromCode(reader.ReadInt32() >> 27);

            mem.Position = 10 * 4;
            sampleCount = reader.ReadInt32();

            mem.Position = 14 * 4;
        }

        var writableBands = new List<PeakInfo>[4];
        for(var i = 0; i < 4; i++)
            writableBands[i] = new();

        while(mem.Position < mem.Length) {
            if(legacyFormat) {
                mem.Position += 4;
            }

            var fat = false;
            var bandIndex = 0;
            var header = reader.ReadInt32();

            if(header == 0x60023e80) {
                fat = true;
            } else {
                bandIndex = header - 0x60030040;
                if(bandIndex < 0 || bandIndex > 3)
                    throw new InvalidOperationException();
            }

            var len = reader.ReadInt32();

            if(legacyFormat) {
                mem.Position += 3 * 4;
            }

            var end = mem.Position + len;
            var stripe = 0;

            while(mem.Position < end) {
                if(end - mem.Position >= 5) {
                    if(fat) {
                        stripe = reader.ReadInt32();
                    } else {
                        var x = reader.ReadByte();
                        if(x == 255) {
                            stripe = reader.ReadInt32();
                            x = reader.ReadByte();
                        }
                        stripe += x;
                    }

                    var word1 = reader.ReadUInt16();
                    var word2 = reader.ReadUInt16();

                    if(fat) {
                        mem.Position += 4;
                        (word1, word2) = (word2, word1);
                    }

                    var magn = word1;
                    var bin = word2 / 64;

                    if(bin == 0 || magn == 0)
                        throw new InvalidOperationException();

                    writableBands[bandIndex].Add(new PeakInfo(stripe, bin, magn));
                } else {
                    if(reader.ReadByte() != 0)
                        throw new InvalidOperationException();
                }
            }

            var pad = CalcPadLen((int)mem.Position);

            while(pad > 0) {
                reader.ReadByte();
                pad--;
            }
        }

        bands = writableBands;
    }

    static byte[][] GetBandData(PeakFinder finder) {
        finder.ApplyRateLimit();
        return finder.EnumerateBandedPeaks().Select(GetBandData).ToArray();
    }

    static byte[] GetBandData(IEnumerable<PeakInfo> peaks) {
        using(var mem = new MemoryStream())
        using(var writer = new BinaryWriter(mem)) {
            var stripeIndex = 0;

            foreach(var p in peaks) {

                if(p.StripeIndex - stripeIndex >= 100) {
                    stripeIndex = p.StripeIndex;
                    writer.Write((byte)255);
                    writer.Write(stripeIndex);
                }

                if(p.StripeIndex < stripeIndex)
                    throw new InvalidOperationException();

                writer.Write(Convert.ToByte(p.StripeIndex - stripeIndex));
                writer.Write(Convert.ToUInt16(p.LogMagnitude));
                writer.Write(Convert.ToUInt16(64 * p.InterpolatedBin));

                stripeIndex = p.StripeIndex;
            }

            while(mem.Length % 4 != 0)
                writer.Write((byte)0);

            return mem.ToArray();
        }
    }

    static int CalcPadLen(int dataLen) {
        var result = -dataLen % 4;
        if(result < 0)
            result += 4;
        return result;
    }

    static int GetSampleRateCode(int sampleRate) {
        switch(sampleRate) {
            case 8000: return 1;
            case 16000: return 3;
            case 32000: return 4;
        }
        throw new NotSupportedException();
    }

    static int SampleRateFromCode(int code) {
        code &= 0xf;
        return code switch {
            1 => 8000,
            3 => 16000,
            4 => 32000,
            _ => throw new NotSupportedException()
        };
    }

}
