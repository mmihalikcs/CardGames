# SignalR Networked Multiplayer for Poker (Embedded Authoritative Host)

## Context

The solution currently has no multiplayer beyond local hot-seat play, and in practice every plugin (WAR, GoFish, Poker) is wired as exactly one human vs. one hard-coded "computer"/AI side. The user wants to add real networked multiplayer, and after discussion we landed on: **SignalR** over WebSockets (idiomatic .NET real-time transport, typed hub methods, automatic reconnection, group support that maps to game rooms) with a **lightweight authoritative host** rather than true serverless P2P — one player's own process hosts the game and is the source of truth for hidden information (hole cards, deck order), other players connect to it directly.

Scope agreed with the user:
- **Pilot game**: Poker (Texas Hold'em first) — it already has a `Seat` list with per-seat `IsHuman` flags and a fixed action-order betting loop, the closest existing shape to a server-authoritative turn model.
- **Hosting model**: Embedded host — a player's own `CardGames.Presentation` process self-hosts Kestrel+SignalR on demand; other players' Presentation processes connect as SignalR clients. No standalone always-on server project.
- **Discovery**: Direct connect — host shares IP:port + a join code; no lobby/matchmaking service.
- **Opt-in switch**: a new `MultiplayerEnabled` application setting (default `false`) gates the feature entirely. When off, the app behaves exactly as it does today — no networking stack is started, and no single-player/multiplayer choice is ever presented. When on, the networking stack becomes available and any plugin that declares multiplayer support prompts the user to choose single-player or multiplayer before variant selection.

A key discovery from exploration: `IGameIO` (`Write`/`WriteLine`/`ReadLine`) is a single shared, synchronous, **player-agnostic** channel, and even Poker's game managers take exactly one `IGameIO` instance for the whole game — `PromptHuman` (`source/plugins/CardGames.Poker/Engine/BettingRound.cs:138-170`) calls it directly with no seat/connection identity. Multiple `IsHuman` seats are only exercised today in tests, sharing one scripted input queue. Also, `SetupSeats()` in both Poker game managers hard-codes exactly one human seat ("You") — so **local multi-human play never worked correctly either** (only the first human ever sees their hole cards). Fixing that is a prerequisite for networked multiplayer and is a good standalone first milestone.

## Design

### 1. New/changed projects

- **New library `source/CardGames.Networking/CardGames.Networking.csproj`** (plain `Microsoft.NET.Sdk`, not `Sdk.Web`, plus `<FrameworkReference Include="Microsoft.AspNetCore.App" />` for server-side Kestrel/SignalR APIs without pulling in web-app scaffolding). Add the `Microsoft.AspNetCore.SignalR.Client` NuGet package for the client (`HubConnection`) side. Only references `CardGames.Domain` — never a plugin project, preserving the existing plugin-isolation model (plugins are loaded via reflection/`AssemblyLoadContext`, and Presentation never compiles against concrete plugin types).
- **New test project `tests/CardGames.Networking.Tests`**, mirroring existing test project conventions (xunit, references `CardGames.Networking` + `CardGames.Common.Tests`).
- **`CardGames.Presentation.csproj`**: add a project reference to `CardGames.Networking`. No direct ASP.NET Core/SignalR references needed in Presentation itself — `CardGames.Networking` hides `WebApplication`/`HubConnection`/Hub types behind small service interfaces (`IGameSessionManager`, `GameServerHost`, `GameClientConnection`).
- **`CardGames.Domain`**: two new, purely additive members (see below). `IGameIO`, `IGameManager` are unchanged; `IPlugin` gains one new default-interface-method property (same non-breaking pattern used to add `Variants`). Zero breaking impact on WAR/GoFish.
- **`CardGames.Poker`**: no new package/project references; only internal method edits plus opting in to the new `SupportsMultiplayer` flag.

Presentation's existing generic `IHost` (`Host.CreateDefaultBuilder`) stays untouched for app-wide singletons. `IGameSessionManager` and friends can be registered there unconditionally (cheap DI wiring — constructing the service does not bind any socket). Actually starting Kestrel/SignalR only happens inside `GameServerHost.StartAsync(...)`, which is only ever reachable from a code path gated by `MultiplayerEnabled` (see §4) — so with the setting off, no listener is ever bound and the app's behavior is identical to today.

### 2. The `MultiplayerEnabled` setting

- `source/CardGames.Domain/Models/ApplicationSettings.cs`: add `public bool MultiplayerEnabled { get; set; } = false;` alongside the existing `PluginDirectory`. `ISettingsService`/`SettingsService` (`source/CardGames.Infrastructure/Services/SettingsService.cs`) need no interface changes — it already round-trips the whole `ApplicationSettings` object through JSON.
- `source/CardGames.Presentation/Program.cs`'s existing "Settings" menu (`case 4`, which currently edits `PluginDirectory`) gains a prompt to view/toggle `MultiplayerEnabled`, following the same edit-and-`Save()` pattern already used there.
- `source/CardGames.Domain/Interfaces/IPlugin.cs`: add `bool SupportsMultiplayer => false;` as a default-interface-method property (identical non-breaking pattern to `Variants`/`CreateGameManager(io, variant)`). Only `PokerPlugin` overrides it to `true`, once Phase 2 below lands. This is deliberately plugin-level (not per-variant), matching "games that have multiplayer enabled as a plugin option."

### 3. The `IGameIO` → network bridge

**New Domain interface** `source/CardGames.Domain/Interfaces/ISeatContextGameIO.cs`:
```csharp
public interface ISeatContextGameIO : IGameIO
{
    IDisposable BeginParticipantScope(string participantId);
}
```
Additive only. Callers use `using ((_Io as ISeatContextGameIO)?.BeginParticipantScope(seat.Name));` — a no-op for `ConsoleGameIO`/`ScriptedGameIO`, which don't implement it, so all existing behavior (including WAR/GoFish) is untouched.

**Poker engine edits** (transport-agnostic fixes, not "networking code" — these also fix local hot-seat multi-human play):
- `Engine/BettingRound.cs`: wrap `PromptHuman`'s body in `BeginParticipantScope(seat.Name)`. The broadcast lines in `Apply()` (fold/check/call/raise announcements, lines ~96-130) already execute outside that scope, so they naturally stay public/broadcast — confirmed by reading the current file.
- `Engine/CommunityCardGameManagerBase.cs` (shared by Texas Hold'em/Omaha) and `FiveCardDrawGameManager.cs`: generalize `SetupSeats()` from "always exactly 1 human named 'You'" to prompting for a human count and a name per human seat (humans added first, preserving the documented human-first action order), then the existing AI-opponent-count prompt. Fix the hole-card-reveal call site(s) (currently `TableRenderer.ShowHoleCards(Io, _Seats.First(s => s.IsHuman))`) to loop over every human seat, each individually scoped.
- No changes to `Seat`, `Pot`, `AiDecisionMaker`, `HandEvaluator`, `ShowdownResolver`, `TableRenderer`, or `PluginDefinition.cs` beyond the new `SupportsMultiplayer => true` override — `CreateGameManager(IGameIO io[, variant])` is called exactly as today, just handed a network-backed `IGameIO`.

**`NetworkGameIO`** (in `CardGames.Networking`, implements `IGameIO` + `ISeatContextGameIO`):
- Holds one `ISeatChannel` per participant: `LocalConsoleSeatChannel` (the host's own seat — direct `Console` I/O, no network hop) and `RemoteSeatChannel` (backed by a SignalR `ConnectionId`; `Write`/`WriteLine` push via `IHubContext<GameHub>`, `ReadLine()` blocks synchronously on a `TaskCompletionSource<string?>` completed when that client calls `GameHub.SubmitInput`).
- `BeginParticipantScope` sets a plain "current participant" field — safe because `StartGame()` runs single-threaded on one background `Task` and Poker never nests/overlaps scopes.
- Unscoped `Write`/`WriteLine` (public table state) broadcast to every channel. Scoped calls route to exactly one channel.
- `SetupSeats()`'s prompts (human count, names, AI count) are unscoped and have no `Seat` to key off yet, so `NetworkGameIO` is preloaded with a `Queue<string>` of answers derived from the lobby roster at "Start" time (host name + each joined player's name in join order, then AI count) — no live socket round-trip needed for seat construction, and Poker's `internal` types are never touched directly from the networking layer.

### 4. Server-side session lifecycle

New files under `source/CardGames.Networking/`:
- `Hubs/GameHub.cs` — thin `Hub` subclass: `JoinSession(joinCode, playerName)`, `SubmitInput(text)` (resolves the pending TCS for `Context.ConnectionId`), `OnDisconnectedAsync` (marks the seat dead, faults any outstanding TCS). Delegates everything to `IGameSessionManager` so it stays easy to test without a real Hub context.
- `Sessions/GameSession.cs` — join code, plugin/variant, roster (`ConcurrentDictionary<connectionId, PlayerSlot>`), the built `NetworkGameIO`, the `Task` running `StartGame()`, pending-TCS map.
- `Sessions/GameSessionManager.cs` — v1 supports exactly one hosted session per process. `CreateSession(...)`, `TryJoin(...)` (from the hub), `StartSession()` (builds the setup queue, calls `plugin.CreateGameManager(networkIo, variant)`, then `Task.Run(() => gameManager.StartGame())` on a background thread — critical, since `StartGame()` blocks synchronously).
- `Hosting/GameServerHost.cs` — wraps `WebApplicationBuilder`/`WebApplication` start/stop, `AddSignalR()`, maps `/gamehub`. `StartAsync(port)` is the only place a real Kestrel listener is bound.
- `Client/GameClientConnection.cs` — wraps `HubConnectionBuilder` + `HubConnection`, exposes `JoinAsync`, `OnMessageReceived`/`OnPromptReceived` events, `SubmitInputAsync`.
- `Dtos/` — small records for hub payloads (`JoinResult`, `RosterEntry`).

**Disconnect handling (explicit v1 simplification)**: a mid-hand disconnect faults the outstanding `ReadLine` TCS or marks the session aborted; the exception propagates through the background `Task`, `GameSessionManager` catches it at the `Task.Run` boundary, broadcasts an abort notice, tears the session down. No reconnect/resume in v1.

### 5. Presentation UX changes

- `Services/ConsoleRenderer.cs`: `DisplayMenu()` takes a `bool multiplayerEnabled` parameter (read from settings by the caller) and only includes a `{ N, "Join Game" }` entry when true; otherwise the menu is exactly today's five entries. No "Host Game" top-level entry — hosting is reached through "Play" (see below), matching the requirement that the single-player/multiplayer choice sits in front of game/variant selection rather than being a separate menu path.
- `Program.cs`, **`case 1` ("Play")**: after the existing `loadedPlugin != null` guard, check `settings.MultiplayerEnabled && loadedPlugin.SupportsMultiplayer`. If false (covers both "feature off" and "plugin doesn't support it" — i.e. WAR/GoFish today, and Poker before Phase 2 lands), behavior is **exactly** what it is today: straight into variant selection (if any) and `CreateGameManager(gameIo)`/`StartGame()` with `ConsoleGameIO`. If true, first show a "1) Single Player  2) Host Multiplayer Game" prompt:
  - **Single Player** → falls straight into today's existing path (`ConsoleGameIO`, same variant submenu, same synchronous `StartGame()` call).
  - **Host Multiplayer Game** → proceeds with the existing variant submenu, then a human-seat-count prompt, then `IGameSessionManager.CreateSession(...)` + `GameServerHost.StartAsync(port)` (this is the only moment a network listener is ever bound), prints the join code/port, shows a live roster while waiting, and on "Start" builds `NetworkGameIO` and runs `StartGame()` on a background `Task`, streaming host-local output to the console.
- `Program.cs`, **new case for "Join Game"** (only ever shown/reachable when `MultiplayerEnabled` is true, per the renderer change above): prompts for IP:port/code/name, builds `GameClientConnection`, runs a client loop rendering incoming messages/prompts and forwarding `Console.ReadLine()` via `SubmitInputAsync` only when prompted. Note a joining player never picks a plugin locally — the host's chosen game/variant governs the session.

### 6. Testability

- Extend the `ScriptedGameIO` pattern (`tests/CardGames.Poker.Tests/Fakes/`) with `ScriptedSeatContextGameIO` implementing `IGameIO` + `ISeatContextGameIO`, recording `(participantId, message)` tuples and supporting per-participant scripted input — used to assert e.g. "Bob's hole cards were only ever written while the active scope was Bob," extending the existing two-human `TexasHoldemGameManagerTests` pattern.
- `CardGames.Networking.Tests` exercises `GameSession`/`GameSessionManager`/`NetworkGameIO` directly via a `RecordingSeatChannel` fake — no real sockets/Kestrel/`HubConnection` needed, keeping `GameHub` itself logic-free.
- `ApplicationSettings`/`SettingsService` round-trip test covering the new `MultiplayerEnabled` field (default `false`, persists correctly). `ConsoleRendererTests` gets a case asserting the "Join Game" entry is present/absent based on the flag passed to `DisplayMenu`.
- New fast/deterministic tests get `[Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]` so they run in the CI post-build gate. Real-socket integration tests (if added later) stay outside that gate — manual two-process LAN playtests are the verification method for actual wire-level SignalR behavior in this plan.

### v1 decisions (adopted, stated explicitly rather than left implicit)

- `MultiplayerEnabled` defaults to `false`; with it off, the app's behavior, menu, and code paths reachable are unchanged from today — no networking types are ever instantiated with live sockets.
- `SupportsMultiplayer` is a plugin-wide flag (not per-variant); Poker sets it once the bridge lands, WAR/GoFish never do (no rework planned for them here).
- Participant identity = `Seat.Name` → session join enforces unique display names per session.
- Host-declared human seat count must be **exactly** filled before "Start" is enabled (no partial-fill-with-AI in v1).
- One concurrently hosted session per process.
- Plain HTTP/WS, no HTTPS, no NAT traversal/UPnP — LAN-first direct connect, matching the agreed discovery model.
- Omaha (near-free follow-on via the shared `CommunityCardGameManagerBase`) and Five-Card Draw (needs its own wiring, doesn't share the base class) are explicitly out of scope for this pass; so is reconnect/resume.

## Phased delivery & verification

**Phase 0 — Poker multi-human engine fix (no networking).**
Add `ISeatContextGameIO`; generalize `SetupSeats()` in both Poker game managers for M human seats; fix hole-card-reveal (and discard-prompt, in Five-Card Draw) call sites to loop+scope per human. Verify: existing `TexasHoldemGameManagerTests`/`FiveCardDrawGameManagerTests` stay green (no-op scoping via `ConsoleGameIO`); new tests via `ScriptedSeatContextGameIO` proving 2+ humans each only see their own cards and are prompted independently; manual local 2-human hot-seat playtest via the existing "Play" flow.

**Phase 1 — Settings gate + SignalR scaffolding, no game logic.**
Add `MultiplayerEnabled` to `ApplicationSettings` and the Settings menu; add `IPlugin.SupportsMultiplayer` (default false); new `CardGames.Networking` project; `GameHub` with join/roster only; wire the "Join Game" menu entry and the Play-flow single/multiplayer prompt behind the setting (no real hosting yet — proves the gating logic and connectivity separately). Verify: settings round-trip test; `ConsoleRenderer` test for conditional "Join Game" visibility; `GameSessionManager` roster/join unit tests with fakes; one manual two-process LAN smoke test for real Kestrel+SignalR connectivity.

**Phase 2 — `NetworkGameIO` bridge into Texas Hold'em.**
`ISeatChannel`/`LocalConsoleSeatChannel`/`RemoteSeatChannel`/`NetworkGameIO`; `GameSessionManager.StartSession` builds the setup queue and drives `PokerPlugin.CreateGameManager(networkIo, TexasHoldem)` + `Task.Run(StartGame)`; `PokerPlugin.SupportsMultiplayer` flips to `true`. Verify: `NetworkGameIO` unit tests (fakes, no sockets) for scoped-vs-broadcast routing and setup scripting; manual two-process full-hand playtest.

**Phase 3 — Presentation host/join UX polish.**
Full menu flow: variant selection reuse, seat-count prompts, live waiting-room roster, start trigger, join-code/IP sharing text, client-side prompt rendering. Verify: manual 2-4 process playtests; any isolable Presentation menu/state logic covered in `CardGames.Presentation.Tests`.

**Phase 4 — Basic disconnect handling.**
`OnDisconnectedAsync` faults outstanding reads / marks session aborted; `GameSessionManager` catches at the `Task.Run` boundary, broadcasts abort, tears down. Verify: unit tests simulating a mid-`ReadLine` fault and asserting clean propagation; manual test killing a client process mid-hand.

### Critical files
- `source/CardGames.Domain/Models/ApplicationSettings.cs` (`MultiplayerEnabled`)
- `source/CardGames.Domain/Interfaces/IPlugin.cs` (`SupportsMultiplayer`)
- `source/CardGames.Domain/Interfaces/ISeatContextGameIO.cs` (new)
- `source/plugins/CardGames.Poker/Engine/BettingRound.cs` (`PromptHuman` scoping seam)
- `source/plugins/CardGames.Poker/Engine/CommunityCardGameManagerBase.cs` (`SetupSeats` + hole-card reveal generalization)
- `source/plugins/CardGames.Poker/FiveCardDrawGameManager.cs` (same generalization, own game manager)
- `source/plugins/CardGames.Poker/PluginDefinition.cs` (`SupportsMultiplayer => true`)
- `source/CardGames.Networking/Sessions/GameSessionManager.cs` (new — session lifecycle, background `StartGame()`)
- `source/CardGames.Networking/Client/NetworkGameIO.cs` (new — core network bridge)
- `source/CardGames.Networking/Hubs/GameHub.cs` (new — thin SignalR hub)
- `source/CardGames.Presentation/Program.cs` and `Services/ConsoleRenderer.cs` (settings-gated Play/Join Game menu wiring)
