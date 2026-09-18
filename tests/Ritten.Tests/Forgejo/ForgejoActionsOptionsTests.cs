using Ritten.Forgejo;

namespace Ritten.Tests.Forgejo;

public class ForgejoActionsOptionsTests
{
    private static ForgejoActionsOptions Configured(Dictionary<string, string> environment)
    {
        var options = new ForgejoActionsOptions();
        ForgejoActionsOptions.ConfigureFromEnvironment(options, name => environment.GetValueOrDefault(name));
        return options;
    }

    [Fact]
    public void RunUrl_IsTheRunsPageByNumberNotId()
    {
        var options = Configured(new Dictionary<string, string>
        {
            ["FORGEJO_SERVER_URL"] = "https://code.example.com/",
            ["FORGEJO_REPOSITORY"] = "tom/lab",
            ["FORGEJO_RUN_NUMBER"] = "42",
            ["FORGEJO_RUN_ID"] = "9001"
        });

        options.RunUrl.ShouldBe("https://code.example.com/tom/lab/actions/runs/42");
        options.RunId.ShouldBe(9001);
    }

    [Fact]
    public void ReadsTheGitHubMirrorWhenTheNativeNameIsAbsent()
    {
        var options = Configured(new Dictionary<string, string>
        {
            ["GITHUB_SERVER_URL"] = "https://code.example.com",
            ["GITHUB_REPOSITORY"] = "tom/lab",
            ["GITHUB_RUN_NUMBER"] = "7",
            ["GITHUB_WORKFLOW"] = "deploy"
        });

        options.RunUrl.ShouldBe("https://code.example.com/tom/lab/actions/runs/7");
        options.Workflow.ShouldBe("deploy");
    }

    [Fact]
    public void RunUrl_IsNullOutsideARun()
    {
        Configured([]).RunUrl.ShouldBeNull();
    }
}
