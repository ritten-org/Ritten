using System.CommandLine;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Core.Runtimes;
using Ritten.GitHub;
using Ritten.Pipelines.DotNet;
using Ritten.Pipelines.DotNetPackage;
using Ritten.Pipelines.DotNetTool;

var pipelines = new PipelineRegistry()
    .Add(new DotNetToolPipeline())
    .Add(new DotNetPackagePipeline())
    .Add(new DotNetPipeline());

var runtimes = new RuntimeRegistry()
    .Add(new GitHubActionsRuntime());

var verbose = new Option<bool>($"--{PipelineArguments.Verbose}", "-v")
{
    Description = "Show every log entry in its highest detail.",
    Recursive = true
};

var quiet = new Option<bool>($"--{PipelineArguments.Quiet}", "-q")
{
    Description = "Show each step's outcome, but only failure detail.",
    Recursive = true
};

var autoApprove = new Option<bool>($"--{PipelineArguments.AutoApprove}")
{
    Description = "Approve a job up front, for runs with nobody there to confirm.",
    Recursive = true
};

var dryRun = new Option<bool>($"--{PipelineArguments.DryRun}")
{
    Description = "Rehearse the job without pushing, tagging, releasing, or commenting.",
    Recursive = true
};

var root = new RootCommand("The Ritten build pipeline.")
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
    command.SetAction((parseResult, cancellationToken) =>
    {
        // --verbose wins over --quiet: the more specific intent. A debug request from the
        // environment is the runtime's to honour, once one has been detected.
        var logLevel = parseResult.GetValue(verbose)
            ? PipelineLogLevel.Verbose
            : parseResult.GetValue(quiet)
                ? PipelineLogLevel.Warning
                : PipelineLogLevel.Detail;
        var args = new RunJobArgs(name)
        {
            LogLevel = logLevel,
            DryRun = parseResult.GetValue(dryRun),
            AutoApprove = parseResult.GetValue(autoApprove)
        };

        return PipelineHost.RunJob(pipelines, runtimes, args, cancellationToken);
    });
    return command;
}
