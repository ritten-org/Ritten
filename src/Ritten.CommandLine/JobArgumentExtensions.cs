using System.CommandLine;
using Ritten.Engine.Workflows;

namespace Ritten.CommandLine;

/// <summary>
/// Contains extension methods for <see cref="JobArgument{T}"/>.
/// </summary>
public static class JobArgumentExtensions
{
    extension<T>(JobArgument<T> argument)
    {
        /// <summary>
        /// Renders this declaration as the option that offers it.
        /// </summary>
        public Option<T> ToOption()
        {
            var option = new Option<T>($"--{argument.Name}", Aliases(argument))
            {
                Description = argument.Description,
                Required = argument.Required
            };

            // System.CommandLine doesn't have any type conversion options.
            // This is the only way to support casting.
            if (argument.Parse is { } parse)
            {
                option.CustomParser = result =>
                {
                    var value = parse(result.Tokens[0].Value);
                    if (value.IsSuccess)
                    {
                        return value.Value;
                    }

                    foreach (var error in value.Errors)
                    {
                        result.AddError(error.Message);
                    }

                    return default;
                };
            }

            return option;
        }
    }

    private static string[] Aliases(JobArgument argument) => argument.Alias is { Length: > 0 } alias ? [alias] : [];
}
