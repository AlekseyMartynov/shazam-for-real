using System;
using System.Collections.Generic;
using System.Linq;

namespace Project;

static class ConsoleHelper {
    static int StickyTop = -1;

    static ConsoleHelper() {
        IsRedirected = Console.IsInputRedirected || Console.IsOutputRedirected || Console.IsErrorRedirected;
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
        WriteFullWidth(default);
        Console.CursorLeft = 0;
    }

    public static void WriteTime(TimeSpan time) {
        Console.Write(time.ToString(@"hh\:mm\:ss"));
        Console.Write(" ");
    }

    public static void WriteStickyLine(string text) {
        if(IsRedirected) {
            Console.WriteLine(text);
        } else {
            if(StickyTop > -1) {
                try {
                    Console.SetCursorPosition(0, StickyTop);
                } catch {
                    // Console buffer shrank or other issue
                    // Drop stale position
                    StickyTop = -1;
                }
            }
            WriteFullWidth(text);
            Console.WriteLine();
            if(StickyTop < 0) {
                StickyTop = Console.CursorTop - 1;
            }
        }
    }

    public static void Unstick() {
        StickyTop = -1;
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
        while(true) {
            var info = Console.ReadKey(true);
            if(info.KeyChar != 0) {
                return info.KeyChar;
            }
        }
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

    static void WriteFullWidth(ReadOnlySpan<char> text) {
        var full = (stackalloc char[Math.Max(0, Console.WindowWidth - 1)]);
        full.Fill(' ');
        text[..Math.Min(text.Length, full.Length)].CopyTo(full);
#if DEBUG
        if(full.ContainsAnyInRange('\0', '\x19')) {
            throw new InvalidOperationException();
        }
#endif
        Console.Write(full);
    }
}
