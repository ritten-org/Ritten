using Ritten.Commands;
using Ritten.Contracts;
using Ritten.Engine.FileSystem;

namespace Ritten.Tests.Commands;

public class EnvironmentFileTests : IDisposable
{
    private readonly string _root = Directory.CreateTempSubdirectory("ritten-env-").FullName;
    private readonly ISecretProvider _secretProvider = Substitute.For<ISecretProvider>();

    public EnvironmentFileTests()
    {
        _secretProvider.Resolve(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<string>().StartsWith("op://", StringComparison.Ordinal) ? $"value-of-{call.Arg<string>()}" : call.Arg<string>());
    }

    public void Dispose() => Directory.Delete(_root, recursive: true);

    [Fact]
    public void Parse_ReadsAssignmentsAndSkipsWhatIsNotOne()
    {
        var entries = EnvironmentFile.Parse("# the repository\n\nRESTIC_REPOSITORY=/Volumes/Data2/restic\nexport TZ=\"Europe/London\"\nNAME='quoted'\n", "restic.env");

        var values = entries.Value.ShouldNotBeNull();
        values.ShouldBe(new Dictionary<string, string>
        {
            ["RESTIC_REPOSITORY"] = "/Volumes/Data2/restic",
            ["TZ"] = "Europe/London",
            ["NAME"] = "quoted"
        });
    }

    [Fact]
    public void Parse_NamesTheLineThatIsNotAnAssignment()
    {
        var entries = EnvironmentFile.Parse("A=1\nnot an assignment\n", "x.env");

        entries.IsError.ShouldBeTrue();
        entries.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldStartWith("x.env:2:");
    }

    [Fact]
    public async Task Load_ReadsReferencesThroughTheStoreAndPassesLiteralsThrough()
    {
        var file = Write("secrets.env", "A=literal\nB=\"op://Vault/item/field\"\n");

        var variables = await EnvironmentFile.Load(file, _secretProvider, TestContext.Current.CancellationToken);

        variables.Value.ShouldNotBeNull().ShouldBe(new Dictionary<string, string>
        {
            ["A"] = "literal",
            ["B"] = "value-of-op://Vault/item/field"
        });
    }

    [Fact]
    public async Task Load_ALaterFileReplacesAnEarlierOnesEntry()
    {
        var shared = Write("state.env", "AWS_ACCESS_KEY_ID=shared\nREGION=garage\n");
        var own = Write("secrets.env", "AWS_ACCESS_KEY_ID=own\n");

        var variables = await EnvironmentFile.Load([shared, own], _secretProvider, TestContext.Current.CancellationToken);

        variables.Value.ShouldNotBeNull().ShouldBe(new Dictionary<string, string>
        {
            ["AWS_ACCESS_KEY_ID"] = "own",
            ["REGION"] = "garage"
        });
    }

    [Fact]
    public async Task Load_RefusesAFileThatIsNotThere()
    {
        var variables = await EnvironmentFile.Load(new PhysicalFile(Path.Combine(_root, "missing.env")), _secretProvider, TestContext.Current.CancellationToken);

        variables.IsError.ShouldBeTrue();
        variables.Errors.ShouldNotBeNull().ShouldHaveSingleItem().Message.ShouldContain("missing.env");
    }

    private PhysicalFile Write(string name, string contents)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, contents);
        return new PhysicalFile(path);
    }
}
