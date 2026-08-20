using Ritten.Contracts;
using Ritten.Engine;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Requires every package to carry the full NuGet metadata.
/// </summary>
/// <param name="report">The build report.</param>
[Step("check package metadata", StepKind.Check)]
public class CheckPackageMetadata(IWorkflowReport report)
{
    /// <summary>
    /// Judges every shipped package's metadata.
    /// </summary>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    public StepResult Run(PackageSet packages)
    {
        List<Error> errors = [];
        List<string> failures = [];
        foreach (var package in packages.Packages)
        {
            List<(string What, string Property)> missing = [];
            if (!package.Metadata.HasDescription)
            {
                missing.Add(("a description", "Description"));
            }

            if (!package.Metadata.HasReadme)
            {
                missing.Add(("a readme", "PackageReadmeFile"));
            }

            if (!package.Metadata.HasLicense)
            {
                missing.Add(("a license", "PackageLicenseExpression"));
            }

            if (missing.Count == 0)
            {
                continue;
            }

            failures.Add($"**{package.Name}** is missing {string.Join(", ", missing.Select(m => $"{m.What} (`{m.Property}`)"))}.");
            errors.Add(new Error($"{package.Name} is missing {string.Join(", ", missing.Select(m => $"{m.What} ({m.Property})"))}."));
        }

        if (errors.Count == 0)
        {
            report.Section("Metadata")
                .Success(packages.Packages.Count == 1
                    ? "The package carries a description, readme, and license."
                    : $"All {packages.Packages.Count} packages carry a description, readme, and license.");
            return StepResult.Successful;
        }

        var section = report.Section("Metadata");
        foreach (var failure in failures)
        {
            section.Failure(failure);
        }

        section.Note("NuGet warns on push without these. Set them in the project file or Directory.Build.props.");
        return StepResult.Failed(errors);
    }
}
