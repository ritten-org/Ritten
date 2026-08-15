using System.CommandLine;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Pipelines.DotNet;
using Ritten.Runtimes.GitHubActions;

var verbose = new Option<bool>($"--{PipelineArguments.Verbose}", "-v")
{
    Description = "Show every log entry in its highest detail.",
    Recursive = true
};

var quiet = new Option<bool>($"--{PipelineArguments.Quiet}", "-q")
{
    Description = "Show only failures.",
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
    Job("build", "Compiles and tests, without any release validation."),
    Job("check", "Validates a pull request: formatting, version, changelog, compile, tests, and pack."),
    Job("deploy", "Validates, packs, tags, creates the GitHub release, and publishes to NuGet.")
};

return await root.Parse(args).InvokeAsync();

Command Job(string name, string description)
{
    var command = new Command(name, description);
    command.SetAction((parseResult, cancellationToken) =>
    {
        // Re-running with debug logging is an in-the-moment request to see more, so it outranks
        // a --quiet that's been sitting in a workflow file since whenever. --verbose wins over
        // --quiet for the same reason: the more specific intent.
        var logLevel = parseResult.GetValue(verbose) || GitHubEnvironment.IsDebug()
            ? PipelineLogLevel.Verbose
            : parseResult.GetValue(quiet)
                ? PipelineLogLevel.Warning
                : PipelineLogLevel.Detail;
        return PipelineHost.Run<DotNetToolPipeline, DotNetToolSettings>(
            name,
            logLevel,
            parseResult.GetValue(dryRun),
            parseResult.GetValue(autoApprove),
            cancellationToken);
    });
    return command;
}
