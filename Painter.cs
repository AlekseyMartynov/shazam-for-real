using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.Versioning;

[SupportedOSPlatform("windows")]
class Painter {
    readonly Analysis Analysis;
    readonly PeakFinder Finder;

    public Painter(Analysis analysis, PeakFinder finder) {
        Analysis = analysis;
        Finder = finder;
    }

    public void Paint(string filename) {
        var w = Analysis.StripeCount;
        var h = Analysis.BIN_COUNT;
        var gamma = 0.25f;
        var maxMagnitudeSquared = Analysis.FindMaxMagnitudeSquared();

        using(var bitmap = new Bitmap(w, h)) {

            for(var x = 0; x < w; x++) {
                for(var y = 0; y < h; y++) {
                    var magnitudeSquared = Analysis.GetMagnitudeSquared(x, y);
                    var shade = Convert.ToByte(255 * MathF.Pow(magnitudeSquared / maxMagnitudeSquared, gamma / 2));
                    bitmap.SetPixel(x, h - y - 1, Color.FromArgb(shade, shade, shade));
                }
            }

            using(var g = Graphics.FromImage(bitmap)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                foreach(var p in Finder.EnumerateAllPeaks()) {
                    var radius = 8 * p.InterpolatedLogMagnitude / UInt16.MaxValue;
                    var cx = p.StripeIndex;
                    var cy = h - p.InterpolatedBin - 1;
                    g.FillEllipse(Brushes.Yellow, cx - radius, cy - radius, 2 * radius, 2 * radius);
                }
            }

            bitmap.Save(filename);
        }
    }

}
