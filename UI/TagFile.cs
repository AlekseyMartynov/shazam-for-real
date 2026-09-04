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

        foreach(var a in args.AsSpan().Slice(1)) {
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

        while(true) {
            captureHelper.SkipTo(startTime);

            ConsoleHelper.WriteTime(captureHelper.CurrentTime);

            try {
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
            } catch(Exception x) {
                Console.WriteLine("error: " + x.Message);
            }

            if(!tillEnd)
                break;

            startTime += TimeSpan.FromSeconds(30);
        }
    }

    internal static bool TryParseTime(ReadOnlySpan<char> text, out TimeSpan result) {
        var count = 0;
        var seconds = 0;
        foreach(var r in text.Split(':')) {
            if(++count > 3 || !Int32.TryParse(text[r], out var n) || n < 0) {
                result = default;
                return false;
            }
            seconds = 60 * seconds + n;
        }
        result = TimeSpan.FromSeconds(seconds);
        return true;
    }

}
