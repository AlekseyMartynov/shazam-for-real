using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Synthback {
    static readonly float[] Envelope = Array.ConvertAll(Window.Hann(2 * Analysis.CHUNK_SIZE), Convert.ToSingle);

    readonly Analysis Analysis;
    readonly LandmarkFinder Finder;

    public Synthback(Analysis analysis, LandmarkFinder finder) {
        Analysis = analysis;
        Finder = finder;
    }

    public void Synth(string filename) {
        var landmarks = Finder.EnumerateAllLandmarks().ToArray();

        var stripeCount = 1 + landmarks.Max(l => l.StripeIndex);
        var wave = new float[stripeCount * Analysis.CHUNK_SIZE];

        foreach(var l in landmarks) {
            var startSample = Analysis.CHUNK_SIZE * (l.StripeIndex - 1);
            var endSample = startSample + 2 * Analysis.CHUNK_SIZE;

            for(var t = startSample; t < endSample; t++)
                wave[t] += MathF.Sin(2 * MathF.PI * Analysis.BinToFreq(l.InterpolatedBin) * t / Analysis.SAMPLE_RATE)
                    * MathF.Exp(l.InterpolatedLogMagnitude / UInt16.MaxValue)
                    * Envelope[t - startSample];
        }

        var maxSample = wave.Max(s => Math.Abs(s));

        var bytes = new List<byte>(2 * wave.Length);
        foreach(var s in wave)
            bytes.AddRange(BitConverter.GetBytes(Convert.ToInt16(Int16.MaxValue * s / maxSample)));

        File.WriteAllBytes(filename, bytes.ToArray());
    }

}
