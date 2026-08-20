using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.DotNet;

public class CheckPackageMetadataTests
{
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly ReportSection _metadataSection = new(SectionName.Metadata);

    public CheckPackageMetadataTests()
    {
        _report.Section(SectionName.Metadata).Returns(_metadataSection);
    }

    [Fact]
    public void PassesWhenEveryPackageCarriesItsMetadata()
    {
        var result = Step().Run(Packages(Complete("My.Package"), Complete("My.Package.Core")));

        result.IsFailure.ShouldBeFalse();
        _metadataSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public void FailsListingEveryGapOfEveryPackage()
    {
        var bare = Complete("My.Package.Core") with { Metadata = Metadata() with { ReadmeFile = null, LicenseExpression = null } };

        var result = Step().Run(Packages(Complete("My.Package"), bare));

        result.IsFailure.ShouldBeTrue();
        var error = result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message;
        error.ShouldContain("My.Package.Core");
        error.ShouldContain("PackageReadmeFile");
        error.ShouldContain("PackageLicenseExpression");
        error.ShouldNotContain("Description");
        _metadataSection.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public void TreatsTheSdkPlaceholderDescriptionAsMissing()
    {
        // The SDK substitutes "Package Description" when a project sets none; that's exactly
        // the package NuGet warns about.
        var placeholder = Complete("My.Package") with { Metadata = Metadata() with { Description = "Package Description" } };

        var result = Step().Run(Packages(placeholder));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("a description (Description)");
    }

    [Fact]
    public void WarnsWithoutFailingForTheRecommendedMetadata()
    {
        // A package without tags still publishes cleanly, so the report says so and the job carries on.
        var untagged = Complete("My.Package") with { Metadata = Metadata() with { Tags = null, Icon = null } };

        var result = Step().Run(Packages(untagged));

        result.IsFailure.ShouldBeFalse();
        _metadataSection.Tone.ShouldBe(ReportTone.Warning);
    }

    [Fact]
    public void NamesTheReplacementForADeprecatedLicenseUrl()
    {
        var deprecated = Complete("My.Package") with
        {
            Metadata = Metadata() with { LicenseExpression = null, LicenseUrl = "https://example.com/LICENSE" }
        };

        var result = Step().Run(Packages(deprecated));

        result.IsFailure.ShouldBeTrue();
        _metadataSection.Entries
            .OfType<ReportParagraph>()
            .ShouldContain(e => e.Markdown.Contains("`PackageLicenseUrl` is deprecated"));
    }

    private static PackageMetadata Metadata() => new()
    {
        Description = "Real.",
        ReadmeFile = "README.md",
        LicenseExpression = "MIT",
        Icon = "icon.png",
        ProjectUrl = "https://example.com",
        Tags = "build"
    };

    private static Project Complete(string name) => new()
    {
        Name = name,
        Version = NuGetVersion.Parse("1.2.0"),
        Metadata = Metadata()
    };

    private static PackageSet Packages(params Project[] projects) => new() { Packages = projects };

    private CheckPackageMetadata Step() => new(_report);
}
