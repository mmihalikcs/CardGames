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
dotnet run --project source/CardGames.Presentation         # run the console app
```

CI (`.github/workflows/dotnet.yml`) runs `dotnet restore`, `dotnet build --no-restore`, then `dotnet test --no-build --filter category=post-build`. The `category`/`post-build` trait comes from `CardGames.Common.Tests/TestCaseConstants.cs` (`BUILD_TEST_TRAIT_NAME` / `BUILD_TEST_TRAIT_VALUE`) — apply it via `[Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]` on tests that should run in that CI gate.

Target framework is `net10.0` across all projects, with `Nullable` and `ImplicitUsings` enabled — keep new projects consistent with that.

## Architecture

Layered solution under `source/`:

- **CardGames.Domain** — core models (`Card`, `DeckOfCards`), enums (`Suit`, `Rank`), and the public interfaces (`IPlugin`, `IGameManger`, `IAssemblyLoaderService`) that other layers and plugins depend on. No dependencies on other projects.
- **CardGames.Application** — application services, notably `AssemblyLoaderService`, which implements `IAssemblyLoaderService`. Depends on Domain only.
- **CardGames.Infrastructure** — infrastructure concerns. Depends on Domain only (currently empty of app-level services).
- **CardGames.Presentation** — the console entry point (`Program.cs`). Wires up a generic `IHost` with DI (`Microsoft.Extensions.Hosting`/`DependencyInjection`), loads plugins, and drives a console menu loop via `ConsoleRenderer`.
- **source/plugins/** — individual games (`CardGames.WAR`, `CardGames.GoFish`) built as separate class libraries, each implementing `IPlugin` and depending only on `CardGames.Domain`.

### Plugin loading model

Each game plugin project sets `<AssemblyName>$(MSBuildProjectName).plugin</AssemblyName>`, producing a `*.plugin.dll` output. `AssemblyLoaderService` discovers plugins by scanning a directory for files matching `*.plugin.dll` and loads each into its own `System.Runtime.Loader.AssemblyLoadContext` (collectible, so it can later be unloaded via `UnloadPluginAssembly`). This isolation is intentional — new plugin functionality should go through this same load/unload lifecycle rather than direct project references from Presentation.

### Tests

`source/tests/` mirrors the layers being tested (`CardGames.Domain.Tests`, `CardGames.Application.Tests`) plus `CardGames.Common.Tests`, a shared library (not a test project itself) holding cross-cutting test constants like `TestCaseConstants`. Tests use xUnit.

## Claude Code rules

- Save all plan-mode documents and any superpowers-style spec files (design docs, implementation plans, spec write-ups produced before/during implementation) to `/docs/plans`, not elsewhere.
