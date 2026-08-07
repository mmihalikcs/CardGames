# Architecture

`CardGames` is a plugin-hosted console application for playing card games. A thin
Console layer drives a console menu loop and delegates gameplay to game
plugins that are discovered and loaded at runtime from `*.plugin.dll` files.

## Contents

- [Solution layout](#solution-layout)
- [Layers](#layers)
- [Plugin loading model](#plugin-loading-model)
- [Console application flow](#console-application-flow)
- [Godot client](#godot-client)
- [Games](#games)
  - [WAR](#war)
  - [Go Fish](#go-fish)
  - [Poker](#poker)
- [Settings](#settings)
- [Tests](#tests)
- [CI](#ci)

## Solution layout

The solution is defined in `CardGames.slnx` and organized into three top-level
folders:

```
source/
  CardGames.Domain/          # models, enums, public interfaces - no dependencies
  CardGames.Application/     # AssemblyLoaderService, SettingsService - depends on Domain only
  CardGames.Console/         # console entry point, DI wiring, menu loop
  CardGames.Godot/           # embedded single-player Godot client - depends on Application + plugins (see below)
  plugins/
    CardGames.WAR/           # WAR plugin - depends on Domain only
    CardGames.GoFish/        # Go Fish plugin - depends on Domain only
    CardGames.Poker/         # Poker plugin - depends on Domain only
tests/
  CardGames.Domain.Tests/
  CardGames.Application.Tests/
  CardGames.Console.Tests/
  CardGames.WAR.Tests/
  CardGames.GoFish.Tests/
  CardGames.Poker.Tests/
  CardGames.Common.Tests/    # shared test constants, not a test project itself
```

All projects target `net10.0` with `Nullable` and `ImplicitUsings` enabled.

## Layers

**CardGames.Domain** is the shared contract layer that every other project
depends on, directly or indirectly:

- Models: `Card`, `DeckOfCards`, `GameVariant`, `ApplicationSettings`
- Enums: `Suit`, `Rank`
- Interfaces: `IPlugin`, `IGameManager`, `IGameIO`, `IAssemblyLoaderService`,
  `ISettingsService`
- `Interaction/`: the structured presentation contract plugins actually build
  against - `GameEvent`, `GamePrompt`/`PromptResponse`, `IGameChannel`,
  `TextGameChannel` (see "Presentation contract" below)

Because plugins only reference Domain, a plugin assembly never needs to
reference Console or Application, which keeps the plugin surface area
small and stable.

**CardGames.Application** implements `IAssemblyLoaderService` via
`AssemblyLoaderService` — the plugin discovery/load/unload logic described
below — and `ISettingsService` via `SettingsService`, which persists
`ApplicationSettings` as JSON under the user's local application data folder
(`%APPDATA%/CardGames/settings.json` or platform equivalent). Both are
application-level services shared by whichever front end hosts them; neither
is specific to the console.

**CardGames.Console** is the console entry point (`Program.cs`). It
builds a generic `Microsoft.Extensions.Hosting` host, registers services via
DI, and runs a numbered console menu loop (`ConsoleRenderer`) for loading,
playing, and unloading game plugins. `ConsoleGameIO` implements `IGameIO` on
top of `Console.Write`/`Console.ReadLine`; `Program.cs` wraps it in a
`TextGameChannel` before handing it to a plugin (see "Presentation contract").

**CardGames.Godot** is a second, embedded presentation layer - a Godot 4
(C#/Mono) project offering the same games through a real UI instead of a
console. It reuses `IAssemblyLoaderService`/`ISettingsService` directly
(no `Microsoft.Extensions.Hosting`) and implements `IGameChannel` itself
rather than going through `TextGameChannel` (see "Godot client" below).

## Presentation contract

Plugins never see `IGameIO` directly - it's a raw text-transport primitive
implemented only by `ConsoleGameIO` and `NetworkGameIO`. `IPlugin.CreateGameManager`
takes an `IGameChannel` (`CardGames.Domain.Interaction`) instead, which
separates *what happened* from *how it's described*:

```csharp
public interface IGameChannel
{
    void Publish(GameEvent gameEvent);           // a fact: "what happened", past tense
    PromptResponse Await(GamePrompt prompt);      // a request: "what do you need from a seat"
}
```

Each plugin defines its own `GameEvent` leaf types (e.g. WAR's `PileAwarded`,
Poker's `SeatFolded`) with a `Describe()` override for text rendering; no
plugin formats prose or parses raw input directly anymore. `GamePrompt` has
just three generic kinds shared by every plugin - `ConfirmPrompt` (press
enter), `ChoicePrompt` (a constrained choice, e.g. GoFish's rank ask or
Poker's fold/check/call/raise), `TextPrompt` (free text, e.g. player names or
Poker's discard-position list) - so channels never need plugin-specific
knowledge to render or parse one.

`TextGameChannel` (also in `CardGames.Domain.Interaction`) is the generic
adapter that bridges this contract onto any existing `IGameIO`: it renders
events/prompts via `Describe()` and parses the raw text answer back into a
typed `PromptResponse`. This is what lets `ConsoleGameIO` and `NetworkGameIO`
(and the whole SignalR/`ISeatChannel` plumbing under it) stay unchanged, plain
text transports - `TextGameChannel` is constructed once, wrapping whichever
`IGameIO` is in play, right before a plugin's `CreateGameManager` is called
(see `Program.cs`'s Play case and `GameSessionManager.StartSessionAsync`).

`GameEvent` also has a `CardGroups` property (`IReadOnlyList<CardGroup>`,
empty by default) - a labeled set of cards significant to that event, e.g.
WAR's `CardsRevealed` exposes `[CardGroup("You", [...]), CardGroup("Computer", [...])]`.
Unlike `GameEvent`'s own leaf types, `CardGroup` is defined once in Domain and
shared by every plugin, so - like `GamePrompt`'s three kinds - a channel can
render it without any per-plugin knowledge. `TextGameChannel.Publish` renders
`CardGroups` as ASCII art (`Card.DisplayCard()`, one labeled row per group)
appended after `Describe()`; this is what previously lived duplicated across
WAR's `RenderVersus`, Poker's `TableRenderer.RenderCardRow`, and GoFish's
`RenderHandRow` - each plugin's `Describe()` is now narrative text only. A
structured client (Godot) instead draws real card graphics from the same
`CardGroups` data (see "Godot client" below).

## Plugin loading model

Each plugin project sets:

```xml
<AssemblyName>$(MSBuildProjectName).plugin</AssemblyName>
```

so its build output is named `CardGames.<Game>.plugin.dll` rather than
`CardGames.<Game>.dll`. `AssemblyLoaderService.DiscoverPlugins(directoryPath)`
scans a directory for files matching `*.plugin.dll` and, for each one:

1. **Verifies** the assembly exposes a type implementing `IPlugin`, using a
   `MetadataLoadContext` (reflection-only load) so untrusted DLLs are
   inspected without executing any of their code.
2. **Loads** the assembly into its own collectible
   `System.Runtime.Loader.AssemblyLoadContext`, named after the DLL's file
   name (e.g. `CardGames.WAR.plugin`). Each plugin gets an isolated context
   rather than being loaded into the default context.
3. **Instantiates** every non-abstract exported type implementing `IPlugin`
   found in the loaded assembly via `Activator.CreateInstance`, and returns
   the resulting `IPlugin` instances to the caller.

Because each plugin lives in its own collectible `AssemblyLoadContext`,
`UnloadPluginAssembly(contextName)` can later unload it (`context.Unload()`)
to free the assembly without restarting the process. This isolation is
intentional: new plugin functionality should go through this same
discover/load/unload lifecycle rather than a direct project reference from
Console to a plugin project.

Both verify and load fall back to resolving a plugin's dependencies (e.g.
`CardGames.Domain`) from DLLs sitting next to the plugin itself, rather than
relying solely on `TRUSTED_PLATFORM_ASSEMBLIES`. Under a normal
`dotnet`-hosted apphost (Console), that environment variable already lists
every dependency of the running app, so this fallback is inert. Hosts with
their own runtime bootstrap - notably Godot's Mono build - populate it with
only their own platform assemblies, so without this fallback a plugin's
dependencies fail to resolve. The real-load fallback (a `Resolving` handler
on the plugin's `AssemblyLoadContext`) specifically prefers an
already-loaded assembly by simple name over loading a second copy: two
separately-loaded copies of the same DLL are distinct, incompatible CLR
types even with identical bytes, which would make a plugin's `IPlugin`
implementation fail `typeof(IPlugin).IsAssignableFrom(...)` against the
host's own `IPlugin` type.

`IPlugin` itself is small and declarative:

```csharp
public interface IPlugin
{
    string Name { get; }
    string Description { get; }
    string Version { get; }
    IGameManager CreateGameManager(IGameChannel io);

    IReadOnlyList<GameVariant> Variants => Array.Empty<GameVariant>();
    IGameManager CreateGameManager(IGameChannel io, GameVariant variant) => CreateGameManager(io);
}
```

A plugin with no variants (WAR, Go Fish) only needs to implement
`CreateGameManager(IGameChannel)`. A plugin that offers multiple modes (Poker)
overrides `Variants` and the variant-aware `CreateGameManager` overload; the
default interface implementations mean existing single-mode plugins needed
zero source changes when the variants feature was added.

## Console application flow

`Program.cs` wires up DI (`IAssemblyLoaderService`, `ISettingsService`,
`IGameIO`, `ConsoleRenderer`) via a generic host, then loops on a menu with
five options. The "Play" case wraps the resolved `IGameIO` in a
`TextGameChannel` before calling `CreateGameManager`:

| # | Command | Behavior |
|---|---------|----------|
| 1 | Play | Starts the currently loaded plugin's `IGameManager`, prompting for a variant first if the plugin declares any. |
| 2 | Load Game | Discovers plugins in the configured plugin directory (`ApplicationSettings.PluginDirectory`, defaulting to `AppContext.BaseDirectory`) and lets the user pick one to load. |
| 3 | Unload Game | Lists currently loaded `AssemblyLoadContext` names and unloads the one the user names. |
| 4 | Settings | Views/edits the configured plugin directory, persisted via `ISettingsService`. |
| 5 | About | Prints a static description of the app. |
| 0 | Exit | Stops the host and exits. |

Only one plugin is "loaded" for play at a time in the current UI, though
multiple plugin assemblies can be loaded into memory simultaneously (see
Unload Game).

## Godot client

`source/CardGames.Godot/` is an embedded, single-player Godot 4 (C#/Mono)
client - a second presentation layer alongside Console, built against the
same `IAssemblyLoaderService`/`ISettingsService`/`IGameChannel` contracts.
Its `CardGames.Godot.csproj` targets `net10.0` via `Godot.NET.Sdk`,
references `CardGames.Application` and each plugin project with
`ReferenceOutputAssembly="false"` (plugins stay runtime-discovered, never
compiled against, exactly like Console), and copies each plugin's
`*.plugin.dll` into Godot's build output with an `AfterTargets="Build"`
MSBuild target mirroring Console's `CopyPluginAssemblies`.

`MainController` (`Scripts/MainController.cs`) is the composition root: its
`_Ready()` instantiates `AssemblyLoaderService`/`SettingsService` directly
(no `Microsoft.Extensions.Hosting` - there's a single consumer) and calls
`DiscoverPlugins` the same way Console's Load Game menu does, populating a
`PluginSelectPanel` list (with a variant sub-list for Poker). Choosing a
game switches to `GameSessionPanel` and starts a `GodotGameChannel`.

Unlike Console/Networking, which wrap `IGameIO` in `TextGameChannel`,
`GodotGameChannel` (`Scripts/GodotGameChannel.cs`) implements
`ISeatContextGameChannel` directly - the doc comment on
`IGamePromptChannel.Await` anticipated exactly this ("a Godot UI event").
Since `GamePrompt`'s three kinds (`ConfirmPrompt`/`ChoicePrompt`/`TextPrompt`)
are generic and known at compile time, `GameSessionPanel.ShowPrompt` pattern-
matches them into real widgets (buttons for `ChoicePrompt.ValidOptions`, a
text field for `TextPrompt`) instead of `TextGameChannel`'s raw-line parsing.
`GameEvent` itself stays `Describe()`-only text in the scrolling event log,
since its leaf types are plugin-internal and the client never references a
specific plugin's assembly - but `GameEvent.CardGroups` (see "Presentation
contract" above) *is* generic across plugins, so `GameSessionPanel.AppendEvent`
also renders it: `CardView` (`Scripts/CardView.cs`) is a small `Control`
that draws one card procedurally via `_Draw()` - no external image assets,
just `DrawRect`/`DrawString` using the same `Suit`/`Rank` extensions
(`GetSuitGlyph()`, `IsRedSuit()`) `Card.DisplayCard()` draws from for the
console's ASCII art. `ShowCardGroups` renders each `CardGroup` as a labeled
column of `CardView`s in a `CardDisplay` row above the event log - a
persistent "what's showing right now" strip, replaced each time a new
`CardGroups`-bearing event arrives, since `RichTextLabel` can't host live
`Control` nodes inline the way the scrolling text log works.

`IGameManager.StartGame()` is fully synchronous/blocking, so
`MainController.OnPluginChosen` always runs it on a background `Task` -
never Godot's main thread, which would freeze the engine - mirroring
`GameSessionManager.StartSessionAsync`'s identical reasoning for SignalR
sessions. `GodotGameChannel.Publish`/`Await` marshal onto Godot's main
thread via `Callable.From(...).CallDeferred()` (safe from any thread, no
`Variant` marshaling needed since the closure carries `GameEvent`/
`GamePrompt` as plain C# state); `Await` then blocks the background thread
on a `TaskCompletionSource<PromptResponse>` that a UI widget's callback
resolves via `SubmitPromptResponse` - the same blocking-on-a-background-Task
pattern `RemoteSeatChannel` uses for SignalR round trips, applied to an
in-process UI event instead.

No `tests/CardGames.Godot.Tests` project exists: `GodotGameChannel`'s
correctness is only meaningfully exercisable inside a live Godot main loop,
and all per-prompt-kind rendering logic lives directly in
`GameSessionPanel`'s widget callbacks rather than an extractable class.

## Games

Three plugins ship today, all under `source/plugins/` and all depending only
on `CardGames.Domain`.

### WAR

`CardGames.WAR` (`WARPlugin` / `WARGameManager`) implements the classic
two-player card-flipping game: deal a shuffled 52-card deck evenly, flip the
top card of each hand each round, higher rank wins the pile, and a tie
triggers a "war" (each side commits `WarCardCount` = 4 cards, with the last
one revealed to break the tie, recursing if it ties again). The game ends
when a player runs out of cards, or after a `DefaultMaxRounds` = 10,000 round
safety cap. No variants.

### Go Fish

`CardGames.GoFish` (`GoFishPlugin` / `GoFishGameManager`) implements
two-player Go Fish against a computer opponent: 7-card initial hands, ask for
a rank you hold, draw on a miss ("Go Fish"), automatic book detection/removal
whenever four of a rank are collected, and turn-passing rules (asking again
on a hit or on drawing the rank you asked for). Ends when the draw pile is
empty and either hand is empty; winner is whoever holds more books. No
variants.

### Poker

`CardGames.Poker` (`PokerPlugin`) is the most substantial plugin, offering
three selectable `GameVariant`s against configurable AI opponents:

- **Texas Hold'em** — 2 hole cards, 5 shared community cards, best 5 of 7.
- **Omaha** — 4 hole cards; must use exactly 2 hole + 3 community cards.
- **Five-Card Draw** — 5 private cards, one betting round, a draw, then a
  final betting round.

Texas Hold'em and Omaha share `CommunityCardGameManagerBase` (in
`Engine/`), which drives the session/hand loop (ante, hole cards, betting
streets, flop/turn/river, showdown) and leaves hole-card count and showdown
hand evaluation as abstract extension points implemented by
`TexasHoldemGameManager` and `OmahaGameManager` respectively. Five-Card Draw
(`FiveCardDrawGameManager`) has no community cards and an extra draw phase,
so it implements `IGameManager` independently rather than sharing the base
class.

Supporting engine types under `Engine/`:

- `Seat`, `Pot`, `BettingRound` — table/session state and betting logic.
- `AiDecisionMaker`, `PreflopHeuristic` — AI opponent decisions, using hand
  strength estimates pre- and post-flop.
- `HandEvaluator`, `HandRank`, `HandCategory`, `ShowdownResolver` — best-hand
  evaluation and showdown resolution/pot award.
- `PokerDeck` — deck management specific to the poker engine.
- `TableRenderer` — publishes stack/hole-card/community-card events
  (`StacksStatus`, `HoleCardsRevealed`, `CommunityCardsRevealed`) via
  `IGameChannel`; `RenderCardRow`'s ASCII layout is called from those events'
  `Describe()`.
- `GameSettings` — tunables such as starting chips, ante amount, and min/max
  AI opponent counts.

## Settings

`ApplicationSettings` currently holds a single field, `PluginDirectory`,
persisted as JSON by `SettingsService` to
`Environment.SpecialFolder.ApplicationData/CardGames/settings.json`. An empty
`PluginDirectory` means "use `AppContext.BaseDirectory`" — the Load Game menu
option falls back to that when the setting is blank.

## Tests

`tests/` mirrors the layers under `source/`: one test project per
application/presentation layer and one per plugin, all using xUnit. `CardGames.Common.Tests` is not itself a test project — it's a shared
library holding cross-cutting test constants, notably `TestCaseConstants`
(`BUILD_TEST_TRAIT_NAME` / `BUILD_TEST_TRAIT_VALUE`), used to tag tests that
should run in the CI post-build gate:

```csharp
[Trait(TestCaseConstants.BUILD_TEST_TRAIT_NAME, TestCaseConstants.BUILD_TEST_TRAIT_VALUE)]
```

## CI

`.github/workflows/dotnet.yml` runs on push/PR to `master`:

```bash
dotnet restore
dotnet build --no-restore
dotnet test --no-build --filter category=post-build
```

Only tests carrying the `category=post-build` trait run in CI; the full
suite (`dotnet test` with no filter) is expected to be run locally.
