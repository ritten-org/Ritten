using System.CommandLine;
using Ritten.Core;
using Ritten.Pipelines.DotNet;

var build = new Command("build", "Validates a pull request: formatting, version, changelog, compile, and tests.");
build.SetAction((_, cancellationToken) => RittenApplication.Run<DotNetPackageBuild>(cancellationToken));

var verify = new Command("verify", "Compiles and tests, without any release validation.");
verify.SetAction((_, cancellationToken) => RittenApplication.Run<DotNetPackageVerify>(cancellationToken));

var deploy = new Command("deploy", "Validates, packs, tags, creates the GitHub release, and publishes to NuGet.");
deploy.SetAction((_, cancellationToken) => RittenApplication.Run<DotNetPackageDeploy>(cancellationToken));

var root = new RootCommand("The Ritten build pipeline.") { build, verify, deploy };
return await root.Parse(args).InvokeAsync();
