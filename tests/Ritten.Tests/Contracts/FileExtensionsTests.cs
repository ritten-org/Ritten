using Ritten.Contracts.FileSystem;
using Ritten.Engine.FileSystem;
using Ritten.Tests.Support;

namespace Ritten.Tests.Contracts;

/// <summary>
/// Reading and writing a file's whole text. The write is atomic unless asked otherwise: a reader
/// sees the old text or the new, and a file's permissions survive being replaced.
/// </summary>
public class FileExtensionsTests : IDisposable
{
    private readonly DirectoryInfo _root = Directory.CreateTempSubdirectory("ritten-files-");

    public void Dispose() => _root.Delete(recursive: true);

    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private PhysicalFile File(string name) => new(Path.Combine(_root.FullName, name));

    [Fact]
    public async Task ReadAllTextIfExists_IsNullWithoutAFile() =>
        (await File("missing.txt").ReadAllTextIfExists(Ct)).ShouldBeNull();

    [Fact]
    public async Task ReadAllTextIfExists_ReadsTheWholeFile()
    {
        await System.IO.File.WriteAllTextAsync(Path.Combine(_root.FullName, "a.txt"), "line one\nline two\n", Ct);

        (await File("a.txt").ReadAllTextIfExists(Ct)).ShouldBe("line one\nline two\n");
    }

    [Fact]
    public async Task WriteAllText_CreatesTheFileAndItsDirectory()
    {
        var file = new PhysicalFile(Path.Combine(_root.FullName, "nested", "deeper", "unit.service"));

        await file.WriteAllText("[Service]\n", cancellationToken: Ct);

        (await file.ReadAllTextIfExists(Ct)).ShouldBe("[Service]\n");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WriteAllText_ReplacesALongerFileWithoutKeepingItsTail(bool atomic)
    {
        var file = File("unit.txt");
        await file.WriteAllText("a much longer first version of the file", cancellationToken: Ct);

        await file.WriteAllText("short", atomic, cancellationToken: Ct);

        (await file.ReadAllTextIfExists(Ct)).ShouldBe("short");
    }

    [Fact]
    public async Task WriteAllText_LeavesNothingBesideTheFile()
    {
        await File("unit.txt").WriteAllText("one", cancellationToken: Ct);
        await File("unit.txt").WriteAllText("two", cancellationToken: Ct);

        _root.GetFiles().Select(f => f.Name).ShouldBe(["unit.txt"]);
    }

    [Fact]
    public async Task WriteAllText_ReplacesTheFileRatherThanWritingIntoIt()
    {
        // The rename is what makes it atomic: a reader holding the old file keeps reading the old
        // text, where an in-place write would have changed it under them.
        var file = File("unit.txt");
        await file.WriteAllText("old", cancellationToken: Ct);
        using var reader = new StreamReader(file.OpenRead());

        await file.WriteAllText("new", cancellationToken: Ct);

        (await reader.ReadToEndAsync(Ct)).ShouldBe("old");
        (await file.ReadAllTextIfExists(Ct)).ShouldBe("new");
    }

    [Fact]
    public async Task WriteAllText_KeepsTheFilesPermissions()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix permissions.");
        var file = File("secret.env");
        await file.WriteAllText("TOKEN=one", cancellationToken: Ct);
        file.SetUnixFileMode(UnixFileMode.UserRead | UnixFileMode.UserWrite);

        await file.WriteAllText("TOKEN=two", cancellationToken: Ct);

        file.GetUnixFileMode().ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact]
    public async Task WriteAllText_GivesANewFileTheModeItIsAskedFor()
    {
        Assert.SkipWhen(OperatingSystem.IsWindows(), "Unix permissions.");
        var file = File("secret.env");

        await file.WriteAllText("TOKEN=one", mode: UnixFileMode.UserRead | UnixFileMode.UserWrite, cancellationToken: Ct);

        file.GetUnixFileMode().ShouldBe(UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }

    [Fact]
    public async Task WriteAllText_InPlaceWritesTheFileItself()
    {
        var file = MemoryFile.Existing("unit.txt", "old");

        await file.WriteAllText("new", atomic: false, cancellationToken: Ct);

        file.Text.ShouldBe("new");
        file.Writes.ShouldBe(1);
        ((MemoryDirectory)file.Directory).FileNames.ShouldBe(["unit.txt"]);
    }

    [Fact]
    public async Task WriteAllText_CleansUpAndKeepsTheOldTextWhenTheReplaceFails()
    {
        var directory = new MemoryDirectory("/repo");
        var file = Substitute.For<IFile>();
        file.Name.Returns("unit.txt");
        file.Directory.Returns(directory);
        // The staged file cannot move onto a destination it does not recognise: the replace fails.

        await Should.ThrowAsync<InvalidCastException>(() => file.WriteAllText("new", cancellationToken: Ct));

        directory.FileNames.ShouldBeEmpty();
    }
}
