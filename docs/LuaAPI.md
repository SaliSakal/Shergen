# Shergen Lua API Reference

Shergen scripts run in a sandboxed Lua 5.5-based environment. This reference is split by area — pick the file for what you're working on:

**Core** (compiled into the main `shergen` project — always present):
- [`LuaAPI-GUI.md`](LuaAPI-GUI.md) — the element wrapper (recommended, how UI is actually written) and the raw `GUI`/`File` bridge underneath it.
- [`LuaAPI-Audio.md`](LuaAPI-Audio.md) — the raw `AUDIO` bridge and the `Sound` Lua wrapper.
- [`LuaAPI-Constants.md`](LuaAPI-Constants.md) — every constant table (`TYPE`, `PROP`, `ALIGN`, `BORDER_TYPE`, `BUTTON`, `CALLBACK`, `SCROLLBAR_TYPE`, `PLATFORM`) and the two version globals.

**Modules** (separate C# projects, not part of the core engine):
- [`LuaAPI-JSON.md`](LuaAPI-JSON.md) — the `JSON` module (`Shergen.Json`).
- [`LuaAPI-XLSX.md`](LuaAPI-XLSX.md) — the raw `XLSX` bridge and the `Excel`/`Workbook`/`Sheet`/`Cell` Lua class wrapper (`Shergen.XLSX`).

Each module ships its own native `TABLE.*` bridge, and — where one exists — an idiomatic Lua wrapper on top, the same split as `GUI` vs. the element wrapper.

Colors are always passed/returned as `{R, G, B, A}` tables with components `0-255`, unless noted otherwise.

---

## Lua↔C# bridge functions (core language)

Registered directly as Lua globals (not inside a table) — these are the only non-standard-library functions available outside `GUI`/`File`/`AUDIO`/`JSON`/`XLSX`.

| Function | Description |
|---|---|
| `include(script1, script2, ... [, envTable])` | Loads and runs one or more `.lua` files (path relative to the app's data folder, `.lua` is appended automatically) into the current environment, or into `envTable` if the last argument is a table. A load error is caught and logged to console; execution continues. |
| `tryInclude(script1, script2, ... [, envTable])` | Same as `include`, but silently does nothing for files that don't exist (no error) — for optional includes. |
| `import(path [, envTable])` → table\|nil | Loads a `.lua` file as an isolated module: runs it with `_ENV` pointing at a **new environment table** (falls back to `_G` for globals unless `envTable` is given), and returns that environment as the module's namespace. Like `require`, but sandbox-safe — the module's globals don't leak into the caller. Returns `nil` if the file doesn't exist or fails to run. |
| `print(...)` | Sandboxed print — writes to console prefixed `[LUA]`. |
| `getCurrentRunID()` → id | Returns the ID of the currently executing script "run". |
| `stopRunByID(id)` | Requests cancellation of the execution with that ID. |

---

## File

| Function | Description |
|---|---|
| `File.Save(path, content)` | Writes a text file. |
| `File.Load(path)` | Reads a text file, returns its contents. |
| `File.Exists(path)` | Returns `true`/`false`. |
| `File.GetFiles(path)` | Returns a Lua array of file names in a directory. |
| `File.GetDirs(path)` | Returns a Lua array of subdirectory names. |

---

### Sandbox

A few pieces of the standard library are removed or frozen for safety:

- **Removed (`nil`):** `io`, `package`, `require`, `loadfile`, `dofile`, `loadstring`, `load`, `getmetatable`, `collectgarbage`, `setfenv`, `getenv`, `newproxy`.
- **Restricted:** `os` only keeps `clock`, `date`, `difftime`, `time`. `debug` only keeps `traceback`.
- **Frozen (`__metatable = false`, can't be reassigned):** `_G`, `coroutine`, `string`, `table`, `math`, `utf8`.
