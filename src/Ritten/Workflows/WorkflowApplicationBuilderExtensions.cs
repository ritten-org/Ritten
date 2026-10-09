using Ritten.Changelogs;
using Ritten.DotNet;
using Ritten.Engine;
using Ritten.Git;
using Ritten.GitHub;
using Ritten.Init;
using Ritten.NuGet;
using Ritten.Reporting;
using Ritten.Workflows.DotNet;
using Ritten.Workflows.DotNetPackage;
using Ritten.Workflows.DotNetTool;

namespace Ritten.Workflows;

/// <summary>
/// Registers the .NET workflows.
/// </summary>
public static class WorkflowApplicationBuilderExtensions
{
    extension(WorkflowApplicationBuilder builder)
    {
        /// <summary>
        /// Adds the .NET workflows, most specific first so a repository is recognised as the closest match, and every
        /// client their jobs use.
        /// </summary>
        public WorkflowApplicationBuilder AddDotNetWorkflows()
        {
            builder.Workflows
                .Add<DotNetToolWorkflow>()
                .Add<DotNetPackageWorkflow>()
                .Add<DotNetWorkflow>();

            builder
                .AddChangelogs()
                .AddDotNet()
                .AddGit()
                .AddNuGet()
                .AddGitHubClient()
                .AddGitHubActions()
                .AddBuildReporting()
                .AddInit(RittenTool.Pin);
            return builder;
        }
    }
}
