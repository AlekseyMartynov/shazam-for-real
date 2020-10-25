using System;

struct LandmarkInfo {
    public readonly int StripeIndex;
    public readonly ushort NormalizedBin;
    public readonly ushort NormalizedMagnitude;
    public readonly double Freq;

    public LandmarkInfo(int stripeIndex, ushort normalizedBin, ushort normalizedMagnitude, double freq) {
        StripeIndex = stripeIndex;
        NormalizedBin = normalizedBin;
        NormalizedMagnitude = normalizedMagnitude;
        Freq = freq;
    }
}
