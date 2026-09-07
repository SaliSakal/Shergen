using Shergen.Lua;
using System;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Collections.Generic;
using System.Linq;

using static Shergen.Lua.Native;

namespace Shergen.Json
{
    public class Module : IShergenModule
    {
        // ── In-memory cache pro GetKey/SetKey ─────────────────────────────────────
        private static readonly Dictionary<string, (JsonNode doc, string fullPath)> _cache = new();

        private static readonly JsonSerializerOptions _writeOptions = new() {
            WriteIndented = true,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping

        };

        private static readonly JsonDocumentOptions _readOptions = new()
        {
            AllowTrailingCommas = true,
            CommentHandling     = JsonCommentHandling.Skip,
        };

        public void Register(Shergen engine)
        {
            engine.RegisterLuaFTable("JSON", new LuaTableEntry[]
            {
                new LuaFunctionEntry { Name = "Load",      Function = Load      },
                new LuaFunctionEntry { Name = "Save",      Function = Save      },
                new LuaFunctionEntry { Name = "Parse",     Function = Parse     },
                new LuaFunctionEntry { Name = "Stringify", Function = Stringify },
                new LuaFunctionEntry { Name = "Unload",    Function = Unload    },

                new LuaFunctionEntry { Name = "GetKey",    Function = GetKey    },
                new LuaFunctionEntry { Name = "SetKey",    Function = SetKey    },
                new LuaFunctionEntry { Name = "SetArray",  Function = SetArray  },
                new LuaFunctionEntry { Name = "SetObject", Function = SetObject },
                new LuaFunctionEntry { Name = "RemoveKey", Function = RemoveKey },
                new LuaValueEntry   { Name = "Version",   Value    = "1.0.0"   },


            });
        }

        // ── Interní helpers pro cache ──────────────────────────────────────────────

        private static JsonNode GetDoc(string file)
        {
            if (_cache.TryGetValue(file, out var cached))
                return cached.doc;

            if (!FileManager.FileExists(file))
                throw new Exception($"File not found: '{file}'");

            var doc = JsonNode.Parse(FileManager.LoadFile(file), null, _readOptions)
                      ?? throw new Exception($"Failed to parse '{file}'");
            _cache[file] = (doc, file);
            return doc;
        }

        private static JsonNode? Navigate(JsonNode? node, string[] parts, int depth)
        {
            if (depth >= parts.Length) return node;
            if (node == null)
                throw new InvalidOperationException($"JSON cesta: uzel je null u segmentu '{parts[depth]}' (index {depth}).");

            var part = parts[depth];

            if (int.TryParse(part, out int luaIdx))
            {
                if (node is not JsonArray arr)
                    throw new InvalidOperationException(
                        $"JSON cesta: segment '{part}' čeká pole, ale uzel je {node.GetType().Name}.");

                int idx = luaIdx - 1; // Lua 1-based → JsonArray 0-based

                if (idx < 0 || idx >= arr.Count)
                    throw new InvalidOperationException(
                        $"JSON cesta: index {luaIdx} je mimo rozsah (pole má {arr.Count} položek, platný rozsah je 1..{arr.Count}).");

                return Navigate(arr[idx], parts, depth + 1);
            }
            else
            {
                if (node is not JsonObject obj)
                    throw new InvalidOperationException(
                        $"JSON cesta: segment '{part}' čeká objekt, ale uzel je {node.GetType().Name}.");

                if (!obj.TryGetPropertyValue(part, out var child))
                    throw new InvalidOperationException($"JSON cesta: klíč '{part}' v objektu neexistuje.");

                return Navigate(child, parts, depth + 1);
            }
        }

        private static void WriteDoc(string file)
        {
            if (!_cache.TryGetValue(file, out var cached)) return;
            FileManager.SaveFile(cached.fullPath, cached.doc.ToJsonString(_writeOptions));
        }

        // ── JSON.GetKey(file, path) → value ───────────────────────────────────────
        private static int GetKey(IntPtr L)
        {
            try
            {
                string file = ToLuaString(L, 1);
                string path = ToLuaString(L, 2);
                var node = Navigate(GetDoc(file), path.Split('.'), 0);

                if (node is JsonValue val)
                {
                    if (val.TryGetValue(out bool b))   { PushLuaBoolean(L, b); return 1; }
                    if (val.TryGetValue(out int i))    { PushLuaInteger(L, i); return 1; }
                    if (val.TryGetValue(out double d)) { PushLuaNumber(L, d);  return 1; }
                    if (val.TryGetValue(out string s)) { PushLuaString(L, s);  return 1; }
                    if (node is JsonArray || node is JsonObject)
                    {
                        PushLuaValue(L, ElementToObject(node.Deserialize<JsonElement>()));
                        return 1;
                    }
                }

                PushLuaNil(L);
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, "JSON.GetKey error: " + ex.Message);
                return 1;
            }
        }

        // ── JSON.SetKey(file, path, value) → okamžitě zapíše na disk ─────────────
        private static int SetKey(IntPtr L)
        {
            try
            {
                SetNodeAtPath(ToLuaString(L, 1), ToLuaString(L, 2), LuaValueToJsonNode(L, 3));
                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex) { PushLuaError(L, "JSON.SetKey error: " + ex.Message); return 1; }
        }

        // ── SetArray a SetObject jsou speciální případy SetKey, které nastaví prvek na prázdné pole nebo objekt.
        private static int SetArray(IntPtr L)
        {
            try
            {
                SetNodeAtPath(ToLuaString(L, 1), ToLuaString(L, 2), new JsonArray());
                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex) { PushLuaError(L, "JSON.SetArray error: " + ex.Message); return 1; }
        }

        private static int SetObject(IntPtr L)
        {
            try
            {
                SetNodeAtPath(ToLuaString(L, 1), ToLuaString(L, 2), new JsonObject());
                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex) { PushLuaError(L, "JSON.SetObject error: " + ex.Message); return 1; }
        }

        // ── JSON.RemoveKey(file, path) ─────────────────
        // pozn: v JSONarray se odstraní prvek na indexu, což posune všechny indexy za ním o 1 doleva.
        private static int RemoveKey(IntPtr L)
        {
            try
            {
                string file = ToLuaString(L, 1);
                string path = ToLuaString(L, 2);
                var parts = path.Split('.');
                var doc = GetDoc(file);

                JsonNode parentNode = parts.Length == 1 ? doc : Navigate(doc, parts[..^1], 0);
                string lastKey = parts[^1];

                bool removed;
                if (parentNode is JsonArray arr)
                {
                    if (!int.TryParse(lastKey, out int luaIdx))
                        throw new InvalidOperationException(
                            $"JSON.RemoveKey: '{path}' v '{file}' — rodič je pole, ale klíč '{lastKey}' není číslo.");

                    int idx = luaIdx - 1;
                    if (idx < 0 || idx >= arr.Count)
                        removed = false;
                    else
                    {
                        arr.RemoveAt(idx);
                        removed = true;
                    }
                }
                else if (parentNode is JsonObject obj)
                {
                    removed = obj.Remove(lastKey);
                }
                else
                {
                    removed = false;
                }

                if (removed) WriteDoc(file);

                PushLuaBoolean(L, removed);
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, "JSON.RemoveKey error: " + ex.Message);
                return 1;
            }
        }

        // ── JSON.Unload(file) → vyhodí z cache ────────────────────────────────────
        private static int Unload(IntPtr L)
        {
            try { _cache.Remove(ToLuaString(L, 1)); }
            catch (Exception ex) { PushLuaError(L, "JSON.Unload error: " + ex.Message); return 1; }
            return 0;
        }

        // JSON.Parse(str) → table
        private static int Parse(IntPtr L)
        {
            if (IsLuaNil(L, 1)) return 0;
            try
            {
                var obj = ParseJson(ToLuaString(L, 1));
                PushLuaValue(L, obj);
                return 1;
            }
            catch (Exception ex) { PushLuaError(L, "JSON.Parse error: " + ex.Message); return 1; }
        }

        // JSON.Stringify(table [, pretty]) → str
        private static int Stringify(IntPtr L)
        {
            if (IsLuaNil(L, 1)) return 0;
            try
            {
                bool pretty = !IsLuaNil(L, 2) && ToLuaBoolean(L, 2);
                object obj = IsLuaTable(L, 1)
                    ? ReadLuaTableToObject(L, 1)
                    : LuaValueToObject(L, 1);
                PushLuaString(L, ObjectToJson(obj, pretty));
                return 1;
            }
            catch (Exception ex) { PushLuaError(L, "JSON.Stringify error: " + ex.Message); return 1; }
        }

        // JSON.Load(path) → table
        private static int Load(IntPtr L)
        {
            try
            {
                if (IsLuaNil(L, 1)) return 0;
                string path = ToLuaString(L, 1);
                if (!FileManager.FileExists(path)) { PushLuaNil(L); return 1; }
                var obj = ParseJson(FileManager.LoadFile(path));
                PushLuaValue(L, obj);
                return 1;
            }
            catch (Exception ex) { PushLuaError(L, "JSON.Load error: " + ex.Message); return 1; }
        }

        // JSON.Save(path, table [, pretty]) → bool
        private static int Save(IntPtr L)
        {
            try
            {
                if (IsLuaNil(L, 1) || IsLuaNil(L, 2)) return 0;
                string path = ToLuaString(L, 1);
                bool pretty = IsLuaNil(L, 3) || ToLuaBoolean(L, 3);
                object obj = IsLuaTable(L, 2)
                    ? ReadLuaTableToObject(L, 2)
                    : LuaValueToObject(L, 2);
                FileManager.SaveFile(path, ObjectToJson(obj, pretty));
                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex) { PushLuaError(L, "JSON.Save error: " + ex.Message); return 1; }
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static object ParseJson(string json)
        {
            using var doc = JsonDocument.Parse(json, _readOptions);
            return ElementToObject(doc.RootElement);
        }

        private static object ElementToObject(JsonElement el) => el.ValueKind switch
        {
            JsonValueKind.Object => (object)el.EnumerateObject()
                                       .ToDictionary(p => p.Name, p => ElementToObject(p.Value)),
            JsonValueKind.Array => el.EnumerateArray().Select(ElementToObject).ToList<object>(),
            JsonValueKind.String => el.GetString(),
            JsonValueKind.Number => el.TryGetInt32(out int i) ? (object)i : el.GetDouble(),
            JsonValueKind.True => (object)true,
            JsonValueKind.False => (object)false,
            _ => null
        };

        private static string ObjectToJson(object obj, bool pretty = true)
            => ToJsonNode(obj)?.ToJsonString(new JsonSerializerOptions { WriteIndented = pretty }) ?? "null";

        private static JsonNode ToJsonNode(object obj) => obj switch
        {
            null => null,
            bool b => JsonValue.Create(b),
            int i => JsonValue.Create(i),
            long l => JsonValue.Create(l),
            double d => JsonValue.Create(d),
            float f => JsonValue.Create(f),
            string s => JsonValue.Create(s),
            List<object> list => new JsonArray(list.Select(ToJsonNode).ToArray()),
            Dictionary<string, object> dict => new JsonObject(
                dict.Select(kv => KeyValuePair.Create(kv.Key, ToJsonNode(kv.Value)))),
            _ => JsonValue.Create(obj.ToString())
        };

        private static JsonNode LuaValueToJsonNode(IntPtr L, int index)
        {
            if (IsLuaNil(L, index)) return null;
            if (IsLuaBoolean(L, index)) return JsonValue.Create(ToLuaBoolean(L, index));
            if (IsLuaNumber(L, index))
                return IsLuaInteger(L, index)
                    ? JsonValue.Create((int)ToLuaInteger(L, index))
                    : JsonValue.Create(ToLuaNumber(L, index));
            if (IsLuaString(L, index)) return JsonValue.Create(ToLuaString(L, index));
            if (IsLuaTable(L, index)) return LuaTableToJsonNode(L, index);
            return null; // funkce, userdata atd. přeskočíme
        }

        private static JsonNode LuaTableToJsonNode(IntPtr L, int index)
        {
            int abs = index > 0 ? index : GetTop(L) + index + 1;

            // 1. průchod: je to čisté pole (klíče 1..N bez děr)?
            bool isArray = true;
            int count = 0;
            PushLuaNil(L);
            while (Next(L, abs) != 0)
            {
                count++;
                if (!IsLuaNumber(L, -2) || ToLuaNumber(L, -2) != count)
                    isArray = false;
                Pop(L, 1); // pop hodnotu, klíč necháme pro Next
            }

            if (isArray && count > 0)
            {
                var arr = new JsonArray();
                for (int i = 1; i <= count; i++)
                {
                    RawGetI(L, i, abs);                 // pushne t[i]
                    arr.Add(LuaValueToJsonNode(L, -1));
                    Pop(L, 1);
                }
                return arr;
            }

            var obj = new JsonObject();
            PushLuaNil(L);
            while (Next(L, abs) != 0)
            {
                string key = IsLuaString(L, -2) ? ToLuaString(L, -2) : AnyToLuaString(L, -2);
                obj[key] = LuaValueToJsonNode(L, -1);
                Pop(L, 1); // pop hodnotu, klíč necháme pro Next
            }
            return obj;
        }
        
        private static void SetNodeAtPath(string file, string path, JsonNode newValue)
        {
            var parts = path.Split('.');
            var doc = GetDoc(file);

            JsonNode parentNode = parts.Length == 1 ? doc : Navigate(doc, parts[..^1], 0);
            string lastKey = parts[^1];

            if (parentNode is JsonArray parentArr)
            {
                if (!int.TryParse(lastKey, out int luaIdx))
                    throw new InvalidOperationException(
                        $"JSON.SetKey: '{path}' v '{file}' — rodič je pole, ale klíč '{lastKey}' není číslo.");

                int idx = luaIdx - 1; // Lua 1-based → JsonArray 0-based

                if (idx < 0 || idx > parentArr.Count)
                    throw new InvalidOperationException(
                        $"JSON.SetKey: '{path}' v '{file}' — index {luaIdx} je mimo rozsah (pole má {parentArr.Count} položek, povolený rozsah je 1..{parentArr.Count + 1}).");

                if (idx == parentArr.Count) parentArr.Add(newValue);
                else parentArr[idx] = newValue;
            }
            else if (parentNode is JsonObject parentObj)
            {
                parentObj[lastKey] = newValue; // i "1" jako string klíč, pokud je rodič objekt
            }
            else
            {
                throw new InvalidOperationException($"Node at parent path is neither array nor object.");
            }

            WriteDoc(file);
        }
        
        
   

    }
}