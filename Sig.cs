using System;
using System.Collections.Generic;
using System.IO;

// Based on https://github.com/marin-m/SongRec/blob/0.1.0/python-version/fingerprinting/signature_format.py

static class Sig {
    public const int
        FREQ_0 = 250,
        FREQ_1 = 520,
        FREQ_2 = 1450,
        FREQ_3 = 3500,
        FREQ_4 = 5500;

    public static byte[] Write(int sampleRate, int sampleCount, IEnumerable<LandmarkInfo> landmarks) {
        using(var mem = new MemoryStream())
        using(var writer = new BinaryWriter(mem)) {
            writer.Write(0xCAFE2580);
            writer.Write(-1);
            writer.Write(-1);
            writer.Write(0);
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

            var bandData = GetBandData(landmarks);
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
    public static byte[] Write2(int _, int sampleCount, IEnumerable<LandmarkInfo> landmarks) {
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
            writer.Write(sampleCount);
            writer.Write(0);
            writer.Write(031100000);
            writer.Write(0x0f);
            writer.Write(0x42700000);

            var bandData = GetBandData(landmarks);
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

    static byte[][] GetBandData(IEnumerable<LandmarkInfo> landmarks) {
        return new[] {
            GetBandData(landmarks, FREQ_0, FREQ_1),
            GetBandData(landmarks, FREQ_1, FREQ_2),
            GetBandData(landmarks, FREQ_2, FREQ_3),
            GetBandData(landmarks, FREQ_3, FREQ_4)
        };
    }

    static byte[] GetBandData(IEnumerable<LandmarkInfo> landmarks, double minFreq, double maxFreq) {
        using(var mem = new MemoryStream())
        using(var writer = new BinaryWriter(mem)) {
            var stripeIndex = 0;

            foreach(var p in landmarks) {
                if(p.Freq < minFreq || p.Freq >= maxFreq)
                    continue;

                if(p.StripeIndex - stripeIndex >= 100) {
                    stripeIndex = p.StripeIndex;
                    writer.Write((byte)255);
                    writer.Write(stripeIndex);
                }

                if(p.StripeIndex < stripeIndex)
                    throw new InvalidOperationException();

                writer.Write(Convert.ToByte(p.StripeIndex - stripeIndex));
                writer.Write(p.NormalizedMagnitude);
                writer.Write(p.NormalizedBin);

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

}
