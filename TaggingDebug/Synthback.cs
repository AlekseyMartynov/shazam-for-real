using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Project;

class Synthback {
    static readonly float[] Envelope = Array.ConvertAll(Window.Hann(2 * Analysis.CHUNK_SIZE), Convert.ToSingle);

    readonly Analysis Analysis;
    readonly PeakFinder Finder;

    public Synthback(Analysis analysis, PeakFinder finder) {
        Analysis = analysis;
        Finder = finder;
    }

    public void Synth(string filename) {
        var peaks = Finder.EnumerateAllPeaks().ToArray();

        var stripeCount = 1 + peaks.Max(l => l.StripeIndex);
        var wave = new float[stripeCount * Analysis.CHUNK_SIZE];

        foreach(var p in peaks) {
            var startSample = Analysis.CHUNK_SIZE * (p.StripeIndex - 1);
            var endSample = startSample + 2 * Analysis.CHUNK_SIZE;

            for(var t = startSample; t < endSample; t++)
                wave[t] += MathF.Sin(2 * MathF.PI * Analysis.BinToFreq(p.InterpolatedBin) * t / Analysis.SAMPLE_RATE)
                    * MathF.Exp(p.InterpolatedLogMagnitude / UInt16.MaxValue)
                    * Envelope[t - startSample];
        }

        var maxSample = wave.Max(s => Math.Abs(s));

        var bytes = new List<byte>(2 * wave.Length);
        foreach(var s in wave)
            bytes.AddRange(BitConverter.GetBytes(Convert.ToInt16(Int16.MaxValue * s / maxSample)));

        File.WriteAllBytes(filename, bytes.ToArray());
    }

}
