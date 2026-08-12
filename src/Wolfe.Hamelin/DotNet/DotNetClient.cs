using System.Xml;
using Hamelin.FileSystem;
using NuGet.Versioning;

namespace Wolfe.Hamelin.DotNet;

internal class DotNetClient : IDotNet
{
    public async Task<Project> ReadProject(IFile file, CancellationToken cancellationToken = default)
    {
        await using var stream = file.OpenRead();
        var document = new XmlDocument();
        document.Load(stream);
        if (document.DocumentElement == null)
        {
            throw new Exception("Could not parse project file.");
        }

        var packageName = document.SelectSingleNode("Project/PropertyGroup/PackageId")?.FirstChild?.Value;
        if (packageName == null)
        {
            throw new Exception("Unable to find PackageId in project file.");
        }

        var packageVersion = document.SelectSingleNode("Project/PropertyGroup/Version")?.FirstChild?.Value;
        if (packageVersion == null)
        {
            throw new Exception("Unable to find Version in project file.");
        }

        return new Project
        {
            Name = packageName,
            Version = NuGetVersion.Parse(packageVersion)
        };
    }
}
