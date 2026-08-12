using System.Runtime.CompilerServices;

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
    }
}
