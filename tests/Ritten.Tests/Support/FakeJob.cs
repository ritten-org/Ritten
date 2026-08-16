using Ritten.Contracts;
using Ritten.Core;

namespace Ritten.Tests.Support;

/// <summary>
/// A job holding whatever steps a test says it has.
/// </summary>
internal sealed class FakeJob(IReadOnlyList<Step> steps, string name = "job") : IJob
{
    public string Name => name;

    public IReadOnlyList<Step> Steps => steps;
}
