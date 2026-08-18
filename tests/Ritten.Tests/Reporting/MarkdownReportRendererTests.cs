using Ritten.Contracts;
using Ritten.Core;
using Ritten.Reporting;

namespace Ritten.Tests.Reporting;

public class MarkdownReportRendererTests
{
    private readonly MarkdownReportRenderer _renderer = new();

    [Fact]
    public void RendersTheAuthoredSections()
    {
        var section = new ReportSection("Build").Failure("The solution failed to build.");

        var markdown = _renderer.Render("Workflow", succeeded: false, [section]);

        markdown.ShouldContain("## ❌ Workflow");
        markdown.ShouldContain("### ❌ Build");
        markdown.ShouldContain("The solution failed to build.");
    }

    [Fact]
    public void FallsBackToTheFailingStepWhenNoSectionReportsAFailure()
    {
        var failure = Failure("dotnet restore", "Command 'dotnet' exited with code 1.");

        var markdown = _renderer.Render("Workflow", succeeded: false, [], failure);

        markdown.ShouldContain("### ❌ dotnet restore");
        markdown.ShouldContain("Command 'dotnet' exited with code 1.");
        markdown.ShouldNotContain("isn't reported here");
    }

    [Fact]
    public void AdmitsWhenTheFailureLeftNothingBehind()
    {
        var markdown = _renderer.Render("Workflow", succeeded: false, []);

        markdown.ShouldContain("check the build logs");
    }

    [Fact]
    public void LeavesAnAuthoredFailureToSpeakForItself()
    {
        var section = new ReportSection("Build").Failure("The solution failed to build.");
        var failure = Failure("dotnet build", "error CS0103");

        var markdown = _renderer.Render("Workflow", succeeded: false, [section], failure);

        markdown.ShouldNotContain("### ❌ dotnet build");
    }

    private static StepOutcome Failure(string stepName, string error) =>
        new(new Step(stepName, StepKind.Work, produces: null, requires: []), StepResult.Failed(error));
}
