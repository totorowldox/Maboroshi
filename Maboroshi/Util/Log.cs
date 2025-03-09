using Spectre.Console;

namespace Maboroshi.Util;

public static class Log
{
    private const string TypedColor = "orchid2";
    
    public static void Debug(string msg, string type = "")
    {
        if (!string.IsNullOrEmpty(type))
        {
            msg = $"[{TypedColor}][[{type}]][/] " + msg;
        }
        AnsiConsole.Markup($"[grey][[MABOROSHI-DEBUG]][/] {msg}");
        AnsiConsole.WriteLine();
    }
    
    public static void Info(string msg, string type = "")
    {
        if (!string.IsNullOrEmpty(type))
        {
            msg = $"[{TypedColor}][[{type}]][/] " + msg;
        }
        AnsiConsole.Markup($"[white][[MABOROSHI-INFO]][/] {msg}");
        AnsiConsole.WriteLine();
    }
    
    public static void Warning(string msg, string type = "")
    {
        if (!string.IsNullOrEmpty(type))
        {
            msg = $"[{TypedColor}][[{type}]][/] " + msg;
        }
        AnsiConsole.Markup($"[yellow][[MABOROSHI-WARN]][/] {msg}");
        AnsiConsole.WriteLine();
    }
    
    public static void Error(string msg, string type = "")
    {
        if (!string.IsNullOrEmpty(type))
        {
            msg = $"[{TypedColor}][[{type}]][/] " + msg;
        }
        AnsiConsole.Markup($"[red][[MABOROSHI-ERROR]][/] {msg}");
        AnsiConsole.WriteLine();
    }
    
    public static void Critical(string msg, string type = "")
    {
        if (!string.IsNullOrEmpty(type))
        {
            msg = $"[{TypedColor}][[{type}]][/] " + msg;
        }
        AnsiConsole.Markup($"[red on white][[MABOROSHI-CRITICAL]][/] {msg}");
        AnsiConsole.WriteLine();
    }

    public static void Exception(Exception ex)
    {
        AnsiConsole.WriteException(ex, 
            ExceptionFormats.ShortenPaths | ExceptionFormats.ShortenTypes |
            ExceptionFormats.ShortenMethods | ExceptionFormats.ShowLinks);
    }
}