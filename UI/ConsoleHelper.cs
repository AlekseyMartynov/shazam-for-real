using System;
using System.Collections.Generic;
using System.Linq;

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

}
