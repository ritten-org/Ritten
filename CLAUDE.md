# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

Ritten is a .NET tool (`dotnet ritten`) that runs build/release workflows described in a `ritten.json` at the repository root. This repository builds Ritten with Ritten: the pinned tool version in `.config/dotnet-tools.json` dogfoods the previous release against this codebase.

## Commands

The SDK version is pinned in `global.json`; the target framework is net10.0.

```sh
dotnet tool restore                # once, to get the pinned ritten tool
dotnet ritten build                # clean, restore, format check, compile, test, coverage
dotnet ritten check                # build + release validation (what CI runs on PRs)
dotnet ritten status               # report version, release state, changelog

dotnet build                       # plain compile (warnings are errors, style enforced in build)
dotnet test                        # all tests (MTP mode — the runner is declared in global.json)
dotnet test --filter-class "*ReleasableGateTests"                    # one class
dotnet test --filter-method "*.ContinuesWhenTheProjectIsReleasable"  # one test
dotnet format                      # fix formatting; `dotnet ritten build` fails on violations
```

`dotnet ritten deploy` publishes for real (tag, GitHub release, NuGet push) — never run it locally except with `--dry-run`. CI deploys via workflow_dispatch. `install-tool.sh` packs and installs the tool globally from source for manual testing.

All jobs accept `--verbose`/`-v`, `--quiet`/`-q`, `--dry-run`, and `--auto-approve`.

## Release conventions (enforced by `check` in CI)

- The version lives in `<Version>` in `src/Ritten/Ritten.csproj`.
- `CHANGELOG.md` follows Keep a Changelog. User-visible changes get an entry under `## [Unreleased]` (bold-lead bullet style, e.g. `- **Steps can be synchronous.** …`). When a release is prepared, the version gets its own heading; a version bump without a matching changelog entry fails `check`.
- A version that is already published is "at rest": `check` passes with nothing to release, and `deploy` stops early with success (`ReleasableGate`).

## Architecture

Everything lives in one project, `src/Ritten`, split into a workflow engine and domain modules.

### Engine (`Contracts/`, `Core/`)

**Workflows and jobs are declarations, authored as object models; only steps are discovered by convention.** An `IWorkflow` (`Name` = the `ritten.json` identifier, `Label` = the printed human name) exposes stable job instances through its `Jobs` property. A job is a class extending `Job<TSettings>`, overriding `Name`, `Steps` (an ordered list built with `Step.FromType<TStep>()`, which throws for a malformed step type — that's a programming error, not configuration), and as needed `ConfigureServices(services, settings)` and `ValidateSettings(settings)` — the latter takes a `SettingsValidator<TSettings>` offering `Require(s => s.Build.Project)` (the error's `ritten.json` key derives from the property chain) and `RequireEnvironment("VAR")` (dry runs warn instead of failing). Validation and registration are deferred until settings exist, so a job's shape cannot depend on any project's configuration, and the whole registry→workflow→job→step tree is data at startup.

`Program.cs` follows the familiar .NET hosting shape: `WorkflowApplication.CreateBuilder()` returns a `WorkflowApplicationBuilder` exposing `Workflows`, `Runtimes`, and `Services` (registrations shared by every job of every workflow; the runtime's and the job's own land later, so the more specific wins), and `builder.Build()` judges the whole registered model (duplicate names and the structural job rules — every registered job, every run) as the first validation exit, narrating failures itself and returning `Result<WorkflowApplication>`. Each CLI job command then calls `application.Run(args, ct)` (`RunJobArgs` carries job/log-level/flags — run-scoped things enter at `Run`, never through the builder). `Run` stages: detect the runtime and create the console narrative from it (`IWorkflowConsole` — the runtime's renderer at the flag-requested level, floored at Verbose when the runtime reports a debug request like `RUNNER_DEBUG`; errors before a runtime exists print through the engine's own Spectre renderer), resolve `ritten.json` (walking up from the cwd), select the workflow by the required `"workflow"` key, select the job *before* settings parse (a typo'd command needs no valid config to diagnose), then `WorkflowRunBuilder.Build(job)` assembles the one chosen job into a `WorkflowRun`: the job loads its settings as one operation (camelCase deserialization against the job's settings record — tolerant of unknown keys, so a project file can carry keys for newer tool versions without breaking a pinned one; every settings record extends `WorkflowSettings` — then `ValidateSettings` judges them, so invalid settings never leave the load), then services, steps, dry-run decoration, workflow-registered rules, and a DI container (`Microsoft.Extensions.DependencyInjection`, with `ValidateOnBuild`). `DefaultWorkflowRunner` executes the steps in order.

**Steps** are minimal-API-style classes, read by reflection into the `Step` model (`Contracts/Step.cs`, `Step.FromType`) — one object carrying both the facts rules and reporters consume (`Name`/`Kind`/`Produces`/`Requires`) and the internal machinery to invoke the step; a `Step` built from its facts alone (as rule tests do) cannot be invoked:

- Must carry `[Step("name", StepKind.X)]` and exactly one public `Run` method.
- `Run` returns `StepResult`, `StepResult<T>`, or `Task<>` of either. Returning `StepResult<T>` stores the `T` in workflow state for later steps.
- `Run` parameters come only from workflow state (values earlier steps produced). A nullable reference parameter is an optional read of state; `CancellationToken` is passed through. Services are constructor-injected — never `Run` parameters — which is what lets produce-then-consume order be judged on the declarations alone.
- `StepResult` with `Continue = false` and a success exit code means "nothing left to do" — the job stops early, successfully.

**Job shape is validated before anything runs.** `StepKind` (Work / Check / Gate / Publish) feeds the `IJobRule` invariants in `Core/Rules/`: produced values must precede their consumers, gates must precede publishes, checks must precede publishes. Adding a step in the wrong position fails at startup, not mid-run.

**Runtimes are detected, never declared.** `Core/Runtimes/` holds the model: hosts register `Runtime` candidates in a `RuntimeRegistry` (mirroring `WorkflowRegistry`; validated with the rest of the model), and `WorkflowApplication.Run` detects the active one before the run builder exists — detection is deliberately uncoupled from run assembly (`RuntimeRegistry.Detect` is public) so a host can reach the runtime's logger before anything else. A runtime *matches* on its own marker variables only and *claims* the variables it owns — compatibility surfaces included — so precedence between matching candidates derives from claims (a runtime that impersonates another's variables outranks it; unresolvable overlap fails loudly), and the claimed names are removed from the `WorkflowEnvironment` view that everything downstream, settings validation included, reads. No match falls back to the engine-owned `LocalRuntime`, which registers nothing. The active runtime supplies the console narrative (`CreateConsole`, Spectre by default) and owns its debug marker (`IsDebug` — only GitHub Actions honours `RUNNER_DEBUG`), and contributes runtime-dependent services — report sinks, the PR comment, `RunContext`, an ambient credential offered via `PostConfigure` — while GitHub-as-*destination* (`AddGitHubClient`: `IGitHubClient`, `IReleaseService`, explicit `GH_TOKEN`) stays declared by jobs, because releasing to GitHub is independent of running on it.

**Dry run is guaranteed at the client layer.** `WorkflowRunBuilder.Build` decorates or replaces the outward-reaching clients (`IGit`, `INuGet`, `IReleaseService`, `ICommentService`), which removes every irreversible action from the execution path no matter what the steps do. Steps whose *flow* changes in a rehearsal (e.g. `ApprovalGate` skips the prompt, `NugetAuthenticate` skips credentials) read the injected `WorkflowJob.DryRun` — but side-effect safety is never a step's responsibility. A new step that reaches outside the working directory must do so through one of these clients (or a new decorated one).

### Domain modules

Each domain folder — `Changelogs/`, `CodeCoverage/`, `Commands/`, `DotNet/`, `Git/`, `GitHub/`, `NuGet/`, `Releases/`, `Reporting/` — owns its client interface, options, steps (in a `Steps/` subfolder), and a `ServiceCollectionExtensions.cs` registering them. External processes (dotnet, git, gh) run through `Commands/ICommandRunner`.

`Workflows/DotNetTool/` (`"workflow": "dotnet-tool"`) holds the workflow and its four job classes (`status`, `build`, `check`, `deploy`), which share the standard service registrations through the workflow-local `DotNetToolJob` base; `Workflows/DotNetPackage/` (`"dotnet-package"`) is its deliberately-identical sibling for library packages — flat siblings, duplicated declarations, no sharing *across* workflows. `Workflows/DotNetToolSettings.cs` and its siblings define the `ritten.json` schema.

**Reporting is two channels:** `IWorkflowLog` is the console narrative (rendered by `SpectreProgressReporter`), while `IBuildReport` accumulates a markdown report that `GitHubCommentSink` posts as the PR comment. Check steps typically write to both.

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
