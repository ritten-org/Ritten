using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Contracts.FileSystem;

namespace Ritten.Tests.Commands;

public class CommandRunnerTests
{
    [Fact]
    public async Task Run_CapturesStandardOutput()
    {
        var result = await Runner().Run(Shell("printf 'one\ntwo\n'"), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(0);
        result.IsSuccess.ShouldBeTrue();
        result.StandardOutput.ShouldBe("one\ntwo\n");
    }

    [Fact]
    public async Task Run_CapturesStandardError()
    {
        var result = await Runner().Run(Shell("echo oops >&2"), TestContext.Current.CancellationToken);

        result.StandardError.ShouldBe("oops\n");
    }

    [Fact]
    public async Task Run_ReturnsANonZeroExitCodeWithoutThrowing()
    {
        var result = await Runner().Run(Shell("exit 4"), TestContext.Current.CancellationToken);

        result.ExitCode.ShouldBe(4);
        result.IsError.ShouldBeTrue();
    }

    [Fact]
    public async Task Run_ThrowsWhenTheCommandOptsIn()
    {
        var command = Shell("echo boom >&2; exit 1").ThrowOnError();

        var exception = await Should.ThrowAsync<CommandFailedException>(() => Runner().Run(command, TestContext.Current.CancellationToken));

        exception.Message.ShouldContain("exited with code 1");
        exception.Message.ShouldContain("boom");
        exception.Result.ExitCode.ShouldBe(1);
    }

    [Fact]
    public async Task Run_OmitsRedactedOutputFromTheFailureMessage()
    {
        var command = Shell("echo boom >&2; exit 1").RedactOutput().ThrowOnError();

        var exception = await Should.ThrowAsync<CommandFailedException>(() => Runner().Run(command, TestContext.Current.CancellationToken));

        exception.Message.ShouldNotContain("boom");
        exception.Result.StandardError.ShouldContain("boom");
    }

    [Fact]
    public async Task Run_PipesStandardInput()
    {
        var result = await Runner().Run(Command.Create("cat").WithInput("hello"), TestContext.Current.CancellationToken);

        result.StandardOutput.ShouldBe("hello\n");
    }

    [Fact]
    public async Task Run_PassesEnvironmentVariables()
    {
        var command = Shell("printf '%s\n' \"$WOLFE_HAMELIN_TEST\"")
            .WithEnvironmentVariables(new Dictionary<string, string> { ["WOLFE_HAMELIN_TEST"] = "abc" });

        var result = await Runner().Run(command, TestContext.Current.CancellationToken);

        result.StandardOutput.ShouldBe("abc\n");
    }

    [Fact]
    public async Task Run_ResolvesTheWorkingDirectoryAgainstThePipelineDirectory()
    {
        using var root = TempDirectory.Create();
        Directory.CreateDirectory(Path.Combine(root.Path, "sub"));

        var result = await Runner(root.Path).Run(Shell("pwd").InDirectory("sub"), TestContext.Current.CancellationToken);

        result.StandardOutput.TrimEnd('\n').ShouldEndWith(Path.Combine("sub"));
    }

    private static CommandRunner Runner(string? currentDirectory = null)
    {
        var fileSystem = Substitute.For<IFileSystem>();
        fileSystem.CurrentDirectory.AbsolutePath.Returns(currentDirectory ?? Path.GetTempPath());
        return new CommandRunner(Substitute.For<IPipelineLog>(), fileSystem);
    }

    private static Command Shell(string script) => Command.Create("/bin/sh").WithArguments("-c", script);

    private sealed class TempDirectory : IDisposable
    {
        public required string Path { get; init; }

        public static TempDirectory Create() =>
            new() { Path = Directory.CreateTempSubdirectory("ritten-tests-").FullName };

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
