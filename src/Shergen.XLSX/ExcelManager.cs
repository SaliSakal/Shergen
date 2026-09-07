using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using static Shergen.Lua.Native;
using static Shergen.Utils;



namespace Shergen.XLSX
{
    using Color = System.Drawing.Color;
    public static class ExcelManager
    {
        public class WorkbookInfo
        {
            public string Id { get; set; }              // Unikátní ID / Unique ID
            public string CurrentPath { get; set; }     // Aktuální cesta (může se měnit při SaveAs) / Current path (can change on SaveAs)
            public string Name { get; set; }            // Jméno workbooku / Workbook name
            public XLWorkbook Workbook { get; set; }    // ClosedXML workbook objekt 
        }
        public static Dictionary<string, WorkbookInfo> workbooks = new Dictionary<string, WorkbookInfo>(); // ID -> WorkbookInfo

        public static Dictionary<string, Dictionary<string, ExcelSheet>> sheets = new(); // Sheets cache: WorkbookID -> (SheetName -> ExcelSheet)

        public static string tableDirectory { get; set; } = "";

        public static int LoadXLSX(IntPtr L)
        {
            string path = tableDirectory + ToLuaString(L, 1);

            string name = Path.GetFileNameWithoutExtension(path);

            try
            {
                path = Utils.FindFileCaseInsensitive(path); // Opravíme cestu na správný case, pokud je to možné

                // Zkontroluj jestli už není načtený podle cesty
                var existing = workbooks.Values.FirstOrDefault(w => w.CurrentPath == path);
                if (existing != null)
                {
                    Console.WriteLine($"Workbook {name} is already loaded, return existing ID!");
                    PushLuaString(L, existing.Id); // Vrátíme existující ID
                    return 1;
                }

                if (!File.Exists(path))
                {
                    PushLuaError(L, $"⚠️ XLSX file {path} doesn't exist!");
                    return 0;
                }

                Console.WriteLine($"Loading Workbook {name}!");

                var workbook = new XLWorkbook(path);

                string id = $"{Guid.NewGuid()}"; // Pro načtené soubory použijeme jedinečné ID // For loaded files, we will use a unique ID

                var info = new WorkbookInfo
                {
                    Id = id,
                    CurrentPath = path,
                    Workbook = workbook,
                    Name = name
                };

                workbooks[id] = info;

                PushLuaString(L, id);
            }
            catch (IOException ex)
            {
                PushLuaError(L, $"Error: XLSX File '{path}' is locked: {ex.Message}");
                return 0;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"Unexpected error loading '{path}': {ex.Message}");
                return 0;
            }

            return 1;
        }
        public static int TableExists(IntPtr L)
        {

            string path = tableDirectory + ToLuaString(L, 1);
            try
            {
                PushLuaBoolean(L, FileManager.FileExists(path));
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"Error checking table file existence: {ex.Message}");
                return 0;

            }
        }

        public static int NewXLSX(IntPtr L)
        {
            // Vygeneruj unikátní identifikátor pro nový workbook v paměti
            string tempId = $"{Guid.NewGuid()}";
            string name = ToLuaString(L, 1);
            if (name == null || name == "") { name = "NewWorkbook"; }
            else name = Path.GetFileNameWithoutExtension(name);
            try
            {
                Console.WriteLine($"✓ Creating new workbook in memory as {name}");
                var workbook = new XLWorkbook();

                var info = new WorkbookInfo
                {
                    Id = tempId,
                    CurrentPath = null, // Zatím nemá cestu
                    Workbook = workbook,
                    Name = name
                };

                workbooks[tempId] = info;
                PushLuaString(L, tempId);
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Unexpected error creating workbook: {ex.Message}");
                return 0;
            }
        }

        // Volitelně přidej i AddSheet pro práci s listy
        public static int AddSheet(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);
            string sheetName = ToLuaString(L, 2);

            if (!workbooks.ContainsKey(workbookId))
            {
                PushLuaError(L, $"⚠️ Workbook with ID {workbookId} wasn't loaded!");
                return 0;
            }

            try
            {
                var info = workbooks[workbookId];
                var workbook = info.Workbook;

                // Zkontroluj jestli sheet už existuje
                if (workbook.Worksheets.Any(ws => ws.Name == sheetName))
                {
                    PushLuaError(L, $"⚠️ Sheet '{sheetName}' already exists!");
                    return 0;
                }

                workbook.Worksheets.Add(sheetName);
                Console.WriteLine($"✅ Sheet '{sheetName}' added to '{info.Name}'");

                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Error adding sheet: {ex.Message}");
                return 0;
            }
        }

        public static int DeleteSheet(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);
            string sheetName = ToLuaString(L, 2);

            if (!workbooks.ContainsKey(workbookId))
            {
                PushLuaError(L, $"⚠️ Workbook ID {workbookId} wasn't loaded!");
                return 0;
            }

            try
            {
                var info = workbooks[workbookId];
                var workbook = info.Workbook;
                var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName);

                if (worksheet == null)
                {
                    PushLuaError(L, $"⚠️ Sheet '{sheetName}' doesn't exist!");
                    return 0;
                }

                worksheet.Delete();

                // Vymaž i z cache
                if (sheets.ContainsKey(workbookId) && sheets[workbookId].ContainsKey(sheetName))
                {
                    sheets[workbookId].Remove(sheetName);
                }

                Console.WriteLine($"✅ Sheet '{sheetName}' deleted from '{info.Name}'");

                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Error deleting sheet: {ex.Message}");
                return 0;
            }
        }

        public static int RenameSheet(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);
            string oldName = ToLuaString(L, 2);
            string newName = ToLuaString(L, 3);

            if (!workbooks.ContainsKey(workbookId))
            {
                PushLuaError(L, $"⚠️ Workbook {workbookId} wasn't loaded!");
                return 0;
            }

            try
            {
                var info = workbooks[workbookId];
                var workbook = info.Workbook;
                var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == oldName);

                if (worksheet == null)
                {
                    PushLuaError(L, $"⚠️ Sheet '{oldName}' doesn't exist in workbook '{info.Name}'!");
                    return 0;
                }

                // Zkontroluj jestli nové jméno už neexistuje
                if (workbook.Worksheets.Any(ws => ws.Name == newName))
                {
                    PushLuaError(L, $"⚠️ Sheet '{newName}' already exists in workbook '{info.Name}'!");
                    return 0;
                }

                worksheet.Name = newName;

                // Aktualizuj cache
                if (sheets.ContainsKey(workbookId) && sheets[workbookId].ContainsKey(oldName))
                {
                    var sheet = sheets[workbookId][oldName];
                    sheets[workbookId].Remove(oldName);
                    sheets[workbookId][newName] = sheet;
                }

                Console.WriteLine($"✅ Sheet renamed from '{oldName}' to '{newName}' in workbook '{info.Name}'");

                PushLuaBoolean(L, true);
                return 1;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Error renaming sheet: {ex.Message}");
                return 0;
            }
        }

        public static int ContainSheet(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);
            string sheetName = ToLuaString(L, 2);




            if (!workbooks.ContainsKey(workbookId))
            {
                //Console.WriteLine($"⚠️ Workbook {filePath} wasn't loaded!");
                PushLuaError(L, $"⚠️ Workbook {workbookId} wasn't loaded!");
                return 0;
            }
            var info = workbooks[workbookId];
            var workbook = info.Workbook;
            if (!workbook.Worksheets.Any(ws => ws.Name == sheetName))
            {
                PushLuaBoolean(L, false);
                return 1;
            }


            PushLuaBoolean(L, true);
            return 1;
        }

        public static int GetSheet(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);
            string sheetName = ToLuaString(L, 2);

            if (!workbooks.ContainsKey(workbookId))
            {
                Console.WriteLine($"⚠️ Workbook ID {workbookId} wasn't loaded!");
                return 0;
            }

            Console.WriteLine($"Loading sheet {sheetName}!");

            var info = workbooks[workbookId];
            var workbook = info.Workbook;
            var worksheet = workbook.Worksheet(sheetName);

            NewTable(L);
            //       int rowCount = worksheet.RowCount();
            //       int colCount = worksheet.ColumnCount();

            int rowCount = worksheet.LastRowUsed()?.RowNumber() ?? 1;
            int colCount = worksheet.LastColumnUsed()?.ColumnNumber() ?? 1;


            Console.WriteLine($"📊 Loading sheet {sheetName}: {rowCount} rows : {colCount} collums");

            for (int row = 1; row <= rowCount; row++)
            {
                PushLuaInteger(L, row);
                NewTable(L);

                for (int col = 1; col <= colCount; col++)
                {
                    PushLuaInteger(L, col);
                    PushLuaString(L, worksheet.Cell(row, col).GetString());
                    SetTable(L, -3);
                }

                SetTable(L, -3);

                if (row % 10 == 0)
                {
                    Console.WriteLine($"✅ {row}/{rowCount} rows finished...");

                }
            }

            return 1; // Vracíme Lua tabulku
        }


        public static int SaveXLSX(IntPtr L)
        {
            string WorkbookID = ToLuaString(L, 1);

            if (!workbooks.ContainsKey(WorkbookID))
            {
                PushLuaError(L, $"⚠️ Workbook with ID {WorkbookID} wasn't loaded!");
                return 0;
            }

            try
            {
                // Pokud workbook ještě nebyl nikdy uložen, Save() selže
                // Uživatel musí použít SaveAs()
                var info = workbooks[WorkbookID];
                info.Workbook.Save(); // Save používá aktuální cestu z ClosedXML

                Console.WriteLine($"💾 Workbook {info.Name} was saved!");
                return 0;
            }
            catch (InvalidOperationException)
            {
                PushLuaError(L, $"⚠️ New workbook must use SaveAs() first!");
                return 0;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Error saving: {ex.Message}");
                return 0;
            }

        }

        public static int SaveAsXLSX(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);
            string path = tableDirectory + ToLuaString(L, 2);

            if (!workbooks.ContainsKey(workbookId))
            {
                PushLuaError(L, $"⚠️ Workbook wasn't loaded!");
                return 0;
            }

            try
            {
                //var workbook = workbooks[workbookId];
                var info = workbooks[workbookId];
                string newPath = path.EndsWith(".xlsx") ? path : path + ".xlsx";
                newPath = Utils.FindFileCaseInsensitive(newPath); // Opravíme cestu na správný case, pokud již soubor existuje

                info.Workbook.SaveAs(newPath);

                // Aktualizuj aktuální cestu
                info.CurrentPath = newPath;
                info.Name = Path.GetFileNameWithoutExtension(newPath);

                Console.WriteLine($"💾 Workbook saved as {info.Name}!");

                return 0;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Error saving workbook: {ex.Message}");
                return 0;
            }
        }

        public static int CloseWorkbook(IntPtr L)
        {
            string workbookID = ToLuaString(L, 1);
            ExcelManager.CloseWorkbook(workbookID);
            return 0;
        }

        public static void CloseWorkbook(string workbookID)
        {


            if (workbooks.ContainsKey(workbookID))
            {
                var info = workbooks[workbookID];
                var workbook = info.Workbook;
                var name = info.Name;
                workbook.Dispose(); // Uvolní workbook z paměti
                workbooks.Remove(workbookID);
                sheets.Remove(workbookID); // Vymaže všechny listy daného workbooku
                Console.WriteLine($"📌 Workbook '{name}' was closed and removed from memory.");
            }
            else
            {
                Console.WriteLine($"⚠️ Workbook ID '{workbookID}' wasn't loaded.");
            }

        }

        public static int CloseAllWorkbooks(IntPtr L)
        {
            CloseAllWorkbooks();
            return 0;
        }


        public static void CloseAllWorkbooks()
        {
            foreach (var info in workbooks.Values)
            {
                var workbook = info.Workbook;
                if (workbook != null)
                {
                    try
                    {
                        workbook.Dispose(); // Zavře sešit

                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ Error during closing workbook: {ex.Message}");
                    }
                }
            }
            workbooks.Clear(); // Vyčistí seznam
            sheets.Clear();
            Console.WriteLine($"📌 All Workbooks was closed and removed from memory.");
        }

        // Helper metoda pro získání sheetu
        private static ExcelSheet GetSheet(string sheetId)
        {
            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) ||
                !sheets[parts[0]].ContainsKey(parts[1]))
            {
                Console.WriteLine($"⚠️ Invalid sheet '{sheetId}'!");
                return null;
            }
            return sheets[parts[0]][parts[1]];
        }


        public static int GetSheetObject(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);
            string sheetName = ToLuaString(L, 2);

            if (!workbooks.ContainsKey(workbookId))
            {
                Console.WriteLine($"⚠️ File {workbookId} wasn't loaded!");

                PushLuaNil(L); // Vrátíme nil do Lua
                return 1;
            }

            var info = workbooks[workbookId];
            var workbook = info.Workbook;
            if (!workbook.Worksheets.Any(ws => ws.Name == sheetName))
            {
                Console.WriteLine($"⚠️ Sheet '{sheetName}' doesn't exist in '{info.Name}'!");

                PushLuaNil(L); // Vrátíme nil do Lua
                return 1;
            }

            if (!sheets.ContainsKey(workbookId))
                sheets[workbookId] = new Dictionary<string, ExcelSheet>();

            if (!sheets[workbookId].ContainsKey(sheetName))
            {
                var sheet = workbook.Worksheet(sheetName);
                sheets[workbookId][sheetName] = new ExcelSheet(sheet);
            }

            // Vrátíme Lua string jako identifikátor (ne integer ID)
            PushLuaString(L, $"{workbookId}:{sheetName}");
            return 1;
        }

        // ===== WORKBOOK RECALCULATE =====
        public static int RecalculateWorkbook(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);

            if (!workbooks.ContainsKey(workbookId))
            {
                PushLuaError(L, $"⚠️ Workbook wasn't loaded!");
                return 0;
            }

            try
            {
                var info = workbooks[workbookId];
                info.Workbook.RecalculateAllFormulas();
                Console.WriteLine($"✅ Workbook '{info.Name}' formulas recalculated");
                return 0;
            }
            catch (Exception ex)
            {
                var info = workbooks[workbookId];
                PushLuaError(L, $"❌ Error recalculating workbook '{info.Name}': {ex.Message}");
                return 0;
            }
        }




        public static int GetSheetRange(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) ||
                !sheets[parts[0]].ContainsKey(parts[1]))
            {
                Console.WriteLine($"⚠️ Incorret sheet '{sheetId}'!");
                return 0;
            }
            var sheet = sheets[parts[0]][parts[1]];

            int rowCount = sheet.GetLastRow();
            int colCount = sheet.GetLastColumn();

            PushLuaInteger(L, rowCount);
            PushLuaInteger(L, colCount);
            return 2;
        }

        public static int GetSheetNames(IntPtr L)
        {
            string workbookId = ToLuaString(L, 1);

            if (!workbooks.ContainsKey(workbookId))
            {
                Console.WriteLine($"⚠️ File {workbookId} wasn't loaded!");
                PushLuaNil(L); // Vrátíme nil do Lua
                return 1;
            }

            var info = workbooks[workbookId];
            var workbook = info.Workbook;
            var sheetNames = workbook.Worksheets.Select(ws => ws.Name).ToList();
            NewTable(L);

            for (int i = 0; i < sheetNames.Count; i++)
            {
                PushLuaInteger(L, i + 1); // Lua index začíná na 1
                PushLuaString(L, sheetNames[i]);
                SetTable(L, -3);
            }

            return 1;
        }

        public class ExcelSheet
        {
            private IXLWorksheet _sheet;

            public ExcelSheet(IXLWorksheet sheet)
            {
                _sheet = sheet;
            }

            public string GetCell(int row, int col)
            {
                return _sheet.Cell(row, col).GetString();
            }

            public void SetCell(int row, int col, string value)
            {
                _sheet.Cell(row, col).Value = value;
            }

            public Dictionary<string, object?> GetCellEx(int row, int col)
            {
                var cell = _sheet.Cell(row, col);

                var result = new Dictionary<string, object?>
                {
                    ["value"] = cell.GetValue<string>(),
                    ["format"] = cell.Style.NumberFormat.Format ?? "General",
                    ["formula"] = cell.HasFormula ? cell.FormulaA1 : null,
                    ["address"] = cell.Address.ToString(),
                    ["dataType"] = cell.DataType.ToString(),
                    ["isBold"] = cell.Style.Font.Bold,
                    ["isItalic"] = cell.Style.Font.Italic,
                    ["isStrikethrough"] = cell.Style.Font.Strikethrough,
                    ["isUnderline"] = cell.Style.Font.Underline != XLFontUnderlineValues.None,
                    ["comment"] = cell.HasComment ? cell.GetComment().Text : null,
                    ["fontName"] = cell.Style.Font.FontName,
                    ["fontSize"] = cell.Style.Font.FontSize,
                    ["hAlign"] = cell.Style.Alignment.Horizontal.ToString(),
                    ["vAlign"] = cell.Style.Alignment.Vertical.ToString(),
                    ["wrapText"] = cell.Style.Alignment.WrapText,
                    ["indent"] = cell.Style.Alignment.Indent,
                    ["isHiddenRow"] = cell.WorksheetRow().IsHidden,
                    ["isHiddenColumn"] = cell.WorksheetColumn().IsHidden,
                    ["isMerged"] = cell.IsMerged(),
                    ["mergedRange"] = cell.IsMerged() ? cell.MergedRange().RangeAddress.ToString() : null,
                    ["locked"] = cell.Style.Protection.Locked,
                    ["hidden"] = cell.Style.Protection.Hidden,
                    ["borderTop"] = cell.Style.Border.TopBorder.ToString(),
                    ["borderBottom"] = cell.Style.Border.BottomBorder.ToString(),
                    ["borderLeft"] = cell.Style.Border.LeftBorder.ToString(),
                    ["borderRight"] = cell.Style.Border.RightBorder.ToString(),



                };

                result["fontColor"] = GetFontHex(cell);
                result["bgColor"] = GetFillHex(cell);



                var richParts = cell.GetRichText();
                var richArray = new List<Dictionary<string, object>>();

                foreach (var part in richParts)
                {
                    var fragment = new Dictionary<string, object?>
                    {
                        ["text"] = part.Text,
                        ["fontName"] = part.FontName,
                        ["fontSize"] = part.FontSize,
                        ["isBold"] = part.Bold,
                        ["isItalic"] = part.Italic,
                        ["isStrikethrough"] = part.Strikethrough,
                        ["isUnderline"] = part.Underline != XLFontUnderlineValues.None,
                        ["script"] = part.VerticalAlignment.ToString(), // "Baseline", "Superscript", "Subscript"

                    };

                    var color = part.FontColor;
                    if (color.ColorType == XLColorType.Color)
                    {
                        fragment["fontColor"] = $"#{color.Color.R:X2}{color.Color.G:X2}{color.Color.B:X2}";
                    }


                    richArray.Add(fragment);
                }
                result["richText"] = richArray;

                if (richParts.Count == 1)
                {
                    var part = richParts.First();

                    bool hasNoCustomColor = part.FontColor.ColorType != XLColorType.Color;

                    result["isPlain"] =
                        !part.Bold &&
                        !part.Italic &&
                        !part.Strikethrough &&
                        part.Underline == XLFontUnderlineValues.None &&
                        part.VerticalAlignment == XLFontVerticalTextAlignmentValues.Baseline &&
                        hasNoCustomColor &&
                        part.FontName == cell.Style.Font.FontName &&
                        part.FontSize == cell.Style.Font.FontSize;
                    ;
                }




                return result;
            }



            // DROP-IN: #RRGGBB bez ohledu na Theme/Automatic.
            static string ToHex(Color c) => $"#{c.R:X2}{c.G:X2}{c.B:X2}";

            static Color ApplyTint(Color baseColor, double tint)
            {
                if (tint == 0) return baseColor;
                double t = Math.Max(-1.0, Math.Min(1.0, tint));
                if (t < 0)
                {
                    double f = 1.0 + t; // ztmavení
                    return Color.FromArgb(
                        (int)(baseColor.R * f),
                        (int)(baseColor.G * f),
                        (int)(baseColor.B * f));
                }
                // zesvětlení
                return Color.FromArgb(
                    (int)(baseColor.R + (255 - baseColor.R) * t),
                    (int)(baseColor.G + (255 - baseColor.G) * t),
                    (int)(baseColor.B + (255 - baseColor.B) * t));
            }

            // POZOR: XLColor (ne IXLColor)
            static Color ResolveColor(IXLCell cell, XLColor xc, XLThemeColor automaticFallback)
            {
                switch (xc.ColorType)
                {
                    case XLColorType.Color:
                        return xc.Color;

                    case XLColorType.Theme:
                        {
                            var baseC = cell.Worksheet.Workbook.Theme.ResolveThemeColor(xc.ThemeColor).Color;
                            return ApplyTint(baseC, xc.ThemeTint);
                        }

                    case XLColorType.Indexed:
                        {
                            if (xc.Indexed == 64) // "Automatic"
                                return cell.Worksheet.Workbook.Theme.ResolveThemeColor(automaticFallback).Color;

                            return XLColor.FromIndex(xc.Indexed).Color;
                        }

                    default:
                        return cell.Worksheet.Workbook.Theme.ResolveThemeColor(automaticFallback).Color;
                }
            }

            // ===== Font =====
            static Color GetFontColor(IXLCell cell)
                => ResolveColor(cell, cell.Style.Font.FontColor, XLThemeColor.Text1);

            static string GetFontHex(IXLCell cell)
                => ToHex(GetFontColor(cell));

            // ===== Pozadí (solid); u patternu klidně fallback na Background1 =====
            static Color GetFillColor(IXLCell cell)
            {
                var fill = cell.Style.Fill;

                if (fill.PatternType == XLFillPatternValues.Solid)
                    return ResolveColor(cell, fill.BackgroundColor, XLThemeColor.Background1);

                if (fill.PatternType == XLFillPatternValues.None)
                    return cell.Worksheet.Workbook.Theme.ResolveThemeColor(XLThemeColor.Background1).Color;

                // pattern → vrátíme podkladovou barvu (BackgroundColor)
                return ResolveColor(cell, fill.BackgroundColor, XLThemeColor.Background1);
            }


            static string GetFillHex(IXLCell cell)
                => ToHex(GetFillColor(cell));


            public int GetLastRow()
            {
                return _sheet.LastRowUsed()?.RowNumber() ?? 0;
            }

            public int GetLastColumn()
            {
                return _sheet.LastColumnUsed()?.ColumnNumber() ?? 0;
            }

            public (int row, int col)? FindValue(string searchValue)
            {
                foreach (var cell in _sheet.CellsUsed())
                {
                    if (cell.GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return (cell.Address.RowNumber, cell.Address.ColumnNumber);
                    }
                }
                return null;
            }

            public List<(int row, int col)> FindAllValues(string searchValue)
            {
                var results = new List<(int row, int col)>();

                foreach (var cell in _sheet.CellsUsed())
                {
                    if (cell.GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add((cell.Address.RowNumber, cell.Address.ColumnNumber));
                    }
                }

                return results;
            }

            public (int row, int col)? FindInRange(string searchValue, int startRow, int endRow, int startCol, int endCol)
            {
                foreach (var cell in _sheet.Range(startRow, startCol, endRow, endCol).CellsUsed())
                {
                    if (cell.GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return (cell.Address.RowNumber, cell.Address.ColumnNumber);
                    }
                }
                return null;
            }

            public List<(int row, int col)> FindAllInRange(string searchValue, int startRow, int endRow, int startCol, int endCol)
            {
                var results = new List<(int row, int col)>();

                foreach (var cell in _sheet.Range(startRow, startCol, endRow, endCol).CellsUsed())
                {
                    if (cell.GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add((cell.Address.RowNumber, cell.Address.ColumnNumber));
                    }
                }

                return results;
            }


            public int? FindInColumn(int col, string searchValue, int startRow = 1)
            {
                int lastRow = GetLastRow();
                for (int row = startRow; row <= lastRow; row++)
                {
                    if (_sheet.Cell(row, col).GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return row;
                    }
                }
                return null;
            }


            public int? FindInRow(int row, string searchValue, int startCol = 1)
            {
                int lastCol = GetLastColumn();
                for (int col = startCol; col <= lastCol; col++)
                {
                    if (_sheet.Cell(row, col).GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        return col;
                    }
                }
                return null;
            }


            public List<int> FindAllInColumn(int col, string searchValue, int startRow = 1)
            {
                var results = new List<int>();
                int lastRow = GetLastRow();
                for (int row = startRow; row <= lastRow; row++)
                {
                    if (_sheet.Cell(row, col).GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(row);
                    }
                }
                return results;
            }


            public List<int> FindAllInRow(int row, string searchValue, int startCol = 1)
            {
                var results = new List<int>();
                int lastColumn = GetLastColumn();
                for (int col = startCol; col <= lastColumn; col++)
                {
                    if (_sheet.Cell(row, col).GetString().Equals(searchValue, StringComparison.OrdinalIgnoreCase))
                    {
                        results.Add(col);
                    }
                }
                return results;
            }



            // ===== NOVÉ: TYPOVANÉ HODNOTY =====
            public void SetValue(int row, int col, object value)
            {
                _sheet.Cell(row, col).Value = XLCellValue.FromObject(value);
            }

            public object GetValue(int row, int col)
            {
                var cell = _sheet.Cell(row, col);

                return cell.DataType switch
                {
                    XLDataType.Boolean => cell.GetBoolean(),
                    XLDataType.Number => cell.GetDouble(),
                    XLDataType.DateTime => cell.GetDateTime(),
                    XLDataType.TimeSpan => cell.GetTimeSpan(),
                    _ => cell.GetString()
                };
            }

            // V ExcelSheet třídě
            public void ClearCell(int row, int col)
            {
                _sheet.Cell(row, col).Clear();
            }

            public void ClearCellContents(int row, int col)
            {
                _sheet.Cell(row, col).Clear(XLClearOptions.Contents); // Jen hodnota, formátování zůstane
            }

            public void ClearCellFormats(int row, int col)
            {
                _sheet.Cell(row, col).Clear(XLClearOptions.NormalFormats); // Jen formátování, hodnota zůstane
            }

            // ===== BACKGROUND COLOR - různé varianty =====
            public void SetBackgroundColorHex(int row, int col, string hexColor)
            {
                _sheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromHtml(hexColor);
            }

            public void SetBackgroundColorRGB(int row, int col, int r, int g, int b)
            {
                _sheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromArgb(r, g, b);
            }

            public void SetBackgroundColorTheme(int row, int col, string themeName, double tint = 0.0)
            {
                var themeColor = ParseThemeColor(themeName);
                var color = XLColor.FromTheme(themeColor, tint);
                _sheet.Cell(row, col).Style.Fill.BackgroundColor = color;
            }

            public void SetBackgroundColorIndexed(int row, int col, int colorIndex)
            {
                _sheet.Cell(row, col).Style.Fill.BackgroundColor = XLColor.FromIndex(colorIndex);
            }

            // ===== FONT COLOR - různé varianty =====
            public void SetFontColorHex(int row, int col, string hexColor)
            {
                _sheet.Cell(row, col).Style.Font.FontColor = XLColor.FromHtml(hexColor);
            }

            public void SetFontColorRGB(int row, int col, int r, int g, int b)
            {
                _sheet.Cell(row, col).Style.Font.FontColor = XLColor.FromArgb(r, g, b);
            }

            public void SetFontColorTheme(int row, int col, string themeName, double tint = 0.0)
            {
                var themeColor = ParseThemeColor(themeName);
                var color = XLColor.FromTheme(themeColor, tint);
                _sheet.Cell(row, col).Style.Font.FontColor = color;
            }

            // ===== FONT FORMATTING =====
            public void SetBold(int row, int col, bool bold = true)
            {
                _sheet.Cell(row, col).Style.Font.Bold = bold;
            }

            public void SetItalic(int row, int col, bool italic = true)
            {
                _sheet.Cell(row, col).Style.Font.Italic = italic;
            }

            public void SetUnderline(int row, int col, bool underline = true)
            {
                _sheet.Cell(row, col).Style.Font.Underline = underline ? XLFontUnderlineValues.Single : XLFontUnderlineValues.None;
            }

            public void SetStrikethrough(int row, int col, bool strikethrough = true)
            {
                _sheet.Cell(row, col).Style.Font.Strikethrough = strikethrough;
            }

            public void SetFontSize(int row, int col, double size)
            {
                _sheet.Cell(row, col).Style.Font.FontSize = size;
            }

            public void SetFontName(int row, int col, string fontName)
            {
                _sheet.Cell(row, col).Style.Font.FontName = fontName;
            }

            // ===== ALIGNMENT =====
            public void SetAlignment(int row, int col, XLAlignmentHorizontalValues h, XLAlignmentVerticalValues v)
            {
                var cell = _sheet.Cell(row, col);
                cell.Style.Alignment.Horizontal = h;
                cell.Style.Alignment.Vertical = v;
            }

            public void SetWrapText(int row, int col, bool wrap = true)
            {
                _sheet.Cell(row, col).Style.Alignment.WrapText = wrap;
            }

            // ===== BORDERS =====
            public void SetBorder(int row, int col, XLBorderStyleValues style)
            {
                _sheet.Cell(row, col).Style.Border.OutsideBorder = style;
            }

            // Nastaví všechny strany najednou
            public void SetBorderAround(int row, int col, XLBorderStyleValues style)
            {
                var cell = _sheet.Cell(row, col);
                cell.Style.Border.TopBorder = style;
                cell.Style.Border.BottomBorder = style;
                cell.Style.Border.LeftBorder = style;
                cell.Style.Border.RightBorder = style;
            }

            // Nastaví jen vnější ohraničení (outline)
            public void SetBorderOutside(int row, int col, XLBorderStyleValues style)
            {
                _sheet.Cell(row, col).Style.Border.OutsideBorder = style;
            }

            // Nastaví jednotlivé strany
            public void SetBorderTop(int row, int col, XLBorderStyleValues style)
            {
                _sheet.Cell(row, col).Style.Border.TopBorder = style;
            }

            public void SetBorderBottom(int row, int col, XLBorderStyleValues style)
            {
                _sheet.Cell(row, col).Style.Border.BottomBorder = style;
            }

            public void SetBorderLeft(int row, int col, XLBorderStyleValues style)
            {
                _sheet.Cell(row, col).Style.Border.LeftBorder = style;
            }

            public void SetBorderRight(int row, int col, XLBorderStyleValues style)
            {
                _sheet.Cell(row, col).Style.Border.RightBorder = style;
            }

            // ===== NUMBER FORMAT =====
            public void SetNumberFormat(int row, int col, string format)
            {
                _sheet.Cell(row, col).Style.NumberFormat.Format = format;
            }

            // ===== RANGE OPERATIONS =====
            public void SetRangeBackgroundColorHex(int startRow, int startCol, int endRow, int endCol, string hexColor)
            {
                _sheet.Range(startRow, startCol, endRow, endCol).Style.Fill.BackgroundColor = XLColor.FromHtml(hexColor);
            }

            public void SetRangeBackgroundColorRGB(int startRow, int startCol, int endRow, int endCol, int r, int g, int b)
            {
                _sheet.Range(startRow, startCol, endRow, endCol).Style.Fill.BackgroundColor = XLColor.FromArgb(r, g, b);
            }

            public void SetRangeBold(int startRow, int startCol, int endRow, int endCol, bool bold = true)
            {
                _sheet.Range(startRow, startCol, endRow, endCol).Style.Font.Bold = bold;
            }

            public void SetRangeBorder(int startRow, int startCol, int endRow, int endCol, XLBorderStyleValues style)
            {
                _sheet.Range(startRow, startCol, endRow, endCol).Style.Border.OutsideBorder = style;
            }

            public void MergeCells(int startRow, int startCol, int endRow, int endCol)
            {
                _sheet.Range(startRow, startCol, endRow, endCol).Merge();
            }

            // ===== COLUMN/ROW OPERATIONS =====
            public void SetColumnWidth(int col, double width)
            {
                _sheet.Column(col).Width = width;
            }

            public void AutoFitColumn(int col)
            {
                _sheet.Column(col).AdjustToContents();
            }

            public void SetRowHeight(int row, double height)
            {
                _sheet.Row(row).Height = height;
            }

            public void DeleteRow(int row)
            {
                _sheet.Row(row).Delete();
            }

            public void DeleteColumn(int col)
            {
                _sheet.Column(col).Delete();
            }

            public void InsertRowAbove(int row)
            {
                _sheet.Row(row).InsertRowsAbove(1);
            }

            public void InsertColumnBefore(int col)
            {
                _sheet.Column(col).InsertColumnsBefore(1);
            }

            // ===== FREEZE PANES =====

            // Zmrazí řádky nad zadaným řádkem a sloupce před zadaným sloupcem
            public void FreezePanes(int row, int col)
            {
                _sheet.SheetView.FreezeRows(row - 1);     // Zmrazí řádky nad tímto
                _sheet.SheetView.FreezeColumns(col - 1);  // Zmrazí sloupce před tímto
            }

            // Zmrazí jen řádky (např. hlavička)
            public void FreezeRows(int count)
            {
                _sheet.SheetView.FreezeRows(count);
            }

            // Zmrazí jen sloupce
            public void FreezeColumns(int count)
            {
                _sheet.SheetView.FreezeColumns(count);
            }

            // Zruší zmrazení
            public void UnfreezePanes()
            {
                _sheet.SheetView.FreezeRows(0);
                _sheet.SheetView.FreezeColumns(0);
            }
            // ===== HELPER METHODS =====
            private XLThemeColor ParseThemeColor(string name)
            {
                return name.ToLower() switch
                {
                    //"dark1" => XLThemeColor.Dark1,
                    //"light1" => XLThemeColor.Light1,
                    //"dark2" => XLThemeColor.Dark2,
                    //"light2" => XLThemeColor.Light2,
                    "accent1" => XLThemeColor.Accent1,
                    "accent2" => XLThemeColor.Accent2,
                    "accent3" => XLThemeColor.Accent3,
                    "accent4" => XLThemeColor.Accent4,
                    "accent5" => XLThemeColor.Accent5,
                    "accent6" => XLThemeColor.Accent6,
                    "hyperlink" => XLThemeColor.Hyperlink,
                    "followedhyperlink" => XLThemeColor.FollowedHyperlink,
                    "text1" => XLThemeColor.Text1,
                    "text2" => XLThemeColor.Text2,
                    "background1" => XLThemeColor.Background1,
                    "background2" => XLThemeColor.Background2,
                    _ => XLThemeColor.Text1
                };
            }


            // ===== FORMULA =====
            public void SetFormula(int row, int col, string formula)
            {
                _sheet.Cell(row, col).FormulaA1 = formula;
            }

            public string GetFormula(int row, int col)
            {
                var cell = _sheet.Cell(row, col);
                return cell.HasFormula ? cell.FormulaA1 : null;
            }

            public bool HasFormula(int row, int col)
            {
                return _sheet.Cell(row, col).HasFormula;
            }

            // ===== GET FUNKCE - COLORS =====
            public string GetBackgroundColor(int row, int col)
            {
                var cell = _sheet.Cell(row, col);
                var color = cell.Style.Fill.BackgroundColor;

                if (color.ColorType == XLColorType.Color)
                {
                    return $"#{color.Color.R:X2}{color.Color.G:X2}{color.Color.B:X2}";
                }
                return null;
            }

            public string GetFontColor(int row, int col)
            {
                var cell = _sheet.Cell(row, col);
                var color = cell.Style.Font.FontColor;

                if (color.ColorType == XLColorType.Color)
                {
                    return $"#{color.Color.R:X2}{color.Color.G:X2}{color.Color.B:X2}";
                }
                return null;
            }

            // ===== GET FUNKCE - FONT =====
            public bool GetBold(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Font.Bold;
            }

            public bool GetItalic(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Font.Italic;
            }

            public bool GetUnderline(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Font.Underline != XLFontUnderlineValues.None;
            }

            public bool GetStrikethrough(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Font.Strikethrough;
            }

            public double GetFontSize(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Font.FontSize;
            }

            public string GetFontName(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Font.FontName;
            }

            // ===== GET FUNKCE - ALIGNMENT =====
            public string GetHorizontalAlignment(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Alignment.Horizontal.ToString();
            }

            public string GetVerticalAlignment(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Alignment.Vertical.ToString();
            }

            public bool GetWrapText(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Alignment.WrapText;
            }

            // ===== GET FUNKCE - BORDERS =====
            public string GetBorderTop(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Border.TopBorder.ToString();
            }

            public string GetBorderBottom(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Border.BottomBorder.ToString();
            }

            public string GetBorderLeft(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Border.LeftBorder.ToString();
            }

            public string GetBorderRight(int row, int col)
            {
                return _sheet.Cell(row, col).Style.Border.RightBorder.ToString();
            }

            // ===== GET FUNKCE - NUMBER FORMAT =====
            public string GetNumberFormat(int row, int col)
            {
                return _sheet.Cell(row, col).Style.NumberFormat.Format;
            }

            // ===== GET FUNKCE - DIMENSIONS =====
            public double GetColumnWidth(int col)
            {
                return _sheet.Column(col).Width;
            }

            public double GetRowHeight(int row)
            {
                return _sheet.Row(row).Height;
            }

            // ===== GET FUNKCE - MERGED =====
            public bool IsMerged(int row, int col)
            {
                return _sheet.Cell(row, col).IsMerged();
            }

            public string GetMergedRange(int row, int col)
            {
                var cell = _sheet.Cell(row, col);
                if (cell.IsMerged())
                {
                    return cell.MergedRange().RangeAddress.ToString();
                }
                return null;
            }


            // ===== ROW FORMATTING =====
            public void SetRowBackgroundColor(int row, XLColor color)
            {
                _sheet.Row(row).Style.Fill.BackgroundColor = color;
            }

            public void SetRowBackgroundColorHex(int row, string hexColor)
            {
                _sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromHtml(hexColor);
            }

            public void SetRowBackgroundColorRGB(int row, int r, int g, int b)
            {
                _sheet.Row(row).Style.Fill.BackgroundColor = XLColor.FromArgb(r, g, b);
            }

            public void SetRowBold(int row, bool bold = true)
            {
                _sheet.Row(row).Style.Font.Bold = bold;
            }

            public void SetRowFontColor(int row, XLColor color)
            {
                _sheet.Row(row).Style.Font.FontColor = color;
            }

            public void SetRowFontColorRGB(int row, int r, int g, int b)
            {
                _sheet.Row(row).Style.Font.FontColor = XLColor.FromArgb(r, g, b);
            }

            public void SetRowFontColorHex(int row, string hexColor)
            {
                _sheet.Row(row).Style.Font.FontColor = XLColor.FromHtml(hexColor);
            }

            // ===== COLUMN FORMATTING =====
            public void SetColumnBackgroundColor(int col, XLColor color)
            {
                _sheet.Column(col).Style.Fill.BackgroundColor = color;
            }

            public void SetColumnBackgroundColorHex(int col, string hexColor)
            {
                _sheet.Column(col).Style.Fill.BackgroundColor = XLColor.FromHtml(hexColor);
            }

            public void SetColumnBackgroundColorRGB(int col, int r, int g, int b)
            {
                _sheet.Column(col).Style.Fill.BackgroundColor = XLColor.FromArgb(r, g, b);
            }

            public void SetColumnBold(int col, bool bold = true)
            {
                _sheet.Column(col).Style.Font.Bold = bold;
            }

            public void SetColumnFontColor(int col, XLColor color)
            {
                _sheet.Column(col).Style.Font.FontColor = color;
            }

            public void SetColumnFontColorRGB(int col, int r, int g, int b)
            {
                _sheet.Column(col).Style.Font.FontColor = XLColor.FromArgb(r, g, b);
            }

            public void SetColumnFontColorHex(int col, string hexColor)
            {
                _sheet.Column(col).Style.Font.FontColor = XLColor.FromHtml(hexColor);
            }


            // ===== RECALCULATE =====

            // Přepočet jedné buňky
            public void CalculateCell(int row, int col)
            {
                var cell = _sheet.Cell(row, col);

                if (cell.HasFormula)
                {
                    // Přístup k Value vynutí přepočet formule
                    var _ = cell.Value;
                }
            }

            // Přepočet všech formulí v sheetu
            public void RecalculateSheet()
            {
                _sheet.RecalculateAllFormulas();
            }


        }

        public static int FindInSheet(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) ||
                !sheets[parts[0]].ContainsKey(parts[1]))
            {
                //Console.WriteLine($"⚠️ Incorret sheet '{sheetId}'!");

                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            var result = sheet.FindValue(searchValue);

            if (result.HasValue)
            {
                PushLuaInteger(L, result.Value.row);
                PushLuaInteger(L, result.Value.col);
                return 2; // Vracíme řádek a sloupec
            }

            return 0; // Pokud se nic nenajde, vrátíme nil
        }



        public static int FindAllInSheet(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) ||
                !sheets[parts[0]].ContainsKey(parts[1]))
            {
                //Console.WriteLine($"⚠️ Incorret sheet '{sheetId}'!");
                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            var results = sheet.FindAllValues(searchValue);

            if (results.Count == 0)
            {
                return 0; // Vrátíme nil, pokud jsme nic nenašli
            }

            // ✅ Vrátíme tabulku všech nalezených hodnot
            NewTable(L);
            int index = 1;

            foreach (var result in results)
            {
                PushLuaInteger(L, index);
                NewTable(L);

                PushLuaString(L, "row");
                PushLuaInteger(L, result.row);
                SetTable(L, -3);

                PushLuaString(L, "col");
                PushLuaInteger(L, result.col);
                SetTable(L, -3);

                SetTable(L, -3);
                index++;
            }

            return 1; // Vracíme tabulku s nalezenými hodnotami
        }


        public static int FindInRange(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);
            int startRow = ToLuaInteger(L, 3);
            int endRow = ToLuaInteger(L, 4);
            int startCol = ToLuaInteger(L, 5);
            int endCol = ToLuaInteger(L, 6);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !ExcelManager.sheets.ContainsKey(parts[0]) ||
                !ExcelManager.sheets[parts[0]].ContainsKey(parts[1]))
            {
                //Console.WriteLine($"⚠️ Incorred sheet '{sheetId}'!");
                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = ExcelManager.sheets[parts[0]][parts[1]];
            var result = sheet.FindInRange(searchValue, startRow, endRow, startCol, endCol);

            if (result.HasValue)
            {
                PushLuaInteger(L, result.Value.row);
                PushLuaInteger(L, result.Value.col);
                return 2;
            }

            return 0; // Nenalezeno
        }

        public static int FindAllInRange(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);
            int startRow = ToLuaInteger(L, 3);
            int endRow = ToLuaInteger(L, 4);
            int startCol = ToLuaInteger(L, 5);
            int endCol = ToLuaInteger(L, 6);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) || !sheets[parts[0]].ContainsKey(parts[1]))
            {
                //Console.WriteLine($"⚠️ Incorrect sheet '{sheetId}'!");
                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            var results = sheet.FindAllInRange(searchValue, startRow, endRow, startCol, endCol);

            // Vytvoříme Lua tabulku s výsledky
            NewTable(L);
            int index = 1;
            foreach (var (row, col) in results)
            {
                PushLuaInteger(L, index); // Index v tabulce
                NewTable(L); // Nová tabulka {row, col}
                PushLuaString(L, "row");
                PushLuaInteger(L, row);
                SetTable(L, -3); // Nastaví {row = X}
                PushLuaString(L, "col");
                PushLuaInteger(L, col);
                SetTable(L, -3); // Nastaví {col = Y}
                SetTable(L, -3); // Přidá tabulku do hlavní tabulky
                index++;
            }

            return 1; // Vracíme jednu tabulku
        }


        public static int FindInColumn(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);
            int column = ToLuaInteger(L, 3);


            // Pokud není parametr 4 (startRow) zadán, výchozí je 1
            int startRow = IsLuaNil(L, 4) ? 1 : ToLuaInteger(L, 4);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) || !sheets[parts[0]].ContainsKey(parts[1]))
            {
                //Console.WriteLine($"⚠️ Incorrect sheet '{sheetId}'!");
                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            int? row = sheet.FindInColumn(column, searchValue, startRow); // Už v sobě obsahuje volitelný startRow

            if (row.HasValue)
            {
                PushLuaInteger(L, row.Value);
                return 1;
            }
            return 0; // Není výsledek
        }


        public static int FindInRow(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);
            int row = ToLuaInteger(L, 3);
            int startCol = IsLuaNil(L, 4) ? 1 : ToLuaInteger(L, 4);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) || !sheets[parts[0]].ContainsKey(parts[1]))
            {
                //Console.WriteLine($"⚠️ Incorrect sheet '{sheetId}'!");
                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            int? column = sheet.FindInRow(row, searchValue, startCol);

            if (column.HasValue)
            {
                PushLuaInteger(L, column.Value);
                return 1;
            }
            return 0; // Není výsledek
        }


        public static int FindAllInColumn(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);
            int column = ToLuaInteger(L, 3);
            int startCol = IsLuaNil(L, 4) ? 1 : ToLuaInteger(L, 4);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) || !sheets[parts[0]].ContainsKey(parts[1]))
            {
                // Console.WriteLine($"⚠️ Incorrect sheet '{sheetId}'!");
                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            List<int> rows = sheet.FindAllInColumn(column, searchValue, startCol);

            NewTable(L); // Vytvoříme tabulku v Lua
            int index = 1;
            foreach (var row in rows)
            {
                PushLuaInteger(L, index); // Klíč
                PushLuaInteger(L, row);   // Hodnota
                SetTable(L, -3); // Přidá do tabulky
                index++;
            }
            return 1; // Vracíme tabulku
        }

        public static int FindAllInRow(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            string searchValue = ToLuaString(L, 2);
            int row = ToLuaInteger(L, 3);
            int startCol = IsLuaNil(L, 4) ? 1 : ToLuaInteger(L, 4);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) || !sheets[parts[0]].ContainsKey(parts[1]))
            {
                //Console.WriteLine($"⚠️ Incorrect sheet '{sheetId}'!");
                PushLuaError(L, $"⚠️ Incorrect sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            List<int> columns = sheet.FindAllInRow(row, searchValue, startCol);

            NewTable(L); // Vytvoříme tabulku v Lua
            int index = 1;
            foreach (var col in columns)
            {
                PushLuaInteger(L, index); // Klíč
                PushLuaInteger(L, col);   // Hodnota
                SetTable(L, -3); // Přidá do tabulky
                index++;
            }
            return 1; // Vracíme tabulku
        }



        // ===== SHEET RECALCULATE =====
        public static int RecalculateSheet(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            try
            {
                sheet.RecalculateSheet();
                Console.WriteLine($"✅ Sheet formulas recalculated");
                return 0;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Error recalculating sheet: {ex.Message}");
                return 0;
            }
        }








        // Helper pro parsování alignmentu
        private static XLAlignmentHorizontalValues ParseHorizontalAlignment(string align)
        {
            return align.ToLower() switch
            {
                "left" => XLAlignmentHorizontalValues.Left,
                "center" => XLAlignmentHorizontalValues.Center,
                "right" => XLAlignmentHorizontalValues.Right,
                "justify" => XLAlignmentHorizontalValues.Justify,
                _ => XLAlignmentHorizontalValues.General
            };
        }

        private static XLAlignmentVerticalValues ParseVerticalAlignment(string align)
        {
            return align.ToLower() switch
            {
                "top" => XLAlignmentVerticalValues.Top,
                "center" => XLAlignmentVerticalValues.Center,
                "bottom" => XLAlignmentVerticalValues.Bottom,
                "justify" => XLAlignmentVerticalValues.Justify,
                _ => XLAlignmentVerticalValues.Center
            };
        }

        private static XLBorderStyleValues ParseBorderStyle(string style)
        {
            return style.ToLower() switch
            {
                "none" => XLBorderStyleValues.None,
                "thin" => XLBorderStyleValues.Thin,
                "medium" => XLBorderStyleValues.Medium,
                "thick" => XLBorderStyleValues.Thick,
                "double" => XLBorderStyleValues.Double,
                "dotted" => XLBorderStyleValues.Dotted,
                "dashed" => XLBorderStyleValues.Dashed,
                _ => XLBorderStyleValues.Thin
            };
        }

        // ===== TYPOVANÉ HODNOTY =====
        public static int SetCellValue(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            int luaType = LuaType(L, 4);

            switch (luaType)
            {
                case LUA_TNUMBER:
                    if (IsLuaInteger(L, 4))
                        sheet.SetValue(row, col, ToLuaInteger(L, 4));
                    else
                        sheet.SetValue(row, col, ToLuaNumber(L, 4));
                    break;

                case LUA_TBOOLEAN:
                    sheet.SetValue(row, col, ToLuaBoolean(L, 4));
                    break;

                case LUA_TSTRING:
                    sheet.SetValue(row, col, ToLuaString(L, 4));
                    break;

                case LUA_TNIL:
                    sheet.SetValue(row, col, null);
                    break;

                default:
                    sheet.SetValue(row, col, ToLuaString(L, 4));
                    break;
            }

            return 0;
        }

        public static int GetCellValue(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var value = sheet.GetValue(row, col);

            switch (value)
            {
                case null:
                    PushLuaNil(L);
                    break;
                case bool b:
                    PushLuaBoolean(L, b);
                    break;
                case int i:
                    PushLuaInteger(L, i);
                    break;
                case double d:
                    PushLuaNumber(L, d);
                    break;
                case DateTime dt:
                    PushLuaNumber(L, dt.ToOADate());
                    break;
                default:
                    PushLuaString(L, value.ToString());
                    break;
            }

            return 1;
        }

        // Clear cell
        public static int ClearCell(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.ClearCell(row, col);
            return 0;
        }

        public static int ClearCellContents(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.ClearCellContents(row, col);
            return 0;
        }

        public static int ClearCellFormats(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.ClearCellFormats(row, col);
            return 0;
        }

        // ===== BACKGROUND COLOR =====
        public static int SetCellBackgroundColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string colorHex = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetBackgroundColorHex(row, col, colorHex);
            return 0;
        }

        public static int SetCellBackgroundColorRGB(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            int r = ToLuaInteger(L, 4);
            int g = ToLuaInteger(L, 5);
            int b = ToLuaInteger(L, 6);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetBackgroundColorRGB(row, col, r, g, b);
            return 0;
        }

        public static int SetCellBackgroundColorTheme(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string themeName = ToLuaString(L, 4);
            double tint = IsLuaNil(L, 5) ? 0.0 : ToLuaNumber(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetBackgroundColorTheme(row, col, themeName, tint);
            return 0;
        }

        public static int SetCellBackgroundColorIndexed(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            int colorIndex = ToLuaInteger(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetBackgroundColorIndexed(row, col, colorIndex);
            return 0;
        }

        // ===== FONT COLOR =====
        public static int SetCellFontColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string colorHex = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetFontColorHex(row, col, colorHex);
            return 0;
        }

        public static int SetCellFontColorRGB(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            int r = ToLuaInteger(L, 4);
            int g = ToLuaInteger(L, 5);
            int b = ToLuaInteger(L, 6);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetFontColorRGB(row, col, r, g, b);
            return 0;
        }

        public static int SetCellFontColorTheme(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string themeName = ToLuaString(L, 4);
            double tint = IsLuaNil(L, 5) ? 0.0 : ToLuaNumber(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetFontColorTheme(row, col, themeName, tint);
            return 0;
        }

        // ===== FONT FORMATTING =====
        public static int SetCellBold(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            bool bold = IsLuaNil(L, 4) ? true : ToLuaBoolean(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetBold(row, col, bold);
            return 0;
        }

        public static int SetCellItalic(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            bool italic = IsLuaNil(L, 4) ? true : ToLuaBoolean(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetItalic(row, col, italic);
            return 0;
        }

        public static int SetCellUnderline(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            bool underline = IsLuaNil(L, 4) ? true : ToLuaBoolean(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetUnderline(row, col, underline);
            return 0;
        }

        public static int SetCellStrikethrough(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            bool strikethrough = IsLuaNil(L, 4) ? true : ToLuaBoolean(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetStrikethrough(row, col, strikethrough);
            return 0;
        }

        public static int SetCellFontSize(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            double size = ToLuaNumber(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetFontSize(row, col, size);
            return 0;
        }

        public static int SetCellFontName(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string fontName = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetFontName(row, col, fontName);
            return 0;
        }

        // ===== ALIGNMENT =====
        public static int SetCellAlignment(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string hAlign = ToLuaString(L, 4);
            string vAlign = ToLuaString(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var h = ParseHorizontalAlignment(hAlign);
            var v = ParseVerticalAlignment(vAlign);

            sheet.SetAlignment(row, col, h, v);
            return 0;
        }

        public static int SetCellWrapText(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            bool wrap = IsLuaNil(L, 4) ? true : ToLuaBoolean(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetWrapText(row, col, wrap);
            return 0;
        }

        // ===== BORDERS =====
        public static int SetCellBorder(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string styleStr = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var style = ParseBorderStyle(styleStr);
            sheet.SetBorderAround(row, col, style);
            return 0;
        }

        // Jen vnější ohraničení
        public static int SetCellBorderOutside(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string styleStr = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var style = ParseBorderStyle(styleStr);
            sheet.SetBorderOutside(row, col, style);
            return 0;
        }

        // Jednotlivé strany
        public static int SetCellBorderTop(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string styleStr = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var style = ParseBorderStyle(styleStr);
            sheet.SetBorderTop(row, col, style);
            return 0;
        }

        public static int SetCellBorderBottom(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string styleStr = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var style = ParseBorderStyle(styleStr);
            sheet.SetBorderBottom(row, col, style);
            return 0;
        }

        public static int SetCellBorderLeft(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string styleStr = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var style = ParseBorderStyle(styleStr);
            sheet.SetBorderLeft(row, col, style);
            return 0;
        }

        public static int SetCellBorderRight(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string styleStr = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var style = ParseBorderStyle(styleStr);
            sheet.SetBorderRight(row, col, style);
            return 0;
        }


        // ===== NUMBER FORMAT =====
        public static int SetCellNumberFormat(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string format = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetNumberFormat(row, col, format);
            return 0;
        }

        // ===== RANGE OPERATIONS =====
        public static int SetRangeBackgroundColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int startRow = ToLuaInteger(L, 2);
            int startCol = ToLuaInteger(L, 3);
            int endRow = ToLuaInteger(L, 4);
            int endCol = ToLuaInteger(L, 5);
            string colorHex = ToLuaString(L, 6);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRangeBackgroundColorHex(startRow, startCol, endRow, endCol, colorHex);
            return 0;
        }

        public static int SetRangeBackgroundColorRGB(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int startRow = ToLuaInteger(L, 2);
            int startCol = ToLuaInteger(L, 3);
            int endRow = ToLuaInteger(L, 4);
            int endCol = ToLuaInteger(L, 5);
            int r = ToLuaInteger(L, 6);
            int g = ToLuaInteger(L, 7);
            int b = ToLuaInteger(L, 8);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRangeBackgroundColorRGB(startRow, startCol, endRow, endCol, r, g, b);
            return 0;
        }

        public static int SetRangeBold(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int startRow = ToLuaInteger(L, 2);
            int startCol = ToLuaInteger(L, 3);
            int endRow = ToLuaInteger(L, 4);
            int endCol = ToLuaInteger(L, 5);
            bool bold = IsLuaNil(L, 6) ? true : ToLuaBoolean(L, 6);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRangeBold(startRow, startCol, endRow, endCol, bold);
            return 0;
        }

        public static int SetRangeBorder(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int startRow = ToLuaInteger(L, 2);
            int startCol = ToLuaInteger(L, 3);
            int endRow = ToLuaInteger(L, 4);
            int endCol = ToLuaInteger(L, 5);
            string styleStr = ToLuaString(L, 6);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            var style = ParseBorderStyle(styleStr);
            sheet.SetRangeBorder(startRow, startCol, endRow, endCol, style);
            return 0;
        }

        public static int MergeRange(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int startRow = ToLuaInteger(L, 2);
            int startCol = ToLuaInteger(L, 3);
            int endRow = ToLuaInteger(L, 4);
            int endCol = ToLuaInteger(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.MergeCells(startRow, startCol, endRow, endCol);
            return 0;
        }

        // ===== COLUMN/ROW OPERATIONS =====
        public static int SetColumnWidth(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);
            double width = ToLuaNumber(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetColumnWidth(col, width);
            return 0;
        }

        public static int AutoFitColumn(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.AutoFitColumn(col);
            return 0;
        }

        public static int SetRowHeight(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            double height = ToLuaNumber(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRowHeight(row, height);
            return 0;
        }

        public static int DeleteRow(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.DeleteRow(row);
            return 0;
        }

        public static int DeleteColumn(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.DeleteColumn(col);
            return 0;
        }

        public static int InsertRowAbove(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.InsertRowAbove(row);
            return 0;
        }

        public static int InsertColumnBefore(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.InsertColumnBefore(col);
            return 0;
        }

        // ===== ROW FORMATTING =====
        public static int SetRowBackgroundColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            string colorHex = ToLuaString(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRowBackgroundColorHex(row, colorHex);
            return 0;
        }

        public static int SetRowBackgroundColorRGB(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int r = ToLuaInteger(L, 3);
            int g = ToLuaInteger(L, 4);
            int b = ToLuaInteger(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRowBackgroundColorRGB(row, r, g, b);
            return 0;
        }

        public static int SetRowBold(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            bool bold = IsLuaNil(L, 3) ? true : ToLuaBoolean(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRowBold(row, bold);
            return 0;
        }

        public static int SetRowFontColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            string colorHex = ToLuaString(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRowFontColorHex(row, colorHex);
            return 0;
        }

        public static int SetRowFontColorRGB(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int r = ToLuaInteger(L, 3);
            int g = ToLuaInteger(L, 4);
            int b = ToLuaInteger(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetRowFontColorRGB(row, r, g, b);
            return 0;
        }

        // ===== COLUMN FORMATTING =====
        public static int SetColumnBackgroundColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);
            string colorHex = ToLuaString(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetColumnBackgroundColorHex(col, colorHex);
            return 0;
        }

        public static int SetColumnBackgroundColorRGB(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);
            int r = ToLuaInteger(L, 3);
            int g = ToLuaInteger(L, 4);
            int b = ToLuaInteger(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetColumnBackgroundColorRGB(col, r, g, b);
            return 0;
        }

        public static int SetColumnBold(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);
            bool bold = IsLuaNil(L, 3) ? true : ToLuaBoolean(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetColumnBold(col, bold);
            return 0;
        }

        public static int SetColumnFontColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);
            string colorHex = ToLuaString(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetColumnFontColorHex(col, colorHex);
            return 0;
        }

        public static int SetColumnFontColorRGB(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);
            int r = ToLuaInteger(L, 3);
            int g = ToLuaInteger(L, 4);
            int b = ToLuaInteger(L, 5);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetColumnFontColorRGB(col, r, g, b);
            return 0;
        }

        public static int FreezePanes(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.FreezePanes(row, col);
            return 0;
        }

        public static int FreezeRows(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int count = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.FreezeRows(count);
            return 0;
        }

        public static int FreezeColumns(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int count = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.FreezeColumns(count);
            return 0;
        }

        public static int UnfreezePanes(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.UnfreezePanes();
            return 0;
        }



        /// Cell Funcitons


        public static int SetCell(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string value = ToLuaString(L, 4);


            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) ||
                !sheets[parts[0]].ContainsKey(parts[1]))
            {
                Console.WriteLine($"⚠️ Incorret sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            sheet.SetCell(row, col, value);

            return 0;
        }

        public static int GetCell(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) ||
                !sheets[parts[0]].ContainsKey(parts[1]))
            {
                Console.WriteLine($"⚠️ Incorret sheet '{sheetId}'!");
                return 0;
            }

            var sheet = sheets[parts[0]][parts[1]];
            string cellValue = (sheet.GetCell(row, col));

            PushLuaString(L, cellValue); // Jinak vrátíme jako string


            return 1;
        }

        public static int GetCellEx(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            string[] parts = sheetId.Split(':', 2);
            if (parts.Length != 2 || !sheets.ContainsKey(parts[0]) ||
                !sheets[parts[0]].ContainsKey(parts[1]))
            {
                Console.WriteLine($"⚠️ Invalid sheet ID '{sheetId}'");
                PushLuaNil(L);
                return 1;
            }

            var sheet = sheets[parts[0]][parts[1]];
            var data = sheet.GetCellEx(row, col); // => Dictionary<string, object?>

            NewTable(L);

            foreach (var kvp in data)
            {
                PushLuaString(L, kvp.Key);

                switch (kvp.Value)
                {
                    case null:
                        PushLuaNil(L);
                        break;

                    case bool b:
                        PushLuaBoolean(L, b);
                        break;

                    case int i:
                        PushLuaInteger(L, i);
                        break;

                    case double d:
                        PushLuaNumber(L, d);
                        break;

                    case float f:
                        PushLuaNumber(L, f);
                        break;

                    case Dictionary<string, object> nestedDict:
                        PushLuaTable(L, nestedDict);
                        break;

                    case List<Dictionary<string, object>> listOfDicts:
                        NewTable(L);
                        int index = 1;
                        foreach (var item in listOfDicts)
                        {
                            PushLuaInteger(L, index++);
                            PushLuaTable(L, item);
                            SetTable(L, -3);
                        }
                        break;

                    default:
                        PushLuaString(L, kvp.Value.ToString());
                        break;
                }

                SetTable(L, -3);
            }


            return 1;
        }



        // ===== FORMULA =====
        public static int SetCellFormula(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);
            string formula = ToLuaString(L, 4);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            sheet.SetFormula(row, col, formula);
            return 0;
        }

        public static int GetCellFormula(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            string formula = sheet.GetFormula(row, col);
            if (formula != null)
            {
                PushLuaString(L, formula);
                return 1;
            }

            return 0;
        }

        public static int HasCellFormula(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaBoolean(L, sheet.HasFormula(row, col));
            return 1;
        }

        // ===== GET COLORS =====
        public static int GetCellBackgroundColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            string color = sheet.GetBackgroundColor(row, col);
            if (color != null)
            {
                PushLuaString(L, color);
                return 1;
            }

            return 0;
        }

        public static int GetCellFontColor(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            string color = sheet.GetFontColor(row, col);
            if (color != null)
            {
                PushLuaString(L, color);
                return 1;
            }

            return 0;
        }

        // ===== GET FONT =====
        public static int GetCellBold(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaBoolean(L, sheet.GetBold(row, col));
            return 1;
        }

        public static int GetCellItalic(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaBoolean(L, sheet.GetItalic(row, col));
            return 1;
        }

        public static int GetCellUnderline(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaBoolean(L, sheet.GetUnderline(row, col));
            return 1;
        }

        public static int GetCellStrikethrough(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaBoolean(L, sheet.GetStrikethrough(row, col));
            return 1;
        }

        public static int GetCellFontSize(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaNumber(L, sheet.GetFontSize(row, col));
            return 1;
        }

        public static int GetCellFontName(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetFontName(row, col));
            return 1;
        }

        // ===== GET ALIGNMENT =====
        public static int GetCellHorizontalAlignment(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetHorizontalAlignment(row, col));
            return 1;
        }

        public static int GetCellVerticalAlignment(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetVerticalAlignment(row, col));
            return 1;
        }

        public static int GetCellWrapText(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaBoolean(L, sheet.GetWrapText(row, col));
            return 1;
        }

        // ===== GET BORDERS =====
        public static int GetCellBorderTop(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetBorderTop(row, col));
            return 1;
        }

        public static int GetCellBorderBottom(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetBorderBottom(row, col));
            return 1;
        }

        public static int GetCellBorderLeft(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetBorderLeft(row, col));
            return 1;
        }

        public static int GetCellBorderRight(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetBorderRight(row, col));
            return 1;
        }

        // ===== GET NUMBER FORMAT =====
        public static int GetCellNumberFormat(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaString(L, sheet.GetNumberFormat(row, col));
            return 1;
        }

        // ===== GET DIMENSIONS =====
        public static int GetColumnWidth(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int col = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaNumber(L, sheet.GetColumnWidth(col));
            return 1;
        }

        public static int GetRowHeight(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaNumber(L, sheet.GetRowHeight(row));
            return 1;
        }

        // ===== GET MERGED =====
        public static int IsCellMerged(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            PushLuaBoolean(L, sheet.IsMerged(row, col));
            return 1;
        }

        public static int GetCellMergedRange(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            string range = sheet.GetMergedRange(row, col);
            if (range != null)
            {
                PushLuaString(L, range);
                return 1;
            }

            return 0;
        }


        // ===== CELL RECALCULATE =====
        public static int CalculateCell(IntPtr L)
        {
            string sheetId = ToLuaString(L, 1);
            int row = ToLuaInteger(L, 2);
            int col = ToLuaInteger(L, 3);

            var sheet = GetSheet(sheetId);
            if (sheet == null) return 0;

            try
            {
                sheet.CalculateCell(row, col);
                return 0;
            }
            catch (Exception ex)
            {
                PushLuaError(L, $"❌ Error calculating cell: {ex.Message}");
                return 0;
            }
        }

    }
}

