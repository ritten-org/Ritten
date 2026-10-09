using System.CommandLine;
using Ritten.Engine.Workflows;

namespace Ritten.CommandLine;

/// <summary>
/// Contains extension methods for <see cref="JobOption"/>.
/// </summary>
public static class JobOptionExtensions
{
    extension(JobOption jobOption)
    {
        /// <summary>
        /// The command-line option for this job option.
        /// </summary>
        public Option ToOption()
        {
            string[] aliases = jobOption.Alias is { Length: > 0 } alias ? [alias] : [];
            if (jobOption.IsFlag)
            {
                return new Option<bool>($"--{jobOption.Name}", aliases) { Description = jobOption.Description };
            }

            return new Option<object>($"--{jobOption.Name}", aliases)
            {
                Description = jobOption.Description,
                HelpName = jobOption.Name,
                CustomParser = result =>
                {
                    var value = jobOption.Read(result.Tokens[0].Value);
                    if (value.IsSuccess)
                    {
                        return value.Value;
                    }

                    foreach (var error in value.Errors)
                    {
                        result.AddError(error.Message);
                    }

                    return null;
                }
            };
        }
    }
}
