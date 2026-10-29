using ClaudeQueue.Plans;

namespace ClaudeQueue;

/// <summary>Settings for the queue worker, bound from the <c>Queue</c> configuration section.</summary>
public sealed class QueueOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Queue";

    /// <summary>Base URL of the Jira Cloud site.</summary>
    public string JiraBaseUrl { get; set; } = "https://nlundgaard.atlassian.net";

    /// <summary>Jira account email; supplied via user secrets.</summary>
    public string JiraEmail { get; set; } = "";

    /// <summary>Jira API token; supplied via user secrets.</summary>
    public string JiraApiToken { get; set; } = "";

    /// <summary>Jira project key to poll.</summary>
    public string ProjectKey { get; set; } = "SCRUM";

    /// <summary>Status name that marks a card as ready to run.</summary>
    public string ReadyStatus { get; set; } = "Ready for Claude";

    /// <summary>Status name applied while a run is active.</summary>
    public string InProgressStatus { get; set; } = "In Progress";

    /// <summary>Status name applied when a run has finished.</summary>
    public string ReviewStatus { get; set; } = "In Review";

    /// <summary>The only GitHub repository (owner/name) cards may target.</summary>
    public string DemoRepo { get; set; } = "nluangroup/agentic-demo";

    /// <summary>Delay between Jira polls.</summary>
    public TimeSpan PollInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>Delay between live-progress updates during a run.</summary>
    public TimeSpan ProgressInterval { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Directory for per-run working files; environment variables are expanded at startup.</summary>
    public string RunsDirectory { get; set; } = @"%LOCALAPPDATA%\ClaudeQueue\runs";

    /// <summary>Claude CLI executable name or path.</summary>
    public string ClaudeExecutable { get; set; } = "claude";

    /// <summary>Wall-clock limit for one Claude run.</summary>
    public TimeSpan ClaudeTimeout { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>Maximum spend in USD for one Claude run.</summary>
    public decimal ClaudeMaxBudgetUsd { get; set; } = 5;

    /// <summary>How Claude executes the plan.</summary>
    public ExecutionMode ExecutionMode { get; set; } = ExecutionMode.SubagentDriven;

    /// <summary>Verification script run in the repo after Claude finishes.</summary>
    public string VerifyScript { get; set; } = "verify.ps1";
}
