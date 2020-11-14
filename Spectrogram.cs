using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

class Spectrogram {
    readonly int SampleRate;
    readonly int FFTSize;
    readonly double[] Hann;
    readonly List<double[]> Stripes = new List<double[]>();

    public readonly int BinCount;

    public Spectrogram(int sampleRate, int fftSize) {
        SampleRate = sampleRate;
        FFTSize = fftSize;
        Hann = Window.Hann(fftSize);
        BinCount = fftSize / 2 + 1;
    }

    public int StripeCount => Stripes.Count;

    public void AddStripe(IReadOnlyList<short> samples) {
        if(samples.Count != FFTSize)
            throw new ArgumentException();

        var fft = new Complex[FFTSize];
        for(var i = 0; i < FFTSize; i++)
            fft[i] = new Complex(samples[i] * Hann[i], 0);

        Fourier.Forward(fft, FourierOptions.NoScaling);

        var stripe = new double[BinCount];

        for(var bin = 0; bin < BinCount; bin++)
            stripe[bin] = fft[bin].MagnitudeSquared();

        Stripes.Add(stripe);
    }

    public double GetMagnitudeSquared(int stripe, int bin) {
        return Stripes[stripe][bin];
    }

    public int FreqToBin(double freq) {
        return Convert.ToInt32(freq * FFTSize / SampleRate);
    }

    public double BinToFreq(int bin) {
        return 1d * bin * SampleRate / FFTSize;
    }

    public double FindMaxMagnitudeSquared() {
        return Stripes.Max(s => s.Max());
    }

}
