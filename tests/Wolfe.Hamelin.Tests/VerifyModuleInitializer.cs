using System.Runtime.CompilerServices;
using NuGet.Versioning;
using Wolfe.Hamelin.Changelogs;

namespace Wolfe.Hamelin.Tests;

/// <summary>
/// Global Verify configuration.
/// Snapshots live in a <c>Snapshots</c> folder next to the test source file that produced them.
/// </summary>
public static class VerifyModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        DerivePathInfo((sourceFile, _, type, method) => new PathInfo(
            directory: Path.Combine(Path.GetDirectoryName(sourceFile)!, "Snapshots"),
            typeName: type.Name,
            methodName: method.Name
        ));

        // Changelog snapshots contain real release dates and versions; render them literally.
        VerifierSettings.DontScrubDateTimes();
        VerifierSettings.AddExtraSettings(settings => settings.Converters.Add(new NuGetVersionConverter()));

        // Derived from Entries; snapshotting it would duplicate the first entry.
        VerifierSettings.IgnoreMember<Changelog>(c => c.Unreleased);
    }

    private sealed class NuGetVersionConverter : WriteOnlyJsonConverter<NuGetVersion>
    {
        public override void Write(VerifyJsonWriter writer, NuGetVersion value) => writer.WriteValue(value.ToString());
    }
}
