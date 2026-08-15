# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.0.2]

### Changed

- **The `verify` job is now called `build`, and the old `build` is now `check`.** `build` answers "does it compile and pass its tests"; `check` is the pull request gate, asking "could this ship". It does everything `build` does, plus release validation.
- **The `build` job now packs the project.** This means packaging errors surface in pull requests instead of at deploy time.
- **The `dotnet restore` step now happens before `dotnet format`.** Running `dotnet format` implicitly restore packages, meaning restore failures got logged as formatting errors with a generic exception. Running a dedicated restore first causes the error to get reported more accurately.
- **The `dotnet format` step now runs with `--no-restore`.** In combination with the above change, this stops Ritten trying to restore packages a second time.

## [0.0.1] - 2026-08-15

Initial release.

[0.0.2]: https://github.com/ritten-org/Ritten/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/ritten-org/Ritten/releases/tag/v0.0.1
