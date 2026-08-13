using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;
using Ritten.Runtimes.GitHubActions.Logging;

namespace Ritten.Tests.Runtimes.GitHubActions.Logging;

[Collection("Console")]
public class GitHubActionsConsoleFormatterTests
{
    private readonly StringWriter _writer = new();
    private readonly ILogger _logger;
    private readonly ConsoleLoggerProvider _provider;

    public GitHubActionsConsoleFormatterTests()
    {
        Console.SetOut(_writer);

        var formatter = new GitHubActionsConsoleFormatter();

        var options = new ConsoleLoggerOptions { FormatterName = Constants.FormatterName, };
        var monitor = Substitute.For<IOptionsMonitor<ConsoleLoggerOptions>>();
        monitor.CurrentValue.Returns(options);

        _provider = new ConsoleLoggerProvider(monitor, [formatter]);
        _logger = _provider.CreateLogger("TestLogger");
    }

    [Fact]
    public void LogCritical_TextOnly_LogsError()
    {
        // Arrange

        // Act
        _logger.LogCritical("Test Critical");
        _provider.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::error::Test Critical\n");
    }

    [Fact]
    public void LogError_TextOnly_LogsError()
    {
        // Arrange

        // Act
        _logger.LogError("Test Error");
        _provider.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::error::Test Error\n");
    }

    [Fact]
    public void LogError_WithException_LogsErrorWithExceptionDetails()
    {
        // Arrange
        var ex = new Exception("Test Exception");

        // Act
        _logger.LogError(ex, "Test Error");
        _provider.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::error::Test Error%0ASystem.Exception: Test Exception\n");
    }

    [Fact]
    public void LogWarning_TextOnly_LogsWarning()
    {
        // Arrange

        // Act
        _logger.LogWarning("Test Warning");
        _provider.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::warning::Test Warning\n");
    }

    [Fact]
    public void LogInformation_TextOnly_LogsInformation()
    {
        // Arrange

        // Act
        _logger.LogInformation("Test Information");
        _provider.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("Information: Test Information\n");
    }

    [Fact]
    public void LogDebug_TextOnly_LogsDebug()
    {
        // Arrange

        // Act
        _logger.LogDebug("Test Debug");
        _provider.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::debug::Test Debug\n");
    }

    [Fact]
    public void LogTrace_TextOnly_LogsTrace()
    {
        // Arrange

        // Act
        _logger.LogTrace("Test Trace");
        _provider.Dispose();

        // Assert
        var output = _writer.ToString();
        output.ShouldBe("::debug::Test Trace\n");
    }
}
