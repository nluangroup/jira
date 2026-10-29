using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;

namespace ClaudeQueue.Logging;

/// <summary>Console formatter producing <c>[yyyy-MM-dd HH:mm:ss] LEVEL: message</c> lines in local time.</summary>
public sealed class TimestampConsoleFormatter : ConsoleFormatter
{
    /// <summary>Name under which the formatter is registered.</summary>
    public const string FormatterName = "timestamp";

    /// <summary>Creates the formatter.</summary>
    public TimestampConsoleFormatter() : base(FormatterName)
    {
    }

    /// <summary>Formats one log line without the exception.</summary>
    /// <param name="local">Local time of the entry.</param>
    /// <param name="level">Log level.</param>
    /// <param name="message">Rendered message.</param>
    public static string FormatLine(DateTime local, LogLevel level, string message) =>
        $"[{local.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}] {LevelName(level)}: {message}";

    /// <inheritdoc />
    public override void Write<TState>(
        in LogEntry<TState> logEntry, IExternalScopeProvider? scopeProvider, TextWriter textWriter)
    {
        var message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception);
        if (message is null && logEntry.Exception is null)
        {
            return;
        }

        textWriter.WriteLine(FormatLine(DateTime.Now, logEntry.LogLevel, message ?? ""));
        if (logEntry.Exception is not null)
        {
            textWriter.WriteLine(logEntry.Exception.ToString());
        }
    }

    private static string LevelName(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRACE",
        LogLevel.Debug => "DEBUG",
        LogLevel.Information => "INFO",
        LogLevel.Warning => "WARNING",
        LogLevel.Error => "ERROR",
        LogLevel.Critical => "CRITICAL",
        _ => level.ToString().ToUpperInvariant(),
    };
}
