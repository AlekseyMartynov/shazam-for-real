using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

static class Interactive {

    public static async Task RunAsync() {
        if(!ConsoleHelper.IsRedirected)
            PrintHotkeys();

#if WASAPI_CAPTURE
        WasapiLoopbackHelper.Set(false);
#endif

        while(true) {
            var key = Char.ToLower(ReadKey());

            if(key == 'q' || key == '\0')
                break;

#if WASAPI_CAPTURE
            if(key == 's') {
                WasapiLoopbackHelper.Toggle();
                continue;
            }
#endif

            if(key == ' ') {
                await TagLive.RunAsync(false);
                continue;
            }

            if(key == 'a') {
                await TagLive.RunAsync(true);
            }
        }

    }

    static void PrintHotkeys() {
        Console.WriteLine(String.Join(", ",
            "SPACE - tag",
            "A - auto",
#if WASAPI_CAPTURE
            "S - source",
#endif
            "Q - quit"
        ));
    }

    static char ReadKey() {
        try {
            return Console.ReadKey(true).KeyChar;
        } catch(InvalidOperationException) {
            return Console.In.ReadToEnd().FirstOrDefault();
        }
    }
}
