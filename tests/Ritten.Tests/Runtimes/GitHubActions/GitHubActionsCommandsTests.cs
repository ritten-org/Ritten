using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Ritten.Runtimes.GitHubActions;
using Ritten.Runtimes.GitHubActions.Logging;

namespace Ritten.Tests.Runtimes.GitHubActions;

[Collection("Console")]
public class GitHubActionsCommandsTests
{
    private readonly StringWriter _writer = new();
    private readonly ILoggerFactory _loggerFactory;

    private readonly GitHubActionsCommands _sut;

    public GitHubActionsCommandsTests()
    {
        Console.SetOut(_writer);

        _loggerFactory = LoggerFactory.Create(b => b
            .AddConsole(o => o.FormatterName = Constants.FormatterName)
            .AddConsoleFormatter<GitHubActionsConsoleFormatter, ConsoleFormatterOptions>()
        );
        var logger = _loggerFactory.CreateLogger<GitHubActionsCommands>();
        _sut = new GitHubActionsCommands(logger);
    }

    [Fact]
    public void LogDebug_Message_LogsDebugMessage()
    {
        // Arrange

        // Act
        _sut.LogDebug("This is a debug message");
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::debug::This is a debug message\n");
    }

    [Fact]
    public void LogNotice_AllArgs_LogsNotice()
    {
        // Arrange

        // Act
        _sut.LogNotice(
            message: "This is a notice message",
            title: "Title",
            file: "file.txt",
            startLine: 1,
            endLine: 2,
            startColumn: 3,
            endColumn: 4
        );
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::notice title=Title,file=file.txt,line=1,endLine=2,col=3,endColumn=4::This is a notice message\n");
    }

    [Fact]
    public void LogNotice_NoOptionalArgs_LogsNotice()
    {
        // Arrange

        // Act
        _sut.LogNotice("This is a notice message");
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::notice::This is a notice message\n");
    }

    [Fact]
    public void LogWarning_AllArgs_LogsNotice()
    {
        // Arrange

        // Act
        _sut.LogWarning(
            message: "This is a warning message",
            title: "Title",
            file: "file.txt",
            startLine: 1,
            endLine: 2,
            startColumn: 3,
            endColumn: 4
        );
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::warning title=Title,file=file.txt,line=1,endLine=2,col=3,endColumn=4::This is a warning message\n");
    }

    [Fact]
    public void LogWarning_NoOptionalArgs_LogsNotice()
    {
        // Arrange

        // Acts
        _sut.LogWarning("This is a warning message");
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::warning::This is a warning message\n");
    }

    [Fact]
    public void LogError_AllArgs_LogsNotice()
    {
        // Arrange

        // Act
        _sut.LogError(
            message: "This is an error message",
            title: "Title",
            file: "file.txt",
            startLine: 1,
            endLine: 2,
            startColumn: 3,
            endColumn: 4
        );
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::error title=Title,file=file.txt,line=1,endLine=2,col=3,endColumn=4::This is an error message\n");
    }

    [Fact]
    public void LogError_NoOptionalArgs_LogsNotice()
    {
        // Arrange

        // Acts
        _sut.LogError("This is an error message");
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::error::This is an error message\n");
    }

    [Fact]
    public void BeginGroup_WithTitle_LogsCommand()
    {
        // Arrange

        // Act
        _sut.BeginGroup("Title");
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::group::Title\n");
    }

    [Fact]
    public void EndGroup_LogsCommand()
    {
        // Arrange

        // Act
        _sut.EndGroup();
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::endgroup::\n");
    }

    [Fact]
    public async Task AppendJobSummary_WritesToSummaryFile()
    {
        // Arrange
        var tempFile = Path.GetTempFileName();
        Environment.SetEnvironmentVariable("GITHUB_STEP_SUMMARY", tempFile);

        try
        {
            // Act
            await _sut.AppendJobSummary("### Hello world! :rocket:", TestContext.Current.CancellationToken);

            // Assert
            var output = await File.ReadAllTextAsync(tempFile, TestContext.Current.CancellationToken);
            output.ShouldBe("### Hello world! :rocket:");
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void WithGroup_DisposesCorrectly_LogsBothGroupAndEndGroup()
    {
        // Arrange
        var group = _sut.WithGroup("Test Group");

        // Act
        group.Dispose();
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::group::Test Group\n::endgroup::\n");
    }

    [Fact]
    public void WithGroup_MultipleDisposeCalls_OnlyLogsOnce()
    {
        // Arrange
        IDisposable group = _sut.WithGroup("Test Group");

        // Act
        group.Dispose();
        group.Dispose();
        _loggerFactory.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::group::Test Group\n::endgroup::\n");
    }
}
