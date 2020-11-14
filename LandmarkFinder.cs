using System;
using System.Collections.Generic;
using System.Linq;

class LandmarkFinder {
    public const int
        RADIUS_TIME = 47,
        RADIUS_FREQ = 9;

    // Locations per second in a band
    const int RATE = 12;

    static readonly IReadOnlyList<int> BAND_FREQS = new[] { 250, 520, 1450, 3500, 5500 };

    static readonly double
        MIN_MAGN_SQUARED = 64 * 64,
        LOG_MIN_MAGN_SQUARED = Math.Log(MIN_MAGN_SQUARED);

    readonly Spectrogram Spectro;
    readonly TimeSpan StripeDuration;
    readonly int MinBin, MaxBin;
    readonly IReadOnlyList<List<(int stripe, int bin)>> Bands;

    public LandmarkFinder(Spectrogram spectro, TimeSpan stripeDuration) {
        Spectro = spectro;
        StripeDuration = stripeDuration;

        MinBin = Math.Max(spectro.FreqToBin(BAND_FREQS.Min()), RADIUS_FREQ);
        MaxBin = Math.Min(spectro.FreqToBin(BAND_FREQS.Max()), spectro.BinCount - RADIUS_FREQ);

        Bands = Enumerable.Range(0, BAND_FREQS.Count - 1)
            .Select(_ => new List<(int, int)>())
            .ToList();
    }

    public void Find(int stripe) {
        for(var bin = MinBin; bin < MaxBin; bin++) {

            if(Spectro.GetMagnitudeSquared(stripe, bin) < MIN_MAGN_SQUARED)
                continue;

            if(!IsPeak(stripe, bin, RADIUS_TIME, 0))
                continue;

            if(!IsPeak(stripe, bin, 3, RADIUS_FREQ))
                continue;

            AddLocation(stripe, bin);
        }
    }

    public IEnumerable<IEnumerable<LandmarkInfo>> EnumerateBandedLandmarks() {
        return Bands.Select(locations => locations.Select(LocationToLandmark));
    }

    public IEnumerable<(int stripe, int bin)> EnumerateAllLocations() {
        return Bands.SelectMany(i => i);
    }

    int GetBandIndex(int bin) {
        var freq = Spectro.BinToFreq(bin);

        if(freq < BAND_FREQS[0])
            throw new ArgumentOutOfRangeException();

        for(var i = 1; i < BAND_FREQS.Count; i++) {
            if(freq < BAND_FREQS[i])
                return i - 1;
        }

        throw new ArgumentOutOfRangeException();
    }

    LandmarkInfo LocationToLandmark((int, int) loc) {
        var (stripe, bin) = loc;

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

        return new LandmarkInfo(
            stripe,
            Convert.ToUInt16(64 * (bin + p)),
            Convert.ToUInt16(beta - (alpha - gamma) * p / 4)
        );
    }

    double GetLogMagnitude(int stripe, int bin) {
        // Pack 64..2^38 into uint16
        return 3 * 4096 * (Math.Log(Spectro.GetMagnitudeSquared(stripe, bin)) / LOG_MIN_MAGN_SQUARED - 1);
    }

    bool IsPeak(int stripe, int bin, int stripeRadius, int binRadius) {
        var center = Spectro.GetMagnitudeSquared(stripe, bin);
        for(var s = -stripeRadius; s <= stripeRadius; s++) {
            for(var b = -binRadius; b <= binRadius; b++) {
                if(s == 0 && b == 0)
                    continue;
                if(Spectro.GetMagnitudeSquared(stripe + s, bin + b) >= center)
                    return false;
            }
        }
        return true;
    }

    void AddLocation(int stripe, int bin) {
        var bandLocations = Bands[GetBandIndex(bin)];

        if(bandLocations.Any()) {
            var capturedDuration = StripeDuration.TotalSeconds * (stripe - bandLocations.First().stripe);
            var allowedCount = 1 + capturedDuration * RATE;
            if(bandLocations.Count > allowedCount) {
                var magnitudeSquared = Spectro.GetMagnitudeSquared(stripe, bin);
                var pruneIndex = bandLocations.FindLastIndex(l => Spectro.GetMagnitudeSquared(l.stripe, l.bin) < magnitudeSquared);
                if(pruneIndex < 0)
                    return;

                bandLocations.RemoveAt(pruneIndex);
            }
        }

        bandLocations.Add((stripe, bin));
    }

}
