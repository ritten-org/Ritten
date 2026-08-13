using Ritten.DotNet;

namespace Ritten.Tests.DotNet;

public class DotNetOutputParserTests
{
    [Fact]
    public void ParseDiagnostics_ReadsFileLocatedDiagnostics()
    {
        var diagnostics = DotNetOutputParser.ParseDiagnostics(
            "/repo/src/Program.cs(12,34): error CS0103: The name 'x' does not exist in the current context [/repo/src/My.csproj]");

        var diagnostic = diagnostics.ShouldHaveSingleItem();
        diagnostic.Severity.ShouldBe(DiagnosticSeverity.Error);
        diagnostic.Code.ShouldBe("CS0103");
        diagnostic.Message.ShouldBe("The name 'x' does not exist in the current context");
        diagnostic.File.ShouldBe("/repo/src/Program.cs");
        diagnostic.Line.ShouldBe(12);
        diagnostic.Column.ShouldBe(34);
    }

    [Fact]
    public void ParseDiagnostics_ReadsLocationlessDiagnostics()
    {
        var diagnostics = DotNetOutputParser.ParseDiagnostics(
            """
            error NU1101: Unable to find package Missing.Package.
            MSBUILD : error MSB1009: Project file does not exist.
            """);

        diagnostics.Select(d => d.Code).ShouldBe(["NU1101", "MSB1009"]);
        diagnostics.ShouldAllBe(d => d.File == null);
    }

    [Fact]
    public void ParseDiagnostics_ReadsWarnings()
    {
        var diagnostics = DotNetOutputParser.ParseDiagnostics(
            "Program.cs(1,1): warning CS0219: The variable 'y' is assigned but never used");

        diagnostics.ShouldHaveSingleItem().Severity.ShouldBe(DiagnosticSeverity.Warning);
    }

    [Fact]
    public void ParseDiagnostics_CollapsesMultiTargetDuplicates()
    {
        var diagnostics = DotNetOutputParser.ParseDiagnostics(
            """
            Program.cs(5,10): error CS0246: The type or namespace name 'Foo' could not be found [My.csproj::TargetFramework=net8.0]
            Program.cs(5,10): error CS0246: The type or namespace name 'Foo' could not be found [My.csproj::TargetFramework=net10.0]
            """);

        diagnostics.ShouldHaveSingleItem();
    }

    [Fact]
    public void ParseDiagnostics_IgnoresOrdinaryBuildOutput()
    {
        var diagnostics = DotNetOutputParser.ParseDiagnostics(
            """
            Determining projects to restore...
              My.Project -> /repo/bin/Release/net10.0/My.Project.dll
            Build succeeded.
                0 Warning(s)
                0 Error(s)
            """);

        diagnostics.ShouldBeEmpty();
    }

    [Fact]
    public void ToString_RendersTheFamiliarMSBuildFormat()
    {
        var diagnostics = DotNetOutputParser.ParseDiagnostics(
            "Program.cs(12,34): error CS0103: The name 'x' does not exist [My.csproj]");

        diagnostics.ShouldHaveSingleItem().ToString()
            .ShouldBe("Program.cs(12,34): error CS0103: The name 'x' does not exist");
    }
}
