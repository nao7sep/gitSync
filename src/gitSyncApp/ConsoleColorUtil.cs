using System;

namespace gitSyncApp
{
    public static class ConsoleColorUtil
    {
        public static void WriteColored(string text, ConsoleColor? foreground = null, ConsoleColor? background = null)
        {
            var origFg = Console.ForegroundColor;
            var origBg = Console.BackgroundColor;
            if (foreground.HasValue) Console.ForegroundColor = foreground.Value;
            if (background.HasValue) Console.BackgroundColor = background.Value;
            Console.Write(text);
            Console.ForegroundColor = origFg;
            Console.BackgroundColor = origBg;
        }

        public static void WriteColoredLine(string text, ConsoleColor? foreground = null, ConsoleColor? background = null)
        {
            WriteColored(text + Environment.NewLine, foreground, background);
        }
    }
}
