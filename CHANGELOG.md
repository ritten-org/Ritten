# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

- **The report is written to a file.** Every run now leaves its report at `artifacts/report.md`, so the best thing a run produces is no longer readable only on GitHub Actions.
- **Steps can reach the repository root.** `IGit.RepositoryRoot()` returns the root of the repository the workflow is running in, which isn't always the project root.

### Fixed

- **The pending comment links to the run logs.** Just the same as the completed report comment.

## [0.8.0] - 2026-08-21

### Added

- **`ritten prepare` stages the next release.** It gives the unreleased notes a version heading, brings the changelog's version links up to date, updates the project version, and formats the code. The version is derived from the unreleased notes and confirmed before anything is written; use `--version` to name one explicitly.
- **Jobs can now take arguments.** A job's `Arguments` name what it needs to run. The job reads them in `Configure` and registers the domain values its steps consume, so steps never see a command line and stay reusable.
- **The command line ships as a package.** `Ritten.CommandLine` renders a System.CommandLine surface from the registered model: commands are the jobs of whichever workflow the repository declares, carrying each job's `Description` and an option per argument it takes. A host wires it up in a line — `await command.InstallRitten(application).
- **Changelogs can be written, not just read.** `IChangelog.Write` and `Render` join the client's read half, with a dry-run pairing so a rehearsal narrates the file it would write instead of writing it.

### Changed

- **`--force` belongs to the job that honors it.** It was a global flag that every job accepted and only `install` read. It's now an argument `install` declares, so it appears exactly where it means something.
- **`Changelog` and `ChangelogEntry` are records.** Preparing a release is a change to one entry of an otherwise untouched document, which wants `with`.

## [0.7.0] - 2026-08-21

### Added

- **Tools install from source.** The dotnet-tool workflow gains an `install` job: it builds, packs, and installs the tool globally from the working tree, so no feed is needed to try a build. A version that's already installed stops the job early; `--force` reinstalls it.
- **Package metadata is checked.** `check` and `deploy` now fail when a shipped package is missing a description, readme, or license — the gaps NuGet only warns about after the push.
- **Report sections share a vocabulary.** `SectionName` is a value object for the sections of the workflow report.

### Fixed

- **Every package carries the readme.** The engine and domain packages now pack the repository readme, so their NuGet pages aren't bare.

## [0.6.0] - 2026-08-19

### Changed

- **Commands show their output.** A command's output now prints by default, so long-running tools like `dotnet test` narrate themselves the way they do outside Ritten.
- **`AddGit()` needs no tag prefix.** The prefix configures the tagging steps, not the client, so a host that never tags adds the client alone.

## [0.5.0] - 2026-08-19

### Added

- **Git can read files and changes.** `IGit` gains `Show` (a file's content as it exists at a reference) and `ChangedFiles` (the paths that differ from the committed state, untracked files included), so checks can compare against a base branch through the same client that tags.

## [0.4.0] - 2026-08-19

### Added

- **GitHub ships as a package.** The GitHub Actions runtime now ship as `Ritten.GitHub`.
- **Git and .NET ship as packages.** `Ritten.Git` and `Ritten.DotNet` carry their own tech-specific stuff.
- **The engine runs external commands.** The `Ritten.Commands` module moved from the CLI into `Ritten.Core`.
- **Steps can read the pull request under review.** The engine owns a `PullRequest` contract (number, base ref) and an `IPullRequestLabels` read returning `Label` values (name, color, description), and the active runtime supplies them — GitHub Actions fills both from its own claims and the GitHub API. Where nothing can answer — a local run, a runtime without the concept — labels read as null, distinct from a pull request that simply carries none, and a host can always register its own implementation to take precedence.

### Changed

- **IBuildReport is now IWorkflowReport.** It more accurately reflects that the report covers one workflow execution.
- **Run context is injected directly.** `RunContext` (and the new `PullRequest`) are immutable records that steps and services take by type, no longer configured or consumed through `IOptions`.
- **Exit codes are value objects.** `ExitCode` replaces the bare `int` (and the `WorkflowExitCodes` constants, which now live on it as `ExitCode.Success` and friends). It converts implicitly to and from `int` at the process boundaries.
- **Result sinks are called on start and finish.** `IWorkflowResultSink` (was `IReportSink`) can announce the run when it starts with a pending pull request comment, and publishes a single artifact.
- **`MarkdownReportRenderer` is public.** And now takes the `Report` whole, for any sinks that want to render in Markdown.

## [0.3.0] - 2026-08-19

### Changed

- **Separate `Ritten.Core` package.** With this release, `Ritten.Core` is now published for direct consumption in building custom CI workflows.

## [0.2.0] - 2026-08-19

### Added

- **The Core is a separate project.** Ritten.Core now lives in a separate project. In a future release, it will be consumable directly.
- **A repository can ship several packages.** `build.projects` lists every project to pack and publish (single-target projects keep using `build.project`). Packages release in lockstep, with one key version, one changelog entry, and one tag. The first project is used as the metadata source.
- **Decorators are builder-level now.** Service decorators and replacements for dry runs can now be registered directly on the builder using `builder.Decorators`.
- **The project file is the host's to name.** An application built on the engine can point at its own file (`builder.ProjectFileName = "build.json"`), and every error message names that file, so embedding Ritten never has to be announced in the repository.

## [0.1.0] - 2026-08-18

### Changed

- **Projects declare a `workflow`, not a `pipeline`.** Ritten's unit of jobs is now called a workflow, completing the job/step taxonomy it already shares with GitHub Actions. `ritten.json` must now say `"workflow": "dotnet-tool"`; the old `pipeline` key is no longer read.
- **Debug logging belongs to the runtime.** `RUNNER_DEBUG` now enables verbose output only when the run is actually on GitHub Actions.
- **Familiar builder pattern.** The pipeline host builder is now a pipeline application builder and follow a similar shape to the .NET standard hosts.

### Fixed

- **A test run that dies before reporting shows its output.** When `dotnet test` fails without producing any test results, the step and the pull request comment now carry the command's output tail, instead of requiring `--verbose`.

## [0.0.6] - 2026-08-17

### Changed

- **Tests now target Microsoft Testing Platform 2.** The `dotnet test` command now uses the MTP v2.x argument pattern for collecting code coverage.
- **The runtime is now detected automatically.** Ritten now identifies the environment it's running in (currently only GitHub Actions, or the local terminal), and uses that to provide platform portability.
- **`GITHUB_TOKEN` only authenticates the GitHub API on GitHub Actions.** Outside GitHub Actions the variable can belong to a different forge entirely, so local runs now use an explicit `GH_TOKEN` or the gh CLI's stored login; a `GITHUB_TOKEN` exported in the shell is no longer read.

## [0.0.5] - 2026-08-17

### Added

- **Unpublished project support.** There's a new `dotnet` pipeline for projects that ship nothing. It just runs the build and tests, but doesn't bother with a deploy.

### Changed

- **The format check verifies whitespace only.** The formatting step now runs `dotnet format whitespace --verify-no-changes` instead of the full style-and-analyzer pass, which was the slowest step in the pipeline.
- **Validation steps are now called checks.** `Validation` is now `Check`. It fits better with the domain grammar and is fewer characters to type.

### Fixed

- **`build` no longer leaves a stray format report.** The dotnet client owns where its `dotnet format` report lives and removes it once the result has been read, so `temp/` is gone again after a run.
- **Restore failures name the problem.** When `dotnet restore` fails, the NuGet and MSBuild errors, like a vulnerable package, or an unreachable feed, are parsed and reported the same way build failures are.
- **The pull request comment no longer vaguely mentions unknown errors.** When a failing step writes nothing to the report, the comment now shows the step's name and its errors.
- **Command failure messages read stdout too.** MSBuild-family tools report their errors on standard output, so a failed command's message now falls back to the stdout tail when stderr is empty, rather than reporting the exit code alone.

## [0.0.4] - 2026-08-16

### Added

- **NuGet package projects.** A `dotnet-package` pipeline runs the same jobs as the tool pipeline for plain library packages; declare `"pipeline": "dotnet-package"` to use it.

### Changed

- **`ritten.json` must declare its pipeline.** Every project now names the pipeline it runs — `"pipeline": "dotnet-tool"` or `"dotnet-package"`.
- **`deploy` needs no environment variables up front.** The repository ID requirement was left over from before releases were addressed by owner and name, and the NuGet API key is now resolved by a dedicated `nuget auth` step: from `RITTEN_NUGET_API_KEY` when set, otherwise by asking at the terminal.
- **Report sections are more granular.** Changelog remarks come under **Changelog** and the version's standing against the feed under **Version**, leaving **Release** for what actually shipped.

## [0.0.3] - 2026-08-15

### Added

- **Steps can be synchronous.** Step classes can return a plain `StepResult` or `StepResult<T>` directly.
- **`ritten status`.** Reports the current state of the project, including its version, release state, and the changelog,.
- **Code coverage.** Tests collect coverage by default via the coverlet collector, and the line and branch rates are reported alongside the results. A `"coverage"` section in `ritten.json` with `line`/`branch` minimums makes the numbers enforced. Requires the `coverlet.collector` package in test projects.
- **The pull request comment links to the run logs.** The comment now ends with a link to the GitHub Actions run page for when the report isn't detailed enough.
- **The repository URL is picked up automatically.** Ritten reads the URL from the project file's `RepositoryUrl`, or failing that the origin git remote, when it's not given explicitly.

### Changed

- **The repository is now set using `repository`.** Rather than being a property of `changelog`, the `repository` setting now hangs off the document root.
- **Deployments no longer depend on GitHub Actions.** The GitHub release is created against the repository derived from the project, instead of the repository ID that only Actions provides, so `deploy` can run from anywhere.
- **GitHub authentication is picked up ambiently.** When neither `GH_TOKEN` nor `GITHUB_TOKEN` is set, Ritten asks the gh CLI for its stored login before falling back to anonymous access, so a local `deploy` works if you're signed in to `gh`.
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

[Unreleased]: https://github.com/ritten-org/Ritten/compare/v0.8.0...HEAD
[0.8.0]: https://github.com/ritten-org/Ritten/compare/v0.7.0...v0.8.0
[0.7.0]: https://github.com/ritten-org/Ritten/compare/v0.6.0...v0.7.0
[0.6.0]: https://github.com/ritten-org/Ritten/compare/v0.5.0...v0.6.0
[0.5.0]: https://github.com/ritten-org/Ritten/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/ritten-org/Ritten/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/ritten-org/Ritten/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/ritten-org/Ritten/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/ritten-org/Ritten/compare/v0.0.6...v0.1.0
[0.0.6]: https://github.com/ritten-org/Ritten/compare/v0.0.5...v0.0.6
[0.0.5]: https://github.com/ritten-org/Ritten/compare/v0.0.4...v0.0.5
[0.0.4]: https://github.com/ritten-org/Ritten/compare/v0.0.3...v0.0.4
[0.0.3]: https://github.com/ritten-org/Ritten/compare/v0.0.2...v0.0.3
[0.0.2]: https://github.com/ritten-org/Ritten/compare/v0.0.1...v0.0.2
[0.0.1]: https://github.com/ritten-org/Ritten/releases/tag/v0.0.1
