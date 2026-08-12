using System.Text;
using Hamelin.FileSystem;
using NuGet.Versioning;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Tests.DotNet;

public class DotNetClientTests
{
    private readonly DotNetClient _client = new();

    [Fact]
    public async Task ReadProject_ExtractsThePackageIdAndVersion()
    {
        var file = ProjectFile(
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <PackageId>My.Package</PackageId>
                <Version>1.2.3-beta.1</Version>
              </PropertyGroup>
            </Project>
            """);

        var project = await _client.ReadProject(file, TestContext.Current.CancellationToken);

        project.Name.ShouldBe("My.Package");
        project.Version.ShouldBe(NuGetVersion.Parse("1.2.3-beta.1"));
    }

    [Fact]
    public async Task ReadProject_ThrowsWhenThePackageIdIsMissing()
    {
        var file = ProjectFile("<Project><PropertyGroup><Version>1.0.0</Version></PropertyGroup></Project>");

        var exception = await Should.ThrowAsync<Exception>(() => _client.ReadProject(file, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("PackageId");
    }

    [Fact]
    public async Task ReadProject_ThrowsWhenTheVersionIsMissing()
    {
        var file = ProjectFile("<Project><PropertyGroup><PackageId>My.Package</PackageId></PropertyGroup></Project>");

        var exception = await Should.ThrowAsync<Exception>(() => _client.ReadProject(file, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("Version");
    }

    private static IFile ProjectFile(string content)
    {
        var file = Substitute.For<IFile>();
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return file;
    }
}
