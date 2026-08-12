using System.Text.Json;
using System.Xml;
using Hamelin;
using Hamelin.FileSystem;
using NuGet.Versioning;
using Wolfe.Hamelin.Commands;

namespace Wolfe.Hamelin.DotNet;

internal class DotNetClient(ICommandRunner commands, IPipelineContext context) : IDotNet
{
    private static readonly JsonSerializerOptions FormatReportJson = new() { PropertyNameCaseInsensitive = true };

    public async Task<Project> ReadProject(IFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenRead();
        var document = new XmlDocument();
        document.Load(stream);
        if (document.DocumentElement == null)
        {
            throw new Exception("Could not parse project file.");
        }

        var packageName = document.SelectSingleNode("Project/PropertyGroup/PackageId")?.FirstChild?.Value;
        if (packageName == null)
        {
            throw new Exception("Unable to find PackageId in project file.");
        }

        var packageVersion = document.SelectSingleNode("Project/PropertyGroup/Version")?.FirstChild?.Value;
        if (packageVersion == null)
        {
            throw new Exception("Unable to find Version in project file.");
        }

        return new Project
        {
            Name = packageName,
            Version = NuGetVersion.Parse(packageVersion)
        };
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
        args.ReportDirectory.Create();

        var command = Command
            .Create("dotnet")
            .WithArguments("format", "--verify-no-changes", "--report", args.ReportDirectory.AbsolutePath);
        var result = await commands.Run(command, cancellationToken);
        if (result.IsSuccess)
        {
            return new FormatResult { Succeeded = true };
        }

        return new FormatResult
        {
            Succeeded = false,
            UnformattedFiles = await ReadUnformattedFiles(args.ReportDirectory.GetFile("format-report.json"), cancellationToken)
        };
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
            .Select(d => Path.GetRelativePath(context.CurrentDirectory, d.FilePath!))
            .Distinct()
            .Order()
            .ToList();
    }

    private sealed record FormatReportDocument(string? FilePath);
}
