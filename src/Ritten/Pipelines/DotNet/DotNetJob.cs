using Microsoft.Extensions.DependencyInjection;
using Ritten.CodeCoverage;
using Ritten.Core;
using Ritten.DotNet;
using Ritten.Reporting;

namespace Ritten.Pipelines.DotNet;

/// <summary>
/// What every plain .NET job shares.
/// </summary>
internal abstract class DotNetJob : Job<DotNetSettings>
{
    /// <inheritdoc />
    protected override void ConfigureServices(IServiceCollection services, DotNetSettings settings) => services
        .AddDotNet(settings.Build)
        .AddCoverage(settings.Coverage)
        .AddBuildReporting();
}
