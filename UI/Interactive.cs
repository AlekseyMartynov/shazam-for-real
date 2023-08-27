using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

static class Interactive {

    public static async Task RunAsync() {
        PrintHotkeys();

#if WASAPI_CAPTURE
        WasapiLoopbackHelper.Set(false);
#endif

        while(true) {
            var key = Console.ReadKey(true);

            if(Char.ToLower(key.KeyChar) == 'q')
                break;

#if WASAPI_CAPTURE
            if(Char.ToLower(key.KeyChar) == 's') {
                WasapiLoopbackHelper.Toggle();
                continue;
            }
#endif

            if(key.Key == ConsoleKey.Spacebar) {
                Console.Write("Listening... ");

                try {
                    using var captureHelper = CreateCaptureHelper();
                    captureHelper.Start();

                    var result = await CaptureAndTag.RunAsync(captureHelper);

                    if(result.Success) {
                        Console.CursorLeft = 0;
                        Console.WriteLine(result.Url);
                        if(OperatingSystem.IsWindows()) {
                            Process.Start("explorer", result.Url);
                        }
                    } else {
                        Console.WriteLine(":(");
                    }
                } catch(Exception x) {
                    Console.WriteLine("error: " + x.Message);
                }
            }
        }

    }

    static void PrintHotkeys() {
        Console.WriteLine(String.Join(", ",
            "SPACE - tag",
#if WASAPI_CAPTURE
            "S - source",
#endif
            "Q - quit"
        ));
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
