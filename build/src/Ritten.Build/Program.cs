using System.CommandLine;
using Hamelin;
using Hamelin.Runtimes.GitHubActions;
using Ritten.Extensions;

var build = new Command("build", "Validates a pull request: formatting, version, changelog, compile, and tests.");
build.SetAction((_, cancellationToken) => RunPipeline(p => p.UseDotNetPackageBuild(), cancellationToken));

var verify = new Command("verify", "Compiles and tests, without any release validation.");
verify.SetAction((_, cancellationToken) => RunPipeline(p => p.UseDotNetPackageVerify(), cancellationToken));

var deploy = new Command("deploy", "Validates, packs, tags, creates the GitHub release, and publishes to NuGet.");
deploy.SetAction((_, cancellationToken) => RunPipeline(p => p.UseDotNetPackageDeploy(), cancellationToken));

var root = new RootCommand("The Ritten build pipeline.") { build, verify, deploy };
return await root.Parse(args).InvokeAsync();

static Task<int> RunPipeline(Func<PipelineApplication, PipelineApplication> compose, CancellationToken cancellationToken)
{
    var builder = PipelineApplication.CreateBuilder();

    builder.Services
        .AddGitHub("Ritten.Build")
        .AddGitHubActionsRuntime()
        .AddDotNetPackagePipeline();

    return compose(builder.Build()).RunWithExitCodeAsync(cancellationToken);
}
