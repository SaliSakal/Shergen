# JSON

*Module — separate C# project (`Shergen.Json`), not part of the core engine.*

A path-based JSON reader/writer. Paths are dot-separated (`"player.inventory.1"`), and **numeric path segments are 1-based**, matching Lua array conventions. Files are cached in memory after the first `GetKey`/`SetKey`/... call on them; `Unload` drops a file from the cache.

| Function | Description |
|---|---|
| `JSON.GetKey(file, path)` | Returns the value at `path` — a scalar (bool/number/string), a full Lua table (for arrays/objects), or `nil` if not found. |
| `JSON.SetKey(file, path, value)` | Sets the value at `path` and writes the file to disk immediately. `value` can be a scalar or a Lua table (array or object — array/object shape is inferred: a table with only sequential integer keys `1..N` becomes a JSON array, anything else becomes a JSON object). Setting the index one past the end of an array appends to it. |
| `JSON.SetArray(file, path)` | Sets `path` to an empty JSON array `[]`. Needed because an *empty* Lua table can't tell `SetKey` whether you meant an array or an object. |
| `JSON.SetObject(file, path)` | Sets `path` to an empty JSON object `{}`. Same reasoning as `SetArray`. |
| `JSON.RemoveKey(file, path)` | Removes the key/array element at `path`. Returns `true` if something was removed. Removing an array element shifts later indices down by one. |
| `JSON.Load(path)` | Parses a JSON file straight into a Lua table (no caching, no path/mutation support). |
| `JSON.Save(path, table [, pretty])` | Serializes a Lua table straight to a JSON file. |
| `JSON.Parse(str)` | Parses a JSON string into a Lua table. |
| `JSON.Stringify(table [, pretty])` | Serializes a Lua table to a JSON string. |
| `JSON.Unload(file)` | Drops a file from the `GetKey`/`SetKey` cache. |

```lua
JSON.SetKey("save.json", "player.name", "Tarek");
JSON.SetArray("save.json", "player.inventory");
JSON.SetKey("save.json", "player.inventory.1", { name = "Sword", qty = 1 });
local name = JSON.GetKey("save.json", "player.name");
```
