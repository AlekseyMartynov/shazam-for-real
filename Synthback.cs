using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Synthback {
    readonly Spectrogram Spectro;
    readonly LandmarkFinder LandmarkFinder;
    readonly int SampleRate, ChunkSize;
    readonly float[] Envelope;

    public Synthback(Spectrogram spectro, LandmarkFinder finder, int sampleRate, int chunkSize) {
        Spectro = spectro;
        LandmarkFinder = finder;
        SampleRate = sampleRate;
        ChunkSize = chunkSize;
        Envelope = Array.ConvertAll(Window.Hann(2 * ChunkSize), Convert.ToSingle);
    }

    public void Synth(string filename) {
        var landmarks = LandmarkFinder.EnumerateAllLandmarks().ToArray();

        var stripeCount = 1 + landmarks.Max(l => l.StripeIndex);
        var wave = new float[stripeCount * ChunkSize];

        foreach(var l in landmarks) {
            var startSample = ChunkSize * (l.StripeIndex - 1);
            var endSample = startSample + 2 * ChunkSize;

            for(var t = startSample; t < endSample; t++)
                wave[t] += MathF.Sin(2 * MathF.PI * Spectro.BinToFreq(l.InterpolatedBin) * t / SampleRate)
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
