# Menu flow rework: Play submenu absorbs Join Multiplayer, adds version negotiation

## Context

The multiplayer feature (per `docs/plans/signalr-multiplayer-poker.md`) is already implemented end-to-end: `MultiplayerEnabled` setting, `IPlugin.SupportsMultiplayer`, `CardGames.Networking` (SignalR host/client), and a working Play → Single Player/Host Multiplayer flow in `Program.cs`. But the menu structure that landed doesn't match the intended UX:

- "Join Game" is its own **top-level main-menu entry** (`ConsoleRenderer.JoinGameCommandKey`), reachable without loading any plugin at all.
- Under "Play", the mode choice only offers **Single Player / Host Multiplayer** — no Join option.

The user wants Join folded into the same Play-time mode choice as Single Player/Host Multiplayer (all three gated behind "a multiplayer-capable plugin is loaded" + `MultiplayerEnabled=true`, exactly like Host/Single Player are today), with variant selection skipped entirely on the Join path (the host dictates the game). Confirmed with the user:
- "Unload Game" stays on the main menu as-is (not part of this change).
- Join requires a plugin to be loaded first, same gate as Single Player/Host — this is what lets the app know to offer the 3-way submenu at all.
- Additionally: the join handshake must negotiate and reject on mismatch — the joining client's loaded plugin (name + version) must match what the host is running, so two mismatched builds can't join the same session.

## Design

### 1. `ConsoleRenderer` — drop the standalone "Join Game" entry

`source/CardGames.Presentation/Services/ConsoleRenderer.cs`:
- Remove `JoinGameCommandKey`, the `multiplayerEnabled` parameter on `GetCommands`/`DisplayMenu`, and the conditional injection logic. `_BaseCommandDictionary` (Play/Load Game/Unload Game/Settings/About/Exit) becomes the only menu again, matching the user's list plus the already-confirmed "keep Unload Game."
- No new menu-rendering code needed for the 3-way mode choice — reuse the existing generic `DisplaySubmenu(title, options)` (already used for variant selection), which already renders numbered options + "0) Cancel". Call it with `["Single Player", "Host Multiplayer Game", "Join Multiplayer Game"]`.

### 2. `Program.cs` — restructure the Play flow

`source/CardGames.Presentation/Program.cs`, `case 1` (Play):
- `consoleRenderer.DisplayMenu()` / `GetCommands()` calls drop the `settings.MultiplayerEnabled` argument.
- Remove the `case ConsoleRenderer.JoinGameCommandKey` branch entirely — Join is no longer reachable from the main menu.
- Replace the ad hoc `Console.WriteLine("1) Single Player"); ...` block with `consoleRenderer.DisplaySubmenu("Choose how to play", new[] { "Single Player", "Host Multiplayer Game", "Join Multiplayer Game" })`, parsed the same way variant selection already is (0 = cancel, 1-3 = choice).
- Control flow: only run the existing variant-selection block when the mode is Single Player or Host Multiplayer. When the mode is Join Multiplayer, skip variant selection and go straight to `JoinMultiplayerGameAsync(loadedPlugin)` — no `CreateGameManager` call on this path today, so behavior otherwise unchanged.
- `JoinMultiplayerGameAsync` gains an `IPlugin plugin` parameter, used only to supply `plugin.Name`/`plugin.Version` to the join handshake (see below) — the joining side still never calls `CreateGameManager`.

### 3. Version negotiation on join

Currently `JoinSession(joinCode, playerName)` carries no plugin identity, and `GameSessionManager.CreateSession` doesn't record what the host is running (only `StartSessionAsync`, called later, receives the `IPlugin`). Thread plugin identity through the whole join path so a mismatch is rejected with a clear error, not discovered mid-game:

- `source/CardGames.Networking/Sessions/GameSession.cs`: constructor gains `hostPluginName`, `hostPluginVersion`; store as `HostPluginName`/`HostPluginVersion`.
- `source/CardGames.Networking/Sessions/IGameSessionManager.cs` / `GameSessionManager.cs`:
  - `CreateSession(hostPlayerName, requiredRemotePlayers, aiOpponentCount, hostPluginName, hostPluginVersion)`.
  - `TryJoin(joinCode, playerName, pluginName, pluginVersion, connectionId)`: after the existing join-code/full/name checks, reject (mirroring the existing `JoinResult(false, "...", null, [])` pattern) if `pluginName`/`pluginVersion` don't match `session.HostPluginName`/`HostPluginVersion` (`StringComparison.Ordinal` — these are build identifiers, not display text like player names). Error message should name both what the host is running and what the client sent, e.g. `"Game version mismatch: host is running {HostPluginName} v{HostPluginVersion}, you have {pluginName} v{pluginVersion}."`.
- `source/CardGames.Networking/Hubs/GameHub.cs`: `JoinSession(joinCode, playerName, pluginName, pluginVersion)` passes through to `TryJoin` with the new args.
- `source/CardGames.Networking/Client/GameClientConnection.cs`: `JoinAsync(joinCode, playerName, pluginName, pluginVersion)` forwards to the hub invoke.
- `Program.cs`'s `JoinMultiplayerGameAsync(IPlugin plugin)` calls `clientConnection.JoinAsync(enteredJoinCode, joinDisplayName, plugin.Name, plugin.Version)`; the "Could not join" failure path already prints `joinResult.ErrorMessage`, so a version mismatch surfaces there with no further UI work.
- `Program.cs`'s `HostMultiplayerGameAsync` passes `plugin.Name, plugin.Version` into `sessionManager.CreateSession(...)`.

### 4. Test updates

- `tests/CardGames.Presentation.Tests/Services/ConsoleRendererTests.cs`: remove the `multiplayerEnabled`/Join-Game-specific tests (`GetCommands_MultiplayerDisabled_HasNoJoinGameEntry`, `GetCommands_MultiplayerEnabled_IncludesJoinGameEntry`, `DisplayMenu_MultiplayerEnabled_IncludesJoinGame`); update `Commands_HasExpectedMenuEntriesInOrder`/`DisplayMenu_WritesEveryCommandAndExit` if signatures change (drop the bool arg).
- `tests/CardGames.Networking.Tests/Sessions/GameSessionManagerTests.cs`: update every `CreateSession`/`TryJoin` call site for the new `pluginName`/`pluginVersion` args (e.g. `"Poker", "1.0.0"` for the host, matching in most tests, deliberately mismatched in a new test). Add a new test, e.g. `TryJoin_PluginVersionMismatch_Fails`, asserting rejection + message content when the joining client's plugin name/version differs from the host's.
- `tests/CardGames.Networking.Tests/GameServerConnectivityTests.cs`: update the one `CreateSession`/`JoinAsync` call site to pass matching plugin identity through the real socket path.

### Verification

- `dotnet build`
- `dotnet test` (full suite) — in particular `CardGames.Presentation.Tests` and `CardGames.Networking.Tests`.
- Manual smoke test via `dotnet run --project source/CardGames.Presentation`: load Poker, confirm Play shows the 3-way submenu (Single Player/Host/Join) only when `MultiplayerEnabled=true`; confirm it's skipped straight to variant selection when the setting is off or a non-multiplayer plugin (WAR/GoFish) is loaded; confirm the main menu no longer shows a standalone "Join Game" item in either state.
