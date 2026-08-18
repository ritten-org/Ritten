using Ritten.Core;

namespace Ritten.Tests.Support;

/// <summary>
/// A pipeline declared inline: the jobs a test hands it, nothing more.
/// </summary>
internal sealed class TestPipeline(string name = "test", IReadOnlyList<IJob>? jobs = null) : IPipeline
{
    public string Name => name;

    public string Label => "Test";

    public IReadOnlyList<IJob> Jobs { get; } = jobs ?? [];
}
