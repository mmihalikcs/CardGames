# Rudimentary Settings Menu (Plugin Discovery Directory)

## Context

The plugin discovery directory is currently hardcoded to `AppContext.BaseDirectory` at the single call site in `Program.cs` (`assemblyLoaderService.DiscoverPlugins(AppContext.BaseDirectory)`). There is no way to point the app at a different plugin folder without rebuilding, and no configuration/persistence infrastructure exists anywhere in the repo (no appsettings.json, no `IConfiguration`/`IOptions<T>` usage, no JSON serialization, no settings classes). This plan adds a minimal "Settings" menu item — positioned before "About" — that lets the user view and change the plugin discovery directory, persists that choice to a JSON file in the OS user-config folder so it survives restarts, and wires the stored value into the actual `DiscoverPlugins` call.

Decisions already confirmed with the user:
- Persist to `Environment.SpecialFolder.ApplicationData` (e.g. `~/.config/CardGames/settings.json` on Linux, `%AppData%\CardGames\settings.json` on Windows) — not next to the executable, since that gets wiped by clean rebuilds.
- If the user enters a plugin directory that doesn't exist yet, warn but still save it (rudimentary, non-blocking).

## Layering

- **`CardGames.Domain`** gets the settings model and the service interface — dependency-free, mirrors how `IAssemblyLoaderService` lives in Domain today.
- **`CardGames.Infrastructure`** gets the concrete `SettingsService` (file I/O). This project is currently empty and has **no `ProjectReference` at all** (verified — `CardGames.Infrastructure.csproj` has only a `PropertyGroup`, despite CLAUDE.md saying it depends on Domain). Must add a `ProjectReference` to `CardGames.Domain.csproj`.
- **`CardGames.Presentation`** needs no new project references — it already references `CardGames.Infrastructure.csproj` directly and gets `CardGames.Domain` transitively via `CardGames.Application`.

## Implementation

**1. `source/CardGames.Domain/Models/ApplicationSettings.cs`** (new)
```csharp
namespace CardGames.Domain.Models;

public class ApplicationSettings
{
    public string PluginDirectory { get; set; } = string.Empty;
}
```
Empty string is the "unset" sentinel, checked via `string.IsNullOrWhiteSpace`.

**2. `source/CardGames.Domain/Interfaces/ISettingsService.cs`** (new)
```csharp
namespace CardGames.Domain.Interfaces;

public interface ISettingsService
{
    ApplicationSettings Load();
    void Save(ApplicationSettings settings);
}
```
`Load()` never throws, never returns null — returns `new ApplicationSettings()` defaults if the file is missing or fails to parse (catch + `LogWarning`, same pattern `AssemblyLoaderService` uses for its own exception handling). `Save()` creates the target directory if missing and overwrites the file; it does not validate `PluginDirectory` — that's a menu-layer concern per the "warn but allow" decision.

**3. `source/CardGames.Infrastructure/Services/SettingsService.cs`** (new)
- `public SettingsService(ILoggerFactory loggingFactory)` — same single-arg shape as `AssemblyLoaderService`, so DI registration is a one-liner.
- Internal second constructor `SettingsService(ILoggerFactory, string settingsFilePath)` for test isolation (writes to a temp path instead of the real per-user config folder). Requires `<InternalsVisibleTo Include="CardGames.Infrastructure.Tests" />` in the csproj.
- Default path: `Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "CardGames", "settings.json")`.
- `System.Text.Json` with `WriteIndented = true`; synchronous (matches the rest of the codebase — no async anywhere except host start/stop).

**4. `source/CardGames.Infrastructure/CardGames.Infrastructure.csproj`** — add:
```xml
<ItemGroup>
  <ProjectReference Include="..\CardGames.Domain\CardGames.Domain.csproj" />
</ItemGroup>
<ItemGroup>
  <InternalsVisibleTo Include="CardGames.Infrastructure.Tests" />
</ItemGroup>
```

**5. `source/CardGames.Presentation/Program.cs`**
- Add `using CardGames.Infrastructure.Services;` and register `services.AddSingleton<ISettingsService, SettingsService>();`.
- Resolve `var settingsService = host.Services.GetRequiredService<ISettingsService>();` alongside the other services.
- New **case 4 (Settings)**, inserted before the renumbered About case: prints the current `PluginDirectory` (or "(not set — defaults to {AppContext.BaseDirectory})"), prompts for a new value (blank input = keep current / no-op), warns via `Directory.Exists` check but saves regardless, then `settingsService.Save(...)`.
- **Case 2 (Load Game)** changes from `assemblyLoaderService.DiscoverPlugins(AppContext.BaseDirectory)` to load settings fresh each call (`settingsService.Load()`), falling back to `AppContext.BaseDirectory` when `PluginDirectory` is unset. Load fresh (not cached at startup) so a mid-session Settings change takes effect on the next "Load Game" without restarting — the cost is one small local file read. Add a `Directory.Exists` guard before calling `DiscoverPlugins`: it internally does an unguarded `Directory.GetFiles`, which throws `DirectoryNotFoundException` and would propagate past the switch to the outer `try/catch` → `finally { Environment.Exit(0) }`, crashing the whole app instead of just failing that menu action — exactly the failure mode the "warn but allow saving a nonexistent path" decision opens up.
- **Case 4 → case 5 (About)**: unchanged body, renumbered.

**6. `source/CardGames.Presentation/Services/ConsoleRenderer.cs`** — in `LoadCommandDictionary()`, insert `{ 4, "Settings" }` and renumber `{ 4, "About" }` → `{ 5, "About" }`. No other change needed (`DisplayMenu()` already iterates generically).

**7. Tests** — new `source/tests/CardGames.Infrastructure.Tests` project (none exists yet; mirrors `CardGames.Application.Tests`' shape: net10.0, xunit, references `CardGames.Infrastructure.csproj` + `CardGames.Common.Tests.csproj`), added to `CardGames.sln` via `dotnet sln add`. New `Services/SettingsServiceTests.cs` using the internal temp-path constructor, `IDisposable` per-test cleanup, covering: load-with-no-file-returns-defaults, save-then-load-round-trips, load-corrupt-file-returns-defaults-without-throwing, save-creates-missing-directory. Follow the `AssemblyServiceTests` precedent of **not** applying the post-build `[Trait(...)]` (that gate is reserved for no-I/O unit tests; these do real temp-file I/O).

### Critical files
- `source/CardGames.Presentation/Program.cs`
- `source/CardGames.Presentation/Services/ConsoleRenderer.cs`
- `source/CardGames.Domain/Interfaces/ISettingsService.cs` (new)
- `source/CardGames.Domain/Models/ApplicationSettings.cs` (new)
- `source/CardGames.Infrastructure/Services/SettingsService.cs` (new)
- `source/CardGames.Infrastructure/CardGames.Infrastructure.csproj`

## Verification

1. `dotnet build` — confirm the new Infrastructure→Domain reference and new test project compile cleanly.
2. `dotnet test` — new `SettingsServiceTests` pass; existing `AssemblyServiceTests` unaffected (its API/constructor is untouched).
3. `dotnet run --project source/CardGames.Presentation` — manually walk the menu:
   - Select Settings, confirm it shows "(not set — defaults to ...)" on first run, enter a new (existing) directory, confirm "Settings saved."
   - Restart the app, select Settings again, confirm the previously-entered directory is now shown as current (persistence works).
   - Select Load Game, confirm it scans the configured directory rather than `AppContext.BaseDirectory`.
   - Select Settings again, enter a nonexistent path, confirm it warns but still saves; then select Load Game and confirm it prints the friendly "does not exist" message instead of crashing.
   - Inspect `~/.config/CardGames/settings.json` (Linux) directly to confirm the JSON shape.

## Outcome

Implemented as planned. All items above were completed without deviation, including the new `CardGames.Infrastructure.Tests` project and its 4 tests (all passing). One addition not anticipated in the original plan: `CardGames.Infrastructure.csproj` needed an explicit `Microsoft.Extensions.Logging` package reference (build failed without it — `ILogger<>`/`ILoggerFactory` weren't otherwise resolvable in that project). `dotnet sln add` also nested the new test project under a duplicate "tests" solution folder; this was corrected by hand to reuse the existing "tests" folder GUID that `CardGames.Domain.Tests`/`CardGames.Application.Tests`/`CardGames.Common.Tests` already share.
