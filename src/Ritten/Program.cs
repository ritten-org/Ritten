using System.CommandLine;
using Ritten.CommandLine;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Forgejo;
using Ritten.GitHub;
using Ritten.OpenTelemetry;
using Ritten.Workflows;
using Wolfe.CommandLine;
using Wolfe.CommandLine.Completions;

var builder = WorkflowApplication.CreateBuilder();

builder.AddDotNetWorkflows();

builder.Runtimes
    .Add<ForgejoActionsRuntime>()
    .Add<GitHubActionsRuntime>();

builder.AddOpenTelemetry();

var built = builder.Build();
if (built.IsError)
{
    return ExitCode.ConfigurationError;
}

await using var application = built.Value;

var root = new RootCommand("The Ritten build workflow.")
    .AddCompletions("ritten");
await root.InstallRitten(application);
await CompletionAutoInstall.Run("ritten", args);

return await root.Parse(args).InvokeAsync();
