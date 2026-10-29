using ClaudeQueue;
using ClaudeQueue.Jira;
using ClaudeQueue.Logging;
using Microsoft.Extensions.Logging.Console;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddConsole(o => o.FormatterName = TimestampConsoleFormatter.FormatterName);
builder.Logging.AddConsoleFormatter<TimestampConsoleFormatter, ConsoleFormatterOptions>();

builder.Services.Configure<QueueOptions>(builder.Configuration.GetSection(QueueOptions.SectionName));
builder.Services.PostConfigure<QueueOptions>(o =>
    o.RunsDirectory = Environment.ExpandEnvironmentVariables(o.RunsDirectory));

builder.Services.AddHttpClient<IJira, JiraClient>().AddStandardResilienceHandler();

var host = builder.Build();
host.Run();
