using System.Text.Json;
using NuGet.Versioning;
using Ritten.Commands;
using Ritten.Contracts.FileSystem;
using Ritten.Engine;
using Ritten.Git;

namespace Ritten.DotNet;

internal class DotNetClient(ICommandRunner commands, IFileSystem fileSystem) : IDotNet
{
    /// <summary>The name the coverage report is written under; the coverage step globs for it.</summary>
    private const string CoverageFileName = "coverage.cobertura.xml";

    private static readonly JsonSerializerOptions FormatReportJson = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<Project>> ReadProject(IFile file, CancellationToken cancellationToken = default)
    {
        // MSBuild evaluates the project for real, so properties inherited from
        // Directory.Build.props, conditions, and SDK defaults are all resolved.
        var command = Command
            .Create("dotnet")
            .WithArguments(
                "msbuild", file.AbsolutePath,
                "-getProperty:PackageId", "-getProperty:Version", "-getProperty:RepositoryUrl",
                "-getProperty:PackAsTool", "-getProperty:ToolCommandName",
                "-getProperty:Description", "-getProperty:PackageReadmeFile",
                "-getProperty:PackageLicenseExpression", "-getProperty:PackageLicenseFile", "-getProperty:PackageLicenseUrl",
                "-getProperty:PackageIcon", "-getProperty:PackageIconUrl",
                "-getProperty:PackageProjectUrl", "-getProperty:PackageTags"
            )
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
            Repository = repository,
            IsTool = string.Equals(Property(properties, "PackAsTool"), "true", StringComparison.OrdinalIgnoreCase),
            ToolCommand = Property(properties, "ToolCommandName"),
            Metadata = new PackageMetadata
            {
                Description = Property(properties, "Description"),
                ReadmeFile = Property(properties, "PackageReadmeFile"),
                LicenseExpression = Property(properties, "PackageLicenseExpression"),
                LicenseFile = Property(properties, "PackageLicenseFile"),
                LicenseUrl = Property(properties, "PackageLicenseUrl"),
                Icon = Property(properties, "PackageIcon"),
                IconUrl = Property(properties, "PackageIconUrl"),
                ProjectUrl = Property(properties, "PackageProjectUrl"),
                Tags = Property(properties, "PackageTags")
            }
        };
    }

    public async Task<NuGetVersion?> InstalledToolVersion(string packageId, CancellationToken cancellationToken = default)
    {
        // A probe whose output the caller consumes, not part of the step's story.
        var command = Command.Create("dotnet").WithArguments("tool", "list", "--global").QuietOutput().ThrowOnError();
        var result = await commands.Run(command, cancellationToken);

        // The table's first two lines are the header and its underline, and ids print lowercased.
        foreach (var line in result.StandardOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries).Skip(2))
        {
            var columns = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (columns.Length >= 2 && string.Equals(columns[0], packageId, StringComparison.OrdinalIgnoreCase))
            {
                return NuGetVersion.Parse(columns[1]);
            }
        }

        return null;
    }

    public async Task ToolInstall(ToolInstallArgs args, CancellationToken cancellationToken = default)
    {
        // --source rather than --add-source: the artifacts directory replaces every configured
        // feed, so a published package with the same version can't shadow the build being installed.
        var command = Command
            .Create("dotnet")
            .WithArguments(
                "tool", "install", args.PackageId,
                "--global",
                "--version", args.Version.ToString(),
                "--source", args.Source.AbsolutePath)
            .ThrowOnError();
        await commands.Run(command, cancellationToken);
    }

    public async Task ToolUninstall(string packageId, CancellationToken cancellationToken = default)
    {
        var command = Command.Create("dotnet").WithArguments("tool", "uninstall", packageId, "--global").ThrowOnError();
        await commands.Run(command, cancellationToken);
    }

    private static string? Property(JsonElement properties, string name) =>
        properties.TryGetProperty(name, out var value) && value.GetString() is { Length: > 0 } text ? text : null;

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
            // The MTP mode of `dotnet test` rejects a bare positional; the project must be named.
            command = command.AndArguments("--project", args.Project);
        }

        if (args.NoBuild)
        {
            command = command.AndArguments("--no-build");
        }

        if (args.Configuration is not null)
        {
            command = command.AndArguments("--configuration", args.Configuration);
        }

        // Microsoft.Testing.Platform options, not VSTest ones: the .NET 10 SDK refuses to run an MTP test
        // application through the VSTest target, so `--logger trx` and `--collect` are no longer the spellings.
        // Both reports need their extension package referenced by the test project to be recognised.
        command = command.AndArguments("--report-trx", "--results-directory", args.ResultsDirectory.AbsolutePath);

        if (args.CollectCoverage)
        {
            command = command.AndArguments(
                "--coverage", "--coverage-output-format", "cobertura", "--coverage-output", CoverageFileName);
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
            FailureOutput = result.IsSuccess ? [] : result.ErrorTail(),
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
                .WithArguments("format", "whitespace", "--verify-no-changes", "--report", reportDirectory.AbsolutePath);
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
