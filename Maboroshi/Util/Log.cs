using Spectre.Console;
using System.Runtime.CompilerServices;

namespace Maboroshi.Util;

public static class Log
{
    private const string TypedColor = "orchid2";
    private const string DebugLevel = "DEBUG";
    private const string InfoLevel = "INFO";
    private const string WarnLevel = "WARN";
    private const string ErrorLevel = "ERROR";
    private const string CriticalLevel = "CRITICAL";
    private const string DateTimeFormat = "yyyy-MM-dd HH:mm:ss.fff";

    // Configuration options
    private static bool EnableDebug { get; set; } = true;
    private static bool IncludeScope { get; set; } = true;
    private static bool IncludeSource { get; set; } = true;

    private static void LogMessage(
        string level,
        string msg,
        string scope = "",
        string color = "white",
        string backgroundColor = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        string formattedMessage = string.Empty;

        // Time
        formattedMessage += $"[grey]{DateTime.Now.ToString(DateTimeFormat)}[/] ";

        // Source Information
        if (IncludeSource)
        {
            var fileName = Path.GetFileName(sourceFilePath);
            formattedMessage += $"[grey]({fileName}:{sourceLineNumber} - {memberName})[/]\n";
        }

        // Scope
        if (IncludeScope && !string.IsNullOrEmpty(scope))
        {
            formattedMessage += $"[{TypedColor}][[{scope}]][/] ";
        }

        var markup = $"[{color}";
        if (!string.IsNullOrEmpty(backgroundColor))
        {
            markup += $" on {backgroundColor}";
        }
        markup += $"][[MABOROSHI]] [[{level}]][/] {formattedMessage}{{0}}";

        AnsiConsole.Markup(markup, Markup.Escape(msg));
        AnsiConsole.WriteLine();
    }

    public static void Debug(string msg, string scope = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        if (EnableDebug)
        {
            LogMessage(DebugLevel, msg, scope, "grey", memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
        }
    }

    public static void Info(string msg, string scope = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(InfoLevel, msg, scope, "white", memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public static void Warning(string msg, string scope = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(WarnLevel, msg, scope, "yellow", memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public static void Error(string msg, string scope = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(ErrorLevel, msg, scope, "red", memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public static void Critical(string msg, string scope = "",
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string sourceFilePath = "",
        [CallerLineNumber] int sourceLineNumber = 0)
    {
        LogMessage(CriticalLevel, msg, scope, "red", "white", memberName: memberName, sourceFilePath: sourceFilePath, sourceLineNumber: sourceLineNumber);
    }

    public static void Exception(Exception ex)
    {
        AnsiConsole.WriteException(ex,
            ExceptionFormats.ShortenEverything | ExceptionFormats.ShowLinks);
    }
}
