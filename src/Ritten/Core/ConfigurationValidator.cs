using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Ritten.Core;

/// <summary>
/// Validates every options type registered with <c>ValidateOnStart()</c>.
/// </summary>
internal static class ConfigurationValidator
{
    /// <summary>
    /// Returns the validation failures for the given services, or an empty list if there are none.
    /// </summary>
    public static bool TryValidate(IServiceProvider services, out IReadOnlyList<string> failures)
    {
        failures = [];

        if (services.GetService<IStartupValidator>() is not { } validator)
        {
            return true;
        }

        try
        {
            validator.Validate();
            return true;
        }
        catch (Exception exception)
        {
            failures = [.. Failures(exception)];
            return false;
        }
    }

    // One failing options type surfaces as an OptionsValidationException;
    // several are wrapped in an AggregateException.
    private static IEnumerable<string> Failures(Exception exception) => exception switch
    {
        AggregateException aggregate => aggregate.InnerExceptions.SelectMany(Failures),
        OptionsValidationException validation => validation.Failures,
        _ => [exception.Message]
    };
}
