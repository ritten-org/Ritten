# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **Steps can be synchronous.** Step classes can return a plain `StepResult` or `StepResult<T>` directly.
- **`ritten status`.** Reports the current state of the project, including its version, release state, and the changelog,.
- **Code coverage.** Tests collect coverage by default via the coverlet collector, and the line and branch rates are reported alongside the results. A `"coverage"` section in `ritten.json` with `line`/`branch` minimums makes the numbers enforced. Requires the `coverlet.collector` package in test projects.

### Changed

- **Determining the release state is its own step.** Similar to the changelog step refactor in the last iteration, this should help keep steps loosely coupled and composable.
- **Validating the changelog links is its own step.** The links are a lint on the document, applying in every release state, while the entry requirement attaches to the release being prepared.
- **Namespaces are organized by domain.** Each domain (Changelogs, DotNet, Git, NuGet, GitHub, and Releases) owns its client, steps, and options in one place, instead of being split between an infrastructure tree and a pipelines tree.

## [0.0.2] - 2026-08-15

### Added

- **The `build` job now packs the project.** This means packaging errors surface in pull requests instead of at deploy time.
- **Backports.** Versions are now validated against their own release line, so a fix can ship to an older major when a newer one is already out. Projects that treat the major number as a product version can set `"release": { "lines": "minor" }` to allow releasing to older minors too. Backported releases are not marked latest.
- **Steps are now minimal-API-style methods.** A step's `Run` method can use parameter-based dependency injection to inject a value `T` returned by any earlier step, and returning `Task<StepResult<T>>` is how you provide that `T` in the first place. Task ordering is validated at runtime.
- **Job-shape rules.** A job's shape is validated before anything runs: steps must run in produce-then-consume order, nothing irreversible can run before a gate, and validations must come ahead of any publish step.
- **`--quiet` shows the job's shape.** Step names, kind glyphs, outcomes, and timings render at every verbosity; quiet silences only what the steps say in between.

### Changed

- **The `verify` job is now called `build`, and the old `build` is now `check`.** `build` answers "does it compile and pass its tests"; `check` is the pull request gate, asking "could this ship". It does everything `build` does, plus release validation.
- **The `dotnet restore` step now happens before `dotnet format`.** Running `dotnet format` implicitly restore packages, meaning restore failures got logged as formatting errors with a generic exception. Running a dedicated restore first causes the error to get reported more accurately.
- **The `dotnet format` step now runs with `--no-restore`.** In combination with the above change, this stops Ritten trying to restore packages a second time.
- **`check` now recognises when a version is the latest of its line.** When a version has already been published and is at the tip of its major/minor line, the project is considered "at rest", so new changes are expected to accrue under `[Unreleased]`, and no version bump is required.
- **Running `deploy` on a project that has already been deployed now succeeds.** Deployments should be re-runnable in the case of intermittent issues, and even just for reassurance. The deploymeny reports if the version is already published and stops before tagging or pushing, and exits 0.
- **Steps declarations are attribute-based.** Steps must now declare a `[Step]` attribute that describes them as work, validation, gate, or publish, which is shown as a colored glyph beside each step in the terminal.
- **Reading the changelog is its own step.** This allows better decoupling between multiple steps that need to access the changelog without validating it.
- **Normal output is noisier.** Steps report more about what they're doing by default. Raw commands now require `--verbose` for their output to show.

### Removed

- **`RITTEN_SKIP_VERSION_CHECK` and `RITTEN_SKIP_CHANGELOG` are gone.** They existed for dependabot pull requests, which the at-rest state now handles naturally.
- **`IPipelineState` is gone.** Steps take their inputs as `Run` parameters and return what they produce, so nothing needed the blackboard any more.

## [0.0.1] - 2026-08-15

Initial release.

[Unreleased]: https://github.com/ritten-org/Ritten/compare/v0.0.2...HEAD
[0.0.2]: https://github.com/ritten-org/Ritten/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/ritten-org/Ritten/releases/tag/v0.0.1
