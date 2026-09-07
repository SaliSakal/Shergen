# Shergen

A 2D UI framework and Lua scripting layer written in C#, built on [Silk.NET](https://github.com/dotnet/Silk.NET) (OpenGL bindings) for rendering, with a native Lua C API binding for scripting. Targets Windows, Linux and Android (x64 / ARM64).

Shergen is not (yet) a full game engine — right now it covers rendering, a UI toolkit, and Lua scripting. Game-specific systems (physics, entity/scene management, etc.) aren't part of it.

## Features

- **UI toolkit** — labels, text fields, text logs, scroll boxes/bars, buttons and other standard elements, all driven from Lua.
- **Rich text** — inline formatting tags (`<b>`, `<i>`, `<u>`, `<st>`, `<c=RRGGBB>`, `<s=scale>`) and inline icons (`<icon=name>`) registered from Lua, with word wrap and per-character layout.
- **Lua scripting** — a native C API binding exposes engine functionality to Lua as global tables (`GUI`, `JSON`, ...); game/UI logic is written entirely in Lua.
- **JSON persistence** — a `JSON` Lua module (`Load`, `Save`, `Parse`, `Stringify`, `GetKey`, `SetKey`, `SetArray`, `SetObject`, `RemoveKey`) for reading and mutating JSON files directly from Lua, with 1-based (Lua-style) path indexing.
- **Cross-platform rendering** — SDF font rendering, batched draw calls, async texture loading, built on Silk.NET/OpenGL so the same renderer runs on Windows, Linux and Android.

## Project structure

```
src/
  program/              Entry point (executable)
  shergen/               Core engine: renderer, UI system, Lua bindings
  Shergen.Json/           JSON Lua module
  Shergen.XLSX/           XLSX Lua module
  Shergen.RendererTK/     Legacy OpenTK-based renderer (superseded by the Silk.NET renderer; kept for reference)

Data/
  ShergenData/            Shared engine assets (fonts, shaders, ...)
  ShergenDataWin/         Windows-specific data
  ShergenDataLin/         Linux-specific data
  ShergenDataAndr/        Android-specific native libraries
  UIData/                 UI theme assets (panels, buttons, backgrounds)
```

Game/project-specific content (e.g. a Lua-scripted app built on top of Shergen) is expected to live outside this repo and be wired in separately — see `Program.csproj` for how content folders are pulled in at build time.

## Building

Requirements: [.NET SDK](https://dotnet.microsoft.com/) matching the `net10.0` target, Visual Studio (recommended) or the `dotnet` CLI.

1. Open `shergen.slnx` in Visual Studio, or run `dotnet build` from the repository root.
2. The `program` project is the entry point; the other projects under `src/` are referenced by it.
3. Build target platforms are `x64` and `ARM64`.

## Scripting

Engine functionality is exposed to Lua through global tables, e.g.:

```lua
-- UI
local label = GUI.CreateLabel(parent, "Hello, <c=FFD700>world</c>!");

-- JSON persistence
JSON.SetKey("save.json", "player.name", "Tarek");
local name = JSON.GetKey("save.json", "player.name");
```

## License

*Not yet chosen — add a `LICENSE` file before treating this as open source.*
