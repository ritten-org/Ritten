# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.2]

### Added

- **The `build` job now packs the project.** This means packaging errors surface in pull requests instead of at deploy time.
- **Backports.** Versions are now validated against their own release line, so a fix can ship to an older major when a newer one is already out. Projects that treat the major number as a product version can set `"release": { "lines": "minor" }` to allow releasing to older minors too. Backported releases are not marked latest.

### Changed

- **The `verify` job is now called `build`, and the old `build` is now `check`.** `build` answers "does it compile and pass its tests"; `check` is the pull request gate, asking "could this ship". It does everything `build` does, plus release validation.
- **The `dotnet restore` step now happens before `dotnet format`.** Running `dotnet format` implicitly restore packages, meaning restore failures got logged as formatting errors with a generic exception. Running a dedicated restore first causes the error to get reported more accurately.
- **The `dotnet format` step now runs with `--no-restore`.** In combination with the above change, this stops Ritten trying to restore packages a second time.
- **`check` now recognises when a version is the latest of its line.** When a version has already been published and is at the tip of its major/minor line, the project is considered "at rest", so new changes are expected to accrue under `[Unreleased]`, and no version bump is required.
- **Running `deploy` on a project that has already been deployed now succeeds.** Deployments should be re-runnable in the case of intermittent issues, and even just for reassurance. The deploymeny reports if the version is already published and stops before tagging or pushing, and exits 0.

### Removed

- **`RITTEN_SKIP_VERSION_CHECK` and `RITTEN_SKIP_CHANGELOG` are gone.** They existed for dependabot pull requests, which the at-rest state now handles naturally.

## [0.0.1] - 2026-08-15

Initial release.

[0.0.2]: https://github.com/ritten-org/Ritten/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/ritten-org/Ritten/releases/tag/v0.0.1
