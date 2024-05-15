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

            var startTime = DateTime.Now;

            try {
                using var captureHelper = CreateCaptureHelper();
                captureHelper.Start();

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

            var nextStartTime = startTime + TimeSpan.FromSeconds(15);
            while(DateTime.Now < nextStartTime)
                await Task.Delay(100);
        }
    }

    static void Navigate(string url) {
        if(OperatingSystem.IsWindows()) {
            using var proc = Process.Start("explorer", '"' + url + '"');
            proc.WaitForExit();
        }
    }

    static ICaptureHelper CreateCaptureHelper() {
#if WASAPI_CAPTURE
        return new WasapiCaptureHelper();
#else
        if(!OperatingSystem.IsWindows()) {
            return new SoxCaptureHelper();
        }

        return new MciCaptureHelper();
#endif
    }

}
