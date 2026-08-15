using Ritten.GitHub;

namespace Ritten.Tests.GitHub;

public class RepositoryPathTests
{
    [Theory]
    [InlineData("https://github.com/example/repo")]
    [InlineData("https://github.com/example/repo/")]
    [InlineData("https://github.com/example/repo.git")]
    [InlineData("http://ghe.example.corp/example/repo")]
    public void Parse_ReadsTheOwnerAndNameFromAWebUrl(string url)
    {
        RepositoryPath.Parse(url).ShouldBe(new RepositoryPath("example", "repo"));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not a url")]
    [InlineData("git@github.com:example/repo.git")]
    [InlineData("https://github.com/example")]
    [InlineData("https://github.com/example/repo/tree/main")]
    [InlineData("https://github.com//repo")]
    public void Parse_IsNullForAnythingElse(string? url)
    {
        RepositoryPath.Parse(url).ShouldBeNull();
    }

    [Fact]
    public void ToString_IsTheApiSlug()
    {
        new RepositoryPath("example", "repo").ToString().ShouldBe("example/repo");
    }
}
