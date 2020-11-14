using System;

struct LandmarkInfo {
    public readonly int StripeIndex;
    public readonly float InterpolatedBin;
    public readonly float InterpolatedLogMagnitude;

    public LandmarkInfo(int stripeIndex, float interpolatedBin, float interpolatedLogMagnitude) {
        StripeIndex = stripeIndex;
        InterpolatedBin = interpolatedBin;
        InterpolatedLogMagnitude = interpolatedLogMagnitude;
    }
}
