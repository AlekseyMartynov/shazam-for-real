using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;

class Analysis {
    public const int SAMPLE_RATE = 16000;
    public const int CHUNKS_PER_SECOND = 125;
    public const int CHUNK_SIZE = SAMPLE_RATE / CHUNKS_PER_SECOND;
    public const int WINDOW_SIZE = CHUNK_SIZE * 16;
    public const int BIN_COUNT = WINDOW_SIZE / 2 + 1;

    readonly static float[] HANN = Array.ConvertAll(Window.Hann(WINDOW_SIZE), Convert.ToSingle);

    readonly float[] WindowRing = new float[WINDOW_SIZE];
    readonly List<float[]> Stripes = new List<float[]>(3 * CHUNKS_PER_SECOND);

    readonly Complex32[] FFTBuf = new Complex32[WINDOW_SIZE];

    public int ProcessedSamples { get; private set; }
    public int ProcessedMs => ProcessedSamples * 1000 / SAMPLE_RATE;
    public int StripeCount => Stripes.Count;

    int WindowRingPos => ProcessedSamples % WINDOW_SIZE;

    public void ReadChunk(ISampleProvider sampleProvider) {
        if(sampleProvider.Read(WindowRing, WindowRingPos, CHUNK_SIZE) != CHUNK_SIZE)
            throw new Exception();

        ProcessedSamples += CHUNK_SIZE;

        if(ProcessedSamples >= WINDOW_SIZE)
            AddStripe();
    }

    void AddStripe() {
        for(var i = 0; i < WINDOW_SIZE; i++) {
            var waveRingIndex = (WindowRingPos + i) % WINDOW_SIZE;
            FFTBuf[i] = new Complex32(WindowRing[waveRingIndex] * HANN[i], 0);
        }

        Fourier.Forward(FFTBuf, FourierOptions.NoScaling);

        var stripe = new float[BIN_COUNT];
        for(var bin = 0; bin < BIN_COUNT; bin++)
            stripe[bin] = FFTBuf[bin].MagnitudeSquared;

        Stripes.Add(stripe);
    }

    public float GetMagnitudeSquared(int stripe, int bin) {
        return Stripes[stripe][bin];
    }

    public float FindMaxMagnitudeSquared() {
        return Stripes.Max(s => s.Max());
    }

    public static int FreqToBin(float freq) {
        return Convert.ToInt32(freq * WINDOW_SIZE / SAMPLE_RATE);
    }

    public static float BinToFreq(float bin) {
        return bin * SAMPLE_RATE / WINDOW_SIZE;
    }

}
