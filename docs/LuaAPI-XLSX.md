# XLSX

*Module — separate C# project (`Shergen.XLSX`), not part of the core engine.*

Like `GUI`, this has two layers: the `Excel`/`Workbook`/`Sheet`/`Cell` Lua class wrapper (`Shergen.XLSX/Lua/Shergen/xlsx.lua`) — the way this module is actually meant to be used — and the raw `XLSX.*` bridge underneath it. Almost everything below is `:method()` calls on chainable objects, not raw `XLSX.*` calls.

---

## Excel wrapper (recommended)

### Opening & creating workbooks

| Function | Description |
|---|---|
| `Workbook(path)` / `Excel.Workbook(path)` → `Workbook` | Opens an existing workbook. Opens for read **and** write — no other process may have the file open at the same time. |
| `NewWorkbook([name])` / `Excel.NewWorkbook([name])` → `Workbook` | Creates a new, empty in-memory workbook. |
| `WorkbookExists(path)` | Returns `true`/`false`. |
| `CloseAllWorkbooks()` | Closes every open workbook. |

`Workbook`/`NewWorkbook`/`WorkbookExists`/`CloseAllWorkbooks` are global aliases for `Excel.Workbook`/`Excel.NewWorkbook`/`Excel.WorkbookExists`/`Excel.CloseAllWorkbooks` — both names work.

### Workbook methods (`WorkbookClass`)

| Method | Description |
|---|---|
| `wb:Sheet(name)` → `Sheet`\|nil | Gets a sheet by name. Errors if the sheet doesn't exist. |
| `wb:AddSheet(name)` → `Sheet`\|nil | Creates a new sheet. |
| `wb:DeleteSheet(name)` | Deletes a sheet. |
| `wb:RenameSheet(oldName, newName)` | Renames a sheet. |
| `wb:GetSheetNames()` | Returns a Lua array of sheet names. |
| `wb:ContainSheet(name)` | Returns `true`/`false`. |
| `wb:Save()` | Saves in place. |
| `wb:SaveAs(path)` | Saves to a new path. |
| `wb:Close()` | Closes the workbook — `self.ID` becomes `nil`, and any further call on this `wb` errors ("workbook closed"). |
| `wb:Recalculate()` | Recalculates every formula in the workbook. |

### Sheet methods (`SheetClass`)

**Cell access**

| Method | Description |
|---|---|
| `sheet:Cell(row, col)` → `Cell` | Builds a `Cell` wrapper — just `{sheet, row, col}` metadata, no read from the file. Cheap; call this whenever you just want to set/get one value. 1-based row/col. |
| `sheet:CellEx(row, col)` → `Cell` (rich) | Same idea, but eagerly reads the cell's full formatting/content via `XLSX.GetCellEx` — see the field list below. Has `__tostring`/`__concat` metamethods that resolve to `.value`, so `print(cell)` and `"x = " .. cell` both just work. |
| `sheet:GetCell(row, col)` / `sheet:SetCell(row, col, value)` | **Deprecated** macro-compat shortcuts, equivalent to `Cell:GetS()` / `Cell:SetS()`. |

**Search** — each returns a `Cell`/`nil`, or (`FindAll*`) an array of `Cell`s:

`sheet:Find(value)`, `sheet:FindAll(value)`, `sheet:FindInRange(value, range)`, `sheet:FindAllInRange(value, range)`, `sheet:FindInRow(value, row [, startCol])`, `sheet:FindAllInRow(value, row [, startCol])`, `sheet:FindInColumn(value, col [, startRow])`, `sheet:FindAllInColumn(value, col [, startRow])`.

**Columns**: `sheet:SetColumnWidth(col, width)`, `sheet:AutoFitColumn(col)`, `sheet:DeleteColumn(col)`, `sheet:InsertColumnBefore(col)`, `sheet:SetColumnBackgroundColor(col, color)`, `sheet:SetColumnBold(col, bool)`, `sheet:SetColumnFontColor(col, color)`, `sheet:GetColumnWidth(col)`.

**Rows**: `sheet:SetRowHeight(row, height)`, `sheet:DeleteRow(row)`, `sheet:InsertRowAbove(row)`, `sheet:SetRowBackgroundColor(row, color)`, `sheet:SetRowBold(row, bool)`, `sheet:SetRowFontColor(row, color)`, `sheet:GetRowHeight(row)`.

`color` on any `Set*Color` method above accepts either a hex/theme color string, or a table `{red=, green=, blue=}` (lowercase field names — different from the element wrapper's `RGBA()`, which uses `R`/`G`/`B`/`A`).

**Ranges**: `sheet:SetRangeBackgroundColor(range, color)`, `sheet:SetRangeBold(range, bool)`, `sheet:SetRangeBorder(range, style)`, `sheet:MergeRange(range)`. `range` is an `ERange`-shaped table — see below.

**Freeze panes**: `sheet:FreezePanes(row, col)` (freezes rows above *and* columns left of the given cell), `sheet:FreezeRows(count)`, `sheet:FreezeColumns(count)`, `sheet:UnfreezePanes()`.

**Other**: `sheet:GetSheetRange()` → rows, cols. `sheet:Recalculate()` (this sheet only).

### Cell methods (`CellClass`)

All setters return `self`, so calls chain:

```lua
sheet:Cell(1, 1):Set("Total"):SetBold(true):SetFontColor("#FF0000");
```

| Method | Description |
|---|---|
| `cell:Set(value)` / `cell:Get()` | Typed value read/write. |
| `cell:SetS(value)` / `cell:GetS()` | String value read/write (macro-compat). |
| `cell:GetAddress()` | Returns e.g. `"C5"`. |
| `cell:SetBackgroundColor(color)` / `SetBackgroundColorTheme(theme, tint)` / `SetBackgroundColorIndexed(index)` | `color` is a hex string or `{red=,green=,blue=}` table. |
| `cell:SetFontColor(color)` / `SetFontColorTheme(theme, tint)` | Same `color` shapes. |
| `cell:SetBold(bool)` / `SetItalic(bool)` / `SetUnderline(bool)` / `SetStrikethrough(bool)` | All default to `true` if called with no argument. |
| `cell:SetFontSize(size)` / `SetFontName(name)` | |
| `cell:SetAlignment(hAlign, vAlign)` / `SetWrapText(bool)` | |
| `cell:SetBorder(style)` / `SetBorderOutside(style)` / `SetBorderTop(style)` / `SetBorderBottom(style)` / `SetBorderLeft(style)` / `SetBorderRight(style)` | |
| `cell:SetNumberFormat(format)` | Excel number-format string, e.g. `"#,##0.00"`. |
| `cell:SetFormula(formula)` / `GetFormula()` / `HasFormula()` | |
| `cell:Calculate()` | Recalculates just this one cell. |
| `cell:GetBackgroundColor()` / `GetFontColor()` / `GetBold()` / `GetItalic()` / `GetUnderline()` / `GetStrikethrough()` / `GetFontSize()` / `GetFontName()` / `GetHorizontalAlignment()` / `GetVerticalAlignment()` / `GetWrapText()` / `GetBorderTop()` / `GetBorderBottom()` / `GetBorderLeft()` / `GetBorderRight()` / `GetNumberFormat()` | Getters mirroring the setters above. |
| `cell:IsMerged()` / `GetMergedRange()` | |
| `cell:GetEx()` | Re-fetches this same cell as the rich `CellEx` form (see below). |

### `CellEx` fields

`sheet:CellEx(row, col)` returns a `Cell` pre-populated with everything `XLSX.GetCellEx` reads, plus `sheet`/`row`/`col`/`hasFormula`/`hasComment`:

- **Content & position**: `value` (string), `dataType` (`"Text"`/`"Number"`/`"DateTime"`/`"Boolean"`/...), `address`, `row`, `col`, `sheetID`.
- **Formatting & style**: `format` (number-format string, or `"General"`), `isBold`, `isItalic`, `isUnderline`, `isStrikethrough`, `fontName`, `fontSize`.
- **Alignment & flow**: `hAlign` (`"Left"`/`"Center"`/`"Right"`/`"Justify"`/`"General"`), `vAlign` (`"Top"`/`"Center"`/`"Bottom"`/`"Justify"`), `wrapText`, `indent`.
- **Merging & visibility**: `isMerged`, `isHiddenRow`, `isHiddenColumn`, `locked`.
- **Borders**: `borderTop`/`borderBottom`/`borderLeft`/`borderRight` (`"None"`/`"Thin"`/`"Medium"`/`"Dashed"`/`"Dotted"`/...).
- **Colors** (only present if explicitly set): `bgColor`, `fontColor` — hex strings like `"#FFFFFF"`.
- **Rich text**: `richText` — array of `{text, fontName, fontSize, isBold, isItalic, isStrikethrough, isUnderline, fontColor, script}` segments (`script` is `"None"`/`"Superscript"`/`"Subscript"`). A non-rich-text cell usually has one entry matching the whole value.
- **Comments**: `comment` — text of the cell comment, or `nil`.

`value` and `richText[1].text` are usually identical unless the cell has inline formatting mid-string.

### Range helpers

`Excel.Range(...)` / global alias `ERange(...)` → `{row1, col1, row2, col2}`, accepting any of:

- `"A1:C3"` — a range string
- `"A1", "C5"` — two cell-address strings
- `cell1, cell2` — two `Cell` objects
- `row1, col1, row2, col2` — four numbers

Always normalized to top-left/bottom-right regardless of input order (`Excel.NormalizeRange`).

| Function | Description |
|---|---|
| `Excel.AddressToRowCol(addr)` → row, col | `"C5"` → `5, 3`. |
| `Excel.RowColToAddress(row, col)` → addr | `5, 3` → `"C5"`. |
| `Excel.ColumnIndexToLetter(col)` / `Excel.LetterToColumnIndex(letters)` | `28` ↔ `"AB"`. |
| `Excel.RangeToString(row1, row2, col1, col2)` | → `"A1:C3"`. |
| `Excel.ExpandRange("A1:B2")` | → `{"A1", "A2", "B1", "B2"}` — every address in the range. |
| `Excel.IsCellAddress(str)` / `Excel.StringIsRange(str)` | Pattern-match checks (`^%u+%d+$` / `^%u+%d+:%u+%d+$`). |
| `Excel.FormatToType(numberFormatStr)` | Heuristic guess — `"string"`/`"number"`/`"date"`/`"time"` — from a number-format string. |

`Excel.MaxRow` (`1048576`) and `Excel.MaxCol` (`16384`) are Excel's hard limits — every row/col argument throughout the wrapper is bounds-checked against them.

---

## XLSX (raw bridge)

A large `.xlsx` spreadsheet read/write API (workbook, sheet, row/column, range, and cell-level operations — formatting, formulas, borders, merges, search, and recalculation) that the `Excel`/`Workbook`/`Sheet`/`Cell` wrapper above is built on. Function names are self-describing and grouped by scope:

- **Workbook**: `Load`, `Save`, `SaveAs`, `New`, `WorkbookExists`, `CloseWorkbook`, `CloseAllWorkbooks`
- **Sheets**: `AddSheet`, `DeleteSheet`, `RenameSheet`, `GetSheetNames`, `ContainSheet`, `GetSheet`, `GetSheetRange`
- **Rows/Columns**: `SetRowBackgroundColor(RGB)`, `SetRowBold`, `SetRowFontColor(RGB)`, equivalent `SetColumn*` variants, `SetColumnWidth`, `AutoFitColumn`, `SetRowHeight`, `DeleteRow`/`DeleteColumn`, `InsertRowAbove`/`InsertColumnBefore`, `GetColumnWidth`/`GetRowHeight`
- **Ranges**: `SetRangeBackgroundColor(RGB)`, `SetRangeBold`, `SetRangeBorder`, `MergeRange`
- **Cells**: `SetCell`/`GetCell`/`GetCellEx`, `SetCellValue`/`GetCellValue`, background/font color (`...Color`, `...ColorRGB`, `...ColorTheme`/`Indexed` variants), `SetCellBold`/`Italic`/`Underline`/`Strikethrough`/`FontSize`/`FontName`, `SetCellAlignment`/`WrapText`, `SetCellBorder*`, `SetCellNumberFormat`, `SetCellFormula`/`GetCellFormula`/`HasCellFormula`, `ClearCell`/`ClearCellContents`/`ClearCellFormats`, matching `Get*` readers, `IsCellMerged`/`GetCellMergedRange`
- **Search**: `FindInSheet`/`FindAllInSheet`, `FindInRow`/`FindAllInRow`, `FindInColumn`/`FindAllInColumn`, `FindInRange`/`FindAllInRange`
- **Freeze panes**: `FreezePanes`, `FreezeRows`, `FreezeColumns`, `UnfreezePanes`
- **Recalculation**: `CalculateCell`, `RecalculateSheet`, `Recalculate` (whole workbook)

`XLSX.Ver` is a constant string (currently `"1.3"`). Exact per-function argument lists are documented as comments in `src/Shergen.XLSX/XLSXModul.cs` / `ExcelManager.cs`.
