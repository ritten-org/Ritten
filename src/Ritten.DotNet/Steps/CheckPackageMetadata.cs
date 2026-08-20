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
        List<string> warnings = [];
        foreach (var package in packages.Packages)
        {
            if (Required(package.Metadata) is { Count: > 0 } missing)
            {
                failures.Add($"**{package.Name}** is missing {Join(missing, markdown: true)}.");
                errors.Add(new Error($"{package.Name} is missing {Join(missing, markdown: false)}."));
            }

            if (Recommended(package.Metadata) is { Count: > 0 } absent)
            {
                warnings.Add($"**{package.Name}** could also carry {Join(absent, markdown: true)}.");
            }
        }

        var section = report.Section(SectionName.Metadata);
        if (failures.Count == 0 && warnings.Count == 0)
        {
            section.Success(packages.Packages.Count == 1
                ? "The package carries everything a feed asks for."
                : $"All {packages.Packages.Count} packages carry everything a feed asks for.");
            return StepResult.Successful;
        }

        foreach (var failure in failures)
        {
            section.Failure(failure);
        }

        // Recommendations never fail the job: a package without tags still publishes cleanly.
        foreach (var warning in warnings)
        {
            section.Warning(warning);
        }

        // The URL forms predate the packed ones and nuget.org no longer honours them, so a project
        // carrying only those reads as licensed or illustrated when the package itself isn't.
        if (packages.Packages.Any(p => p.Metadata.LicensedByUrlOnly))
        {
            section.Note("`PackageLicenseUrl` is deprecated: set `PackageLicenseExpression` to an SPDX id like `MIT`, or `PackageLicenseFile` for custom terms.");
        }

        if (packages.Packages.Any(p => p.Metadata.IconByUrlOnly))
        {
            section.Note("`PackageIconUrl` is deprecated: pack the image and name it in `PackageIcon`.");
        }

        if (errors.Count == 0)
        {
            return StepResult.Successful;
        }

        section.Note("NuGet warns on push without these. Set them in the project file or Directory.Build.props.");
        return StepResult.Failed(errors);
    }

    /// <summary>
    /// What a package cannot publish cleanly without.
    /// </summary>
    private static List<(string What, string Property)> Required(PackageMetadata metadata)
    {
        List<(string, string)> missing = [];
        if (!metadata.HasDescription)
        {
            missing.Add(("a description", "Description"));
        }

        if (!metadata.HasReadme)
        {
            missing.Add(("a readme", "PackageReadmeFile"));
        }

        if (!metadata.HasLicense)
        {
            missing.Add(("a license", "PackageLicenseExpression"));
        }

        return missing;
    }

    /// <summary>
    /// What makes a package findable and legible on the feed, but never blocks a push.
    /// </summary>
    private static List<(string What, string Property)> Recommended(PackageMetadata metadata)
    {
        List<(string, string)> missing = [];
        if (!metadata.HasIcon)
        {
            missing.Add(("an icon", "PackageIcon"));
        }

        if (!metadata.HasProjectUrl)
        {
            missing.Add(("a project URL", "PackageProjectUrl"));
        }

        if (!metadata.HasTags)
        {
            missing.Add(("search tags", "PackageTags"));
        }

        return missing;
    }

    private static string Join(List<(string What, string Property)> items, bool markdown) =>
        string.Join(", ", items.Select(i => markdown ? $"{i.What} (`{i.Property}`)" : $"{i.What} ({i.Property})"));
}
