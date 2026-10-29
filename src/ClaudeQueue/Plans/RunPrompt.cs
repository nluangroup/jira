namespace ClaudeQueue.Plans;

/// <summary>How Claude is told to execute an approved plan.</summary>
public enum ExecutionMode
{
    /// <summary>Execute the plan using subagent-driven development.</summary>
    SubagentDriven,

    /// <summary>Execute the plan inline in the main session.</summary>
    Inline,
}
