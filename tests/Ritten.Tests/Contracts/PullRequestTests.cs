using Ritten.Contracts;

namespace Ritten.Tests.Contracts;

public class PullRequestTests
{
    [Fact]
    public void ReviewsNothingUntilARuntimeSaysOtherwise()
    {
        // The unconfigured default is the escape hatch: on a runtime that knows nothing about
        // pull requests, a step sees "not reviewing one" rather than a missing registration.
        var pullRequest = new PullRequest();

        pullRequest.IsPullRequest.ShouldBeFalse();
        pullRequest.Number.ShouldBeNull();
        pullRequest.BaseRef.ShouldBeNull();
    }

    [Fact]
    public void ReviewsAPullRequestWhenNumbered()
    {
        new PullRequest { Number = 42 }.IsPullRequest.ShouldBeTrue();
    }
}
