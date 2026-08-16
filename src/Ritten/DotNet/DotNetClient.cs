using System.Text.Json;
using NuGet.Versioning;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Core;
using Ritten.Git;

namespace Ritten.DotNet;

internal class DotNetClient(ICommandRunner commands, IFileSystem fileSystem) : IDotNet
{
    private static readonly JsonSerializerOptions FormatReportJson = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<Project>> ReadProject(IFile file, CancellationToken cancellationToken = default)
    {
        // MSBuild evaluates the project for real, so properties inherited from
        // Directory.Build.props, conditions, and SDK defaults are all resolved.
        var command = Command
            .Create("dotnet")
            .WithArguments("msbuild", file.AbsolutePath, "-getProperty:PackageId", "-getProperty:Version", "-getProperty:RepositoryUrl")
            .ThrowOnError();
        var result = await commands.Run(command, cancellationToken);

        var properties = JsonDocument.Parse(result.StandardOutput).RootElement.GetProperty("Properties");
        var packageId = properties.GetProperty("PackageId").GetString();
        var version = properties.GetProperty("Version").GetString();

        // Report both, rather than sending someone round the loop twice.
        if (string.IsNullOrEmpty(packageId) || string.IsNullOrEmpty(version))
        {
            List<Error> errors = [];
            if (string.IsNullOrEmpty(packageId))
            {
                errors.Add($"'{file.Name}' does not set a PackageId.");
            }

            if (string.IsNullOrEmpty(version))
            {
                errors.Add($"'{file.Name}' does not set a Version.");
            }

            return errors;
        }

        var repository = properties.TryGetProperty("RepositoryUrl", out var repositoryUrl)
            ? RepositoryUrls.ToWebUrl(repositoryUrl.GetString())
            : null;

        return new Project
        {
            Name = packageId,
            Version = NuGetVersion.Parse(version),
            Repository = repository
        };
    }

    public async Task<RestoreResult> Restore(RestoreArgs args, CancellationToken cancellationToken = default)
    {
        var command = Command.Create("dotnet").WithArguments("restore");
        if (args.Project is not null)
        {
            command = command.AndArguments(args.Project);
        }

        var result = await commands.Run(command, cancellationToken);
        return new RestoreResult
        {
            Succeeded = result.IsSuccess,
            RestoredProjects = DotNetOutputParser.ParseRestoredProjects(result.StandardOutput),
            Diagnostics = ParseDiagnostics(result.StandardOutput)
        };
    }

    public async Task<PackResult> Pack(PackArgs args, CancellationToken cancellationToken = default)
    {
        args.Output.Create();

        var command = Command.Create("dotnet").WithArguments("pack");
        if (args.Project is not null)
        {
            command = command.AndArguments(args.Project);
        }

        if (args.NoBuild)
        {
            command = command.AndArguments("--no-build");
        }

        if (args.Configuration is not null)
        {
            command = command.AndArguments("--configuration", args.Configuration);
        }

        command = command.AndArguments("--output", args.Output.AbsolutePath);
        await commands.Run(command.ThrowOnError(), cancellationToken);

        return new PackResult { Packages = [.. args.Output.GetFiles("*.nupkg")] };
    }

    public async Task<BuildResult> Build(BuildArgs args, CancellationToken cancellationToken = default)
    {
        var command = Command.Create("dotnet").WithArguments("build");
        if (args.Project is not null)
        {
            command = command.AndArguments(args.Project);
        }

        if (args.NoRestore)
        {
            command = command.AndArguments("--no-restore");
        }

        if (args.Configuration is not null)
        {
            command = command.AndArguments("--configuration", args.Configuration);
        }

        var result = await commands.Run(command, cancellationToken);
        return new BuildResult
        {
            Succeeded = result.IsSuccess,
            Diagnostics = ParseDiagnostics(result.StandardOutput)
        };
    }

    public async Task<TestResult> Test(TestArgs args, CancellationToken cancellationToken = default)
    {
        args.ResultsDirectory.Create();

        var command = Command.Create("dotnet").WithArguments("test");
        if (args.Project is not null)
        {
            command = command.AndArguments(args.Project);
        }

        if (args.NoBuild)
        {
            command = command.AndArguments("--no-build");
        }

        if (args.Configuration is not null)
        {
            command = command.AndArguments("--configuration", args.Configuration);
        }

        command = command.AndArguments("--logger", "trx", "--results-directory", args.ResultsDirectory.AbsolutePath);

        if (args.CollectCoverage)
        {
            command = command.AndArguments("--collect", "XPlat Code Coverage");
        }

        var result = await commands.Run(command, cancellationToken);

        var runs = new List<TestRun>();
        foreach (var trxFile in args.ResultsDirectory.GetFiles("*.trx"))
        {
            runs.Add(await ReadTestResults(trxFile, cancellationToken));
        }

        return new TestResult
        {
            Succeeded = result.IsSuccess,
            Passed = runs.Sum(r => r.Passed),
            Failed = runs.Sum(r => r.Failed),
            Skipped = runs.Sum(r => r.Skipped),
            Failures = runs.SelectMany(r => r.Failures).ToList()
        };
    }

    public async Task<FormatResult> CheckFormat(FormatArgs args, CancellationToken cancellationToken = default)
    {
        // The report is this client's working space, not the caller's concern: created here,
        // read here, and removed here, so a run leaves nothing behind.
        var reportDirectory = fileSystem.Temp.GetDirectory("format");
        reportDirectory.Create();
        try
        {
            var command = Command
                .Create("dotnet")
                .WithArguments("format", "--verify-no-changes", "--report", reportDirectory.AbsolutePath);
            if (args.NoRestore)
            {
                command = command.AndArguments("--no-restore");
            }
            var result = await commands.Run(command, cancellationToken);
            if (result.IsSuccess)
            {
                return new FormatResult { Succeeded = true };
            }

            return new FormatResult
            {
                Succeeded = false,
                UnformattedFiles = await ReadUnformattedFiles(reportDirectory.GetFile("format-report.json"), cancellationToken)
            };
        }
        finally
        {
            reportDirectory.Delete();
        }
    }

    public async Task<TestRun> ReadTestResults(IFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenRead();
        return await TrxParser.Parse(stream, cancellationToken);
    }

    public IReadOnlyList<DotNetDiagnostic> ParseDiagnostics(string buildOutput) =>
        DotNetOutputParser.ParseDiagnostics(buildOutput);

    private async Task<IReadOnlyList<string>> ReadUnformattedFiles(IFile reportFile, CancellationToken cancellationToken)
    {
        if (!reportFile.Exists)
        {
            return [];
        }

        await using var stream = reportFile.OpenRead();
        var documents = await JsonSerializer.DeserializeAsync<List<FormatReportDocument>>(stream, FormatReportJson, cancellationToken) ?? [];
        return documents
            .Where(d => d.FilePath != null)
            .Select(d => Path.GetRelativePath(fileSystem.ProjectRoot.AbsolutePath, d.FilePath!))
            .Distinct()
            .Order()
            .ToList();
    }

    private sealed record FormatReportDocument(string? FilePath);
}
