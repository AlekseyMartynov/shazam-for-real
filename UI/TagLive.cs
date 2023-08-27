using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

static class TagLive {

    public static async Task RunAsync() {
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
