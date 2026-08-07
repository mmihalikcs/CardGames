# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet restore                                           # restore dependencies
dotnet build                                              # build the solution
dotnet test                                                # run all tests
dotnet test --filter category=post-build                  # run only post-build tests (what CI runs after a build)
dotnet test --filter FullyQualifiedName~ClassName          # run a single test class
dotnet test --filter FullyQualifiedName~ClassName.MethodName  # run a single test method
dotnet run --project source/CardGames.Console               # run the console app
```

CI (`.github/workflows/dotnet.yml`) runs `dotnet restore`, `dotnet build --no-restore`, then `dotnet test --no-build --filter category=post-build`. The `category`/`post-build` trait comes from `CardGames.Common.Tests/TestCaseConstants.cs` (`BUILD_TEST_TRAIT_NAME` / `BUILD_TEST_TRAIT_VALUE`) — apply it via `[Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]` on tests that should run in that CI gate. `TestCaseConstants` also defines `EXTENDED_TEST_TRAIT_VALUE` ("test-suite") for slower/more-extensive coverage that isn't part of that gate.

`CardGames.Godot.Tests` (gdUnit4Net, see below) can't use xUnit's `[Trait]`/`--filter category=...` — its adapter needs `[TestCategory(...)]` and a `TestCategory=...` filter instead, so CI runs it as its own step:
```bash
GODOT_BIN=/path/to/Godot_v4.6.2-stable_mono_linux.x86_64 dotnet test source/CardGames.Godot/Tests/CardGames.Godot.Tests.csproj --filter TestCategory=post-build --settings source/CardGames.Godot/Tests/.runsettings   # CI gate for the Godot client
GODOT_BIN=/path/to/Godot_v4.6.2-stable_mono_linux.x86_64 dotnet test source/CardGames.Godot/Tests/CardGames.Godot.Tests.csproj --filter TestCategory=test-suite --settings source/CardGames.Godot/Tests/.runsettings  # slower/extensive Godot-client coverage
```
`GODOT_BIN` must point at a real Godot 4.6.2 mono executable. `--settings .../.runsettings` forces the `--headless` flag on the Godot process gdUnit4Net launches for `[RequireGodotRuntime]` tests — without it, that process tries to open a real display and fails outright on machines with none (discovery alone doesn't need it, only actual test execution does).

Target framework is `net10.0` across all projects, with `Nullable` and `ImplicitUsings` enabled — keep new projects consistent with that.

## Architecture

Solution defined in `CardGames.slnx`. Application layers live under `source/`, all test projects live under the sibling top-level `tests/`:

- **CardGames.Domain** — core models (`Card`, `DeckOfCards`), enums (`Suit`, `Rank`), and the public interfaces (`IPlugin`, `IGameManager`, `IGameIO`, `IAssemblyLoaderService`) that other layers and plugins depend on. No dependencies on other projects. Also holds the structured presentation contract under `Interaction/` (`GameEvent`/`GamePrompt`/`PromptResponse`, `IGameChannel`) that plugins actually build against - see "Presentation contract" below.
- **CardGames.Application** — application services: `AssemblyLoaderService` (implements `IAssemblyLoaderService`) and `SettingsService` (implements `ISettingsService` — reads/writes `ApplicationSettings` as JSON under the user's app-data folder). Depends on Domain only.
- **CardGames.Console** — the console entry point (`Program.cs`). Wires up a generic `IHost` with DI (`Microsoft.Extensions.Hosting`/`DependencyInjection`), loads plugins, and drives a console menu loop via `ConsoleRenderer`.
- **source/plugins/** — individual games (`CardGames.WAR`, `CardGames.GoFish`) built as separate class libraries, each implementing `IPlugin` and depending only on `CardGames.Domain`.

### Presentation contract

Plugins never touch `IGameIO` (`Write`/`WriteLine`/`ReadLine`) directly - that's a raw text-transport
primitive implemented only by `ConsoleGameIO` and `NetworkGameIO`. `IPlugin.CreateGameManager` takes an
`IGameChannel` instead: `Publish(GameEvent)` for "what happened" and `Await(GamePrompt): PromptResponse`
for "what do you need from a seat" (`ConfirmPrompt`/`ChoicePrompt`/`TextPrompt`, all in
`CardGames.Domain.Interaction`). Each plugin defines its own `GameEvent` leaf types with a `Describe()`
override for text rendering; `TextGameChannel` (`CardGames.Domain.Interaction`) is the generic adapter
that wraps any `IGameIO` and implements `IGameChannel` on top of it, with zero plugin-specific knowledge -
this is what lets `ConsoleGameIO` and `NetworkGameIO` stay untouched text transports while game logic
only ever describes facts and typed requests, never formatted prose or raw input parsing.

### Plugin loading model

Each game plugin project sets `<AssemblyName>$(MSBuildProjectName).plugin</AssemblyName>`, producing a `*.plugin.dll` output. `AssemblyLoaderService` discovers plugins by scanning a directory for files matching `*.plugin.dll` and loads each into its own `System.Runtime.Loader.AssemblyLoadContext` (collectible, so it can later be unloaded via `UnloadPluginAssembly`). This isolation is intentional — new plugin functionality should go through this same load/unload lifecycle rather than direct project references from CardGames.Console.

### Tests

`tests/` mirrors the layers being tested (`CardGames.Domain.Tests`, `CardGames.Application.Tests`, `CardGames.WAR.Tests`, `CardGames.GoFish.Tests`, ...) plus `CardGames.Common.Tests`, a shared library (not a test project itself) holding cross-cutting test constants like `TestCaseConstants`. Tests use xUnit.

**Exception**: `CardGames.Godot.Tests` lives nested inside `source/CardGames.Godot/Tests/`, not under top-level `tests/`. It uses gdUnit4Net (MIT-licensed), whose scene runner (`ISceneRunner.Load("res://...")`) can only resolve `res://` scene/script paths within the same Godot project tree being tested, so the test project needs its own `project.godot` sitting close enough to the real one to share its `Scenes/`/`Scripts/` resources. It gets there via a symlink (`Scripts` → `../Scripts`, `Scenes` → `../Scenes`, created by the `CreateSymlinks` MSBuild target in its `.csproj` before compilation) rather than a `ProjectReference` to `CardGames.Godot.csproj` — referencing and symlinking the same files would double-compile them (CS0436). `CardGames.Godot.csproj` in turn excludes `Tests/**/*.cs` from its own compile glob so it doesn't try to compile the gdUnit4Net-dependent test files.

## Claude Code rules

- Save all plan-mode documents and any superpowers-style spec files (design docs, implementation plans, spec write-ups produced before/during implementation) to `/docs/plans`, not elsewhere.
