using ClaudeQueue.Claude;
using ClaudeQueue.Git;
using ClaudeQueue.Jira;
using ClaudeQueue.Runs;
using ClaudeQueue.Tests.Fakes;
using ClaudeQueue.Verify;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Tests;

public sealed class QueueWorkerTests
{
    private static readonly TimeSpan Poll = TimeSpan.FromMilliseconds(200);

    private static (QueueWorker Worker, FakeJira Jira) Fixture()
    {
        var jira = new FakeJira();
        var ws = new FakeWorkspace();
        var options = Options.Create(new QueueOptions { PollInterval = Poll, ProgressInterval = TimeSpan.FromMilliseconds(20) });
        var progress = new LiveProgress(jira, ws, NullLogger<LiveProgress>.Instance);
        var runner = new PlanRunner(jira, ws, new FakeClaude(), new FakeVerifier(), new FakePullRequests(), progress, options, TimeProvider.System, NullLogger<PlanRunner>.Instance);
        return (new QueueWorker(jira, runner, options, NullLogger<QueueWorker>.Instance), jira);
    }

    private static async Task RunUntilAsync(QueueWorker worker, Func<bool> done)
    {
        await worker.StartAsync(default);
        var deadline = DateTime.UtcNow.AddSeconds(5);
        while (!done() && DateTime.UtcNow < deadline) await Task.Delay(10);
        await worker.StopAsync(default);
        Assert.True(done(), "condition not reached in time");
    }

    private static JiraCard Card(string key) => new(key, "Summary", "no block");

    [Fact] public async Task Card_whose_status_is_no_longer_ready_is_skipped()
    {
        var (worker, jira) = Fixture();
        jira.Status = "In Progress";
        jira.Ready.Enqueue(Card("SCRUM-1"));
        await worker.StartAsync(default);
        await Task.Delay(500);
        await worker.StopAsync(default);
        Assert.Empty(jira.Transitions);
        Assert.InRange(jira.ReadyPolls, 2, 6);
    }

    [Fact] public async Task Runs_cards_one_at_a_time_in_order_and_waits_poll_interval_after_a_fast_run()
    {
        var (worker, jira) = Fixture();
        jira.Ready.Enqueue(Card("SCRUM-1")); jira.Ready.Enqueue(Card("SCRUM-2"));
        var started = DateTime.UtcNow;
        await RunUntilAsync(worker, () => jira.Transitions.Count >= 4);
        Assert.Equal(
            [("SCRUM-1", "In Progress"), ("SCRUM-1", "In Review"), ("SCRUM-2", "In Progress"), ("SCRUM-2", "In Review")],
            jira.Transitions.Take(4));
        Assert.True(DateTime.UtcNow - started >= Poll, "worker did not wait PollInterval after a run shorter than it");
    }

    [Fact] public async Task Poll_failure_is_logged_and_loop_continues()
    {
        var (worker, jira) = Fixture();
        jira.FailNextPoll = true;
        jira.Ready.Enqueue(Card("SCRUM-1"));
        await RunUntilAsync(worker, () => jira.Transitions.Count >= 2);
        Assert.Equal("SCRUM-1", jira.Transitions[0].Key);
    }

    [Fact] public async Task Runner_exception_waits_poll_interval_and_continues()
    {
        var (worker, jira) = Fixture();
        jira.FailTransitionTo = "In Progress";
        jira.Ready.Enqueue(Card("SCRUM-1"));
        await worker.StartAsync(default);
        await Task.Delay(500);
        await worker.StopAsync(default);
        Assert.InRange(jira.ReadyPolls, 2, 6);
    }

    [Fact] public void Service_provider_resolves_worker_dependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<QueueOptions>(o => { o.JiraEmail = "a@b.c"; o.JiraApiToken = "t"; });
        services.AddHttpClient<IJira, JiraClient>();
        services.AddQueueServices();
        using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true });
        Assert.NotNull(ActivatorUtilities.CreateInstance<QueueWorker>(provider));
        Assert.IsType<GitWorkspace>(provider.GetRequiredService<IWorkspace>());
        Assert.IsType<ClaudeCli>(provider.GetRequiredService<IClaude>());
        Assert.IsType<PwshVerifier>(provider.GetRequiredService<IVerifier>());
        Assert.IsType<GhPullRequests>(provider.GetRequiredService<IPullRequests>());
    }

    [Theory]
    [InlineData("", "t")]
    [InlineData("a@b.c", "")]
    public void Missing_jira_credentials_fail_validation_naming_both_commands(string email, string token)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.Configure<QueueOptions>(o => { o.JiraEmail = email; o.JiraApiToken = token; });
        services.AddQueueServices();
        using var provider = services.BuildServiceProvider();
        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<QueueOptions>>().Value);
        var message = string.Join(" ", ex.Failures);
        Assert.Contains("dotnet user-secrets set Queue:JiraEmail <email> --project src/ClaudeQueue", message);
        Assert.Contains("dotnet user-secrets set Queue:JiraApiToken <token> --project src/ClaudeQueue", message);
    }
}
