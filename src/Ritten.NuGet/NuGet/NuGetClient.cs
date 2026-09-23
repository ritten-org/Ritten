using NuGet.Configuration;
using NuGet.Protocol;
using NuGet.Protocol.Core.Types;
using NuGet.Versioning;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Reporting;

namespace Ritten.NuGet;

internal class NuGetClient(IWorkflowLog log, ICommandRunner commands) : INuGet
{
    public async Task<IReadOnlyList<NuGetVersion>> GetPublishedVersions(NuGetFeed feed, string packageId, CancellationToken cancellationToken = default)
    {
        var source = new PackageSource(feed.Url)
        {
            Credentials = new PackageSourceCredential(feed.Url, feed.Username ?? "dummy", feed.Password ?? "", isPasswordClearText: true, validAuthenticationTypesText: null)
        };

        var repository = Repository.Factory.GetCoreV3(source);
        var resource = await repository.GetResourceAsync<FindPackageByIdResource>(cancellationToken)
            ?? throw new InvalidOperationException($"The feed '{feed.Url}' does not support package lookup.");

        using var cache = new SourceCacheContext { NoCache = true };
        var versions = await resource.GetAllVersionsAsync(packageId, cache, log.ForNuGet(), cancellationToken);
        return [.. versions.OrderBy(v => v)];
    }

    public async Task Push(NuGetFeed feed, IFile package, CancellationToken cancellationToken = default)
    {
        var command = Command
            .Create("dotnet")
            .WithArguments("nuget", "push", package.AbsolutePath)
            .AndArguments("--source", feed.Url)
            .AndArguments("--skip-duplicate");

        if (feed.ApiKey is not null)
        {
            command = command.AndArguments("--api-key", feed.ApiKey).RedactArguments();
        }

        await commands.Run(command.ThrowOnError(), cancellationToken);
    }
}
