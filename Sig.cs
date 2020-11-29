﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Based on https://github.com/marin-m/SongRec/blob/0.1.0/python-version/fingerprinting/signature_format.py

static class Sig {

    public static byte[] Write(int sampleRate, int sampleCount, LandmarkFinder finder) {
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
    public static byte[] Write2(int sampleRate, int sampleCount, LandmarkFinder finder) {
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

    static byte[][] GetBandData(LandmarkFinder finder) {
        return finder.EnumerateBandedLandmarks().Select(GetBandData).ToArray();
    }

    static byte[] GetBandData(IEnumerable<LandmarkInfo> landmarks) {
        using(var mem = new MemoryStream())
        using(var writer = new BinaryWriter(mem)) {
            var stripeIndex = 0;

            foreach(var p in landmarks) {

                if(p.StripeIndex - stripeIndex >= 100) {
                    stripeIndex = p.StripeIndex;
                    writer.Write((byte)255);
                    writer.Write(stripeIndex);
                }

                if(p.StripeIndex < stripeIndex)
                    throw new InvalidOperationException();

                writer.Write(Convert.ToByte(p.StripeIndex - stripeIndex));
                writer.Write(Convert.ToUInt16(p.InterpolatedLogMagnitude));
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

}
