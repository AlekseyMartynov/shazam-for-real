using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

            var bandData = new[] {
                SerializeLandmarks(landmarks, FREQ_0, FREQ_1),
                SerializeLandmarks(landmarks, FREQ_1, FREQ_2),
                SerializeLandmarks(landmarks, FREQ_2, FREQ_3),
                SerializeLandmarks(landmarks, FREQ_3, FREQ_4)
            };

            var bandPads = bandData.Select(i => CalcPadLen(i.Length)).ToArray();

            var contentLen = 8 + 8 * bandData.Length
                + bandData.Sum(i => i.Length)
                + bandPads.Sum();

            writer.Write(0xCAFE2580);
            writer.Write(-1);
            writer.Write(contentLen);
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
            writer.Write(contentLen);

            var padding = new byte[] { 0, 0, 0 };

            for(var i = 0; i < 4; i++) {
                writer.Write(0x60030040 + i);
                writer.Write(bandData[i].Length);
                writer.Write(bandData[i]);
                writer.Write(padding, 0, bandPads[i]);
            }

            var crc = Force.Crc32.Crc32Algorithm.Compute(mem.GetBuffer(), 8, (int)mem.Length - 8);
            mem.Position = 4;
            writer.Write(crc);

            return mem.ToArray();
        }
    }

    static byte[] SerializeLandmarks(IEnumerable<LandmarkInfo> landmarks, double minFreq, double maxFreq) {
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
