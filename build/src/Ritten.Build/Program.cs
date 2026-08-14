using System.CommandLine;
using Ritten.Core;
using Ritten.Pipelines.DotNet;

var root = new RootCommand("The Ritten build pipeline.")
{
    Job("build", "Validates a pull request: formatting, version, changelog, compile, and tests."),
    Job("verify", "Compiles and tests, without any release validation."),
    Job("deploy", "Validates, packs, tags, creates the GitHub release, and publishes to NuGet.")
};

return await root.Parse(args).InvokeAsync();

Command Job(string name, string description)
{
    var command = new Command(name, description);
    command.SetAction((_, cancellationToken) => PipelineHost.Run<DotNetPackagePipeline, DotNetPackageSettings>(name, cancellationToken));
    return command;
}
