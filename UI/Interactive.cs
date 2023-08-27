using System;
using System.Collections.Generic;
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
                await TagLive.RunAsync();
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

}
