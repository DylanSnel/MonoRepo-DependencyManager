using System;

namespace MonoRepo.DependencyManager.Helpers;

public static class Cli
{
    public static T AskFor<T>(string question, string defaultValue = null)
    {
        if (defaultValue != null)
        {
            ColorConsole.WriteEmbeddedColorLine($"{question}: [{defaultValue}]");
        }
        else
        {
            ColorConsole.WriteEmbeddedColorLine($"{question}: ");
        }
        var input = Console.ReadLine();
        if (input == "" && defaultValue != null)
        {
            return (T)Convert.ChangeType(defaultValue, typeof(T));
        }

        return (T)Convert.ChangeType(input, typeof(T));
    }

    public static bool Confirm(string question, bool? defaultValue = null)
    {
        ConsoleKey response;
        do
        {
            if (defaultValue != null)
            {
                ColorConsole.WriteEmbeddedColorLine($"{question}: y/n [{defaultValue}]");
            }
            else
            {
                ColorConsole.WriteEmbeddedColorLine($"{question}: y/n");
            }
            response = Console.ReadKey(false).Key;
            if (response != ConsoleKey.Enter)
            {
                ColorConsole.WriteEmbeddedColorLine("");
            }
            if (response == ConsoleKey.Enter && defaultValue != null)
            {
                return defaultValue.Value;
            }
            else if (response == ConsoleKey.Enter)
            {
                continue;
            }
            else if (response != ConsoleKey.Y && response != ConsoleKey.N)
            {
                ColorConsole.WriteEmbeddedColorLine("Invalid input");
            }
        } while (response != ConsoleKey.Y && response != ConsoleKey.N);

        return response == ConsoleKey.Y;
    }

    /// <remarks>
    /// https://stackoverflow.com/a/8946847/1188513
    /// </remarks>>
    private static void ClearCurrentLine()
    {
        var currentLine = Console.CursorTop;
        Console.SetCursorPosition(0, Console.CursorTop);
        Console.Write(new string(' ', Console.WindowWidth));
        Console.SetCursorPosition(0, currentLine);
    }
}
