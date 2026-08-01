# Wire up real plugin discovery

## Context

The plugin architecture exists on paper (`IPlugin`, `AssemblyLoaderService`, collectible `AssemblyLoadContext`s, the `*.plugin.dll` naming convention) but nothing actually works end-to-end today:

- `Program.cs` calls `VerifyAssemblyInterfaces(...)` with a hardcoded Windows path from the original dev machine (`C:\Users\mmiha\...\M2.CardGames.War.dll`) that doesn't exist here, and `VerifyAssemblyInterfaces` is a stub that always returns `false` anyway. Its return value is discarded, so nothing downstream happens.
- The console menu loop exits on the *first* valid selection regardless of which one was picked — there's no per-command dispatch, so even if plugins were discovered there'd be nowhere to show them.
- `AssemblyLoaderService.UnloadPluginAssembly` uses `.First()` instead of `.FirstOrDefault()`, so an unknown context name throws instead of returning `false`.
- `VerifyPluginAssemblies` (directory scan) exists on the concrete class but isn't on the interface, so `Program.cs` can't reach it via DI, and it swallows load failures silently and always returns `true`.
- `source/tests/CardGames.Application.Tests/Services/AssemblyServiceTests.cs` hardcodes the same stale Windows path, so both its tests fail on any other machine.
- `source/tests/CardGames.Application.Tests/DomainMarkerTests.cs` is a copy-paste duplicate of the Domain project's test (same namespace/class, wrong expected string) — currently failing and unrelated to Application at all.
- `source/plugins/CardGames.GoFish/PluginDefinition.cs` is copy-pasted from WAR: namespace `CardGames.WAR`, class `WARPlugin`.

Goal: make plugin discovery real — scan a directory for `*.plugin.dll`, safely verify + load them, instantiate their `IPlugin` types, and surface that through the console menu (list loaded plugins, unload by name). Game *rules* (`IGameManger`) stay out of scope — this is purely getting the plugin pipeline working end-to-end.

## Design

**Where plugin DLLs come from:** Presentation must not take a compile-time reference to the plugin projects (that would let the CLR eagerly resolve/pin those assemblies in the default `AssemblyLoadContext`, defeating the whole point of loading them into a collectible context that can be unloaded later). Instead:

- `CardGames.Presentation.csproj` gets `<ProjectReference ReferenceOutputAssembly="false">` to both `CardGames.WAR.csproj` and `CardGames.GoFish.csproj`, purely to order the build. A small `AfterTargets="Build"` `Copy` target copies `CardGames.WAR.plugin.dll` / `CardGames.GoFish.plugin.dll` from their own bin output into Presentation's output directory. Discovery then scans `AppContext.BaseDirectory` — this is exactly what the `.plugin.dll` naming convention was already set up for (distinguishing plugin assemblies from ordinary dependencies sitting in the same folder).
- `CardGames.Application.Tests.csproj` gets plain `<ProjectReference>`s (normal, compile-time-visible) to the same two plugin projects — this is test-only, gets the plugin DLLs copied into the test output directory for free via standard MSBuild copy-local, and is safe: nothing in the tests actually touches `CardGames.WAR`/`CardGames.GoFish` types, so the CLR never resolves those assemblies except through the explicit `AssemblyLoadContext` calls being tested.

**`IAssemblyLoaderService` (`source/CardGames.Domain/Interfaces/IAssemblyLoaderService.cs`)** gains one method:
```csharp
IReadOnlyList<IPlugin> DiscoverPlugins(string directoryPath);
IReadOnlyList<string> GetLoadedPluginNames();
```

**`AssemblyLoaderService` (`source/CardGames.Application/Services/AssemblyLoaderService.cs`)**:
- `VerifyAssemblyInterfaces` implemented for real using `System.Reflection.MetadataLoadContext` (new package reference on `CardGames.Application.csproj`) — reflection-only load against the trusted platform assemblies + the target path, check if any exported type's interfaces match `interfaceType.FullName`. Catches/logs and returns `false` on failure, matching the existing logging style.
- `UnloadPluginAssembly`: `.First()` → `FirstOrDefault()`, return `false` when not found instead of throwing.
- Remove `VerifyPluginAssemblies` (superseded, silently-swallowing, always-`true` dead end).
- Add `DiscoverPlugins(directoryPath)`: for each `*.plugin.dll` in the directory, `VerifyAssemblyInterfaces(file, typeof(IPlugin))` → skip+log if false; `LoadPluginAssembly(file)` → skip if false; pull the just-loaded `Assembly` back off the matching `AssemblyLoadContext` (`context.Assemblies.First()`), reflect for concrete types assignable to `IPlugin`, `Activator.CreateInstance` each, collect into the returned list.
- Add `GetLoadedPluginNames()`: projects `_AssemblyLoadContexts` to their `.Name`s, for the "Unload Game" menu flow.

**`Program.cs`**: fix the menu loop so it actually dispatches instead of exiting on the first valid keystroke (`while (selection != 0)`, `switch` on the selection). On "Load Game" (2): call `DiscoverPlugins(AppContext.BaseDirectory)`, print each plugin's Name/Description/Version. On "Unload Game" (3): list `GetLoadedPluginNames()`, prompt for one, call `UnloadPluginAssembly`. "Play" (1) prints a "not implemented yet" message (no `IGameManger` implementations exist — intentionally out of scope). "About" (4) prints a short static line.

**Bug fixes bundled in (small, directly touched by this work):**
- `source/plugins/CardGames.GoFish/PluginDefinition.cs`: fix namespace to `CardGames.GoFish`, class to `GoFishPlugin`.
- Delete `source/tests/CardGames.Application.Tests/DomainMarkerTests.cs` (duplicate of the Domain test project's file, doesn't belong here).

**Tests (`AssemblyServiceTests.cs` + fixture)**:
- Fix `LoadAssembly`/`UnloadAssembly` to use the real, now-copied `CardGames.WAR.plugin.dll` path (`Path.Combine(AppContext.BaseDirectory, "CardGames.WAR.plugin.dll")`) instead of the stale Windows path.
- Add coverage for `VerifyAssemblyInterfaces` (true against the WAR plugin + `IPlugin`, false against `CardGames.Domain.dll` which doesn't implement it).
- Add coverage for `DiscoverPlugins(AppContext.BaseDirectory)` asserting both WAR and GoFish plugins are found with their expected `Name`s.

## Verification

- `dotnet build` — clean build, plugin DLLs land next to `CardGames.Presentation.dll` in its output folder.
- `dotnet test` — all tests pass (currently 2 failing: `DomainMarkerTests.Name_IsExpected`, `AssemblyServiceTests.UnloadAssembly`; `LoadAssembly` also silently broken since it never asserts).
- `dotnet run --project source/CardGames.Presentation` — pick "Load Game", confirm both "WAR!" and "Go Fish" print with their metadata; pick "Unload Game", unload one by name; confirm "0" is the only way to exit the loop.
