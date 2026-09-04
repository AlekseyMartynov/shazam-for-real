using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

static class TagLive {

    public static async Task RunAsync(bool auto) {
        var prevUrl = default(string);

        while(true) {
            ConsoleHelper.WriteProgress("Listening... ");

            var startTime = DateTimeOffset.Now;

            try {
                using var captureHelper = CreateCaptureHelper();

                var result = await CaptureAndTag.RunAsync(captureHelper);

                if(result.Success) {
                    var text = result.Url != prevUrl ? result.Url : "...";

                    ConsoleHelper.ClearProgress();
                    if(auto) {
                        Console.Write(startTime.ToString("HH:mm:ss"));
                        Console.Write(' ');
                    }
                    Console.WriteLine(text);

                    if(!ConsoleHelper.IsRedirected && !auto) {
                        Navigate(result.Url);
                    }

                    prevUrl = result.Url;
                } else {
                    if(!auto)
                        Console.WriteLine(":(");
                }
            } catch(Exception x) {
                Console.WriteLine("error: " + x.Message);
            }

            if(!auto)
                break;

            ConsoleHelper.WriteProgress("Idle... ");

            var delay = startTime.AddSeconds(15) - DateTimeOffset.Now;
            if(delay > TimeSpan.Zero) {
                await Task.Delay(delay);
            }
        }
    }

    static void Navigate(string url) {
        if(OperatingSystem.IsWindows()) {
            using var proc = Process.Start("explorer", '"' + url + '"');
            proc.WaitForExit();
        }
    }

    static CaptureHelper CreateCaptureHelper() {
        if(OperatingSystem.IsWindows()) {
            return new WasapiCaptureHelper();
        }

        return new SoxCaptureHelper();
    }

}
