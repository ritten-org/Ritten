using System.CommandLine;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Pipelines.DotNet;

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
    Job("build", "Validates a pull request: formatting, version, changelog, compile, and tests."),
    Job("verify", "Compiles and tests, without any release validation."),
    Job("deploy", "Validates, packs, tags, creates the GitHub release, and publishes to NuGet.")
};

return await root.Parse(args).InvokeAsync();

Command Job(string name, string description)
{
    var command = new Command(name, description);
    command.SetAction((parseResult, cancellationToken) =>
    {
        // --verbose wins if both are given: someone asking to see more has the more specific intent.
        var logLevel = parseResult.GetValue(verbose)
            ? PipelineLogLevel.Verbose
            : parseResult.GetValue(quiet)
                ? PipelineLogLevel.Warning
                : PipelineLogLevel.Detail;
        return PipelineHost.Run<DotNetPackagePipeline, DotNetPackageSettings>(
            name,
            logLevel,
            parseResult.GetValue(dryRun),
            parseResult.GetValue(autoApprove),
            cancellationToken);
    });
    return command;
}
