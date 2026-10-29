using ClaudeQueue.Claude;
using ClaudeQueue.Git;
using ClaudeQueue.Runs;
using ClaudeQueue.Verify;
using Microsoft.Extensions.Options;

namespace ClaudeQueue;

/// <summary>Dependency-injection wiring for the queue service.</summary>
public static class QueueServiceCollectionExtensions
{
    /// <summary>Registers the queue worker, its collaborators and startup validation of the Jira credentials. Jira client and options binding are registered by the host.</summary>
    /// <param name="services">Service collection.</param>
    public static IServiceCollection AddQueueServices(this IServiceCollection services)
    {
        services.AddOptions<QueueOptions>()
            .Validate(o => !string.IsNullOrWhiteSpace(o.JiraEmail) && !string.IsNullOrWhiteSpace(o.JiraApiToken),
                "Jira credentials are not configured. Run:\n"
                + "  dotnet user-secrets set Queue:JiraEmail <email> --project src/ClaudeQueue\n"
                + "  dotnet user-secrets set Queue:JiraApiToken <token> --project src/ClaudeQueue")
            .ValidateOnStart();

        services.AddSingleton<IWorkspace, GitWorkspace>();
        services.AddSingleton<IClaude, ClaudeCli>();
        services.AddSingleton<IVerifier, PwshVerifier>();
        services.AddSingleton<IPullRequests, GhPullRequests>();
        services.AddSingleton<LiveProgress>();
        services.AddSingleton<PlanRunner>();
        services.AddSingleton(TimeProvider.System);
        services.AddHostedService<QueueWorker>();
        return services;
    }
}
