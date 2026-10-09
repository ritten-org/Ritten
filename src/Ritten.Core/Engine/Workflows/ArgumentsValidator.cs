using System.Linq.Expressions;
using System.Text.Json;
using Ritten.Reporting;

namespace Ritten.Engine.Workflows;

/// <summary>
/// Allows a job to validate its arguments before it's run.
/// </summary>
/// <typeparam name="TArguments">The arguments type being validated.</typeparam>
public sealed class ArgumentsValidator<TArguments>
{
    private readonly TArguments _arguments;
    private readonly Func<string, string?> _environment;
    private readonly bool _dryRun;
    private readonly IWorkflowLog _log;
    private readonly string _source;
    private readonly List<Error> _errors = [];

    internal ArgumentsValidator(TArguments arguments, Func<string, string?> environment, bool dryRun, IWorkflowLog log, string source)
    {
        _arguments = arguments;
        _environment = environment;
        _dryRun = dryRun;
        _log = log;
        _source = source;
    }

    /// <summary>
    /// All the issues found with the arguments.
    /// </summary>
    internal IReadOnlyList<Error> Errors => _errors;

    /// <summary>
    /// Requires a value to be present, as a property chain.
    /// </summary>
    /// <param name="setting">The value that must be present.</param>
    public ArgumentsValidator<TArguments> Require(Expression<Func<TArguments, string?>> setting)
    {
        if (string.IsNullOrEmpty(setting.Compile()(_arguments)))
        {
            _errors.Add(Result.Error($"'{SettingKey(setting)}' not set in {_source}."));
        }

        return this;
    }

    /// <summary>
    /// Requires a condition the property-chain form can't express — one value or another, say.
    /// </summary>
    /// <param name="satisfied">Whether the arguments meet the requirement.</param>
    /// <param name="error">The complete error to report when they don't.</param>
    public ArgumentsValidator<TArguments> Require(Func<TArguments, bool> satisfied, string error)
    {
        if (!satisfied(_arguments))
        {
            _errors.Add(Result.Error(error));
        }

        return this;
    }

    /// <summary>
    /// Requires an environment variable to be set.
    /// </summary>
    /// <param name="variable">The name of the environment variable.</param>
    public ArgumentsValidator<TArguments> RequireEnvironment(string variable)
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
    private static string SettingKey(Expression<Func<TArguments, string?>> setting)
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
            throw new InvalidOperationException("A required value must be a property chain, e.g. a => a.Build.Project.");
        }

        return string.Join('.', segments);
    }
}
