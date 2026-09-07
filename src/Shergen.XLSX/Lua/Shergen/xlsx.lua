----------------------------------------------
--  Name:      Excel modul
--	Version:   1.3
--  Author:    Sali
--  Category:  Utils
----------------------------------------------

-----------------  Excel class definition  -----------------

--- Excel Range Class
---@class ExcelRange
---@field row1 number
---@field col1 number
---@field row2 number
---@field col2 number

---@class Excel
Excel = {};

---- Excel Cell Class
---@class Cell
---@field row number
---@field col number
---@field sheet Sheet
CellClass = {};

---- Excel Sheet Class
---@class Sheet
---@field ID string
---@field name string
---@field workbook Workbook
SheetClass = {};

---- Excel Workbook Class
---@class Workbook
---@field ID string
---@field path string
WorkbookClass = {};


----------------- EXCEL UTILS ----------------

Excel.MaxCol = 16384;
Excel.MaxRow = 1048576;
local maxCol = Excel.MaxCol;
local maxRow = Excel.MaxRow;

-- Address e.g. "C5" to row, col
---@return number row
---@return number col
function Excel.AddressToRowCol(address)
	local col, row = address:match("([A-Z]+)(%d+)");
	if not col or not row then
		error(Loc(TID_XLSX_ERROR_INVALID_CELL_ADDRESS, {address = address}));
	end;

	local function parseColumn(colStr)
		local result = 0;
		for i = 1, #colStr do
			result = result * 26 + (colStr:byte(i) - string.byte('A') + 1);
		end;
		return result;
	end;


	return tonumber(row), parseColumn(col);   -- return row, col (2 values)
end;

-- From row, col to e.g. "C5"
---@return string A1
function Excel.RowColToAddress(row, col)
	local colStr = "";
	while col > 0 do
		local rem = (col - 1) % 26;
		colStr = string.char(string.byte('A') + rem) .. colStr;
		col = math.floor((col - 1) / 26);
	end;
	return colStr .. tostring(row);			-- return string;
end;

-- Parses a range e.g. "A1:C3" into row1, row2, col1, col2
---@return number row1
---@return number row2
---@return number col1
---@return number col2
function Excel.ParseStringRange(range)
 
	local function parseColumn(col)
		local result = 0;
		for i = 1, #col do
			result = result * 26 + (col:byte(i) - string.byte('A') + 1);
		end;
		return result;
	end;
	
	local col1, row1, col2, row2 = range:match("([A-Z]+)(%d+):([A-Z]+)(%d+)");
    if col1 and row1 and col2 and row2 then
        return tonumber(row1), tonumber(row2) , parseColumn(col1), parseColumn(col2) ;
        
    else
        error(TID_XLSX_ERROR_INVALIT_RANGE);
    end;
end;

-- Convert column index (1-based) to letters, e.g. 1 -> "A", 28 -> "AB"
---@return string ColID
function Excel.ColumnIndexToLetter(col)
	local colStr = "";
	while col > 0 do
		local rem = (col - 1) % 26;
		colStr = string.char(string.byte('A') + rem) .. colStr;
		col = math.floor((col - 1) / 26);
	end;
	return colStr;
end;

-- Convert letters to column index, e.g. "A" -> 1, "AB" -> 28
---@return number ColNumber
function Excel.LetterToColumnIndex(col)
	local result = 0;
	for i = 1, #col do
		result = result * 26 + (col:byte(i) - string.byte('A') + 1);
	end;
	return result;
end;

-- Returns true if it is a valid cell address (e.g. "B3")
function Excel.IsCellAddress(str)
	return str:match("^%u+%d+$") ~= nil;
end;

-- Returns true if it is a range of cells (e.g. "A1:C3")
function Excel.StringIsRange(str)
	return str:match("^%u+%d+:%u+%d+$") ~= nil;
end;

-- Excel.ExpandRange("A1:B2") → {"A1", "A2", "B1", "B2"}
-- Generates all addresses within the range.
function Excel.ExpandRange(range)
	local function colToStr(col)
		local s = "";
		while col > 0 do
			local r = (col - 1) % 26;
			s = string.char(65 + r) .. s;
			col = math.floor((col - 1) / 26);
		end;
		return s;
	end;

	local row1, row2, col1, col2 = Excel.ParseStringRange(range);
	local cells = {};

	for row = row1, row2 do
		for col = col1, col2 do
			table.insert(cells, colToStr(col) .. row);
		end;
	end;

	return cells;
end;

-- Returns a sorted range (top left and bottom right corners)
function Excel.NormalizeRange(row1, row2, col1, col2)
	local r1, r2 = math.min(row1, row2), math.max(row1, row2);
	local c1, c2 = math.min(col1, col2), math.max(col1, col2);
	return r1, r2, c1, c2;
end;

-- Creates a heuristic type guess based on the format (optional mapping, just for detecting)
function Excel.FormatToType(formatStr)
	if not formatStr or formatStr == "" or formatStr == "General" then
		return "string";
	elseif formatStr:find("0%.0") or formatStr:find("#") then
		return "number";
	elseif formatStr:find("d") or formatStr:find("y") then
		return "date";
	elseif formatStr:find("h") then
		return "time";
	end;
	return "string";
end;
-- row1, row2, col1, col2 → "A1:C3"
function Excel.RangeToString(row1, row2, col1, col2)
	return Excel.RowColToAddress(row1, col1) .. ":" .. Excel.RowColToAddress(row2, col2);
end;

---Creates ERange from different inputs:  
--- - "A1:C5" 
--- - "A1", "C5"  
--- - Cell, Cell  
--- - 1, 1, 5, 3  
---@overload fun(range:string): ERange
---@overload fun(addr1:string, addr2:string): ERange 
---@overload fun(cell1:Cell, cell2:Cell): ERange  
---@overload fun(row1:integer, col1:integer, row2:integer, col2:integer): ERange 
function Excel.Range(...)
    local argc = select("#", ...);
    local a1, a2, a3, a4 = ...;

    local row1, col1, row2, col2;

    -- 1) Jeden string: "A1:C3"
    if argc == 1 and type(a1) == "string" then
        if Excel.StringIsRange(a1) then
            row1, row2, col1, col2 = Excel.ParseStringRange(a1);
        elseif Excel.IsCellAddress(a1) then
            row1, col1 = Excel.AddressToRowCol(a1);
            row2, col2 = row1, col1;
        else
            error(Loc(TID_XLSX_ERROR_INVALID_RANGE_STRING, {range = a1}));
        end;

    -- 2) Dva argumenty: Cell + Cell | "A1" + "C5"
    elseif argc == 2 then
        -- Cell + Cell
        if type(a1) == "table" and type(a2) == "table" and a1.row and a1.col and a2.row and a2.col then
            row1, col1 = a1.row, a1.col;
            row2, col2 = a2.row, a2.col;

        -- "A1", "C5"
        elseif type(a1) == "string" and type(a2) == "string"
            and Excel.IsCellAddress(a1) and Excel.IsCellAddress(a2) then

            row1, col1 = Excel.AddressToRowCol(a1);
            row2, col2 = Excel.AddressToRowCol(a2);

        else
            error(Loc(TID_XLSX_ERROR_INVALID_RANGE_STRING, {range = a1 .. ":" .. a2}));
        end;

    -- 3) Čtyři čísla: row1, col1, row2, col2
    elseif argc == 4
        and type(a1) == "number" and type(a2) == "number"
        and type(a3) == "number" and type(a4) == "number" then

        row1, col1, row2, col2 = a1, a2, a3, a4;

    else
        error(TID_XLSX_ERROR_INVALID_ERANGE);
    end;

    -- Normalizace (levý horní → pravý dolní roh)
    row1, row2, col1, col2 = Excel.NormalizeRange(row1, row2, col1, col2);

    return { row1 = row1, col1 = col1, row2 = row2, col2 = col2 };
end;




function CellClass:GetAddress() 
	return Excel.RowColToAddress(self.row, self.col);
end;

-- ===== HODNOTY =====

---@param value string|number|boolean|nil 
---@return self Cell
function CellClass:Set(value)
    XLSX.SetCellValue(self.sheet.ID, self.row, self.col, value);
    return self -- For chaining
end;

function CellClass:Get()
    return XLSX.GetCellValue(self.sheet.ID, self.row, self.col);
end;

-- String version (Macro compatibility)
---@param value string|number|nil everything will be change to string
---@return self Cell
function CellClass:SetS(value)
    XLSX.SetCell(self.sheet.ID, self.row, self.col, value);
    return self;
end;
---@return string
function CellClass:GetS()
    return XLSX.GetCell(self.sheet.ID, self.row, self.col);
end;

-- ===== BACKGROUND COLOR =====
---@param color string|RGBA 
---@return Cell
function CellClass:SetBackgroundColor(color)
    if type(color) == "string" then
        XLSX.SetCellBackgroundColor(self.sheet.ID, self.row, self.col, color)
    elseif type(color) == "table" then
        XLSX.SetCellBackgroundColorRGB(self.sheet.ID, self.row, self.col, color.red, color.green, color.blue);
    else
        error(Loc(TID_XLSX_ERROR_INVALID_COLOR))
    end;
    return self;
end;


function CellClass:SetBackgroundColorTheme(theme, tint)
    XLSX.SetCellBackgroundColorTheme(self.sheet.ID, self.row, self.col, theme, tint);
    return self;
end;

function CellClass:SetBackgroundColorIndexed(index)
    XLSX.SetCellBackgroundColorIndexed(self.sheet.ID, self.row, self.col, index);
    return self;
end;

-- ===== FONT COLOR =====

---@param color string|RGBA
---@return Cell
function CellClass:SetFontColor(color)
    if type(color) == "string" then
        XLSX.SetCellFontColor(self.sheet.ID, self.row, self.col, color);
    elseif type(color) == "table" then
        XLSX.SetCellFontColorRGB(self.sheet.ID, self.row, self.col, color.red, color.green, color.blue);
    else    
        error(Loc(TID_XLSX_ERROR_INVALID_COLOR));
    end;
    return self;
end;

function CellClass:SetFontColorTheme(theme, tint)
    XLSX.SetCellFontColorTheme(self.sheet.ID, self.row, self.col, theme, tint);
    return self;
end;

-- ===== FONT FORMATTING =====
function CellClass:SetBold(bold)
    if bold == nil then bold = true end;
    XLSX.SetCellBold(self.sheet.ID, self.row, self.col, bold);
    return self;
end;

function CellClass:SetItalic(italic)
    if italic == nil then italic = true end;
    XLSX.SetCellItalic(self.sheet.ID, self.row, self.col, italic);
    return self;
end;

function CellClass:SetUnderline(underline)
    if underline == nil then underline = true end;
    XLSX.SetCellUnderline(self.sheet.ID, self.row, self.col, underline);
    return self;
end;

function CellClass:SetStrikethrough(strikethrough)
    if strikethrough == nil then strikethrough = true end;
    XLSX.SetCellStrikethrough(self.sheet.ID, self.row, self.col, strikethrough);
    return self;
end;

function CellClass:SetFontSize(size)
    XLSX.SetCellFontSize(self.sheet.ID, self.row, self.col, size);
    return self;
end;

function CellClass:SetFontName(name)
    XLSX.SetCellFontName(self.sheet.ID, self.row, self.col, name);
    return self;
end;

-- ===== ALIGNMENT =====
function CellClass:SetAlignment(hAlign, vAlign)
    XLSX.SetCellAlignment(self.sheet.ID, self.row, self.col, hAlign, vAlign);
    return self;
end;

function CellClass:SetWrapText(wrap)
    if wrap == nil then wrap = true end;
    XLSX.SetCellWrapText(self.sheet.ID, self.row, self.col, wrap);
    return self;
end;

-- ===== BORDERS =====
function CellClass:SetBorder(style)
    XLSX.SetCellBorder(self.sheet.ID, self.row, self.col, style)
    return self
end;


function CellClass:SetBorderOutside(style)
    XLSX.SetCellBorderOutside(self.sheet.ID, self.row, self.col, style)
    return self
end;

function CellClass:SetBorderTop(style)
    XLSX.SetCellBorderTop(self.sheet.ID, self.row, self.col, style)
    return self
end;

function CellClass:SetBorderBottom(style)
    XLSX.SetCellBorderBottom(self.sheet.ID, self.row, self.col, style)
    return self
end;

function CellClass:SetBorderLeft(style)
    XLSX.SetCellBorderLeft(self.sheet.ID, self.row, self.col, style)
    return self
end;

function CellClass:SetBorderRight(style)
    XLSX.SetCellBorderRight(self.sheet.ID, self.row, self.col, style)
    return self
end;

-- ===== NUMBER FORMAT =====
function CellClass:SetNumberFormat(format)
    XLSX.SetCellNumberFormat(self.sheet.ID, self.row, self.col, format)
    return self
end;

-- ===== FORMULA =====
function CellClass:SetFormula(formula)
    XLSX.SetCellFormula(self.sheet.ID, self.row, self.col, formula)
    return self
end;

function CellClass:GetFormula()
    return XLSX.GetCellFormula(self.sheet.ID, self.row, self.col)
end;

function CellClass:HasFormula()
    return XLSX.HasCellFormula(self.sheet.ID, self.row, self.col)
end;

-- ===== GET COLORS =====
function CellClass:GetBackgroundColor()
    return XLSX.GetCellBackgroundColor(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetFontColor()
    return XLSX.GetCellFontColor(self.sheet.ID, self.row, self.col)
end;

-- ===== GET FONT =====
function CellClass:GetBold()
    return XLSX.GetCellBold(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetItalic()
    return XLSX.GetCellItalic(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetUnderline()
    return XLSX.GetCellUnderline(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetStrikethrough()
    return XLSX.GetCellStrikethrough(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetFontSize()
    return XLSX.GetCellFontSize(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetFontName()
    return XLSX.GetCellFontName(self.sheet.ID, self.row, self.col)
end;

-- ===== GET ALIGNMENT =====
function CellClass:GetHorizontalAlignment()
    return XLSX.GetCellHorizontalAlignment(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetVerticalAlignment()
    return XLSX.GetCellVerticalAlignment(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetWrapText()
    return XLSX.GetCellWrapText(self.sheet.ID, self.row, self.col)
end;

-- ===== GET BORDERS =====
function CellClass:GetBorderTop()
    return XLSX.GetCellBorderTop(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetBorderBottom()
    return XLSX.GetCellBorderBottom(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetBorderLeft()
    return XLSX.GetCellBorderLeft(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetBorderRight()
    return XLSX.GetCellBorderRight(self.sheet.ID, self.row, self.col)
end;

-- ===== GET NUMBER FORMAT =====
function CellClass:GetNumberFormat()
    return XLSX.GetCellNumberFormat(self.sheet.ID, self.row, self.col)
end;

-- ===== GET MERGED =====
function CellClass:IsMerged()
    return XLSX.IsCellMerged(self.sheet.ID, self.row, self.col)
end;

function CellClass:GetMergedRange()
    return XLSX.GetCellMergedRange(self.sheet.ID, self.row, self.col)
end;

-- ===== ADVANCED =====
function CellClass:GetEx()
    -- Volá CellEx která vrací detailní info
    return self.sheet:CellEx(self.row, self.col)
end;


-- Recalculate formula in this cell
function CellClass:Calculate()
    XLSX.CalculateCell(self.sheet.ID, self.row, self.col)
    return self  -- Pro chaining
end;




-- New simple object Cell - Created by Lua wrapper
---@param row number
---@param col number
---@return Cell
function SheetClass:Cell(row, col)
    if not row or type(row) ~= "number" then error(Loc(TID_XLSX_ERROR_CELL_ROW)); end;
    if row < 1 or row > maxRow then error(Loc(TID_XLSX_ERROR_CELL_ROW)); end;
    if not col or type(col) ~= "number" then error(Loc(TID_XLSX_ERROR_CELL_COL)); end;
    if col < 1 or col > maxCol then error(Loc(TID_XLSX_ERROR_CELL_COL)); end;

    -- Jen jednoduchý Lua table s metadaty
    local cell = {
        sheet = self,  -- reference na sheet
        row = row,
        col = col
    };
    
    setmetatable(cell, { __index = CellClass });
    
    return cell;
end;

--[[
Structure returned by CellEx(sheet, row, col)

Each Excel cell is converted to a Lua table containing its content, formatting, and metadata.
Below is a list of all possible keys and their meanings:

CONTENT AND POSITION:
  value           – Cell content as a string. Empty if the cell is empty.
  dataType        – Type of value in the cell, such as "Text", "Number", "DateTime", "Boolean", etc.
  address         – Cell address as a string (e.g., "B2").
  row             – 1-based row index.
  col             – 1-based column index.
  sheetID         – Sheet identifier in the format "fileXLSX:sheetname" (manually added, not native to Excel, XLSX must be already opened).

FORMATTING AND STYLE:
  format          – Number format string (e.g., "#,##0.00", "dd.mm.yyyy", or "General" for General).
  isBold          – `true` if bold style is applied, `false` otherwise.
  isItalic        – `true` if italic style is applied.
  isUnderline     – `true` if the text is underlined.
  isStrikethrough – `true` if strikethrough is applied.
  fontName        – Font name (e.g., "Arial", "Calibri", "Arial CE").
  fontSize        – Font size as a number (float/int).

ALIGNMENT AND TEXT FLOW:
  hAlign          – Horizontal alignment: "Left", "Center", "Right", "Justify", or "General".
  vAlign          – Vertical alignment: "Top", "Center", "Bottom", or "Justify".
  wrapText        – `true` if text wrapping is enabled.
  indent          – number indent level (usually 0 if not set manually).

MERGING AND VISIBILITY:
  isMerged        – `true` if the cell is part of a merged range.
  isHiddenRow     – `true` if the row is hidden.
  isHiddenColumn  – `true` if the column is hidden.
  locked          – `true` if the cell is locked for editing (within a protected sheet).

BORDERS:
  borderTop       – Top border style: "None", "Thin", "Medium", "Dashed", "Dotted", etc.
  borderBottom    – Bottom border style (same as above).
  borderLeft      – Left border style.
  borderRight     – Right border style.

COLORS (optional):
  bgColor         – Background color as a hex string (e.g., "#FFFFFF"). Only present if explicitly set.
  fontColor       – Font color as a hex string (e.g., "#000000"). Only present if explicitly set.

RICH TEXT:
  richText        – Array of individually formatted segments of text:
                    {
                      text = "word",
                      fontName = "Arial",
                      fontSize = 10.0,
                      isBold = false,
                      isItalic = false,
                      isStrikethrough = false,
                      isUnderline = false,
                      fontColor = "#000000"
                      script = "None"		-  Indicates whether the text run is normal, a superscript (e.g., x²), or subscript (e.g., H₂O).
						- "None", "Superscript", "Subscript"
                    }
                  If the cell is not rich text, it usually contains one entry matching the whole value.

COMMENTS:
  comment         – Text of the comment if the cell has one; otherwise `nil`.

Notes:
- `value` and `richText[1].text` are often identical if no inline formatting is applied.
- Alignment and border values are stringified enum values from ClosedXML.
- `format` affects how the value is displayed in Excel, but not the raw content itself.

--]]
function SheetClass:CellEx(row, col)
	if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
	if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;

	local cell = XLSX.GetCellEx(self.ID, row, col);
	
	cell.sheet = self;  -- !!! Please NEVER change this value manually. Used internally only.
	cell.row = row;          -- !!! Please NEVER change this value manually. Used internally only.
	cell.col = col;          -- !!! Please NEVER change this value manually. Used internally only.
	cell.hasFormula = cell.formula ~= nil;
	cell.hasComment = cell.comment ~= nil;
	
	local function result(self)
		return self.value;
	end;

	setmetatable(cell, { 
		__tostring = result,
		__concat = result,
		__index = CellClass,

	});

	return cell;
end



--- Backward macro compatibility, same as Cell:GetS.
---@return string
---@deprecated
function SheetClass:GetCell(row, col)		--- Return just value
	if not row or not (type(row) == "number") then error (TID_XLSX_ERROR_CELL_ROW); end;
	if row < 1 or row > maxRow then error (TID_XLSX_ERROR_CELL_ROW); end;
	if not col or not (type(col) == "number") then error (TID_XLSX_ERROR_CELL_COL); end;
	if col < 1 or col > maxCol then error (TID_XLSX_ERROR_CELL_COL); end;
	return XLSX.GetCell(self.ID, row, col); -- Call original C# function
end;

--- Backward macro compatibility, same as Cell:SetS.
---@return self
---@deprecated
function SheetClass:SetCell(row, col, value)	--- Set just value
	if not row or not (type(row) == "number") then error (TID_XLSX_ERROR_CELL_ROW); end;
	if row < 1 or row > maxRow then error (TID_XLSX_ERROR_CELL_ROW); end;
	if not col or not (type(col) == "number") then error (TID_XLSX_ERROR_CELL_COL); end;
	if col < 1 or col > maxCol then error (TID_XLSX_ERROR_CELL_COL); end;
	XLSX.SetCell(self.ID, row, col, value);
    return self;
end;

-- Return two values -  rows, columns
---@return number rows
---@return number cols
function SheetClass:GetSheetRange()		
	return XLSX.GetSheetRange(self.ID);
end;

-- Return Cell - of first one  or nil when not found
---@return Cell|nil
function SheetClass:Find(searchValue)  
    local row, col = XLSX.FindInSheet(self.ID, searchValue);
	if row and col then
        return self:Cell(row, col);
    else
        return nil;
    end;
end;

-- Return Table of all cells of same values {[1] = CellObj ...};
---@return Cell[]|nil
function SheetClass:FindAll(searchValue)  

	local FR = XLSX.FindAllInSheet(self.ID, searchValue);

    if not FR then
        return {};
    else
        local FRT = {};
        for k, v in ipairs(FR) do
            table.insert(FRT, self:Cell(v.row,v.col));
        end;
        return FRT;
    end;
end;



-- startRow can by table or string range, in that case, everyelse is nil
---@return Cell|nil
function SheetClass:FindInRange(searchValue, range ) 

    if not (type(range.row1) == "number") or range.row1 < 1 or range.row1 > maxRow or not (type(range.row2) == "number") or range.row2 < 1 or range.row2 > maxRow then error (TID_XLSX_ERROR_CELL_ROW); end;
    if not (type(range.col1) == "number") or range.col1 < 1 or range.col1 > maxCol or not (type(range.col2) == "number") or range.col2 < 1 or range.col2 > maxCol then error (TID_XLSX_ERROR_CELL_COL); end;
    
    local row, col = XLSX.FindInRange(self.ID, searchValue, range.row1, range.row2, range.col1, range.col2 );
    if row and col then
        return self:Cell(row, col);
    else
        return nil;
    end;
		
		

end;

-- startRow can by table or string range, in that case, everyelse is nil  
-- Return Table of all cells of same values {[1] = CellObj ...};
---@return Cell[]|nil
function SheetClass:FindAllInRange(searchValue, range ) 

    if not (type(range.row1) == "number") or range.row1 < 1 or range.row1 > maxRow or not (type(range.row2) == "number") or range.row2 < 1 or range.row2 > maxRow then error (TID_XLSX_ERROR_CELL_ROW); end;
    if not (type(range.col1) == "number") or range.col1 < 1 or range.col1 > maxCol or not (type(range.col2) == "number") or range.col2 < 1 or range.col2 > maxCol then error (TID_XLSX_ERROR_CELL_COL); end;
 
    local FR = XLSX.FindAllInRange(self.ID, searchValue, range.row1, range.row2, range.col1, range.col2);
        if not FR then
            return {};
        else
            local FRT = {};
            for k, v in ipairs(FR) do
                table.insert(FRT, self:Cell(v.row,v.col));
            end;
            return FRT;
        end;

end;

---@return Cell|nil
function SheetClass:FindInColumn(searchValue, col, starRow)		
	if not (type(col) == "number") or col < 1 or col > maxRow then error (TID_XLSX_ERROR_CELL_COL); end;
	local row = XLSX.FindInColumn(self.ID, searchValue, col, starRow);
    if not row then
        
        return nil;
    else
        return self:Cell(row,col);
    end;
end;

---@return Cell|nil
function SheetClass:FindInRow(searchValue, row, startCol)	-- return number 
	if not (type(row) == "number") or row < 1 or row > maxRow then error (TID_XLSX_ERROR_CELL_ROW); end;
    local col = XLSX.FindInRow(self.ID, searchValue, row, startCol);
    if not col then
        return nil;
    else
        return self:Cell(row,col);
    end;
end;

---@return Cell[]
function SheetClass:FindAllInColumn(searchValue, col, starRow)
	if not (type(col) == "number") or col < 1 or col > maxRow then error (TID_XLSX_ERROR_CELL_COL); end;

    local FR = XLSX.FindAllInColumn(self.ID,  searchValue, col, starRow);	
    if not FR then
        return {};
    else
        local FRT = {};
        for k, v in ipairs(FR) do
            table.insert(FRT, self:Cell(v,col));
        end;
        return FRT;
    end;

end;

---@return Cell[]|nil
function SheetClass:FindAllInRow(searchValue, row, startCol)
	if not (type(row) == "number") or row < 1 or row > maxRow then error (TID_XLSX_ERROR_CELL_ROW); end;

    local FR = XLSX.FindAllInRow(self.ID, searchValue, row, startCol);
    if not FR then
        return {};
    else
        local FRT = {};
        for k, v in ipairs(FR) do
            table.insert(FRT, self:Cell(row,v));
        end;
        return FRT;
    end;
end;

-- ===== COLUMN OPERATIONS =====

---@return self
function SheetClass:SetColumnWidth(col, width)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.SetColumnWidth(self.ID, col, width);
    return self;
end;

function SheetClass:AutoFitColumn(col)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.AutoFitColumn(self.ID, col);
    return self;
end;

function SheetClass:DeleteColumn(col)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.DeleteColumn(self.ID, col);
    return self;
end;

function SheetClass:InsertColumnBefore(col)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.InsertColumnBefore(self.ID, col);
    return self;
end;

-- ===== COLUMN FORMATTING =====

---@param col number
---@param color string|RGBA
---@return Sheet
function SheetClass:SetColumnBackgroundColor(col, color)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;

    if type(color) == "string" then
        XLSX.SetColumnBackgroundColor(self.ID, col, color);
    elseif type(color) == "table" then
        XLSX.SetColumnBackgroundColorRGB(self.ID, col, color.red, color.green, color.blue);
    else
        error(TID_ERROR_COLOR_FORMAT);
    end;
    return self;
end;


function SheetClass:SetColumnBold(col, bold)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.SetColumnBold(self.ID, col, bold);
    return self;
end;

---@param col number
---@param color string|RGBA
---@return Sheet
function SheetClass:SetColumnFontColor(col, color)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;

    if type(color) == "string" then
        XLSX.SetColumnFontColor(self.ID, col, color);
    elseif type(color) == "table" then
        XLSX.SetColumnFontColorRGB(self.ID, col, color.red, color.green, color.blue);
    else
        error(TID_ERROR_COLOR_FORMAT);
    end;
    return self;
end;

-- ===== ROW OPERATIONS =====

function SheetClass:SetRowHeight(row, height)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    XLSX.SetRowHeight(self.ID, row, height);
    return self;
end;

function SheetClass:DeleteRow(row)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    XLSX.DeleteRow(self.ID, row);
    return self;
end;

function SheetClass:InsertRowAbove(row)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    XLSX.InsertRowAbove(self.ID, row);
    return self;
end;

-- ===== ROW FORMATTING =====

---@param row number
---@param color string|RGBA
---@return Sheet
function SheetClass:SetRowBackgroundColor(row, color)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;

    if type(color) == "string" then
        XLSX.SetRowBackgroundColor(self.ID, row, color);
    elseif type(color) == "table" then
        XLSX.SetRowBackgroundColorRGB(self.ID, row, color.red, color.green, color.blue);
    else
        error(TID_ERROR_COLOR_FORMAT);
    end;
    return self;
end;


function SheetClass:SetRowBold(row, bold)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    XLSX.SetRowBold(self.ID, row, bold);
    return self;
end;

---
---@param row number
---@param color string|RGBA
---@return Sheet
function SheetClass:SetRowFontColor(row, color)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;


    if type(color) == "string" then
        XLSX.SetRowFontColor(self.ID, row, color);
    elseif type(color) == "table" then
        XLSX.SetRowFontColorRGB(self.ID, row, color.red, color.green, color.blue);
    else
        error(TID_ERROR_COLOR_FORMAT);
    end;
    return self;
end;

-- ===== RANGE OPERATIONS =====

---
---@param range ExcelRange
---@param color string|RGBA
---@return Sheet
function SheetClass:SetRangeBackgroundColor(range, color)
    if not range.row1 or type(range.row1) ~= "number" or range.row1 < 1 or range.row1 > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    if not range.row2 or type(range.row2) ~= "number" or range.row2 < 1 or range.row2 > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    if not range.col1 or type(range.col1) ~= "number" or range.col1 < 1 or range.col1 > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    if not range.col2 or type(range.col2) ~= "number" or range.col2 < 1 or range.col2 > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;

    if type(color) == "string" then
        XLSX.SetRangeBackgroundColor(self.ID, range.row1, range.col1, range.row2, range.col2, color);
    elseif type(color) == "table" then
        XLSX.SetRangeBackgroundColorRGB(self.ID, range.row1, range.col1, range.row2, range.col2, color.red, color.green, color.blue);
    else
        error(TID_ERROR_COLOR_FORMAT);
    end;

    return self;
end;


function SheetClass:SetRangeBold(range, bold)
    if not range.row1 or type(range.row1) ~= "number" or range.row1 < 1 or range.row1 > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    if not range.row2 or type(range.row2) ~= "number" or range.row2 < 1 or range.row2 > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    if not range.col1 or type(range.col1) ~= "number" or range.col1 < 1 or range.col1 > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    if not range.col2 or type(range.col2) ~= "number" or range.col2 < 1 or range.col2 > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.SetRangeBold(self.ID, range.row1, range.col1, range.row2, range.col2, bold);
    return self;
end;

function SheetClass:SetRangeBorder(range, style)
    if not range.row1 or type(range.row1) ~= "number" or range.row1 < 1 or range.row1 > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    if not range.row2 or type(range.row2) ~= "number" or range.row2 < 1 or range.row2 > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    if not range.col1 or type(range.col1) ~= "number" or range.col1 < 1 or range.col1 > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    if not range.col2 or type(range.col2) ~= "number" or range.col2 < 1 or range.col2 > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.SetRangeBorder(self.ID, range.row1, range.col1, range.row2, range.col2, style);
    return self;
end;

function SheetClass:MergeRange(range)
    if not (type(range.row1) == "number") or range.row1 < 1 or range.row1 > maxRow or not (type(range.row2) == "number") or range.row2 < 1 or range.row2 > maxRow then error (TID_XLSX_ERROR_CELL_ROW); end;
    if not (type(range.col1) == "number") or range.col1 < 1 or range.col1 > maxCol or not (type(range.col2) == "number") or range.col2 < 1 or range.col2 > maxCol then error (TID_XLSX_ERROR_CELL_COL); end;
    
    XLSX.MergeRange(self.ID, range.row1, range.col1, range.row2, range.col2);
    return self;
end;

-- ===== DIMENSIONS =====
function SheetClass:GetColumnWidth(col)
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    return XLSX.GetColumnWidth(self.ID, col);
end;

function SheetClass:GetRowHeight(row)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    return XLSX.GetRowHeight(self.ID, row);
end;

-- ===== FreezePanes =====

--- Ukotví řádky a sloupce (typicky pro hlavičku vlevo a nahoře)
function SheetClass:FreezePanes(row, col)
    if not row or type(row) ~= "number" or row < 1 or row > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    if not col or type(col) ~= "number" or col < 1 or col > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.FreezePanes(self.ID, row, col);
    return self;
end;

--- Ukotví jen horní řádky (např. hlavička)
function SheetClass:FreezeRows(count)
    if not count or type(count) ~= "number" or count < 0 or count > maxRow then error(TID_XLSX_ERROR_CELL_ROW); end;
    XLSX.FreezeRows(self.ID, count);
    return self;
end;

--- Ukotví jen levé sloupce
function SheetClass:FreezeColumns(count)
    if not count or type(count) ~= "number" or count < 0 or count > maxCol then error(TID_XLSX_ERROR_CELL_COL); end;
    XLSX.FreezeColumns(self.ID, count);
    return self;
end;

--- Zruší ukotvení
function SheetClass:UnfreezePanes()
    XLSX.UnfreezePanes(self.ID);
    return self;
end;

-- ===== LOAD SHEET =====

---@return Sheet object
function Sheet(wb, sheet)
    local obj = {ID = XLSX.GetSheet(wb.ID, sheet), name = sheet, workbook =  wb};

	setmetatable(obj, { __index = SheetClass })
	
    return obj;
end;



-- Nová jednoduchá metoda Cell() - vytvoří Lua wrapper
---@param sheet string
---@return Sheet|nil object
function WorkbookClass:Sheet(sheet)
	if not self.ID then
		error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
		return nil;
	end;
	local isSheet = XLSX.ContainSheet(self.ID, sheet);
	if not isSheet then
		error(Loc(TID_XLSX_ERROR_SHEET_DOESNT_EXIST, {sheet = sheet}));
		return nil;
	end;
	
	return Sheet(self, sheet); -- Vrátí Sheet objekt
end;

function WorkbookClass:Close()
	if self.ID then
		XLSX.CloseWorkbook(self.ID);
		self.ID = nil; -- Workbook je zavřený, ID už neexistuje
	else
		error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
	end;
end;

function WorkbookClass:ContainSheet(sheet)
	if not self.ID then
		error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
		return nil;
	end;
	return XLSX.ContainSheet(self.ID, sheet);
end;

function WorkbookClass:Save()
	if not self.ID then
		error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
		return;
	end;
	XLSX.Save(self.ID);
end;

function WorkbookClass:SaveAs(path)
    if not self.ID then
        error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
        return;
    end;
    
    XLSX.SaveAs(self.ID, path);
end;

function WorkbookClass:GetSheetNames()
	if not self.ID then
		error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
		return nil;
	end;
	return XLSX.GetSheetNames(self.ID);
end;

---
---@param sheetName string
---@return Sheet|nil
function WorkbookClass:AddSheet(sheetName)
    if not self.ID then
        error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
    end;

    local isCreated = XLSX.AddSheet(self.ID, sheetName);

    if isCreated then
        return Sheet(self, sheetName);
    else
        return nil;
    end;
end;

function WorkbookClass:DeleteSheet(sheetName)
    if not self.ID then
        error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
        return false;
    end;
    return XLSX.DeleteSheet(self.ID, sheetName);
end;

function WorkbookClass:RenameSheet(oldName, newName)
    if not self.ID then
        error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
        return false;
    end;
    return XLSX.RenameSheet(self.ID, oldName, newName);
end;


-- Recalculate all formulas in this sheet
function SheetClass:Recalculate()
    XLSX.RecalculateSheet(self.ID);
end;

--- Load exists workbook  
--- note, can't be only for read, so be caryful  
--- and no other process (of another program) may use it.
---@param path string
---@return Workbook object 
function Excel.Workbook(path) 
	local obj =   { ID = XLSX.Load(path), path = path };
	
	setmetatable(obj, { __index = WorkbookClass })

	return obj;

end;

-- Create new workbook  
---@param WorkName string?
---@return Workbook object
function Excel.NewWorkbook(WorkName) -- note, can't be only for read, so be caryful
    local obj = { ID = XLSX.New(WorkName) };
    
    setmetatable(obj, { __index = WorkbookClass })

    return obj;
end;

-- Recalculate formulas in this workbook
function WorkbookClass:Recalculate()
    if not self.ID then
        error(TID_XLSX_ERROR_WORKBOOK_CLOSED);
        return;
    end;
    XLSX.Recalculate(self.ID);
end;




-- Alias for legacy
-- To call also Excel.Workbook like Workbook etc;

Workbook = Excel.Workbook;
NewWorkbook = Excel.NewWorkbook;

Excel.CloseAllWorkbooks = XLSX.CloseAllWorkbooks;
CloseAllWorkbooks = Excel.CloseAllWorkbooks;
Excel.WorkbookExists = XLSX.WorkbookExists;
WorkbookExists = Excel.WorkbookExists;
ERange = Excel.Range;


