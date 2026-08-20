namespace Ritten.DotNet;

/// <summary>
/// The metadata for a package, as read from the project file.
/// </summary>
public sealed record PackageMetadata
{
    /// <summary>
    /// The SDK substitutes this placeholder when a project sets no description, so carrying it counts as unset.
    /// </summary>
    private const string DefaultDescription = "Package Description";

    /// <summary>
    /// The package description (<c>Description</c>).
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The packed readme (<c>PackageReadmeFile</c>).
    /// </summary>
    public string? ReadmeFile { get; init; }

    /// <summary>
    /// The SPDX license expression (<c>PackageLicenseExpression</c>).
    /// </summary>
    public string? LicenseExpression { get; init; }

    /// <summary>
    /// The packed license file (<c>PackageLicenseFile</c>).
    /// </summary>
    public string? LicenseFile { get; init; }

    /// <summary>
    /// Whether the package carries a real description, not the SDK's placeholder.
    /// </summary>
    public bool HasDescription => !string.IsNullOrEmpty(Description) && Description != DefaultDescription;

    /// <summary>
    /// Whether the package carries a readme.
    /// </summary>
    public bool HasReadme => !string.IsNullOrEmpty(ReadmeFile);

    /// <summary>
    /// Whether the package carries a license, as an expression or a packed file.
    /// </summary>
    public bool HasLicense => !string.IsNullOrEmpty(LicenseExpression) || !string.IsNullOrEmpty(LicenseFile);
}
