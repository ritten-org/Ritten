using Ritten.Runtimes.GitHubActions;

namespace Ritten.Tests.Runtimes.GitHubActions;

[Collection("Console")]
public class GitHubActionsCommandsTests
{
    private readonly StringWriter _writer = new();
    private readonly GitHubActionsCommands _sut = new();

    public GitHubActionsCommandsTests()
    {
        Console.SetOut(_writer);
    }

    [Fact]
    public void LogDebug_Message_LogsDebugMessage()
    {
        // Act
        _sut.LogDebug("This is a debug message");

        // Assert
        _writer.ToString().ShouldBe("::debug::This is a debug message\n");
    }

    [Fact]
    public void LogNotice_AllArgs_LogsNotice()
    {
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

        // Assert
        _writer.ToString().ShouldBe("::notice title=Title,file=file.txt,line=1,endLine=2,col=3,endColumn=4::This is a notice message\n");
    }

    [Fact]
    public void LogNotice_NoOptionalArgs_LogsNotice()
    {
        // Act
        _sut.LogNotice("This is a notice message");

        // Assert
        _writer.ToString().ShouldBe("::notice::This is a notice message\n");
    }

    [Fact]
    public void LogWarning_AllArgs_LogsWarning()
    {
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

        // Assert
        _writer.ToString().ShouldBe("::warning title=Title,file=file.txt,line=1,endLine=2,col=3,endColumn=4::This is a warning message\n");
    }

    [Fact]
    public void LogWarning_NoOptionalArgs_LogsWarning()
    {
        // Act
        _sut.LogWarning("This is a warning message");

        // Assert
        _writer.ToString().ShouldBe("::warning::This is a warning message\n");
    }

    [Fact]
    public void LogError_AllArgs_LogsError()
    {
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

        // Assert
        _writer.ToString().ShouldBe("::error title=Title,file=file.txt,line=1,endLine=2,col=3,endColumn=4::This is an error message\n");
    }

    [Fact]
    public void LogError_NoOptionalArgs_LogsError()
    {
        // Act
        _sut.LogError("This is an error message");

        // Assert
        _writer.ToString().ShouldBe("::error::This is an error message\n");
    }

    [Fact]
    public void BeginGroup_WithTitle_LogsCommand()
    {
        // Act
        _sut.BeginGroup("Title");

        // Assert
        _writer.ToString().ShouldBe("::group::Title\n");
    }

    [Fact]
    public void EndGroup_LogsCommand()
    {
        // Act
        _sut.EndGroup();

        // Assert
        _writer.ToString().ShouldBe("::endgroup::\n");
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
        // Act
        var group = _sut.WithGroup("Test Group");
        group.Dispose();

        // Assert
        _writer.ToString().ShouldBe("::group::Test Group\n::endgroup::\n");
    }

    [Fact]
    public void WithGroup_MultipleDisposeCalls_OnlyLogsOnce()
    {
        // Act
        IDisposable group = _sut.WithGroup("Test Group");
        group.Dispose();
        group.Dispose();

        // Assert
        _writer.ToString().ShouldBe("::group::Test Group\n::endgroup::\n");
    }
}
