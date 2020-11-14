using System;
using System.Drawing;
using System.Drawing.Drawing2D;

class Painter {
    readonly Spectrogram Spectro;
    readonly LandmarkFinder Finder;

    public Painter(Spectrogram spectro, LandmarkFinder finder) {
        Spectro = spectro;
        Finder = finder;
    }

    public void Paint(string filename) {
        var w = Spectro.StripeCount;
        var h = Spectro.BinCount;
        var gamma = 0.25f;
        var maxMagnitudeSquared = Spectro.FindMaxMagnitudeSquared();

        using(var bitmap = new Bitmap(w, h)) {

            for(var x = 0; x < w; x++) {
                for(var y = 0; y < h; y++) {
                    var magnitudeSquared = Spectro.GetMagnitudeSquared(x, y);
                    var shade = Convert.ToByte(255 * MathF.Pow(magnitudeSquared / maxMagnitudeSquared, gamma / 2));
                    bitmap.SetPixel(x, h - y - 1, Color.FromArgb(shade, shade, shade));
                }
            }

            using(var g = Graphics.FromImage(bitmap)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                foreach(var l in Finder.EnumerateAllLandmarks()) {
                    var radius = 8 * l.InterpolatedLogMagnitude / UInt16.MaxValue;
                    var cx = l.StripeIndex;
                    var cy = h - l.InterpolatedBin - 1;
                    g.FillEllipse(Brushes.Yellow, cx - radius, cy - radius, 2 * radius, 2 * radius);
                }
            }

            bitmap.Save(filename);
        }
    }

}
