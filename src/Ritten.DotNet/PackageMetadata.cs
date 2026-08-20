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
    /// The license URL (<c>PackageLicenseUrl</c>), deprecated in favour of the expression and the file.
    /// </summary>
    public string? LicenseUrl { get; init; }

    /// <summary>
    /// The packed icon (<c>PackageIcon</c>).
    /// </summary>
    public string? Icon { get; init; }

    /// <summary>
    /// The icon URL (<c>PackageIconUrl</c>), deprecated in favour of the packed icon.
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// The project's home page (<c>PackageProjectUrl</c>).
    /// </summary>
    public string? ProjectUrl { get; init; }

    /// <summary>
    /// The search tags (<c>PackageTags</c>).
    /// </summary>
    public string? Tags { get; init; }

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

    /// <summary>
    /// Whether the package carries an icon.
    /// </summary>
    public bool HasIcon => !string.IsNullOrEmpty(Icon);

    /// <summary>
    /// Whether the package carries a project URL.
    /// </summary>
    public bool HasProjectUrl => !string.IsNullOrEmpty(ProjectUrl);

    /// <summary>
    /// Whether the package carries search tags.
    /// </summary>
    public bool HasTags => !string.IsNullOrEmpty(Tags);

    /// <summary>
    /// Whether the only license is the deprecated URL form, which nuget.org no longer accepts.
    /// </summary>
    public bool LicensedByUrlOnly => !HasLicense && !string.IsNullOrEmpty(LicenseUrl);

    /// <summary>
    /// Whether the only icon is the deprecated URL form.
    /// </summary>
    public bool IconByUrlOnly => !HasIcon && !string.IsNullOrEmpty(IconUrl);
}
