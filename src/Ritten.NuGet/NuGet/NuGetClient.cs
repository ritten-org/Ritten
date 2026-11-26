using System.Text.Json;
using NuGet.Versioning;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;

namespace Ritten.NuGet;

internal class NuGetClient(ICommandRunner commands, IFileSystem fileSystem) : INuGet
{
    // NuGet's own HTTP cache answers for a feed it read within the half hour, and a release decides from this answer
    // whether to push: each read starts from a cache of its own, empty.
    private static readonly EnvironmentVariableName HttpCache = EnvironmentVariableName.From("NUGET_HTTP_CACHE_PATH");

    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<NuGetVersion>> GetPublishedVersions(NuGetFeed feed, string packageId, CancellationToken cancellationToken = default)
    {
        var cache = fileSystem.CreateTempDirectory("ritten-nuget-cache-");
        try
        {
            var result = await commands.Run(Command.Create("dotnet")
                .WithArguments("package", "search", packageId, "--exact-match", "--prerelease", "--format", "json", "--source", feed.Url)
                .WithEnvironmentVariables(EnvironmentVariables.Empty.With(HttpCache, cache.AbsolutePath))
                .WithEffect(CommandEffect.None)
                .QuietOutput()
                .Describe($"read {packageId}'s versions on {feed.Url}")
                .ThrowOnError(), cancellationToken);

            return Versions(result.StandardOutput, packageId);
        }
        finally
        {
            cache.Delete();
        }
    }

    // `dotnet package search --format json`: each source's packages, each version of an exact match its own entry.
    internal static IReadOnlyList<NuGetVersion> Versions(string json, string packageId)
    {
        var found = JsonSerializer.Deserialize<SearchOutput>(json, Json)
            ?? throw new InvalidOperationException("dotnet package search wrote no result.");
        if (found.Problems is { Count: > 0 } problems)
        {
            throw new InvalidOperationException($"dotnet package search could not read every source: {string.Join(" ", problems.Select(problem => problem.GetRawText()))}");
        }

        return [.. found.SearchResult
            .SelectMany(source => source.Packages)
            .Where(package => string.Equals(package.Id, packageId, StringComparison.OrdinalIgnoreCase) && package.Version is { Length: > 0 })
            .Select(package => NuGetVersion.Parse(package.Version ?? ""))
            .Distinct()
            .Order()];
    }

    public async Task Push(NuGetFeed feed, IFile package, CancellationToken cancellationToken = default)
    {
        var command = Command
            .Create("dotnet")
            .WithArguments("nuget", "push", package.AbsolutePath)
            .AndArguments("--source", feed.Url)
            .AndArguments("--skip-duplicate")
            .Describe($"push {package.Name} to {feed.Url}");

        if (feed.ApiKey is not null)
        {
            command = command.AndArguments("--api-key", feed.ApiKey).RedactArguments();
        }

        await commands.Run(command.ThrowOnError(), cancellationToken);
    }
}
