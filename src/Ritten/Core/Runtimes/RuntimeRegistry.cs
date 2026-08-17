namespace Ritten.Core.Runtimes;

/// <summary>
/// The runtimes a host can find itself running in.
/// </summary>
public sealed class RuntimeRegistry
{
    private readonly List<Runtime> _runtimes = [];

    /// <summary>
    /// Registers a runtime candidate.
    /// </summary>
    /// <param name="runtime">The runtime to register.</param>
    public RuntimeRegistry Add(Runtime runtime)
    {
        _runtimes.Add(runtime);
        return this;
    }

    /// <summary>
    /// Validates the entire registered runtime model.
    /// </summary>
    internal IReadOnlyList<Error> Validate()
    {
        List<Error> errors =
        [
            .. _runtimes
                .GroupBy(r => r.Name, StringComparer.OrdinalIgnoreCase)
                .Where(g => g.Count() > 1)
                .Select(g => Result.Error($"Two runtimes are registered under the name '{g.Key}'."))
        ];

        // A runtime that doesn't claim its own markers would leave them visible in the filtered
        // environment, ready to be misread by whatever consumes them next.
        foreach (var runtime in _runtimes)
        {
            errors.AddRange(runtime.Markers
                .Where(marker => !runtime.Claims.Contains(marker))
                .Select(marker => Result.Error($"The {runtime.Name} runtime detects on '{marker}' but doesn't claim it.")));
        }

        return errors;
    }

    /// <summary>
    /// Detects the active runtime.
    /// </summary>
    /// <param name="environment">The environment to detect against.</param>
    internal Result<DetectRuntimeResult> Detect(Func<string, string?> environment)
    {
        var matches = _runtimes
            .Select(runtime => (Runtime: runtime, Evidence: runtime.Markers.Where(m => environment(m) is not null).ToList()))
            .Where(match => match.Evidence.Count > 0)
            .ToList();

        var remaining = matches
            .Where(match => !matches.Any(other => Subsumes(other, match)))
            .ToList();

        return remaining switch
        {
            [] when matches.Count == 0 => Select(new LocalRuntime(), environment),
            [var single] => Select(single.Runtime, environment),
            // Two survivors, or none left because every match subsumed another: the model has no
            // answer, and guessing here would run the wrong runtime's side effects.
            _ => Result.Error($"Runtime detection is ambiguous between: {string.Join(", ", matches.Select(m => m.Runtime.Name))}.")
        };
    }

    private static bool Subsumes(
        (Runtime Runtime, List<string> Evidence) claimant,
        (Runtime Runtime, List<string> Evidence) other) =>
        claimant.Runtime != other.Runtime
        && other.Evidence.All(claimant.Runtime.Claims.Contains)
        && !claimant.Evidence.All(other.Runtime.Claims.Contains);

    private static DetectRuntimeResult Select(Runtime runtime, Func<string, string?> environment) =>
        new(runtime, name => runtime.Claims.Contains(name) ? null : environment(name));
}
