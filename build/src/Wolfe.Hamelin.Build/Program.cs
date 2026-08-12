using Hamelin;
using Hamelin.Runtimes.GitHubActions;
using Microsoft.Extensions.DependencyInjection;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Steps;
using Wolfe.Hamelin.Extensions;
using Version = Wolfe.Hamelin.Build.Steps.Version;

var builder = PipelineApplication.CreateBuilder(args);

builder.Services
    .AddCommandRunner()
    .AddChangelogs()
    .AddDotNet()
    .AddGit()
    .AddNuGet()
    .AddGitHubActionsRuntime()
    .AddStepsFromAssemblyContaining<Program>()
    .AddBuildReporting("Wolfe.Hamelin.Build");

builder.Services.AddOptions<BuildOptions>()
    .BindConfiguration("Build")
    .Validate(b => !string.IsNullOrEmpty(b.ArtifactsDirectory))
    .Validate(b => !string.IsNullOrEmpty(b.TempDirectory))
    .Validate(b => !string.IsNullOrEmpty(b.Configuration))
    .Validate(b => !string.IsNullOrEmpty(b.ProjectFile))
    .ValidateOnStart();

builder.Services.AddOptions<ChangelogOptions>()
    .BindConfiguration("Changelog")
    .Validate(c => !string.IsNullOrEmpty(c.File))
    .ValidateOnStart();

builder.Services.AddOptions<NuGetOptions>()
    .BindConfiguration("NuGet")
    .Validate(n => !string.IsNullOrEmpty(n.Feed))
    .ValidateOnStart();

builder.Services.AddOptions<ReleaseOptions>()
    .BindConfiguration("Release");

var pipeline = builder.Build();
return args switch
{
    ["build"] => pipeline
        .UseStep<Clean>()
        .UseStep<Format>()
        .UseStep<ExtractProject>()
        .UseStep<Version>()
        .UseStep<Changelog>()
        .UseStep<Restore>()
        .UseStep<Build>()
        .UseStep<Test>()
        .RunWithExitCode(),
    ["verify"] => pipeline
        .UseStep<Clean>()
        .UseStep<Format>()
        .UseStep<Restore>()
        .UseStep<Build>()
        .UseStep<Test>()
        .RunWithExitCode(),
    ["deploy"] => pipeline
        .UseStep<Clean>()
        .UseStep<ExtractProject>()
        .UseStep<Version>()
        .UseStep<Changelog>()
        .UseStep<Restore>()
        .UseStep<Build>()
        .UseStep<Test>()
        .UseStep<Pack>()
        .UseStep<CreateTag>()
        .UseStep<CreateRelease>()
        .UseStep<Publish>()
        .RunWithExitCode(),
    _ => Help()
};

static int Help()
{
    Console.Error.WriteLine("Usage: <build|verify|deploy>");
    return 1;
}
