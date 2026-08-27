using System.CommandLine;
using Ritten.CommandLine;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.GitHub;
using Ritten.Workflows.DotNet;
using Ritten.Workflows.DotNetPackage;
using Ritten.Workflows.DotNetTool;
using Wolfe.CommandLine;
using Wolfe.CommandLine.Completions;

var builder = WorkflowApplication.CreateBuilder();

builder.Workflows
    .Add<DotNetToolWorkflow>()
    .Add<DotNetPackageWorkflow>()
    .Add<DotNetWorkflow>();

builder.Runtimes
    .Add<GitHubActionsRuntime>();

var built = builder.Build();
if (built.IsError)
{
    return ExitCode.ConfigurationError;
}

var root = new RootCommand("The Ritten build workflow.")
    .AddCompletions("ritten");
await root.InstallRitten(built.Value);
await CompletionAutoInstall.Run("ritten", args);

return await root.Parse(args).InvokeAsync();
