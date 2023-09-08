#if DEBUG
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using Xunit.Abstractions;

namespace Project.Test;

public class PeakDensityResearch {
    readonly ITestOutputHelper Output;
    readonly ICollection<(int stripe, int bin)> PeakMap;

    public PeakDensityResearch(ITestOutputHelper output) {
        Output = output;
        PeakMap = new HashSet<(int, int)>();
    }

    [Fact]
    public void Run() {
        LoadPeakMap(out var bandedPeakRates);

        for(var i = 0; i < bandedPeakRates.Count; i++)
            Output.WriteLine($"Band {i} peak rate {bandedPeakRates[i]:0.#} peak/sec");

        var noPeakBoundary = new Dictionary<int, int>();

        var binDelta = 0;
        var stripeDelta = 1;

        var nextBinDelta = false;
        var stop = false;

        while(true) {

            foreach(var (stripe, bin) in PeakMap) {
                if(!HasPeak(stripe, bin, stripeDelta, binDelta))
                    continue;

                var minStripeDelta = noPeakBoundary.GetValueOrDefault(binDelta, Int32.MaxValue);
                minStripeDelta = Math.Min(minStripeDelta, stripeDelta);

                var stripeDeltaNoPeaks = minStripeDelta - 1;
                noPeakBoundary[binDelta] = stripeDeltaNoPeaks;

                if(stripeDelta >= minStripeDelta)
                    nextBinDelta = true;

                if(minStripeDelta == 1)
                    stop = true;

                break;
            }

            if(stop)
                break;

            if(nextBinDelta) {
                binDelta++;
                stripeDelta = 1;
                nextBinDelta = false;
            } else {
                stripeDelta++;
            }
        }

        Output.WriteLine("bin\tstripe");
        foreach(var (bin, stripe) in noPeakBoundary.OrderBy(i => i.Key))
            Output.WriteLine($"{bin}\t{stripe}");
    }

    void LoadPeakMap(out IReadOnlyList<double> bandedRates) {
        var path = Path.Combine(TestHelper.DATA_DIR, "gabin-full-sigx-10.1.3.bin");

        Sig.Read(
            File.ReadAllBytes(path),
            out var sampleRate,
            out var sampleCount,
            out var peaks,
            out var bandedCount
        );

        var durationSeconds = 1d * sampleCount / sampleRate;

        bandedRates = bandedCount
            .Select(i => i / durationSeconds)
            .ToList();

        foreach(var p in peaks) {
            var (stripe, bin) = (p.StripeIndex, Convert.ToInt32(p.InterpolatedBin));
            PeakMap.Add((stripe, bin));
        }
    }

    bool HasPeak(int centerStripe, int centerBin, int stripeDelta, int binDelta) {
        return PeakMap.Contains((centerStripe - stripeDelta, centerBin - binDelta))
            || PeakMap.Contains((centerStripe - stripeDelta, centerBin + binDelta))
            || PeakMap.Contains((centerStripe + stripeDelta, centerBin - binDelta))
            || PeakMap.Contains((centerStripe + stripeDelta, centerBin + binDelta));
    }

}
#endif
