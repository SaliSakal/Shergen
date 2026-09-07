# GUI & File

*Core — compiled into the main `shergen` project.*

There are two layers to the UI API:

- **The element wrapper** (`Shergen/core/elements.lua` + `elements.utils.lua`, loaded as part of `ShergenData`) — a Lua-side OOP layer built on top of `GUI.*`, and the way UI is actually written in practice. Documented first, since almost nothing is written against raw `GUI.*` calls directly.
- **`GUI.*`** (documented second) — the raw C#↔Lua bridge. Every call needs an element ID and a `PROP.*` constant. Use this directly only for things the wrapper doesn't cover yet.

See [`LuaAPI-Constants.md`](LuaAPI-Constants.md) for `TYPE`, `PROP`, `ALIGN`, `BORDER_TYPE`, `BUTTON`, `CALLBACK`, `SCROLLBAR_TYPE`.

---

## Element wrapper (recommended)

### Creating elements

| Function | Description |
|---|---|
| `NewElement(parent, anchor, possize, visible, elementTable [, class])` | Generic constructor. Returns a Lua table (the "element") with a metatable giving it `:Set*`/`:Get*` methods. |
| `NewLabel(parent, anchor, possize, font, text, elementTable)` | `TYPE.LABEL`. |
| `NewScrollbox(parent, anchor, possize, elementTable)` | `TYPE.SCROLLBOX` (scissor enabled by default). |
| `NewScrollbar(parent, anchor, possize, bindTo, elementTable)` | `TYPE.SCROLLBAR`. `bindTo` is the scrollbox element to attach to (or `nil`). `elementTable.vertical` (default `true`) picks vertical/horizontal. |
| `NewScrollbar_RightOf(bindTo, anchor, width, elementTable)` / `NewScrollbar_DownOf(bindTo, anchor, height, elementTable)` | Convenience constructors that position the scrollbar directly against another element. |
| `NewTextlog(parent, anchor, possize, font, text, elementTable)` | `TYPE.TEXTLOG` (scissor + top-aligned text by default). |
| `NewTextField(parent, anchor, possize, font, elementTable)` | `TYPE.TEXTFIELD`. |

`parent` is an element (not an ID) — pass `Desktop` for a top-level element. `anchor` is `MakeAnchor(left, top, right, bottom)` (all booleans) or `nil`. `possize` is `XYWH(x, y, width, height)`. `font` is either a font-name string, or `FontDFF(name, size, rangeMin, rangeMax, charSpacing, lineSpacing)`.

`elementTable` carries every other property as plain fields, plus a `callbacks` table:

```lua
local btn = NewLabel(Desktop, MakeAnchor(true, true, false, false), XYWH(10, 10, 200, 30), "MyFont.DFF", "Click me", {
    color       = RGBA(255, 255, 255, 255),
    font_color  = RGBA(0, 0, 0, 255),
    border_size = 1, border_type = BORDER_TYPE.OUTER, border_color = RGB(0, 0, 0),
    can_focus   = true,
    callbacks = {
        [CALLBACK.LCLICK] = { function(id, ax, ay, x, y) print("clicked", id) end, "%id", "%ax", "%ay", "%x", "%y" },
    },
});
```

Field names mirror the `PROP.*` constants in `snake_case` (`font_color`, `border_size`, `auto_size_width`, `scroll_offset_x`, `text_halign`/`text_valign` using `ALIGN.*`, `max_line`, `add_at_bottom`, `hit_mask`, `scissor_mask`, ...). Callback values follow the same three shapes as `GUI.SetCallback` (function, `{func, ...args}`, or a string), and extra args support the placeholders below.

**Callback placeholders** (substituted per-event, for both the property-table `callbacks` and `element:SetCallback(...)`): `%id`, `%x`/`%y` (local), `%ax`/`%ay` (absolute), `%b` (button), `%d`/`%dh` (scroll delta), `%dx`/`%dy` (move delta), `%k`/`%sc` (key/scancode), `%c` (typed char), `%shift`/`%ctrl`/`%alt` (+ `l`/`r` variants), `%vis`, `%w`/`%h`.

### Fluent methods

Every element returned by `New*` has `:SetX(...)`-style methods (each returns the element, so calls chain) and matching `:GetX()` readers. They're plain functions under the hood (`SetX(element, value)`) attached via metatable, so `element:SetX(100)` and `SetX(element, 100)` are equivalent.

**`ElementClass`** (all elements): `SetParent`/`GetParent`, `SetX`/`SetY`/`SetPos`/`GetX`/`GetY`/`GetPos`, `SetWidth`/`SetHeight`/`SetSize`/`SetPosSize`/`SetXYWH`/`GetWidth`/`GetHeight`/`GetSize`/`GetXYWH`, `BringToFront`/`BringToBack`/`BringBy`, `SetVisible`/`Show`/`Hide`/`GetVisible`, `SetFade`/`GetFade`, `SetFocus`, `SetCanFocus`/`GetCanFocus`, `SetScissor`/`SetScissorMask`/`SetScissorMaskThreshold` (+ getters), `SetAnchor`, `SetColor`/`SetColor2`/`SetColor3`/`SetBorderColor` (+ getters), `SetBorder`/`SetBorderSize`/`SetBorderType` (+ getters), `SetTexture`/`SetTexture2`/`SetTexture3`/`SetTextureFallback`/`SetTexture2Fallback`/`SetTexture3Fallback`/`SetSubCoords`/`SetGrayscale` (+ getters, plus `GetTextureID`/`GetTexture2ID`/`GetTexture3ID`/`GetTexture*Width`/`Height`/`Size`), `SetPassThrough`/`SetEventCapture`/`SetHitMask` (+ getters), `SetCallback`, `SetHint`/`GetHint`, `Destroy`, `DestroyChildren`.

**`LabelClass`** (adds to `ElementClass`; used by Label, TextLog, TextField): `SetFontColor`/`SetShadowColor`/`SetOutlineColor` (+ getters), `SetText`/`SetFont`/`SetFontSize`/`SetAutoSizeWidth`/`SetAutoSizeHeight`/`SetAutoSizeWidthMax`/`SetAutoSizeHeightMax`/`SetHAlign`/`SetVAlign`/`SetWordWrap`/`SetShowRich` (+ getters), `SetShadow`/`SetShadowOffsetX`/`SetShadowOffsetY` (+ getters), `SetOutline`/`SetOutlineWidth` (+ getters), `SetCharScaleX`/`SetCharScaleY`/`SetCharSpacing`/`SetLineSpacing` (+ getters), `SetScrollText`/`SetScrollTextSpeed`/`SetScrollTextDelay` (+ getters).

**`ScrollboxClass`** (adds to `ElementClass`): `SetScrollOffset`/`SetScrollOffsetX`/`SetScrollOffsetY`/`GetScrollOffset`, `SetBarToScrollbox`, `SetHorizontalScroll`/`GetHorizontalScroll`, `GetScrollbar`/`GetScrollbar2`.

**`ScrollbarClass`** (adds to `ElementClass`): `GetScrollbarType`.

**`TextlogClass`** (adds to `LabelClass`): `AddLine`, `CleanLines`, `SetMaxLines`/`GetMaxLines`, `SetAddAtBottom`/`GetAddAtBottom`, `SetScrollOffsetY`, `SetBarToScrollbox`, `GetScrollOffset`, `GetScrollbar`.

> Heads up: in the current source, `TextlogClass.SetAddAtBottom` is assigned from `GetAddAtBottom`, and `TextlogClass.GetMaxLines` is assigned from `SetMaxLines` — looks like a copy-paste swap (`Shergen/core/elements.lua`). Worth double-checking if those two behave oddly.

**`TextFieldClass`** (adds to `LabelClass`): no extra methods yet — read-only/max-length/password-mode/cursor properties (`PROP.READONLY`, `PROP.MAXLENGHT`, `PROP.PASSWORDMODE`, `PROP.PLACEHOLDER`, `PROP.CURSORPOSITION`, `PROP.CURSORCOLOR`, `PROP.CURSORSPEED`) currently have to be set via raw `GUI.SetProperty(element.ID, PROP.*, value)`.

### Pre-existing elements & globals

`Desktop` (ID 0, `ElementClass`), `Mouse` (ID 1, has its own cursor methods — see below), `FPSCounter` (ID 2, `LabelClass`), `Profiler` (ID 3, `LabelClass`) are created at startup and available as globals immediately.

`Mouse` methods: `:GetHotSpot()`, `:SetCursor(texture, x, y)`, `:SetTexture(texture)`, `:ResetCursor()`, `:GetPos()`, `:GetRelativePos()` (relative to the element under the cursor — best used inside callbacks), `:SetPos(x, y)`, `:SetLock(bool)`, `:SetCaptured(bool)` (FPS-style: hidden + locked + raw delta), `:SetHidden(bool)`, `:GetDelta()`.

### Other helpers

| Function | Description |
|---|---|
| `RGBA(r, g, b, a)` / `RGB(r, g, b)` (alpha 255) | Builds a color table. |
| `XYWH(x, y, w, h)` | Builds a position/size table. |
| `MakeAnchor(left, top, right, bottom)` | Builds an anchor table. |
| `FontDFF(name, size, rangeMin, rangeMax, charSpacing, lineSpacing)` | Builds a font descriptor for the `font` argument of `NewLabel`/`NewTextlog`/`NewTextField`. |
| `RegTextIcon(name, icon, color, scale, xOffset, yOffset)` / `RemTextIcon(name)` | Thin wrappers over `GUI.RegisterIcon`/`GUI.UnregisterIcon` with argument validation. |
| `DestroyElement(element)` / `DestroyChildren(element)` | Free functions equivalent to `element:Destroy()` / `element:DestroyChildren()`. |

`utils.lua` also adds a handful of general-purpose helpers unrelated to UI: `Loc(str, args, default)` (named-placeholder localization), `switch(value):caseof{...}`, `safeCall(f, ...)`, `table.serialize`/`table.copytable`/`table.contains`/`table.removeValue`/`table.diff`, `string.trim`/`rtrim`/`ctrim`/`startswith`, and assorted `math.*` helpers (`math.round`, `math.trunc`, `math.minmax`, `math.odd`/`even`, `math.div`).

---

## GUI (raw bridge)

### Elements

| Function | Description |
|---|---|
| `GUI.NewElement(type, parentID, isSpecial [, propTable])` | Creates a new UI element of `TYPE.*` under `parentID` (0 = desktop). Returns the new element's ID. Optional 4th argument is a table of `[PROP.*] = value` pairs, a `callbacks = {...}` table, and/or an `anchor = {left=, right=, top=, bottom=}` table — see below. |
| `GUI.DestroyElement(id)` | Destroys an element. IDs 0-2 are reserved system elements and cannot be destroyed. |
| `GUI.DestroyChildren(id)` | Destroys all children of an element. |
| `GUI.GetChildrenIDs(id)` | Returns a Lua array of the element's children IDs. |
| `GUI.GetParentID(id)` | Returns the parent's ID, or `nil`. |
| `GUI.SetParent(id, parentID)` | Reparents an element. |
| `GUI.BringToFront(id)` / `GUI.BringToBack(id)` | Moves the element to the front/back of its parent's draw order. |
| `GUI.BringBy(id, offset)` | Moves the element by `offset` positions in its parent's draw order. |

`NewElement`'s optional 4th argument, in full:

```lua
GUI.NewElement(TYPE.LABEL, parentID, false, {
    [PROP.TEXT]  = "Hello",
    [PROP.WIDTH] = 200,
    [PROP.COLOR1] = {R=255, G=255, B=255, A=255},
    callbacks = {
        [CALLBACK.LCLICK] = function(id) ... end,
        [CALLBACK.LCLICK] = { function(id, extra) ... end, "some_extra_arg" },
        [CALLBACK.LCLICK] = "SomeGlobalLuaFunction(%id)",
    },
    anchor = { left = true, right = true, top = true, bottom = false },
});
```

### Properties

| Function | Description |
|---|---|
| `GUI.SetProperty(id, PROP.*, value)` | Sets a property. `value` is a number/bool/string depending on the property, or a `{R,G,B,A}` table for color properties, or a `{X,Y,W,H}` table for the `SUBCOORDS` property. |
| `GUI.GetProperty(id, PROP.*)` | Returns the property's current value (same shapes as above). |
| `GUI.SetAnchor(id, {left=, right=, top=, bottom=})` | Sets which edges of the element stay anchored to its parent on resize. |

See [`PROP` constants](LuaAPI-Constants.md#prop) for the full list of properties and their value types.

### Fonts & icons

| Function | Description |
|---|---|
| `GUI.SetDefaultFont(type, name)` | `type = "DFF"` sets the default DFF font — this is the only font type the renderer actually uses today. `type = "TTF"` just stores a name for later; TTF rendering isn't implemented yet. |

> **DFF** is the same custom font format used by the game *Original War* — not a Shergen invention.

| Function | Description |
|---|---|
| `GUI.SetFontFallback(fontName, char)` | Sets the glyph rendered when a character is missing from `fontName`. |
| `GUI.RegisterIcon(tag, path, color, scale, offsetX, offsetY)` | Registers an inline rich-text icon usable as `<icon=tag>`. `color` is `nil` (inherits text color) or `{R,G,B,A}`. `scale` is a multiplier of font height (default `1.0`). `offsetX`/`offsetY` are proportional to font height (default `0`). |
| `GUI.UnregisterIcon(tag)` | Removes a registered icon. |

### Callbacks & focus

| Function | Description |
|---|---|
| `GUI.SetCallback(id, CALLBACK.*, func_or_string_or_nil, ...extraArgs)` | Registers (or clears, with `nil`) a callback. `extraArgs` are forwarded to the function, or substituted into a string callback via `%1`, `%2`, ... placeholders. |
| `GUI.SetFocus(id)` | Gives keyboard focus to an element (`0` clears focus). |
| `GUI.RegisterTickCallback(func_or_string)` → id | Registers a per-frame callback; string form receives `%frametime`. |
| `GUI.UnregisterTickCallback(id)` | Removes a tick callback. |
| `GUI.SetResizeCallback(func_or_string)` | Sets the window-resize callback; function receives `(w, h)`, string form receives `%w`/`%h`. |

See [`CALLBACK` constants](LuaAPI-Constants.md#callback) for the full event list.

### Input / event data

Call these from inside a callback to read the current event's data.

| Function | Description |
|---|---|
| `GUI.GetMouseX()` / `GUI.GetMouseY()` | Absolute mouse position. |
| `GUI.GetMouseDeltaX()` / `GUI.GetMouseDeltaY()` | Mouse movement since last frame. |
| `GUI.GetMouseLocalX()` / `GUI.GetMouseLocalY()` | Mouse position relative to the element under the cursor. |
| `GUI.GetButton()` | Last mouse button index — see [`BUTTON`](LuaAPI-Constants.md#button). |
| `GUI.GetScroll()` | Last scroll wheel delta (positive = up). |
| `GUI.GetKey()` | Last keyboard key code. |
| `GUI.GetChar()` | Last text-input character, as a string. |
| `GUI.IsKeyPressed(keyCode)` | Polls current key state (not event-based). |
| `GUI.IsMousePressed(button)` | Polls current mouse button state. |

### Cursor

| Function | Description |
|---|---|
| `GUI.GetCursorID()` | ID of the virtual cursor element (always `1`). |
| `GUI.SetCursor(path, x, y)` | Sets cursor texture and hotspot together (`x`/`y` default `0`). |
| `GUI.SetCursorTexture(path)` | Changes only the texture, keeps the hotspot. |
| `GUI.ResetCursor()` | Restores the OS default cursor. |
| `GUI.GetCursorHotSpot()` → x, y | Returns the current hotspot. |
| `GUI.SetCursorPos(x, y)` | Moves the OS cursor. |
| `GUI.SetCursorLocked(bool)` | Locks the cursor to the window. |
| `GUI.SetCursorHidden(bool)` | Shows/hides the cursor. |
| `GUI.SetCursorCaptured(bool)` | FPS-style capture: hidden + locked + raw input. |

### TextLog

| Function | Description |
|---|---|
| `GUI.AddLine(id, text)` | Appends a line to a `TYPE.TEXTLOG` element. |
| `GUI.ClearLines(id)` | Clears all lines. |

### App

| Function | Description |
|---|---|
| `GUI.Exit()` | Closes the application. |

