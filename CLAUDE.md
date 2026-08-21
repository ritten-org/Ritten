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

- The version lives in `<Version>` in `Directory.Build.props`, shared by every project so lockstep holds by construction; `build.projects` in `ritten.json` lists every shipped project (`build.project`, singular, is the one-package spelling — declare one or the other, never both). `check` fails if a `<Version>` drifts, and a rerun after a half-failed deploy pushes only the missing packages.
- `CHANGELOG.md` follows Keep a Changelog. User-visible changes get an entry under `## [Unreleased]` (bold-lead bullet style, e.g. `- **Steps can be synchronous.** …`). When a release is prepared, the version gets its own heading; a version bump without a matching changelog entry fails `check`.
- A version that is already published is "at rest": `check` passes with nothing to release, and `deploy` stops early with success (`ReleasableGate`).

## Architecture

Projects: `src/Ritten.Core` is the engine — package `Ritten.Core`, released in lockstep with the tool — and `src/Ritten` is the tool (domain modules, workflows, `Program.cs`), referencing it. The engine references no domain module; the compiler enforces the boundary. Engine namespaces map to intent: `Ritten.Contracts` holds the facts steps and rules consume (the step model plus injected run facts like `WorkflowJob`/`WorkflowEnvironment`); `Ritten.Engine` is the application front door (`WorkflowApplication` and its builder, `Result`/`Error`, `RittenProject`, the CLI vocabulary), with `.Workflows` for the declaration model (`IWorkflow`/`Job`/settings), `.Runs` for one run's assembly and execution, and `.Runtimes`/`.Rules`/`.DryRun`/`.FileSystem` as before; `Ritten.Reporting` owns both output channels — the console narrative (`IWorkflowLog`/`IWorkflowConsole`/`IProgressReporter`) and the build report.

### Engine (`src/Ritten.Core`: `Contracts/`, `Engine/`, `Reporting/`)

**Workflows and jobs are declarations, authored as object models; only steps are discovered by convention.** An `IWorkflow` (`Name` = the `ritten.json` identifier, `Label` = the printed human name) exposes stable job instances through its `Jobs` property. A job is a class extending `Job<TSettings>`, overriding `Name`, `Description` (the CLI is rendered from the model, so the help text lives with the job), `Steps` (an ordered list built with `Step.FromType<TStep>()`, which throws for a malformed step type — that's a programming error, not configuration), and as needed `Arguments`/`Configure(builder, settings, args)` (see below) or `Configure(builder, settings)` (an `IWorkflowConfiguration` — just `Services` and the `Decorators` registry, which is all the domain `Add*` extensions need; both builders implement it, so domain registrations compose at run level or application level alike, and a job or runtime can never reach the run controls of the builder that owns it) and `ValidateSettings(settings)` — the latter takes a `SettingsValidator<TSettings>` offering `Require(s => s.Build.Project)` (the error's `ritten.json` key derives from the property chain) and `RequireEnvironment("VAR")` (dry runs warn instead of failing). Validation and registration are deferred until settings exist, so a job's shape cannot depend on any project's configuration, and the whole registry→workflow→job→step tree is data at startup.

**A job declares what it can be asked for, the same way it declares what the repository tells it.** Settings come from `ritten.json`; *arguments* come from the invocation, and take the same path. `Arguments` lists `JobArgument` declarations — `JobArgument.Value<T>(name, description, read)` carries a domain reader (`string → Result<T>`, so a bad value is refused in the domain's own words) and `JobArgument.Flag(name, description)` is presence-only. `WorkflowApplication.Run` reads the supplied text into those types *before* anything is assembled, so an unknown name, an unreadable value, or a missing required one is a configuration error beside "unknown job" — never a step failure several steps in. The job then reads its values in `Configure(builder, settings, args)` through `args.Get(TheArgument)`/`args.IsSet(TheFlag)` — keyed by the declaration instance itself, so nothing is looked up by string or by type — and registers whatever domain value its steps consume (`RequestedVersion`, `ForceReinstall`). **Steps never see `JobArguments`, and never see a settings record either**: they depend on domain types, so a step stays reusable and the job stays free to source that value from anywhere. A scalar is never registered — `NuGetVersion` alone couldn't say whether it's the project's version, the feed's latest, or the one being asked for, and the type is what carries that.

`Program.cs` follows the familiar .NET hosting shape: `WorkflowApplication.CreateBuilder()` returns a `WorkflowApplicationBuilder` exposing `Workflows`, `Runtimes`, and `Services` (registrations shared by every job of every workflow; the runtime's and the job's own land later, so the more specific wins), and `builder.Build()` judges the whole registered model (duplicate names and the structural job rules — every registered job, every run) as the first validation exit, narrating failures itself and returning `Result<WorkflowApplication>`. The commands themselves are rendered from the model, not hardcoded, and by a *package* rather than the engine: `Ritten.CommandLine` (`application.CreateCommandLine(description)`) calls `application.ResolveJobs(directory)` — which resolves `ritten.json` and returns that workflow's jobs, falling back to every registered job when no project resolves so a broken configuration can still ask for help — and turns each into a command carrying its `Description` and an option per declared argument. Each `JobArgument` maps through `JobArgument.Map` and an `IJobArgumentMapper<TResult>`, which recovers the declaration's `T` so a `JobArgument<T>` becomes an `Option<T>` parsed by the domain's own reader; the engine references no CLI library, so System.CommandLine's churn stops at that package. A repository therefore only ever offers jobs its workflow can actually run. Each command then calls `application.Run(args, ct)` (`RunJobArgs` carries job/log-level/flags plus the read argument values — run-scoped things enter at `Run`, never through the builder; values are built through the declarations themselves, so one a job never declared can't be expressed, and the engine only checks that required ones are present). The engine's own flags are the ones true of every job — `--verbose`/`--quiet`, `--dry-run` (it drives the decorators) and `--auto-approve` (gate vocabulary), which is why they live on `WorkflowJob`; anything a single job honours is that job's argument instead (`--force` belongs to `install`). `Run` stages: detect the runtime and create the console narrative from it (`IWorkflowConsole` — the runtime's renderer at the flag-requested level, floored at Verbose when the runtime reports a debug request like `RUNNER_DEBUG`; errors before a runtime exists print through the engine's own Spectre renderer), resolve the project file (walking up from the cwd; `ritten.json` unless the host renames it via `builder.ProjectFileName`), select the workflow by the required `"workflow"` key, select the job *before* settings parse (a typo'd command needs no valid config to diagnose), then `WorkflowRunBuilder.Build(job)` assembles the one chosen job into a `WorkflowRun`: the job loads its settings as one operation (camelCase deserialization against the job's settings record — tolerant of unknown keys, so a project file can carry keys for newer tool versions without breaking a pinned one; every settings record extends `WorkflowSettings` — then `ValidateSettings` judges them, so invalid settings never leave the load), then services, steps, dry-run decoration, workflow-registered rules, and a DI container (`Microsoft.Extensions.DependencyInjection`, with `ValidateOnBuild`). `DefaultWorkflowRunner` executes the steps in order.

**Steps** are minimal-API-style classes, read by reflection into the `Step` model (`Contracts/Step.cs`, `Step.FromType`) — one object carrying both the facts rules and reporters consume (`Name`/`Kind`/`Produces`/`Requires`) and the internal machinery to invoke the step; a `Step` built from its facts alone (as rule tests do) cannot be invoked:

- Must carry `[Step("name", StepKind.X)]` and exactly one public `Run` method.
- `Run` returns `StepResult`, `StepResult<T>`, or `Task<>` of either. Returning `StepResult<T>` stores the `T` in workflow state for later steps.
- `Run` parameters come only from workflow state (values earlier steps produced). A nullable reference parameter is an optional read of state; `CancellationToken` is passed through. Services are constructor-injected — never `Run` parameters — which is what lets produce-then-consume order be judged on the declarations alone.
- `StepResult` with `Continue = false` and a success exit code means "nothing left to do" — the job stops early, successfully.

**Job shape is validated before anything runs.** `StepKind` (Work / Check / Gate / Publish) feeds the `IJobRule` invariants in `Engine/Rules/`: produced values must precede their consumers, gates must precede publishes, checks must precede publishes. Adding a step in the wrong position fails at startup, not mid-run.

**Runtimes are detected, never declared.** `Engine/Runtimes/` holds the model: hosts register `Runtime` candidates in a `RuntimeRegistry` (mirroring `WorkflowRegistry`; validated with the rest of the model), and `WorkflowApplication.Run` detects the active one before the run builder exists — detection is deliberately uncoupled from run assembly (`RuntimeRegistry.Detect` is public) so a host can reach the runtime's logger before anything else. A runtime *matches* on its own marker variables only and *claims* the variables it owns — compatibility surfaces included — so precedence between matching candidates derives from claims (a runtime that impersonates another's variables outranks it; unresolvable overlap fails loudly), and the claimed names are removed from the `WorkflowEnvironment` view that everything downstream, settings validation included, reads. No match falls back to the engine-owned `LocalRuntime`, which registers nothing. The active runtime supplies the console narrative (`CreateConsole`, Spectre by default) and owns its debug marker (`IsDebug` — only GitHub Actions honours `RUNNER_DEBUG`), and contributes runtime-dependent services — report sinks, the PR comment, `RunContext`, an ambient credential offered via `PostConfigure` — while GitHub-as-*destination* (`AddGitHubClient`: `IGitHubClient`, `IReleaseService`, explicit `GH_TOKEN`) stays declared by jobs, because releasing to GitHub is independent of running on it.

**Dry run is guaranteed at the client layer, and the client list is open.** Wherever an outward-reaching client is registered, its rehearsal substitute is declared beside it in the `DecoratorRegistry` (`Engine/DryRun/` — a third registry alongside workflows and runtimes, hanging off both builders as `builder.Decorators`): `builder.Decorators.Decorate<IGit, DryRunGit>()` wraps (reads pass through, side effects stop), `builder.Decorators.Replace<ICommentService, DryRunCommentService>()` substitutes outright (for clients nothing reads from). Application-level decorators join each run through `WithDecorators`, ahead of the runtime's and the job's own, so the more specific declaration wins. `WorkflowRunBuilder.Build` applies whatever pairings the model brought — the engine knows no concrete client — which removes every irreversible action from the execution path no matter what the steps do; a pairing for an unregistered client is a no-op, since a workflow only registers the capabilities it uses. Steps whose *flow* changes in a rehearsal (e.g. `ApprovalGate` skips the prompt, `NugetAuthenticate` skips credentials) read the injected `WorkflowJob.DryRun` — but side-effect safety is never a step's responsibility. A new client that reaches outside the working directory must register a pairing with itself; a new step must reach outside only through a paired client.

### Domain modules

Each domain folder — `Changelogs/`, `CodeCoverage/`, `Commands/`, `DotNet/`, `Git/`, `GitHub/`, `NuGet/`, `Releases/`, `Reporting/` — owns its client interface, options, steps (in a `Steps/` subfolder), and a `WorkflowConfigurationExtensions.cs` registering them against `IWorkflowConfiguration` (services and dry-run decorators together). External processes (dotnet, git, gh) run through `Commands/ICommandRunner`.

`Workflows/DotNetTool/` (`"workflow": "dotnet-tool"`) holds the workflow and its four job classes (`status`, `build`, `check`, `deploy`), which share the standard service registrations through the workflow-local `DotNetToolJob` base; `Workflows/DotNetPackage/` (`"dotnet-package"`) is its deliberately-identical sibling for library packages — flat siblings, duplicated declarations, no sharing *across* workflows. `Workflows/DotNetToolSettings.cs` and its siblings define the `ritten.json` schema.

**Reporting is two channels:** `IWorkflowLog` is the console narrative (rendered by `SpectreProgressReporter`), while `IBuildReport` accumulates a markdown report that `GitHubCommentSink` posts as the PR comment. Check steps typically write to both.

**Errors flow through `Engine/Result<T>` and `Error`** (accumulated, not thrown) for configuration and client calls; exceptions are reserved for programming errors.

## Testing conventions

- xunit v3 + Shouldly + NSubstitute + Verify. These are global usings in the test csproj (including `static VerifyXunit.Verifier`) — don't add `using` lines for them.
- Test folder structure mirrors the source projects (`Engine/` covers `Ritten.Core`). Shared fakes and option factories live in `tests/Ritten.Tests/Support/`; `TestOptions` builds preconfigured options records.
- Verify snapshots live in a `Snapshots/` folder next to the test file (see `VerifyModuleInitializer`). Never hand-edit or reformat `*.received.*`/`*.verified.*` files — `.editorconfig` deliberately exempts them from final-newline/whitespace rules.
- The main project has `InternalsVisibleTo` for the test project; internals are tested directly.

## Style

- `TreatWarningsAsErrors` and `GenerateDocumentationFile` are on: every public (and most internal) member needs an XML doc comment or the build fails.
- Comments in this codebase explain *why* — design intent and trade-offs in full sentences — not what the code does. Match that register.
- Max line length 150; four-space indent; file-scoped namespaces and modern C# (primary constructors, collection expressions) throughout.
