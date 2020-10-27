using System;
using System.Collections.Generic;
using System.Linq;

class Landmarks {
    readonly Spectrogram Spectro;
    readonly int Radius;

    readonly int MinBin, MaxBin;
    readonly IEnumerable<int> Deltas;

    readonly List<(int, int)> LocationsInternal = new List<(int, int)>();

    public Landmarks(Spectrogram spectro, int radius, int minBin, int maxBin) {
        Spectro = spectro;
        Radius = radius;
        MinBin = Math.Max(minBin, radius);
        MaxBin = Math.Min(maxBin, spectro.BinCount - radius);
        Deltas = Enumerable.Range(-Radius, Radius).Concat(Enumerable.Range(1, Radius));
    }

    public IReadOnlyList<(int stripe, int bin)> Locations => LocationsInternal;

    public void Detect(int stripe) {
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

            LocationsInternal.Add((stripe, bin));
        }
    }

}
