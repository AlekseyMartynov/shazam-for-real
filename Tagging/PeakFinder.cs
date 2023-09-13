using System;
using System.Collections.Generic;
using System.Linq;

namespace Project;

record PeakInfo(
    int StripeIndex,
    float InterpolatedBin,
    float LogMagnitude
);

class PeakFinder {
    public const int
        STRIPE_DIST = 45,
        BIN_DIST = 9;

    // Peaks per second in a band
    const int RATE = 12;

    static readonly IReadOnlyList<int> BAND_FREQS = new[] { 250, 520, 1450, 3500, 5500 };

    static readonly int
        MIN_BIN = Math.Max(Analysis.FreqToBin(BAND_FREQS.Min()), BIN_DIST),
        MAX_BIN = Math.Min(Analysis.FreqToBin(BAND_FREQS.Max()), Analysis.BIN_COUNT - BIN_DIST);

    static readonly float
        MIN_MAGN_SQUARED = 1f / 512 / 512,
        LOG_MIN_MAGN_SQUARED = MathF.Log(MIN_MAGN_SQUARED);

    readonly Analysis Analysis;
    readonly IReadOnlyList<List<PeakInfo>> Bands;

    public PeakFinder(Analysis analysis) {
        analysis.SetStripeAddedCallback(Analysis_StripeAddedCallback);

        Analysis = analysis;

        Bands = Enumerable.Range(0, BAND_FREQS.Count - 1)
            .Select(_ => new List<PeakInfo>())
            .ToList();
    }

    void Analysis_StripeAddedCallback() {
        if(Analysis.StripeCount > 2 * STRIPE_DIST)
            Find(Analysis.StripeCount - STRIPE_DIST - 1);
    }

    void Find(int stripe) {
        for(var bin = MIN_BIN; bin < MAX_BIN; bin++) {

            if(Analysis.GetMagnitudeSquared(stripe, bin) < MIN_MAGN_SQUARED)
                continue;

            if(!IsPeak(stripe, bin, STRIPE_DIST, 0))
                continue;

            if(!IsPeak(stripe, bin, 3, BIN_DIST))
                continue;

            AddPeakAt(stripe, bin);
        }
    }

    public IEnumerable<IEnumerable<PeakInfo>> EnumerateBandedPeaks() {
        return Bands;
    }

    public IEnumerable<PeakInfo> EnumerateAllPeaks() {
        return Bands.SelectMany(i => i);
    }

    int GetBandIndex(float bin) {
        var freq = Analysis.BinToFreq(bin);

        if(freq < BAND_FREQS[0])
            return -1;

        for(var i = 1; i < BAND_FREQS.Count; i++) {
            if(freq < BAND_FREQS[i])
                return i - 1;
        }

        return -1;
    }

    PeakInfo CreatePeakAt(int stripe, int bin) {
        // Quadratic Interpolation of Spectral Peaks
        // https://stackoverflow.com/a/59140547
        // https://ccrma.stanford.edu/~jos/sasp/Quadratic_Interpolation_Spectral_Peaks.html

        // https://ccrma.stanford.edu/~jos/parshl/Peak_Detection_Steps_3.html
        // "We have found empirically that the frequencies tend to be about twice as accurate"
        // "when dB magnitude is used rather than just linear magnitude"

        var alpha = GetLogMagnitude(stripe, bin - 1);
        var beta = GetLogMagnitude(stripe, bin);
        var gamma = GetLogMagnitude(stripe, bin + 1);
        var p = (alpha - gamma) / (alpha - 2 * beta + gamma) / 2;

        return new PeakInfo(
            stripe,
            bin + p,
            beta // - (alpha - gamma) * p / 4
        );
    }

    float GetLogMagnitude(int stripe, int bin) {
        return 18 * 1024 * (1 - MathF.Log(Analysis.GetMagnitudeSquared(stripe, bin)) / LOG_MIN_MAGN_SQUARED);
    }

    bool IsPeak(int stripe, int bin, int stripeDist, int binDist) {
        var center = Analysis.GetMagnitudeSquared(stripe, bin);
        for(var s = -stripeDist; s <= stripeDist; s++) {
            for(var b = -binDist; b <= binDist; b++) {
                if(s == 0 && b == 0)
                    continue;
                if(Analysis.GetMagnitudeSquared(stripe + s, bin + b) >= center)
                    return false;
            }
        }
        return true;
    }

    void AddPeakAt(int stripe, int bin) {
        var newPeak = CreatePeakAt(stripe, bin);

        var bandIndex = GetBandIndex(newPeak.InterpolatedBin);
        if(bandIndex < 0)
            return;

        var bandPeaks = Bands[bandIndex];

        if(bandPeaks.Any()) {
            var capturedDuration = 1d / Analysis.CHUNKS_PER_SECOND * (stripe - bandPeaks.First().StripeIndex);
            var allowedCount = 1 + capturedDuration * RATE;
            if(bandPeaks.Count > allowedCount) {
                var pruneIndex = bandPeaks.FindLastIndex(l => l.LogMagnitude < newPeak.LogMagnitude);
                if(pruneIndex < 0)
                    return;

                bandPeaks.RemoveAt(pruneIndex);
            }
        }

        bandPeaks.Add(newPeak);
    }

}
