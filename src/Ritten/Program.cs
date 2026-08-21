using System.CommandLine;
using Ritten.CommandLine;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.GitHub;
using Ritten.Workflows.DotNet;
using Ritten.Workflows.DotNetPackage;
using Ritten.Workflows.DotNetTool;

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

var application = built.Value;

var root = new RootCommand("The Ritten build workflow.");
await root.InstallRitten(application);

return await root.Parse(args).InvokeAsync();
