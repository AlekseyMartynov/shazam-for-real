using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Synthback {
    readonly Spectrogram Spectro;
    readonly LandmarkFinder LandmarkFinder;
    readonly int SampleRate, ChunkSize;
    readonly double[] Envelope;

    public Synthback(Spectrogram spectro, LandmarkFinder finder, int sampleRate, int chunkSize) {
        Spectro = spectro;
        LandmarkFinder = finder;
        SampleRate = sampleRate;
        ChunkSize = chunkSize;
        Envelope = Window.Hann(2 * ChunkSize);
    }

    public void Synth(string filename) {
        var locations = LandmarkFinder.EnumerateAllLocations().ToArray();

        var stripeCount = 1 + locations.Max(l => l.stripe);
        var wave = new double[stripeCount * ChunkSize];

        foreach(var (stripe, bin) in locations) {
            var startSample = ChunkSize * (stripe - 1);
            var endSample = startSample + 2 * ChunkSize;

            for(var t = startSample; t < endSample; t++)
                wave[t] += Math.Sin(2 * Math.PI * Spectro.BinToFreq(bin) * t / SampleRate)
                    * Math.Sqrt(Spectro.GetMagnitudeSquared(stripe, bin))
                    * Envelope[t - startSample];
        }

        var maxSample = wave.Max(s => Math.Abs(s));

        var bytes = new List<byte>(2 * wave.Length);
        foreach(var s in wave)
            bytes.AddRange(BitConverter.GetBytes(Convert.ToInt16(Int16.MaxValue * s / maxSample)));

        File.WriteAllBytes(filename, bytes.ToArray());
    }

}
