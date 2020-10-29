using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

class Synthback {
    readonly int SampleRate, ChunkSize;
    readonly double[] Envelope;

    public Synthback(int sampleRate, int chunkSize) {
        SampleRate = sampleRate;
        ChunkSize = chunkSize;
        Envelope = Window.Hann(2 * ChunkSize);
    }

    public void Synth(IEnumerable<LandmarkInfo> landmarks, string filename) {
        var stripeCount = 1 + landmarks.Max(l => l.StripeIndex);
        var wave = new double[stripeCount * ChunkSize];

        foreach(var l in landmarks) {
            var startSample = ChunkSize * (l.StripeIndex - 1);
            var endSample = startSample + 2 * ChunkSize;

            for(var t = startSample; t < endSample; t++)
                wave[t] += Math.Sin(2 * Math.PI * l.Freq * t / SampleRate)
                    * l.NormalizedMagnitude
                    * Envelope[t - startSample];
        }

        var maxSample = wave.Max(s => Math.Abs(s));

        var bytes = new List<byte>(2 * wave.Length);
        foreach(var s in wave)
            bytes.AddRange(BitConverter.GetBytes(Convert.ToInt16(Int16.MaxValue * s / maxSample)));

        File.WriteAllBytes(filename, bytes.ToArray());
    }

}
