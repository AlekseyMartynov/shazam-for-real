using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;

class Spectrogram {
    readonly int SampleRate;
    readonly int FFTSize;
    readonly float[] Hann;
    readonly List<float[]> Stripes = new List<float[]>();

    public readonly int BinCount;

    public Spectrogram(int sampleRate, int fftSize) {
        SampleRate = sampleRate;
        FFTSize = fftSize;
        Hann = Array.ConvertAll(Window.Hann(fftSize), Convert.ToSingle);
        BinCount = fftSize / 2 + 1;
    }

    public int StripeCount => Stripes.Count;

    public void AddStripe(IReadOnlyList<short> samples) {
        if(samples.Count != FFTSize)
            throw new ArgumentException();

        var fft = new Complex32[FFTSize];
        for(var i = 0; i < FFTSize; i++)
            fft[i] = new Complex32(samples[i] * Hann[i], 0);

        Fourier.Forward(fft, FourierOptions.NoScaling);

        var stripe = new float[BinCount];

        for(var bin = 0; bin < BinCount; bin++)
            stripe[bin] = fft[bin].MagnitudeSquared;

        Stripes.Add(stripe);
    }

    public float GetMagnitudeSquared(int stripe, int bin) {
        return Stripes[stripe][bin];
    }

    public int FreqToBin(float freq) {
        return Convert.ToInt32(freq * FFTSize / SampleRate);
    }

    public float BinToFreq(float bin) {
        return bin * SampleRate / FFTSize;
    }

    public float FindMaxMagnitudeSquared() {
        return Stripes.Max(s => s.Max());
    }

}
