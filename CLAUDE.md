# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Ritten is a .NET tool (`dotnet ritten`) that runs build/release pipelines described in a `ritten.json` at the repository root. This repository builds Ritten with Ritten: the pinned tool version in `.config/dotnet-tools.json` dogfoods the previous release against this codebase.

## Commands

The SDK version is pinned in `global.json`; the target framework is net10.0.

```sh
dotnet tool restore                # once, to get the pinned ritten tool
dotnet ritten build                # clean, restore, format check, compile, test, coverage
dotnet ritten check                # build + release validation (what CI runs on PRs)
dotnet ritten status               # report version, release state, changelog

dotnet build                       # plain compile (warnings are errors, style enforced in build)
dotnet test                        # all tests
dotnet test --filter "FullyQualifiedName~ReleasableGateTests"                  # one class
dotnet test --filter "FullyQualifiedName~ReleasableGateTests.ContinuesWhenTheProjectIsReleasable"  # one test
dotnet format                      # fix formatting; `dotnet ritten build` fails on violations
```

`dotnet ritten deploy` publishes for real (tag, GitHub release, NuGet push) — never run it locally except with `--dry-run`. CI deploys via workflow_dispatch. `install-tool.sh` packs and installs the tool globally from source for manual testing.

All jobs accept `--verbose`/`-v`, `--quiet`/`-q`, `--dry-run`, and `--auto-approve`.

## Release conventions (enforced by `check` in CI)

- The version lives in `<Version>` in `src/Ritten/Ritten.csproj`.
- `CHANGELOG.md` follows Keep a Changelog. User-visible changes get an entry under `## [Unreleased]` (bold-lead bullet style, e.g. `- **Steps can be synchronous.** …`). When a release is prepared, the version gets its own heading; a version bump without a matching changelog entry fails `check`.
- A version that is already published is "at rest": `check` passes with nothing to release, and `deploy` stops early with success (`ReleasableGate`).

## Architecture

Everything lives in one project, `src/Ritten`, split into a pipeline engine and domain modules.

### Engine (`Contracts/`, `Core/`)

`Program.cs` maps each CLI job name to `PipelineHost.Run<DotNetToolPipeline, DotNetToolSettings>`. That resolves `ritten.json` (walking up from the cwd; strict camelCase deserialization, unknown keys rejected), then `PipelineHostBuilder`/`JobBuilder` compose the requested job from step types and build a DI container (`Microsoft.Extensions.DependencyInjection`, with `ValidateOnBuild`). `DefaultPipelineRunner` executes the steps in order.

**Steps** are minimal-API-style classes, discovered by reflection in `Core/StepDescriptor.cs`:

- Must carry `[Step("name", StepKind.X)]` and exactly one public `Run` method.
- `Run` returns `StepResult`, `StepResult<T>`, or `Task<>` of either. Returning `StepResult<T>` stores the `T` in pipeline state for later steps.
- `Run` parameters are injected: from pipeline state (a value some earlier step produced) first, then DI services. A nullable reference parameter is an optional read of state; `CancellationToken` is passed through. Constructor injection is for services only.
- `StepResult` with `Continue = false` and a success exit code means "nothing left to do" — the job stops early, successfully.

**Job shape is validated before anything runs.** `StepKind` (Work / Validation / Gate / Publish) feeds the `IJobRule` invariants in `Core/Rules/`: produced values must precede their consumers, gates must precede publishes, validations must precede publishes. Adding a step in the wrong position fails at startup, not mid-run.

**Dry run is guaranteed at the client layer.** `PipelineHostBuilder.Build` decorates or replaces the outward-reaching clients (`IGit`, `INuGet`, `IReleaseService`, `ICommentService`), which removes every irreversible action from the execution path no matter what the steps do. Steps whose *flow* changes in a rehearsal (e.g. `ApprovalGate` skips the prompt, `NugetAuthenticate` skips credentials) read the injected `PipelineJob.DryRun` — but side-effect safety is never a step's responsibility. A new step that reaches outside the working directory must do so through one of these clients (or a new decorated one).

### Domain modules

Each domain folder — `Changelogs/`, `CodeCoverage/`, `Commands/`, `DotNet/`, `Git/`, `GitHub/`, `NuGet/`, `Releases/`, `Reporting/` — owns its client interface, options, steps (in a `Steps/` subfolder), and a `ServiceCollectionExtensions.cs` registering them. External processes (dotnet, git, gh) run through `Commands/ICommandRunner`.

`Pipelines/DotNetToolPipeline.cs` is where the four jobs (`status`, `build`, `check`, `deploy`) are composed from steps; `Pipelines/DotNetToolSettings.cs` and its siblings define the `ritten.json` schema.

**Reporting is two channels:** `IPipelineLog` is the console narrative (rendered by `SpectreProgressReporter`), while `IBuildReport` accumulates a markdown report that `GitHubCommentSink` posts as the PR comment. Validation steps typically write to both.

**Errors flow through `Core/Result<T>` and `Error`** (accumulated, not thrown) for configuration and client calls; exceptions are reserved for programming errors.

## Testing conventions

- xunit v3 + Shouldly + NSubstitute + Verify. These are global usings in the test csproj (including `static VerifyXunit.Verifier`) — don't add `using` lines for them.
- Test folder structure mirrors `src/Ritten`. Shared fakes and option factories live in `tests/Ritten.Tests/Support/`; `TestOptions` builds preconfigured options records.
- Verify snapshots live in a `Snapshots/` folder next to the test file (see `VerifyModuleInitializer`). Never hand-edit or reformat `*.received.*`/`*.verified.*` files — `.editorconfig` deliberately exempts them from final-newline/whitespace rules.
- The main project has `InternalsVisibleTo` for the test project; internals are tested directly.

## Style

- `TreatWarningsAsErrors` and `GenerateDocumentationFile` are on: every public (and most internal) member needs an XML doc comment or the build fails.
- Comments in this codebase explain *why* — design intent and trade-offs in full sentences — not what the code does. Match that register.
- Max line length 150; four-space indent; file-scoped namespaces and modern C# (primary constructors, collection expressions) throughout.
