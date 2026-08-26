using System;
using System.Collections.Generic;
using System.Linq;

namespace Project;

static class ConsoleHelper {

    static ConsoleHelper() {
        IsRedirected = Console.IsInputRedirected || Console.IsOutputRedirected;
    }

    public static bool IsRedirected { get; private set; }

    public static void WriteProgress(string text) {
        if(IsRedirected)
            return;

        ClearProgress();
        Console.Write(text);
    }

    public static void ClearProgress() {
        if(IsRedirected)
            return;

        Console.CursorLeft = 0;
        Console.Write(new String(' ', Console.WindowWidth - 1));
        Console.CursorLeft = 0;
    }

    public static void WriteTime(TimeSpan time) {
        Console.Write(time.ToString(@"hh\:mm\:ss"));
        Console.Write(" ");
    }

    public static char ReadKey() {
        if(IsRedirected) {
            return ReadKeyFromStdIn();
        }
        try {
            return ReadKeyFromConsole();
        } catch {
            return ReadKeyFromStdIn();
        }
    }

    static char ReadKeyFromConsole() {
        // https://stackoverflow.com/a/3769828
        while(Console.KeyAvailable) {
            Console.ReadKey(true);
        }
        return Console.ReadKey(true).KeyChar;
    }

    static char ReadKeyFromStdIn() {
        var last = 0;
        while(true) {
            var next = Console.In.Read();
            switch(next) {
                case -1:
                    return (char)last;
                case 0xA or 0xD:
                    continue;
                default:
                    last = next;
                    break;
            }
        }
    }
}
