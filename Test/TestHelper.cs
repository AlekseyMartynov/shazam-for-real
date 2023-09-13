#if DEBUG
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Project.Test;

static class TestHelper {
    public static readonly string DATA_DIR;

    static TestHelper() {
        DATA_DIR = Path.Combine(
            Path.GetDirectoryName(typeof(TestHelper).Assembly.Location),
            "../../../TestData"
        );
    }

    public static void SaveRaw(ISampleProvider sampleProvider, string path) {
        using var rawFile = File.OpenWrite(path);

        var wave = new SampleToWaveProvider16(sampleProvider);
        var bufLen = 4096;
        var buf = new byte[bufLen];

        while(true) {
            var readLen = wave.Read(buf, 0, bufLen);
            rawFile.Write(buf, 0, readLen);
            if(readLen < bufLen)
                break;
        }
    }

}
#endif
