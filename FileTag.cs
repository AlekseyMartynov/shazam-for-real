using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

static class FileTag {

    public static async Task RunAsync(string[] args) {
        var filePath = args[0];
        var startTime = TimeSpan.Zero;
        var tillEnd = false;

        foreach(var a in args.Skip(1)) {
            if(a == "till-end") {
                tillEnd = true;
            } else {
                startTime = TimeSpan.Parse(a);
            }
        }

        await RunAsync(filePath, startTime, tillEnd);
    }

    public static async Task RunAsync(string filePath, TimeSpan startTime, bool tillEnd) {
        using var captureHelper = new FileCaptureHelper(filePath, startTime);
        captureHelper.Start();

        while(true) {
            Console.Write(captureHelper.CurrentTime.ToString(@"hh\:mm\:ss"));
            Console.Write(" ");

            var result = await CaptureAndTag.RunAsync(captureHelper, 12000);

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
        }
    }

}
