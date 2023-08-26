using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

static class Interactive {

    public static async Task RunAsync() {
        PrintHotkeys();
        CaptureSourceHelper.Set(false);

        while(true) {
            var key = Console.ReadKey(true);

            if(Char.ToLower(key.KeyChar) == 'q')
                break;

#if !MCI_CAPTURE
            if(Char.ToLower(key.KeyChar) == 's') {
                CaptureSourceHelper.Toggle();
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
#if !MCI_CAPTURE
            "S - source",
#endif
            "Q - quit"
        ));
    }

    static ICaptureHelper CreateCaptureHelper() {
        if(!OperatingSystem.IsWindows()) {
            return new SoxCaptureHelper();
        }

#if MCI_CAPTURE
        return new MciCaptureHelper();
#else
        return new WasapiCaptureHelper();
#endif
    }

}
