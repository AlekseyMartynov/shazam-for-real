using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Project;

static class Interactive {

    public static async Task RunAsync() {
        if(!ConsoleHelper.IsRedirected)
            PrintHotkeys();

        if(IsSourceSwitchEnabled()) {
            WasapiLoopbackHelper.Set(false);
        }

        while(true) {
            var key = Char.ToLower(ConsoleHelper.ReadKey());

            if(key == 'q' || key == '\0')
                break;

            if(key == 's' && IsSourceSwitchEnabled()) {
                WasapiLoopbackHelper.Toggle();
                continue;
            }

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
        Console.Write("SPACE - tag, A - auto, ");
        if(IsSourceSwitchEnabled()) {
            Console.Write("S - source, ");
        }
        Console.WriteLine("Q - quit");
    }

    static bool IsSourceSwitchEnabled() {
        return !ConsoleHelper.IsRedirected && OperatingSystem.IsWindows();
    }
}
