using System.Linq.Expressions;
using System.Text.Json;
using Ritten.Contracts;
using Ritten.Reporting;

namespace Ritten.Engine.Workflows;

/// <summary>
/// Allows a job to validate its settings before it's run.
/// </summary>
/// <typeparam name="TSettings">The settings type being validated.</typeparam>
public sealed class SettingsValidator<TSettings> where TSettings : WorkflowSettings
{
    private readonly TSettings _settings;
    private readonly Func<string, string?> _environment;
    private readonly bool _dryRun;
    private readonly IWorkflowLog _log;
    private readonly string _fileName;
    private readonly List<Error> _errors = [];

    internal SettingsValidator(TSettings settings, Func<string, string?> environment, bool dryRun, IWorkflowLog log, string fileName)
    {
        _settings = settings;
        _environment = environment;
        _dryRun = dryRun;
        _log = log;
        _fileName = fileName;
    }

    /// <summary>
    /// All the issues found with the settings.
    /// </summary>
    internal IReadOnlyList<Error> Errors => _errors;

    /// <summary>
    /// Requires a setting to be present, as a property chain.
    /// </summary>
    /// <param name="setting">The setting that must be present.</param>
    public SettingsValidator<TSettings> Require(Expression<Func<TSettings, string?>> setting)
    {
        if (string.IsNullOrEmpty(setting.Compile()(_settings)))
        {
            _errors.Add(Result.Error($"'{SettingKey(setting)}' not set in {_fileName}."));
        }

        return this;
    }

    /// <summary>
    /// Requires an environment variable to be set.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    public SettingsValidator<TSettings> RequireEnvironment(string variable)
    {
        if (!string.IsNullOrEmpty(_environment(variable)))
        {
            return this;
        }

        if (_dryRun)
        {
            // A rehearsal can finish without it, but finding out that the real run couldn't
            // is most of what a rehearsal is for. Warned, not failed.
            _log.Warning($"{variable} is not set; a real run would stop before starting.");
        }
        else
        {
            _errors.Add(Result.Error($"{variable} is not set."));
        }

        return this;
    }

    /// <summary>
    /// Turns <c>s =&gt; s.Build.Project</c> into <c>build.project</c>.
    /// </summary>
    private static string SettingKey(Expression<Func<TSettings, string?>> setting)
    {
        List<string> segments = [];
        var expression = setting.Body;
        while (expression is MemberExpression member)
        {
            segments.Insert(0, JsonNamingPolicy.CamelCase.ConvertName(member.Member.Name));
            expression = member.Expression!;
        }

        if (expression is not ParameterExpression || segments.Count == 0)
        {
            throw new InvalidOperationException("A required setting must be a property chain, e.g. s => s.Build.Project.");
        }

        return string.Join('.', segments);
    }
}
