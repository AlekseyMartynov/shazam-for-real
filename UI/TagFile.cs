using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

static class TagFile {

    public static async Task RunAsync(string[] args) {
        var filePath = args[0];
        var startTime = TimeSpan.Zero;
        var tillEnd = false;

        foreach(var a in args.Skip(1)) {
            if(a == "till-end") {
                tillEnd = true;
            } else {
                if(!TryParseTime(a, out startTime))
                    throw new Exception("Cannot parse time: " + a);
            }
        }

        await RunAsync(filePath, startTime, tillEnd);
    }

    public static async Task RunAsync(string filePath, TimeSpan startTime, bool tillEnd) {
        using var captureHelper = new FileCaptureHelper(filePath, startTime);
        captureHelper.Start();

        while(true) {
            captureHelper.SkipTo(startTime);

            ConsoleHelper.WriteTime(captureHelper.CurrentTime);

            var result = await CaptureAndTag.RunAsync(captureHelper);

            if(result == null) {
                Console.WriteLine("END");
                break;
            }

            if(result.Success) {
                Console.WriteLine(result.Url);
            } else {
                Console.WriteLine("-");
            }

            if(!tillEnd)
                break;

            startTime += TimeSpan.FromSeconds(30);
        }
    }

    static bool TryParseTime(string text, out TimeSpan result) {
        var segments = text.Split(':');
        var values = new int[3];
        var count = Math.Min(3, segments.Length);

        Array.Reverse(segments);

        for(var i = 0; i < count; i++) {
            if(!Int32.TryParse(segments[i], out values[i])) {
                result = TimeSpan.Zero;
                return false;
            }
        }

        result = new TimeSpan(values[2], values[1], values[0]);
        return true;
    }

}
