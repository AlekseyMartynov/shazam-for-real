using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Numerics;

class Spectrogram {

    // FFT fields

    public const int SAMPLE_RATE = 16000;
    public const int SAMPLES_IN_SEGMENT = 128;

    const int FFT_SIZE = 2048;
    const int BIN_COUNT = FFT_SIZE / 2 + 1;

    static readonly double[] HANN = Window.Hann(FFT_SIZE);

    readonly List<double[]> Stripes = new List<double[]>();
    double MaxMagnitude;

    // Landmark fields

    const int LANDMARK_RADIUS = 48;

    static readonly int MIN_LANDMARK_BIN = Math.Max(Sig.FREQ_0 * FFT_SIZE / SAMPLE_RATE, LANDMARK_RADIUS);
    static readonly int MAX_LANDMARK_BIN = Math.Min(Sig.FREQ_4 * FFT_SIZE / SAMPLE_RATE, FFT_SIZE - LANDMARK_RADIUS);

    static readonly IEnumerable<int> LANDMARK_NEIGHBOR_DELTAS = Enumerable.Range(-LANDMARK_RADIUS, LANDMARK_RADIUS)
        .Concat(Enumerable.Range(1, LANDMARK_RADIUS))
        .ToArray();

    List<(int x, int y)> LandmarkLocations = new List<(int, int)>();

    public int LandmarkCount => LandmarkLocations.Count;

    // Wave fields

    const int SEGMENTS_IN_STRIPE = FFT_SIZE / SAMPLES_IN_SEGMENT;

    readonly Queue<IReadOnlyCollection<short>> WaveSegments = new Queue<IReadOnlyCollection<short>>(SEGMENTS_IN_STRIPE);

    public int SampleCount { get; private set; }
    public int SampleMs => SampleCount * 1000 / SAMPLE_RATE;

    public void AddWaveSegment(IReadOnlyCollection<short> newSegment) {
        if(newSegment.Count != SAMPLES_IN_SEGMENT)
            throw new ArgumentException();

        SampleCount += SAMPLES_IN_SEGMENT;

        if(WaveSegments.Count == SEGMENTS_IN_STRIPE) {
            AddStripe();
            WaveSegments.Dequeue();

            if(Stripes.Count > 2 * LANDMARK_RADIUS)
                FindLandmarkLocations(Stripes.Count - LANDMARK_RADIUS - 1);
        }

        WaveSegments.Enqueue(newSegment);
    }

    void AddStripe() {
        var fft = new Complex[FFT_SIZE];
        var fftIndex = 0;

        foreach(var seg in WaveSegments) {
            foreach(var sample in seg) {
                fft[fftIndex] = new Complex(sample * HANN[fftIndex], 0);
                fftIndex++;
            }
        }

        Fourier.Forward(fft);

        var stripe = new double[BIN_COUNT];

        for(var bin = 0; bin < BIN_COUNT; bin++) {
            var magnitude = fft[bin].Magnitude;
            MaxMagnitude = Math.Max(MaxMagnitude, magnitude);

            stripe[bin] = magnitude;
        }

        Stripes.Add(stripe);
    }

    void FindLandmarkLocations(int stripeIndex) {
        var stripe = Stripes[stripeIndex];

        for(var bin = MIN_LANDMARK_BIN; bin < MAX_LANDMARK_BIN; bin++) {
            var magnitude = stripe[bin];

            var maxNeighbor = 0d;

            foreach(var delta in LANDMARK_NEIGHBOR_DELTAS)
                maxNeighbor = Math.Max(maxNeighbor, stripe[bin + delta]);

            if(magnitude <= maxNeighbor)
                continue;

            foreach(var delta in LANDMARK_NEIGHBOR_DELTAS)
                maxNeighbor = Math.Max(maxNeighbor, Stripes[stripeIndex + delta][bin]);

            if(magnitude <= maxNeighbor)
                continue;

            LandmarkLocations.Add((stripeIndex, bin));
        }
    }

    public Bitmap Draw() {
        var w = Stripes.Count;
        var h = BIN_COUNT;

        var bitmap = new Bitmap(w, h);
        var gamma = 0.25;

        for(var x = 0; x < w; x++) {
            for(var y = 0; y < h; y++) {
                var magnitude = Stripes[x][y];
                var shade = Convert.ToByte(255 * Math.Pow(magnitude / MaxMagnitude, gamma));
                bitmap.SetPixel(x, h - y - 1, Color.FromArgb(shade, shade, shade));
            }
        }

        using(var g = Graphics.FromImage(bitmap)) {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            foreach(var (x, y) in LandmarkLocations) {
                var magnitude = Stripes[x][y];
                var radius = 3 + Convert.ToInt32(4 * magnitude / MaxMagnitude);
                var cx = x;
                var cy = h - y - 1;
                g.FillEllipse(Brushes.Yellow, cx - radius, cy - radius, 2 * radius, 2 * radius);
            }
        }

        return bitmap;
    }

    public IReadOnlyCollection<LandmarkInfo> GetLandmarks() {
        var list = new List<LandmarkInfo>();
        foreach(var (x, y) in LandmarkLocations) {
            var magnitude = Stripes[x][y];
            var normalizedBin = Convert.ToUInt16(64 * y - 1);
            var normalizedMagnitude = Convert.ToUInt16(UInt16.MaxValue * magnitude / MaxMagnitude);
            var freq = y * SAMPLE_RATE / FFT_SIZE;
            list.Add(new LandmarkInfo(x, normalizedBin, normalizedMagnitude, freq));
        }
        return list;
    }
}
