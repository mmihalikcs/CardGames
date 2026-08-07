# Escape/Pause Menu Overlay for CardGames.Godot

## Context

The Godot client (`source/CardGames.Godot`) currently has no way to leave an in-progress game
session except letting it finish naturally, at which point `GameOverPanel`'s "Back to Menu" button
appears. There's no pause/escape menu, and no graceful way to exit the whole application from
inside a game. We're adding an Escape-triggered pause overlay during gameplay with two actions —
"Quit Game" (back to the plugin-select screen) and "Exit to Desktop" (closes the app) — styled as a
translucent grey full-screen overlay with center-aligned buttons, that also visually/functionally
blocks the playfield underneath. It needs to be trivially extensible for more options later (e.g.
Settings, Restart) without a rework.

Investigating the existing session-lifecycle code surfaced a real correctness gap: `IGameManager.StartGame()`
runs synchronously on a background `Task` and blocks on `GodotGameChannel.Await()` between prompts,
with no cancellation support anywhere in `IGameManager`/`IGameChannel`. If "Quit Game" just hid the
screen, that background thread would in some cases park forever waiting on a prompt response that
will never arrive. The user chose to fix this properly (add a `Cancel()` path scoped entirely to
`GodotGameChannel`, no changes to `CardGames.Domain` or plugins) rather than ship it as a known bug.

## Design

### 1. New scene `Scenes/PauseMenuPanel.tscn` + script `Scripts/PauseMenuPanel.cs`

A full-rect `Control`, `visible = false`, `process_mode = 3` (`Always`) set in the `.tscn` (matches
this codebase's convention of declaring static visibility/structure in `.tscn`, e.g. `GameOverPanel`).
Structure:

- `Background` — a `ColorRect` sibling, full-rect, `color = Color(0.1, 0.1, 0.1, 0.6)` (grey,
  slightly opaque), `mouse_filter = 0` (`MOUSE_FILTER_STOP`) as defense-in-depth blocking (note: `1`
  is `PASS`, not `Stop`, in Godot 4's `MouseFilter` enum — `0` is `Stop`).
- `MenuCenter` (`CenterContainer`, full-rect) → `MenuOptions` (`VBoxContainer`, `alignment = 1` i.e.
  `ALIGNMENT_CENTER`) → two `Button`s: "Quit Game", "Exit to Desktop". `CenterContainer` +
  center-aligned `VBoxContainer` gives both horizontal and vertical centering.

`PauseMenuPanel.cs`:
- `event Action? QuitGameRequested`, `event Action? ExitToDesktopRequested`, fired from each button's
  `Pressed`.
- `Open()` sets `Visible = true` and `GetTree().Paused = true`; `Close()` reverses both.
- Its own `_UnhandledInput` closes the panel on `ui_cancel` (Escape) — this node's `ProcessMode` is
  `Always`, so it still receives input while the tree is paused, which is what makes Escape work as a
  toggle. Call `GetViewport().SetInputAsHandled()` after handling.

**Why `SceneTree.Paused` instead of manually disabling widgets:** everything under `Layout` in
`GameSessionPanel` stays at the default `ProcessMode.Inherit` (→ `Pausable`), so once
`GetTree().Paused = true`, all of it — card buttons, prompt buttons, `LineEdit` — stops receiving
input automatically. No per-widget `mouse_filter`/focus-release code needed. Important: do **not**
set `GameSessionPanel`'s own `ProcessMode` to `Always` — `Inherit` children resolve by walking up to
the nearest non-`Inherit` ancestor, so that would make `Layout` effectively `Always` too and defeat
the blocking.

**Extensibility:** a future third option is one more `Button` node in the `.tscn`, one more event +
`Pressed` wire-up in `PauseMenuPanel.cs`, and one more subscription in `GameSessionPanel.cs` —
mirroring `GameOverPanel`'s existing single-`BackButton` precedent. No data-driven menu-item
framework; not warranted here.

### 2. `Scenes/GameSessionPanel.tscn`

Instance `PauseMenuPanel.tscn` as a new child of the root, added **after** `Layout` (later siblings
draw/hit-test on top in Godot), full-rect anchors, `visible = false`.

### 3. `Scripts/GameSessionPanel.cs`

- Add field `_PauseMenu`, resolved via `GetNode<PauseMenuPanel>("PauseMenuPanel")` in `_Ready()`;
  subscribe `QuitGameRequested`/`ExitToDesktopRequested`.
- New `_UnhandledInput` override: on `ui_cancel`, call `_PauseMenu.Open()` — but only if
  `!_PauseMenu.Visible && !_GameOverPanel.Visible` (don't reopen if already open; don't offer "Quit
  Game" on top of an already-finished game's "Back to Menu" button). `SetInputAsHandled()` after.
- `OnQuitGameRequested`: `_PauseMenu.Close()`, `_Channel?.Cancel()`, then
  `BackToMenuRequested?.Invoke()` — reuses the exact same event `MainController.ReturnToMenu` already
  handles, so no new navigation plumbing is needed.
- `OnExitToDesktopRequested`: `GetTree().Quit()`. No need to cancel the channel — process exit takes
  the background thread with it.

### 4. `Scripts/GodotGameChannel.cs` — add cancellation

```csharp
private volatile bool _Cancelled;

public PromptResponse Await(GamePrompt prompt)
{
    if (_Cancelled)
        throw new OperationCanceledException("Game session was cancelled by the user.");

    var tcs = new TaskCompletionSource<PromptResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
    _Pending = tcs;
    Callable.From(() => _OnPromptMainThread(prompt)).CallDeferred();
    return tcs.Task.GetAwaiter().GetResult();
}

public void Cancel()
{
    _Cancelled = true;
    var tcs = _Pending;
    _Pending = null;
    tcs?.TrySetException(new OperationCanceledException("Game session was cancelled by the user."));
}
```

`_Cancelled` is `volatile`: written on the main thread (`Cancel()`), read on the background thread
(`Await()`), no other synchronization between them. Two-part fix is necessary, not just resolving
`_Pending`: if the background thread is between prompts (not currently blocked in `Await`) at the
moment `Cancel()` fires, `_Pending` is `null` and there's nothing to resolve — without the `_Cancelled`
check at the top of `Await()`, the thread would create a *new* `TaskCompletionSource` on its next call
and block again, forever, since the UI has already navigated away and nothing will ever call
`SubmitPromptResponse`. `Publish()` needs no change — it's fire-and-forget and never blocks, so a
stray deferred call against a hidden panel after cancellation is harmless.

### 5. `Scripts/MainController.cs` — `OnPluginChosen`

```csharp
_ = Task.Run(() =>
{
    var cancelled = false;
    try
    {
        gameManager.StartGame();
    }
    catch (OperationCanceledException)
    {
        cancelled = true;
    }
    finally
    {
        if (!cancelled)
            Callable.From(_GameSessionPanel.ShowGameOver).CallDeferred();
    }
});
```

Catch only `OperationCanceledException` — a broader catch would also swallow genuine plugin crashes
during normal play. The `cancelled` guard around `ShowGameOver()` matters beyond final-state
correctness: `ShowGameOver()` runs as a *deferred* call at an unpredictable point after
`BackToMenuRequested` already fired synchronously. Without the guard, a fast user who starts a new
game before that deferred call lands could have it stomp the fresh `_GameOverPanel.Visible = false`
that `ResetForNewSession()` just set for the *new* session — flashing the stale game-over panel over
the new game. `ReturnToMenu()` itself needs no changes.

## Critical files

- `source/CardGames.Godot/Scenes/PauseMenuPanel.tscn` (new)
- `source/CardGames.Godot/Scripts/PauseMenuPanel.cs` (new)
- `source/CardGames.Godot/Scenes/GameSessionPanel.tscn`
- `source/CardGames.Godot/Scripts/GameSessionPanel.cs`
- `source/CardGames.Godot/Scripts/GodotGameChannel.cs`
- `source/CardGames.Godot/Scripts/MainController.cs`

## Verification (manual — no automated Godot test project exists in this repo)

Build (`dotnet build`) and run `source/CardGames.Godot/project.godot` in the Godot 4.6 editor (F5).

1. Start a WAR or GoFish session, press Escape mid-game → grey translucent overlay appears, "Quit
   Game"/"Exit to Desktop" centered on screen.
2. With the overlay open, confirm clicks on prompt buttons and typing into a visible `LineEdit`
   underneath do nothing (`SceneTree.Paused` blocking `Layout`'s subtree).
3. Press Escape again → overlay closes, game resumes, a prompt response can be submitted normally.
4. Press Escape while a `TextPrompt`'s `LineEdit` has input focus → confirm the pause menu still
   opens (i.e. `ui_cancel` isn't being swallowed by the focused `LineEdit` before reaching
   `_UnhandledInput`).
5. Let a game finish naturally (`GameOverPanel` shown) and press Escape → confirm the pause overlay
   does **not** appear, and "Back to Menu" still works.
6. Start a session, wait for an active prompt, Escape → Quit Game → confirm immediate, clean return
   to the plugin-select screen, no hang, no exception in the Godot console. Start a new game
   afterward and confirm no stray game-over panel or leftover state appears.
7. Start a session and Escape → Quit Game as fast as possible (before any prompt renders), to
   exercise the `_Cancelled`-checked-at-top-of-`Await` path — confirm still no hang.
8. Escape → Exit to Desktop → confirm the app window closes cleanly, no crash dialog.
9. Repeat 6–7 for both WAR and GoFish (different prompt shapes: `ConfirmPrompt`/`ChoicePrompt` vs.
   `TextPrompt`/`ChoicePrompt`).
10. Play one full game to completion without ever pressing Escape, to confirm the existing
    `GameOverPanel` flow is unaffected.

If Escape doesn't trigger `ui_cancel` when actually run (verify via Project Settings → Input Map —
Godot ships `ui_cancel` bound to Escape by default even with no `[input]` section in
`project.godot`), fall back to checking `InputEventKey { Pressed: true, Keycode: Key.Escape }`
directly in both `_UnhandledInput` overrides.
