using System;
using System.Collections.Generic;
using System.Linq;

class LandmarkFinder {
    static readonly IReadOnlyList<int> BAND_FREQS = new[] { 250, 520, 1450, 3500, 5500 };

    readonly Spectrogram Spectro;
    readonly int Radius;

    readonly int MinBin, MaxBin;
    readonly IEnumerable<int> Deltas;

    readonly IReadOnlyList<List<(int, int)>> Bands;

    public LandmarkFinder(Spectrogram spectro, int radius) {
        Spectro = spectro;
        Radius = radius;
        MinBin = Math.Max(spectro.FreqToBin(BAND_FREQS.Min()), radius);
        MaxBin = Math.Min(spectro.FreqToBin(BAND_FREQS.Max()), spectro.BinCount - radius);
        Deltas = Enumerable.Range(-Radius, Radius).Concat(Enumerable.Range(1, Radius));

        Bands = Enumerable.Range(0, BAND_FREQS.Count - 1)
            .Select(_ => new List<(int, int)>())
            .ToList();
    }

    public void Find(int stripe) {
        for(var bin = MinBin; bin < MaxBin; bin++) {
            var magnitude = Spectro.GetMagnitude(stripe, bin);
            var maxNeighbor = 0d;

            foreach(var delta in Deltas)
                maxNeighbor = Math.Max(maxNeighbor, Spectro.GetMagnitude(stripe, bin + delta));

            if(magnitude <= maxNeighbor)
                continue;

            foreach(var delta in Deltas)
                maxNeighbor = Math.Max(maxNeighbor, Spectro.GetMagnitude(stripe + delta, bin));

            if(magnitude <= maxNeighbor)
                continue;

            Bands[GetBandIndex(bin)].Add((stripe, bin));
        }
    }

    public IEnumerable<IEnumerable<LandmarkInfo>> EnumerateBands() {
        return Bands.Select(locations => locations.Select(LocationToLandmark));
    }

    public IEnumerable<(int stripe, int bin)> EnumerateAllLocations() {
        return Bands.SelectMany(i => i);
    }

    public IEnumerable<LandmarkInfo> EnumerateAllLandmarks() {
        return EnumerateAllLocations().Select(LocationToLandmark);
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
        return new LandmarkInfo(
            stripe,
            Convert.ToUInt16(64 * bin - 1),
            Convert.ToUInt16(UInt16.MaxValue * Spectro.GetMagnitude(stripe, bin) / Spectro.MaxMagnitude),
            Spectro.BinToFreq(bin)
        );
    }

}
