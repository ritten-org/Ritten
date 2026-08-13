using System.Text;
using Ritten.Contracts.FileSystem;
using NuGet.Versioning;
using Ritten.Changelogs;

namespace Ritten.Tests.Changelogs;

public class ChangelogClientTests
{
    private readonly ChangelogClient _client = new();

    [Fact]
    public async Task Read_ParsesTheFileContents()
    {
        var file = FileWithContent(SampleChangelog.Text);

        var changelog = await _client.Read(file, TestContext.Current.CancellationToken);

        changelog.Entries.Count.ShouldBe(3);
    }

    [Fact]
    public async Task ReadEntry_ParsesTheFileContents()
    {
        var file = FileWithContent("## [1.0.0] - 2026-01-01\n\n### Added\n\n- A thing.\n");

        var entry = await _client.ReadEntry(file, TestContext.Current.CancellationToken);

        entry.Version.ShouldBe(NuGetVersion.Parse("1.0.0"));
        entry.Added.ShouldBe(["A thing."]);
    }

    [Fact]
    public async Task Write_RoundTripsTheChangelog()
    {
        var changelog = _client.Parse(SampleChangelog.Text);
        var (file, written) = WritableFile();

        await _client.Write(file, changelog, TestContext.Current.CancellationToken);

        Encoding.UTF8.GetString(written.ToArray()).ShouldBe(SampleChangelog.Text);
    }

    [Fact]
    public async Task WriteEntry_WritesTheRenderedEntry()
    {
        var entry = new ChangelogEntry { Added = ["A thing."] };
        var (file, written) = WritableFile();

        await _client.WriteEntry(file, entry, TestContext.Current.CancellationToken);

        Encoding.UTF8.GetString(written.ToArray()).ShouldBe("### Added\n\n- A thing.");
    }

    private static IFile FileWithContent(string content)
    {
        var file = Substitute.For<IFile>();
        file.OpenRead().Returns(_ => new MemoryStream(Encoding.UTF8.GetBytes(content)));
        return file;
    }

    private static (IFile File, MemoryStream Written) WritableFile()
    {
        var stream = new MemoryStream();
        var file = Substitute.For<IFile>();
        file.OpenWrite().Returns(stream);
        return (file, stream);
    }
}
