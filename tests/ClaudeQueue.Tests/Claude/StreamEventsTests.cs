using ClaudeQueue.Claude;

namespace ClaudeQueue.Tests.Claude;

public class StreamEventsTests
{
    [Fact] public void Subagent_tool_use_becomes_SubagentStarted() =>
        Assert.Equal(new SubagentStarted("Implement Task 1: Health endpoint"), StreamEventParser.Parse(
            """{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Agent","input":{"description":"Implement Task 1: Health endpoint","prompt":"..."}}]}}"""));

    [Fact] public void Task_tool_name_is_also_a_subagent() =>
        Assert.Equal(new SubagentStarted("d"), StreamEventParser.Parse(
            """{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Task","input":{"description":"d"}}]}}"""));

    [Fact] public void Subagent_wins_over_earlier_text() =>
        Assert.Equal(new SubagentStarted("d"), StreamEventParser.Parse(
            """{"type":"assistant","message":{"content":[{"type":"text","text":"hi"},{"type":"tool_use","name":"Agent","input":{"description":"d"}}]}}"""));

    [Fact] public void First_non_empty_text_is_used() =>
        Assert.Equal(new AssistantText("hello"), StreamEventParser.Parse(
            """{"type":"assistant","message":{"content":[{"type":"text","text":"  "},{"type":"text","text":"hello"}]}}"""));

    [Fact] public void Non_subagent_tool_use_is_ignored() =>
        Assert.Null(StreamEventParser.Parse(
            """{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Read","input":{"file_path":"x"}}]}}"""));

    [Fact] public void Result_line_carries_cost_and_text() =>
        Assert.Equal(new RunFinished(false, 0.42m, "All done"), StreamEventParser.Parse(
            """{"type":"result","subtype":"success","is_error":false,"total_cost_usd":0.42,"result":"All done"}"""));

    [Fact] public void Result_line_with_missing_fields_uses_defaults() =>
        Assert.Equal(new RunFinished(false, null, null), StreamEventParser.Parse("""{"type":"result"}"""));

    [Theory]
    [InlineData("""{"type":"result","total_cost_usd":"x","result":"ok"}""")]
    [InlineData("""{"type":"result","total_cost_usd":null,"result":"ok"}""")]
    [InlineData("""{"type":"result","total_cost_usd":1e999,"result":"ok"}""")]
    public void Non_numeric_cost_degrades_to_null_but_keeps_the_result(string line) =>
        Assert.Equal(new RunFinished(false, null, "ok"), StreamEventParser.Parse(line));

    [Fact] public void Error_result_is_flagged() =>
        Assert.Equal(new RunFinished(true, null, null), StreamEventParser.Parse("""{"type":"result","is_error":true}"""));

    [Theory]
    [InlineData("""{"type":"system","subtype":"init"}""")]
    [InlineData("not json")]
    [InlineData("")]
    [InlineData("""{"type":"assistant"}""")]
    [InlineData("""{"type":"assistant","message":{}}""")]
    [InlineData("""{"type":"assistant","message":{"content":"text"}}""")]
    [InlineData("""{"type":"assistant","message":{"content":[5,null]}}""")]
    [InlineData("""[1,2]""")]
    public void Unknown_or_bad_lines_are_ignored(string line) => Assert.Null(StreamEventParser.Parse(line));

    [Fact] public void Console_line_for_subagent() =>
        Assert.Equal("Claude ▶ Implement Task 1: Health endpoint", StreamEventParser.ToConsoleLine(new SubagentStarted("Implement Task 1: Health endpoint")));

    [Fact] public void Console_line_for_text_is_first_line_capped_at_120() =>
        Assert.Equal("Claude: " + new string('x', 120) + "…", StreamEventParser.ToConsoleLine(new AssistantText(new string('x', 200) + "\nsecond")));

    [Fact] public void Console_line_for_short_text_is_not_truncated() =>
        Assert.Equal("Claude: hi", StreamEventParser.ToConsoleLine(new AssistantText("hi\nthere")));

    [Fact] public void Console_line_for_result_is_null() =>
        Assert.Null(StreamEventParser.ToConsoleLine(new RunFinished(false, 1m, "x")));
}
