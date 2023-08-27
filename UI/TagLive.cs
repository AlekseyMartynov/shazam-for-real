using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

static class TagLive {

    public static async Task RunAsync(bool auto) {
        var prevUrl = default(string);
        
        while(true) {
            ClearLine();
            Console.Write("Listening... ");

            try {
                using var captureHelper = CreateCaptureHelper();
                captureHelper.Start();

                var result = await CaptureAndTag.RunAsync(captureHelper);

                if(result.Success) {
                    var text = result.Url != prevUrl ? result.Url : "...";

                    ClearLine();
                    if(auto) {
                        Console.Write(DateTime.Now.ToString("HH:mm:ss"));
                        Console.Write(' ');
                    }
                    Console.WriteLine(text);

                    if(!auto) {
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

            ClearLine();
            Console.Write("Idle... ");

            await Task.Delay(5000);
        }
    }

    static void ClearLine() {
        if(Console.IsInputRedirected || Console.IsOutputRedirected) {
            Console.WriteLine();
        } else {
            Console.CursorLeft = 0;
            Console.Write(new String(' ', Console.WindowWidth - 1));
            Console.CursorLeft = 0;
        }
    }

    static void Navigate(string url) {
        if(OperatingSystem.IsWindows()) {
            using var proc = Process.Start("explorer", url);
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
