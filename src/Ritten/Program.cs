using System.CommandLine;
using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Engine.Workflows;
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

var application = built.Value;

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
    autoApprove
};

// Commands are dynamic based on the current project's workflow.
foreach (var job in await application.ResolveJobs(Environment.CurrentDirectory))
{
    root.Add(JobCommand(job));
}

return await root.Parse(args).InvokeAsync();

Command JobCommand(IJob job)
{
    var command = new Command(job.Name, job.Description);
    var options = job.Arguments.ToDictionary(
        input => input,
        Option (input) => input.TakesValue
            ? new Option<string>($"--{input.Name}") { Description = input.Description, Required = input.Required }
            : new Option<bool>($"--{input.Name}") { Description = input.Description });

    foreach (var option in options.Values)
    {
        command.Options.Add(option);
    }

    command.SetAction(async (parseResult, cancellationToken) =>
    {
        var logLevel = parseResult.GetValue(verbose)
            ? WorkflowLogLevel.Verbose
            : parseResult.GetValue(quiet)
                ? WorkflowLogLevel.Warning
                : WorkflowLogLevel.Detail;

        var args = new RunJobArgs(job.Name)
        {
            LogLevel = logLevel,
            DryRun = parseResult.GetValue(dryRun),
            AutoApprove = parseResult.GetValue(autoApprove),
            Arguments = Supplied(parseResult, options)
        };

        return await application.Run(args, cancellationToken);
    });

    return command;
}

static Dictionary<string, string?> Supplied(ParseResult parseResult, IReadOnlyDictionary<JobArgument, Option> options)
{
    Dictionary<string, string?> supplied = [];
    foreach (var (input, option) in options)
    {
        supplied[input.Name] = option switch
        {
            Option<string> value when parseResult.GetValue(value) is { } text => text,
            Option<bool> flag when parseResult.GetValue(flag) => "",
            _ => supplied[input.Name]
        };
    }

    return supplied;
}
