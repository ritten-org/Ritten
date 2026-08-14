using Microsoft.Extensions.Configuration;

namespace Ritten.Core;

/// <summary>
/// Locates the project Ritten is running against.
/// </summary>
internal static class RittenProject
{
    /// <summary>
    /// The configuration file that marks the root of a project.
    /// </summary>
    public const string FileName = "ritten.json";

    /// <summary>
    /// Walks up from the given directory looking for the directory that contains
    /// <see cref="FileName"/>, or returns <c>null</c> if there isn't one.
    /// </summary>
    public static string? Find(string startDirectory)
    {
        var directory = new DirectoryInfo(Path.GetFullPath(startDirectory));

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, FileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    /// <summary>
    /// Reads the configuration for the project rooted at the given directory.
    /// </summary>
    /// <exception cref="Exception">The configuration file could not be read.</exception>
    public static IConfiguration ReadConfiguration(string rootDirectory) => new ConfigurationBuilder()
        .SetBasePath(rootDirectory)
        .AddJsonFile(FileName, optional: false, reloadOnChange: false)
        .AddEnvironmentVariables()
        .Build();
}
