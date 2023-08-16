using System;
using System.Collections.Generic;
using System.Linq;

static class CaptureSourceHelper {
    public static bool Loopback { get; private set; }

    public static void Set(bool loopback) {
        Loopback = loopback;

        Console.Write("Source: ");
        Console.WriteLine(loopback ? "Loopback device" : "Recording device");
    }

    public static void Toggle() {
        Set(!Loopback);
    }
}
