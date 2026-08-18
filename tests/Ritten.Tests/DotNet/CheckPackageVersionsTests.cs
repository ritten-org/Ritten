using NuGet.Versioning;
using Ritten.Contracts;
using Ritten.DotNet;
using Ritten.DotNet.Steps;
using Ritten.Reporting;

namespace Ritten.Tests.DotNet;

public class CheckPackageVersionsTests
{
    private readonly IBuildReport _report = Substitute.For<IBuildReport>();
    private readonly ReportSection _versionSection = new("Version");

    public CheckPackageVersionsTests()
    {
        _report.Section("Version").Returns(_versionSection);
    }

    [Fact]
    public void PassesWhenEveryPackageCarriesTheReleasesVersion()
    {
        var result = Step().Run(Project("My.Package", "1.2.0"), Packages(("My.Package.Core", "1.2.0"), ("My.Package", "1.2.0")));

        result.IsFailure.ShouldBeFalse();
        _versionSection.Tone.ShouldBe(ReportTone.Success);
    }

    [Fact]
    public void FailsWhenAPackageDrifts()
    {
        var result = Step().Run(Project("My.Package", "1.2.0"), Packages(("My.Package.Core", "1.1.0"), ("My.Package", "1.2.0")));

        result.IsFailure.ShouldBeTrue();
        result.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("My.Package.Core is 1.1.0");
        _versionSection.Tone.ShouldBe(ReportTone.Failure);
    }

    [Fact]
    public void SaysNothingForASinglePackage()
    {
        // A single package can't drift from itself; the report stays quiet.
        var result = Step().Run(Project("My.Package", "1.2.0"), Packages(("My.Package", "1.2.0")));

        result.IsFailure.ShouldBeFalse();
        _report.DidNotReceive().Section(Arg.Any<string>());
    }

    private static Project Project(string name, string version) =>
        new() { Name = name, Version = NuGetVersion.Parse(version) };

    private static PackageSet Packages(params (string Name, string Version)[] packages) =>
        new() { Packages = [.. packages.Select(p => Project(p.Name, p.Version))] };

    private CheckPackageVersions Step() => new(_report);
}
