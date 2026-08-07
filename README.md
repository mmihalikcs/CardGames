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

## Table of Contents

- [Architecture](docs/architecture.md)
