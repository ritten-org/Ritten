using System.CommandLine;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.GitHub;
using Ritten.Reporting;
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

var workflow = built.Value;

var verbose = new Option<bool>($"--{WorkflowArguments.Verbose}", "-v")
{
    Description = "Show every log entry in its highest detail.",
    Recursive = true
};

var quiet = new Option<bool>($"--{WorkflowArguments.Quiet}", "-q")
{
    Description = "Show each step's outcome, but only failure detail.",
    Recursive = true
};

var autoApprove = new Option<bool>($"--{WorkflowArguments.AutoApprove}")
{
    Description = "Approve a job up front, for runs with nobody there to confirm.",
    Recursive = true
};

var dryRun = new Option<bool>($"--{WorkflowArguments.DryRun}")
{
    Description = "Rehearse the job without pushing, tagging, releasing, or commenting.",
    Recursive = true
};

var root = new RootCommand("The Ritten build workflow.")
{
    verbose,
    quiet,
    dryRun,
    autoApprove,
    JobCommand("status", "Reports where the project stands: version, release state, and changelog."),
    JobCommand("build", "Compiles and tests, without any release checks."),
    JobCommand("check", "Checks a pull request: formatting, version, changelog, compile, tests, and pack."),
    JobCommand("deploy", "Checks, packs, tags, creates the GitHub release, and publishes to NuGet.")
};

return await root.Parse(args).InvokeAsync();

Command JobCommand(string name, string description)
{
    var command = new Command(name, description);
    command.SetAction(async (parseResult, cancellationToken) =>
    {
        var logLevel = parseResult.GetValue(verbose)
            ? WorkflowLogLevel.Verbose
            : parseResult.GetValue(quiet)
                ? WorkflowLogLevel.Warning
                : WorkflowLogLevel.Detail;
        var args = new RunJobArgs(name)
        {
            LogLevel = logLevel,
            DryRun = parseResult.GetValue(dryRun),
            AutoApprove = parseResult.GetValue(autoApprove)
        };

        return await workflow.Run(args, cancellationToken);
    });
    return command;
}
