# CardGames

A plugin-hosted console application for playing card games, built on .NET 10.
A small Console layer drives a console menu loop and delegates gameplay
to game plugins that are discovered and loaded at runtime from `*.plugin.dll`
files, each isolated in its own collectible `AssemblyLoadContext`.

Three games ship today:

- **WAR** — classic two-player card-flipping game; ties trigger a war.
- **Go Fish** — ask your opponent for a rank, go fish on a miss, collect books of four.
- **Poker** — Texas Hold'em, Omaha, or Five-Card Draw against configurable AI opponents.

See [docs/architecture.md](docs/architecture.md) for a full breakdown of the
layers, plugin loading model, and each game's implementation.

## Getting Started

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project source/CardGames.Console
```

## Godot Client

`source/CardGames.Godot/` is an embedded, single-player Godot 4 (C#/Mono)
client for the same games, with a real UI in place of console text. Requires
the Godot 4.6+ Mono/C# build (see
[docs/architecture.md#godot-client](docs/architecture.md#godot-client) for
how it fits together).

Open the project directly in the editor (adjust the path to your own Godot install):

```bash
/home/mmihalik/Applications/Godot_v4.6.2-stable_mono_linux_x86_64/Godot_v4.6.2-stable_mono_linux.x86_64 --editor --path source/CardGames.Godot
```

Then press **F5** (or the Play button) to run it — `Main.tscn` is the main
scene, and it opens straight into the plugin-select screen.

Alternatively, launch Godot with no arguments to open the Project Manager,
click **Import**, and browse to `source/CardGames.Godot/project.godot`.

## Releases

Every commit that lands on `master` and passes CI is versioned with
[Nerdbank.GitVersioning](https://github.com/dotnet/Nerdbank.GitVersioning)
(`version.json` at the repo root - the height of commits since the last
version bump becomes the patch number, so each build gets a unique,
deterministic SemVer) and published as a
[GitHub Release](../../releases) by `.github/workflows/release.yml`. Each
release attaches:

- Self-contained, single-file Console builds for `linux-x64` and `win-x64`.
- Exported Godot builds for Linux and Windows, produced by a headless
  Godot 4.6.2 (Mono) export using `source/CardGames.Godot/export_presets.cfg`.

Run `dotnet nbgv get-version` locally to see the version the next build
would produce.

## Table of Contents

- [Architecture](docs/architecture.md)
