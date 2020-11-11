﻿using System;
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
        var gamma = 0.25;
        var maxMagnitude = Spectro.FindMaxMagnitude();

        using(var bitmap = new Bitmap(w, h)) {

            for(var x = 0; x < w; x++) {
                for(var y = 0; y < h; y++) {
                    var magnitude = Spectro.GetMagnitude(x, y);
                    var shade = Convert.ToByte(255 * Math.Pow(magnitude / maxMagnitude, gamma));
                    bitmap.SetPixel(x, h - y - 1, Color.FromArgb(shade, shade, shade));
                }
            }

            using(var g = Graphics.FromImage(bitmap)) {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                foreach(var (x, y) in Finder.EnumerateAllLocations()) {
                    var magnitude = Spectro.GetMagnitude(x, y);
                    var radius = 3 + Convert.ToInt32(4 * magnitude / maxMagnitude);
                    var cx = x;
                    var cy = h - y - 1;
                    g.FillEllipse(Brushes.Yellow, cx - radius, cy - radius, 2 * radius, 2 * radius);
                }
            }

            bitmap.Save(filename);
        }
    }

}
