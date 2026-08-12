using Microsoft.Extensions.Logging.Abstractions;
using NuGet.Packaging;
using NuGet.Versioning;
using Wolfe.Hamelin.NuGet;

namespace Wolfe.Hamelin.Tests.NuGet;

public class NuGetClientTests
{
    private readonly NuGetClient _client = new(NullLogger<NuGetClient>.Instance);

    [Fact]
    public async Task GetPublishedVersions_ReturnsVersionsInAscendingOrder()
    {
        using var feed = LocalFeed.Create();
        feed.AddPackage("Test.Package", "1.0.0");
        feed.AddPackage("Test.Package", "0.9.0");
        feed.AddPackage("Test.Package", "1.1.0-beta.1");
        feed.AddPackage("Other.Package", "5.0.0");

        var versions = await _client.GetPublishedVersions(new NuGetFeed(feed.Root), "Test.Package", TestContext.Current.CancellationToken);

        versions.Select(v => v.ToString()).ShouldBe(["0.9.0", "1.0.0", "1.1.0-beta.1"]);
    }

    [Fact]
    public async Task GetPublishedVersions_ReturnsEmptyForAnUnpublishedPackage()
    {
        using var feed = LocalFeed.Create();
        feed.AddPackage("Other.Package", "5.0.0");

        var versions = await _client.GetPublishedVersions(new NuGetFeed(feed.Root), "Test.Package", TestContext.Current.CancellationToken);

        versions.ShouldBeEmpty();
    }

    [Fact]
    public void WithCredentials_SetsTheUsernameAndPassword()
    {
        var feed = new NuGetFeed("https://example.com/index.json").WithCredentials("user", "token");

        feed.Username.ShouldBe("user");
        feed.Password.ShouldBe("token");
    }

    /// <summary>
    /// A temporary directory of real .nupkg files, readable as a NuGet local folder feed.
    /// </summary>
    private sealed class LocalFeed : IDisposable
    {
        public required string Root { get; init; }

        public static LocalFeed Create() =>
            new() { Root = Directory.CreateTempSubdirectory("wolfe-hamelin-feed-").FullName };

        public void AddPackage(string id, string version)
        {
            var contentFile = Path.Combine(Root, "dummy.txt");
            File.WriteAllText(contentFile, "dummy");

            var builder = new PackageBuilder
            {
                Id = id,
                Version = NuGetVersion.Parse(version),
                Description = "A test package."
            };
            builder.Authors.Add("Wolfe.Hamelin.Tests");
            builder.Files.Add(new PhysicalPackageFile { SourcePath = contentFile, TargetPath = "content/dummy.txt" });

            using var stream = File.Create(Path.Combine(Root, $"{id}.{version}.nupkg"));
            builder.Save(stream);
        }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
