using Microsoft.Extensions.DependencyInjection;
using Ritten.Core.Runtimes;

namespace Ritten.Tests.Core.Runtimes;

public class RuntimeRegistryTests
{
    [Fact]
    public void Detect_FallsBackToLocalWhenNothingMatches()
    {
        var registry = new RuntimeRegistry().Add(Runtime("ci", markers: ["CI_MARKER"]));

        var selection = registry.Detect(Env());

        selection.IsSuccess.ShouldBeTrue();
        selection.Value.Runtime.Name.ShouldBe("local");
    }

    [Fact]
    public void Detect_SelectsTheRuntimeWhoseMarkerIsPresent()
    {
        var registry = new RuntimeRegistry()
            .Add(Runtime("one", markers: ["ONE_MARKER"]))
            .Add(Runtime("two", markers: ["TWO_MARKER"]));

        var selection = registry.Detect(Env(("TWO_MARKER", "true")));

        selection.IsSuccess.ShouldBeTrue();
        selection.Value.Runtime.Name.ShouldBe("two");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Detect_LetsTheClaimantOutrankTheRuntimeItImpersonates(bool reverseRegistration)
    {
        // A compatible runner defines the other system's variables, so both candidates match; the
        // one whose claims explain the other's evidence wins, whatever order they were registered.
        var impersonated = Runtime("impersonated", markers: ["THEIR_MARKER"]);
        var claimant = Runtime("claimant", markers: ["MY_MARKER"], claims: ["MY_MARKER", "THEIR_MARKER"]);
        var registry = reverseRegistration
            ? new RuntimeRegistry().Add(claimant).Add(impersonated)
            : new RuntimeRegistry().Add(impersonated).Add(claimant);

        var selection = registry.Detect(Env(("THEIR_MARKER", "true"), ("MY_MARKER", "true")));

        selection.IsSuccess.ShouldBeTrue();
        selection.Value.Runtime.Name.ShouldBe("claimant");
    }

    [Fact]
    public void Detect_SelectsTheImpersonatedRuntimeOnItsOwnTurf()
    {
        var registry = new RuntimeRegistry()
            .Add(Runtime("impersonated", markers: ["THEIR_MARKER"]))
            .Add(Runtime("claimant", markers: ["MY_MARKER"], claims: ["MY_MARKER", "THEIR_MARKER"]));

        var selection = registry.Detect(Env(("THEIR_MARKER", "true")));

        selection.IsSuccess.ShouldBeTrue();
        selection.Value.Runtime.Name.ShouldBe("impersonated");
    }

    [Fact]
    public void Detect_FailsWhenTwoUnrelatedRuntimesMatch()
    {
        var registry = new RuntimeRegistry()
            .Add(Runtime("one", markers: ["ONE_MARKER"]))
            .Add(Runtime("two", markers: ["TWO_MARKER"]));

        var selection = registry.Detect(Env(("ONE_MARKER", "true"), ("TWO_MARKER", "true")));

        selection.IsError.ShouldBeTrue();
        selection.Errors.ShouldHaveSingleItem().Message.ShouldBe("Runtime detection is ambiguous between: one, two.");
    }

    [Fact]
    public void Detect_FailsWhenTwoRuntimesClaimEachOther()
    {
        // Mutual claims mean neither can outrank the other; guessing would run the wrong
        // runtime's side effects, so the model refuses instead.
        var registry = new RuntimeRegistry()
            .Add(Runtime("one", markers: ["ONE_MARKER"], claims: ["ONE_MARKER", "TWO_MARKER"]))
            .Add(Runtime("two", markers: ["TWO_MARKER"], claims: ["TWO_MARKER", "ONE_MARKER"]));

        var selection = registry.Detect(Env(("ONE_MARKER", "true"), ("TWO_MARKER", "true")));

        selection.IsError.ShouldBeTrue();
        selection.Errors.ShouldHaveSingleItem().Message.ShouldContain("ambiguous");
    }

    [Fact]
    public void Detect_HidesTheSelectedRuntimesClaimsFromTheEnvironment()
    {
        var registry = new RuntimeRegistry().Add(Runtime("ci", markers: ["CI_MARKER"], claims: ["CI_MARKER", "CI_TOKEN"]));

        var selection = registry.Detect(Env(("CI_MARKER", "true"), ("CI_TOKEN", "secret"), ("UNRELATED", "kept")));

        selection.IsSuccess.ShouldBeTrue();
        selection.Value.Environment("CI_MARKER").ShouldBeNull();
        selection.Value.Environment("CI_TOKEN").ShouldBeNull();
        selection.Value.Environment("UNRELATED").ShouldBe("kept");
    }

    [Fact]
    public void Detect_LeavesTheEnvironmentUntouchedForTheLocalFallback()
    {
        var selection = new RuntimeRegistry().Detect(Env(("ANYTHING", "kept")));

        selection.IsSuccess.ShouldBeTrue();
        selection.Value.Environment("ANYTHING").ShouldBe("kept");
    }

    [Fact]
    public void Validate_FlagsDuplicateNames()
    {
        var registry = new RuntimeRegistry()
            .Add(Runtime("ci", markers: ["ONE_MARKER"]))
            .Add(Runtime("CI", markers: ["TWO_MARKER"]));

        var errors = registry.Validate();

        errors.ShouldHaveSingleItem().Message.ShouldBe("Two runtimes are registered under the name 'ci'.");
    }

    [Fact]
    public void Validate_FlagsAMarkerTheRuntimeDoesNotClaim()
    {
        // An unclaimed marker would survive into the filtered environment, ready to be misread by
        // whatever consumes it next.
        var registry = new RuntimeRegistry().Add(Runtime("ci", markers: ["CI_MARKER"], claims: []));

        var errors = registry.Validate();

        errors.ShouldHaveSingleItem().Message.ShouldBe("The ci runtime detects on 'CI_MARKER' but doesn't claim it.");
    }

    private static FakeRuntime Runtime(string name, string[] markers, string[]? claims = null) =>
        new(name, markers, claims ?? markers);

    private static Func<string, string?> Env(params (string Name, string Value)[] variables) =>
        variables.ToDictionary(v => v.Name, v => v.Value).GetValueOrDefault;

    private sealed class FakeRuntime(string name, string[] markers, string[] claims) : Runtime
    {
        public override string Name => name;

        public override IReadOnlyCollection<string> Markers => markers;

        public override IReadOnlyCollection<string> Claims => claims;

        public override void ConfigureServices(IServiceCollection services, Func<string, string?> environment)
        {
        }
    }
}
