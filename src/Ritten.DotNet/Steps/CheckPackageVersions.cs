using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.DotNet.Steps;

/// <summary>
/// Requires every package to share the release's version.
/// </summary>
/// <param name="report">The build report.</param>
[Step("check package versions", StepKind.Check)]
public class CheckPackageVersions(IWorkflowReport report)
{
    /// <summary>
    /// Judges every package's version against the release's.
    /// </summary>
    /// <param name="project">The project whose version is the release's (see <see cref="ResolveRelease"/>).</param>
    /// <param name="packages">The packages the repository ships (see <see cref="ReadProjects"/>).</param>
    public StepResult Run(Project project, PackageSet packages)
    {
        var drifted = packages.Packages.Where(p => p.Version != project.Version).ToList();
        if (drifted.Count == 0)
        {
            // A single package can't drift from itself; saying so would be noise.
            if (packages.Packages.Count > 1)
            {
                report.Section(SectionName.Version)
                    .Success($"All {packages.Packages.Count} packages agree on **{project.Version}**.");
            }

            return StepResult.Successful;
        }

        var details = string.Join(", ", drifted.Select(p => $"{p.Name} is {p.Version}"));
        report.Section(SectionName.Version)
            .Failure($"Packages release in lockstep, but {details} while the release is **{project.Version}**. Align `<Version>` across the packages.");
        return StepResult.Failed($"Packages release in lockstep, but {details} while the release is {project.Version}.");
    }
}
