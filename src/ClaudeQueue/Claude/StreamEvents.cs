using System.Text.Json;

namespace ClaudeQueue.Claude;

/// <summary>An event extracted from Claude's stream-json output.</summary>
public abstract record StreamEvent;

/// <summary>Claude launched a subagent.</summary>
/// <param name="Description">The subagent task description.</param>
public sealed record SubagentStarted(string Description) : StreamEvent;

/// <summary>Claude produced assistant text.</summary>
/// <param name="Text">The text.</param>
public sealed record AssistantText(string Text) : StreamEvent;

/// <summary>The run ended with a result line.</summary>
/// <param name="IsError">Whether Claude reported an error.</param>
/// <param name="CostUsd">Total cost in USD, when reported.</param>
/// <param name="ResultText">Final result text, when reported.</param>
public sealed record RunFinished(bool IsError, decimal? CostUsd, string? ResultText) : StreamEvent;

/// <summary>Parses stream-json lines and renders console lines.</summary>
public static class StreamEventParser
{
    private const int MaxTextLength = 120;

    /// <summary>Parses one stream-json line.</summary>
    /// <param name="jsonLine">A single line of output.</param>
    /// <returns>The event, or null when the line is not relevant or not recognisable.</returns>
    public static StreamEvent? Parse(string jsonLine)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonLine);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
            {
                return null;
            }

            return Str(root, "type") switch
            {
                "assistant" => ParseAssistant(root),
                "result" => ParseResult(root),
                _ => null,
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Renders an event for the console.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The line to print, or null when the event is not shown.</returns>
    public static string? ToConsoleLine(StreamEvent e) => e switch
    {
        SubagentStarted s => $"Claude ▶ {s.Description}",
        AssistantText t => "Claude: " + FirstLineCapped(t.Text),
        _ => null,
    };

    private static string FirstLineCapped(string text)
    {
        var line = text.Split('\n', 2)[0].TrimEnd('\r');
        return line.Length > MaxTextLength ? line[..MaxTextLength] + "…" : line;
    }

    private static StreamEvent? ParseAssistant(JsonElement root)
    {
        if (!root.TryGetProperty("message", out var message) || message.ValueKind != JsonValueKind.Object
            || !message.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        string? firstText = null;
        foreach (var item in content.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            switch (Str(item, "type"))
            {
                case "tool_use" when Str(item, "name") is "Agent" or "Task":
                    if (item.TryGetProperty("input", out var input) && input.ValueKind == JsonValueKind.Object
                        && Str(input, "description") is { } description)
                    {
                        return new SubagentStarted(description);
                    }

                    break;
                case "text" when firstText is null && !string.IsNullOrWhiteSpace(Str(item, "text")):
                    firstText = Str(item, "text");
                    break;
            }
        }

        return firstText is null ? null : new AssistantText(firstText);
    }

    private static RunFinished ParseResult(JsonElement root)
    {
        var isError = root.TryGetProperty("is_error", out var err) && err.ValueKind == JsonValueKind.True;
        decimal? cost = root.TryGetProperty("total_cost_usd", out var c)
            && c.ValueKind == JsonValueKind.Number && c.TryGetDecimal(out var d) ? d : null;
        return new RunFinished(isError, cost, Str(root, "result"));
    }

    private static string? Str(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
