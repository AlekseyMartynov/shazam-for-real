using System;

struct PeakInfo {
    public readonly int StripeIndex;
    public readonly float InterpolatedBin;
    public readonly float InterpolatedLogMagnitude;

    public PeakInfo(int stripeIndex, float interpolatedBin, float interpolatedLogMagnitude) {
        StripeIndex = stripeIndex;
        InterpolatedBin = interpolatedBin;
        InterpolatedLogMagnitude = interpolatedLogMagnitude;
    }
}
