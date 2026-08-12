using Hamelin;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NuGet.Versioning;
using Wolfe.Hamelin.Build.Models;
using Wolfe.Hamelin.Build.Steps;
using Wolfe.Hamelin.Build.Tests.Support;
using Wolfe.Hamelin.Commands;
using Wolfe.Hamelin.DotNet;

namespace Wolfe.Hamelin.Build.Tests.Steps;

public class CreateTagTests
{
    private readonly FakeCommandRunner _commands = new();
    private readonly IPipelineContext _context = Substitute.For<IPipelineContext>();
    private readonly BuildOptions _options = TestOptions.Build();

    public CreateTagTests()
    {
        _context.State.Get<Project>(Arg.Any<string>())
            .Returns(new Project { Name = "My.Package", Version = NuGetVersion.Parse("1.2.0") });
    }

    [Fact]
    public async Task SkipsWhenTheTagAlreadyExistsOnOrigin()
    {
        _commands.Respond(
            c => c.Arguments.Contains("ls-remote"),
            new CommandResult(0, "abc123\trefs/tags/v1.2.0\n", ""));

        await Step().Run(TestContext.Current.CancellationToken);

        _commands.Executed.ShouldHaveSingleItem().Arguments.ShouldContain("ls-remote");
    }

    [Fact]
    public async Task CreatesAndPushesTheTagWhenItDoesNotExist()
    {
        _commands.Respond(c => c.Arguments.Contains("rev-parse"), new CommandResult(1, "", ""));

        await Step().Run(TestContext.Current.CancellationToken);

        _commands.Executed.Single(c => c.Arguments is ["tag", ..]).Arguments.ShouldBe(["tag", "v1.2.0"]);
        _commands.Executed.Single(c => c.Arguments is ["push", ..]).Arguments.ShouldBe(["push", "origin", "v1.2.0"]);
    }

    [Fact]
    public async Task TagsTheConfiguredCommitSha()
    {
        _options.CommitSha = "abc123";
        _commands.Respond(c => c.Arguments.Contains("rev-parse"), new CommandResult(1, "", ""));

        await Step().Run(TestContext.Current.CancellationToken);

        _commands.Executed.Single(c => c.Arguments is ["tag", ..]).Arguments.ShouldBe(["tag", "v1.2.0", "abc123"]);
    }

    [Fact]
    public async Task PushesAnExistingLocalTagWithoutRecreatingIt()
    {
        _commands.Respond(c => c.Arguments.Contains("rev-parse"), new CommandResult(0, "abc123\n", ""));

        await Step().Run(TestContext.Current.CancellationToken);

        _commands.Executed.Any(c => c.Arguments is ["tag", ..]).ShouldBeFalse();
        _commands.Executed.Count(c => c.Arguments is ["push", ..]).ShouldBe(1);
    }

    private CreateTag Step() =>
        new(NullLogger<CreateTag>.Instance, Options.Create(_options), _context, _commands);
}
