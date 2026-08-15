using Ritten.Git;

namespace Ritten.Tests.Git;

public class RepositoryUrlsTests
{
    [Theory]
    [InlineData("https://github.com/owner/repo", "https://github.com/owner/repo")]
    [InlineData("https://github.com/owner/repo/", "https://github.com/owner/repo")]
    [InlineData("https://github.com/owner/repo.git", "https://github.com/owner/repo")]
    [InlineData("git@github.com:owner/repo.git", "https://github.com/owner/repo")]
    [InlineData("ssh://git@github.com/owner/repo.git", "https://github.com/owner/repo")]
    public void ToWebUrl_NormalisesEveryCommonForm(string url, string expected)
    {
        RepositoryUrls.ToWebUrl(url).ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    public void ToWebUrl_ReturnsNullWhenThereIsNothingUsable(string? url)
    {
        RepositoryUrls.ToWebUrl(url).ShouldBeNull();
    }
}
