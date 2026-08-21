using System.CommandLine;
using Ritten.Engine.Workflows;

namespace Ritten.CommandLine;

public static class JobArgumentExtensions
{
    extension<T>(JobArgument<T> argument)
    {
        public Option<T> ToOption()
        {
            var option = new Option<T>($"--{argument.Name}", Aliases(argument))
            {
                Description = argument.Description,
                Required = argument.Required,
            };
            return option;
        }
    }

    private static string[] Aliases(JobArgument argument) => argument.Alias is { Length: > 0 } alias ? [alias] : [];
}
