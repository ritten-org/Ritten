using System.CommandLine;
using Ritten.Contracts;
using Ritten.Core;
using Ritten.Pipelines.DotNet;

var verbose = new Option<bool>("--verbose", "-v")
{
    Description = "Show every log entry in its highest detail.",
    Recursive = true
};

var quiet = new Option<bool>("--quiet", "-q")
{
    Description = "Show only failures.",
    Recursive = true
};

var root = new RootCommand("The Ritten build pipeline.")
{
    verbose,
    quiet,
    Job("build", "Validates a pull request: formatting, version, changelog, compile, and tests."),
    Job("verify", "Compiles and tests, without any release validation."),
    Job("deploy", "Validates, packs, tags, creates the GitHub release, and publishes to NuGet.")
};

return await root.Parse(args).InvokeAsync();

Command Job(string name, string description)
{
    var command = new Command(name, description);
    command.SetAction((parseResult, cancellationToken) =>
        PipelineHost.Run<DotNetPackagePipeline, DotNetPackageSettings>(name, LogLevel(parseResult), cancellationToken));
    return command;
}

// --verbose wins if both are given: someone asking to see more has the more specific intent.
PipelineLogLevel LogLevel(ParseResult parseResult) => parseResult.GetValue(verbose)
    ? PipelineLogLevel.Verbose
    : parseResult.GetValue(quiet)
        ? PipelineLogLevel.Warning
        : PipelineLogLevel.Detail;
