using NuGet.Versioning;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.DotNet;

public class CheckPackageMetadataTests
{
    private readonly IWorkflowReport _report = Substitute.For<IWorkflowReport>();
    private readonly ReportSection _metadataSection = new("Metadata");

    public CheckPackageMetadataTests()
    {
        _report.Section("Metadata").Returns(_metadataSection);
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
        var bare = Complete("My.Package.Core") with { Metadata = new PackageMetadata { Description = "Real." } };

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
        var placeholder = Complete("My.Package") with
        {
            Metadata = new PackageMetadata { Description = "Package Description", ReadmeFile = "README.md", LicenseExpression = "MIT" }
        };

        var result = Step().Run(Packages(placeholder));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("a description (Description)");
    }

    private static Project Complete(string name) => new()
    {
        Name = name,
        Version = NuGetVersion.Parse("1.2.0"),
        Metadata = new PackageMetadata { Description = "Real.", ReadmeFile = "README.md", LicenseExpression = "MIT" }
    };

    private static PackageSet Packages(params Project[] projects) => new() { Packages = projects };

    private CheckPackageMetadata Step() => new(_report);
}
