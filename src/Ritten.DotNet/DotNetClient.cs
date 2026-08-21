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

    /// <summary>The file a repository shares build properties through, version included.</summary>
    private const string DirectoryBuildProps = "Directory.Build.props";

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

    public async Task<FormatResult> Format(FormatArgs args, CancellationToken cancellationToken = default)
    {
        // The report is this client's working space, not the caller's concern: created here,
        // read here, and removed here, so a run leaves nothing behind.
        var reportDirectory = fileSystem.Temp.GetDirectory("format");
        reportDirectory.Create();
        try
        {
            var command = Command.Create("dotnet").WithArguments("format", "whitespace");
            if (args.VerifyNoChanges)
            {
                command = command.AndArguments("--verify-no-changes");
            }

            command = command.AndArguments("--report", reportDirectory.AbsolutePath);
            if (args.NoRestore)
            {
                command = command.AndArguments("--no-restore");
            }

            var result = await commands.Run(command, cancellationToken);

            return new FormatResult
            {
                Succeeded = result.IsSuccess,
                UnformattedFiles = await ReadUnformattedFiles(reportDirectory.GetFile("format-report.json"), cancellationToken)
            };
        }
        finally
        {
            reportDirectory.Delete();
        }
    }

    public async Task<Result<IReadOnlyList<string>>> SetVersion(SetVersionArgs args, CancellationToken cancellationToken = default)
    {
        var current = args.Current.ToString();
        List<string> written = [];
        foreach (var candidate in DeclarationCandidates(args.Projects))
        {
            var file = fileSystem.ProjectRoot.GetFile(candidate);
            if (!file.Exists)
            {
                continue;
            }

            string text;
            using (var reader = new StreamReader(file.OpenRead()))
            {
                text = await reader.ReadToEndAsync(cancellationToken);
            }

            // Rewriting the element's text rather than the document: round-tripping XML would
            // reformat a file the caller has to read, over a change of a few characters.
            var declaration = $"<Version>{current}</Version>";
            if (!text.Contains(declaration, StringComparison.Ordinal))
            {
                continue;
            }

            await WriteText(file, text.Replace(declaration, $"<Version>{args.Version}</Version>", StringComparison.Ordinal), cancellationToken);
            written.Add(candidate);
        }

        if (written.Count == 0)
        {
            return Result.Error(
                $"Nothing declares <Version>{current}</Version>, so there's no version to rewrite. " +
                "Set it in the project file or Directory.Build.props, or pass the version to the tools that compute it.");
        }

        return written;
    }

    public async Task<TestRun> ReadTestResults(IFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenRead();
        return await TrxParser.Parse(stream, cancellationToken);
    }

    /// <summary>
    /// Every file that could declare a project's version.
    /// </summary>
    private static IEnumerable<string> DeclarationCandidates(IReadOnlyList<string> projects) =>
        projects.SelectMany(Ancestry).Distinct(StringComparer.Ordinal);

    private static IEnumerable<string> Ancestry(string project)
    {
        List<string> candidates = [DirectoryBuildProps];
        foreach (var directory in Directories(project))
        {
            candidates.Add(Path.Combine(directory, DirectoryBuildProps));
        }

        candidates.Add(project);
        return candidates;
    }

    /// <summary>
    /// The directories containing the given project, outermost first.
    /// </summary>
    private static IEnumerable<string> Directories(string project)
    {
        var directory = Path.GetDirectoryName(project);
        return string.IsNullOrEmpty(directory) ? [] : [.. Directories(directory), directory];
    }

    private static async Task WriteText(IFile file, string text, CancellationToken cancellationToken)
    {
        var stream = file.OpenWrite();
        stream.SetLength(0); // OpenWrite isn't guaranteed to truncate an existing file.
        await using var writer = new StreamWriter(stream);
        await writer.WriteAsync(text.AsMemory(), cancellationToken);
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
