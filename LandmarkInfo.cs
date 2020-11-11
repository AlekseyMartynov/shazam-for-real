using System;

struct LandmarkInfo {
    public readonly int StripeIndex;
    public readonly ushort NormalizedBin;
    public readonly ushort NormalizedMagnitude;

    public LandmarkInfo(int stripeIndex, ushort normalizedBin, ushort normalizedMagnitude) {
        StripeIndex = stripeIndex;
        NormalizedBin = normalizedBin;
        NormalizedMagnitude = normalizedMagnitude;
    }
}
