using System;
using System.Collections.Generic;
using System.Linq;

namespace Project.Test {

    static class TestExtensions {

        public static IEnumerable<PeakInfo> FindMany(this IEnumerable<PeakInfo> peaks, float bin) {
            return peaks.Where(p => Math.Abs(p.InterpolatedBin - bin) < 1);
        }

        public static PeakInfo FindOne(this IEnumerable<PeakInfo> peaks, float bin) {
            return FindMany(peaks, bin).FirstOrDefault();
        }

        public static PeakInfo FindOne(this IEnumerable<PeakInfo> peaks, int stripe, float bin) {
            return FindMany(peaks, bin).FirstOrDefault(p => p.StripeIndex == stripe);
        }

    }

}
