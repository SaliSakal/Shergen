using Shergen.Lua;
using System.Globalization;
using System.Runtime.Intrinsics.Arm;

namespace Shergen.XLSX
{
    public class Module : IShergenModule
    {

        public void Register(Shergen engine)
        {
            engine.RegisterLuaFTable("XLSX", new LuaTableEntry[]
            {
                // Workbook-level functions
                new LuaFunctionEntry { Name = "Load",   Function = ExcelManager.LoadXLSX },
                new LuaFunctionEntry { Name = "Save",   Function = ExcelManager.SaveXLSX },
                new LuaFunctionEntry { Name = "SaveAs", Function = ExcelManager.SaveAsXLSX},
                new LuaFunctionEntry { Name = "WorkbookExists", Function = ExcelManager.TableExists },
                /////////////////////////////            (NOVÉ)
                new LuaFunctionEntry { Name = "New", Function = ExcelManager.NewXLSX },
                new LuaFunctionEntry { Name = "AddSheet", Function = ExcelManager.AddSheet },
                new LuaFunctionEntry { Name = "DeleteSheet", Function = ExcelManager.DeleteSheet },
                new LuaFunctionEntry { Name = "RenameSheet", Function = ExcelManager.RenameSheet },

                // Sheet-level functions
                new LuaFunctionEntry { Name = "GetSheetNames",   Function = ExcelManager.GetSheetNames },
                new LuaFunctionEntry { Name = "ContainSheet",    Function = ExcelManager.ContainSheet },
                new LuaFunctionEntry { Name = "GetSheet",    Function = ExcelManager.GetSheetObject },

                new LuaFunctionEntry { Name = "CloseWorkbook",   Function = ExcelManager.CloseWorkbook },
                new LuaFunctionEntry { Name = "CloseAllWorkbooks",   Function = ExcelManager.CloseAllWorkbooks },

                // Row formatting
                new LuaFunctionEntry { Name = "SetRowBackgroundColor", Function = ExcelManager.SetRowBackgroundColor },
                new LuaFunctionEntry { Name = "SetRowBackgroundColorRGB", Function = ExcelManager.SetRowBackgroundColorRGB },
                new LuaFunctionEntry { Name = "SetRowBold", Function = ExcelManager.SetRowBold },
                new LuaFunctionEntry { Name = "SetRowFontColor", Function = ExcelManager.SetRowFontColor },
                new LuaFunctionEntry { Name = "SetRowFontColorRGB", Function = ExcelManager.SetRowFontColorRGB },

                // Column formatting
                new LuaFunctionEntry { Name = "SetColumnBackgroundColor", Function = ExcelManager.SetColumnBackgroundColor },
                new LuaFunctionEntry { Name = "SetColumnBackgroundColorRGB", Function = ExcelManager.SetColumnBackgroundColorRGB },
                new LuaFunctionEntry { Name = "SetColumnBold", Function = ExcelManager.SetColumnBold },
                new LuaFunctionEntry { Name = "SetColumnFontColor", Function = ExcelManager.SetColumnFontColor },
                new LuaFunctionEntry { Name = "SetColumnFontColorRGB", Function = ExcelManager.SetColumnFontColorRGB },

                // FreezePanel
                new LuaFunctionEntry { Name = "FreezePanes", Function = ExcelManager.FreezePanes },
                new LuaFunctionEntry { Name = "FreezeRows", Function = ExcelManager.FreezeRows },
                new LuaFunctionEntry { Name = "FreezeColumns", Function = ExcelManager.FreezeColumns },
                new LuaFunctionEntry { Name = "UnfreezePanes", Function = ExcelManager.UnfreezePanes },
                /////////////////////////////            (NOVÉ)
                // Range Operations 
                new LuaFunctionEntry { Name = "SetRangeBackgroundColor", Function = ExcelManager.SetRangeBackgroundColor },
                new LuaFunctionEntry { Name = "SetRangeBackgroundColorRGB", Function = ExcelManager.SetRangeBackgroundColorRGB },
                new LuaFunctionEntry { Name = "SetRangeBold", Function = ExcelManager.SetRangeBold },
                new LuaFunctionEntry { Name = "SetRangeBorder", Function = ExcelManager.SetRangeBorder },
                new LuaFunctionEntry { Name = "MergeRange", Function = ExcelManager.MergeRange },

                // Column/Row Operations 
                new LuaFunctionEntry { Name = "SetColumnWidth", Function = ExcelManager.SetColumnWidth },
                new LuaFunctionEntry { Name = "AutoFitColumn", Function = ExcelManager.AutoFitColumn },
                new LuaFunctionEntry { Name = "SetRowHeight", Function = ExcelManager.SetRowHeight },
                new LuaFunctionEntry { Name = "DeleteRow", Function = ExcelManager.DeleteRow },
                new LuaFunctionEntry { Name = "DeleteColumn", Function = ExcelManager.DeleteColumn },
                new LuaFunctionEntry { Name = "InsertRowAbove", Function = ExcelManager.InsertRowAbove },
                new LuaFunctionEntry { Name = "InsertColumnBefore", Function = ExcelManager.InsertColumnBefore },

                // Get Dimensions
                new LuaFunctionEntry { Name = "GetColumnWidth", Function = ExcelManager.GetColumnWidth },
                new LuaFunctionEntry { Name = "GetRowHeight", Function = ExcelManager.GetRowHeight },

                new LuaFunctionEntry { Name = "GetSheetRange",   Function = ExcelManager.GetSheetRange },

                new LuaFunctionEntry { Name = "FindInSheet",   Function = ExcelManager.FindInSheet },
                new LuaFunctionEntry { Name = "FindAllInSheet",   Function = ExcelManager.FindAllInSheet },
                new LuaFunctionEntry { Name = "FindInRow",   Function = ExcelManager.FindInRow },
                new LuaFunctionEntry { Name = "FindInColumn",   Function = ExcelManager.FindInColumn },
                new LuaFunctionEntry { Name = "FindAllInRow",   Function = ExcelManager.FindAllInRow },
                new LuaFunctionEntry { Name = "FindAllInColumn",   Function = ExcelManager.FindAllInColumn },
                new LuaFunctionEntry { Name = "FindInRange",   Function = ExcelManager.FindInRange },
                new LuaFunctionEntry { Name = "FindAllInRange",   Function = ExcelManager.FindAllInRange },
                // Cell-level functions
                // Basic Set/Get Cell
                new LuaFunctionEntry { Name = "GetCellEx",   Function = ExcelManager.GetCellEx },
                new LuaFunctionEntry { Name = "SetCell",   Function = ExcelManager.SetCell },
                new LuaFunctionEntry { Name = "GetCell",   Function = ExcelManager.GetCell },

                /////////////////////////////            (NOVÉ)
                // Typované hodnoty 
                // Set/Get Cell Value
                new LuaFunctionEntry { Name = "SetCellValue", Function = ExcelManager.SetCellValue },
                new LuaFunctionEntry { Name = "GetCellValue", Function = ExcelManager.GetCellValue },

                // Background Colors 
                // Set Cell Background Color
                new LuaFunctionEntry { Name = "SetCellBackgroundColor", Function = ExcelManager.SetCellBackgroundColor },
                new LuaFunctionEntry { Name = "SetCellBackgroundColorRGB", Function = ExcelManager.SetCellBackgroundColorRGB },
                new LuaFunctionEntry { Name = "SetCellBackgroundColorTheme", Function = ExcelManager.SetCellBackgroundColorTheme },
                new LuaFunctionEntry { Name = "SetCellBackgroundColorIndexed", Function = ExcelManager.SetCellBackgroundColorIndexed },

                // Font Colors
                new LuaFunctionEntry { Name = "SetCellFontColor", Function = ExcelManager.SetCellFontColor },
                new LuaFunctionEntry { Name = "SetCellFontColorRGB", Function = ExcelManager.SetCellFontColorRGB },
                new LuaFunctionEntry { Name = "SetCellFontColorTheme", Function = ExcelManager.SetCellFontColorTheme },

                // Font Formatting 
                new LuaFunctionEntry { Name = "SetCellBold", Function = ExcelManager.SetCellBold },
                new LuaFunctionEntry { Name = "SetCellItalic", Function = ExcelManager.SetCellItalic },
                new LuaFunctionEntry { Name = "SetCellUnderline", Function = ExcelManager.SetCellUnderline },
                new LuaFunctionEntry { Name = "SetCellStrikethrough", Function = ExcelManager.SetCellStrikethrough },
                new LuaFunctionEntry { Name = "SetCellFontSize", Function = ExcelManager.SetCellFontSize },
                new LuaFunctionEntry { Name = "SetCellFontName", Function = ExcelManager.SetCellFontName },

                // Alignment 
                new LuaFunctionEntry { Name = "SetCellAlignment", Function = ExcelManager.SetCellAlignment },
                new LuaFunctionEntry { Name = "SetCellWrapText", Function = ExcelManager.SetCellWrapText },

                // Borders 
                new LuaFunctionEntry { Name = "SetCellBorder", Function = ExcelManager.SetCellBorder },
                new LuaFunctionEntry { Name = "SetCellBorderOutside", Function = ExcelManager.SetCellBorderOutside },
                new LuaFunctionEntry { Name = "SetCellBorderTop", Function = ExcelManager.SetCellBorderTop },
                new LuaFunctionEntry { Name = "SetCellBorderBottom", Function = ExcelManager.SetCellBorderBottom },
                new LuaFunctionEntry { Name = "SetCellBorderLeft", Function = ExcelManager.SetCellBorderLeft },
                new LuaFunctionEntry { Name = "SetCellBorderRight", Function = ExcelManager.SetCellBorderRight },

                // Number Format
                new LuaFunctionEntry { Name = "SetCellNumberFormat", Function = ExcelManager.SetCellNumberFormat },



                // Clear Functions
                new LuaFunctionEntry { Name = "ClearCell", Function = ExcelManager.ClearCell },
                new LuaFunctionEntry { Name = "ClearCellContents", Function = ExcelManager.ClearCellContents },
                new LuaFunctionEntry { Name = "ClearCellFormats", Function = ExcelManager.ClearCellFormats },


                // Formula
                new LuaFunctionEntry { Name = "SetCellFormula", Function = ExcelManager.SetCellFormula },
                new LuaFunctionEntry { Name = "GetCellFormula", Function = ExcelManager.GetCellFormula },
                new LuaFunctionEntry { Name = "HasCellFormula", Function = ExcelManager.HasCellFormula },

                // Get Colors
                new LuaFunctionEntry { Name = "GetCellBackgroundColor", Function = ExcelManager.GetCellBackgroundColor },
                new LuaFunctionEntry { Name = "GetCellFontColor", Function = ExcelManager.GetCellFontColor },

                // Get Font
                new LuaFunctionEntry { Name = "GetCellBold", Function = ExcelManager.GetCellBold },
                new LuaFunctionEntry { Name = "GetCellItalic", Function = ExcelManager.GetCellItalic },
                new LuaFunctionEntry { Name = "GetCellUnderline", Function = ExcelManager.GetCellUnderline },
                new LuaFunctionEntry { Name = "GetCellStrikethrough", Function = ExcelManager.GetCellStrikethrough },
                new LuaFunctionEntry { Name = "GetCellFontSize", Function = ExcelManager.GetCellFontSize },
                new LuaFunctionEntry { Name = "GetCellFontName", Function = ExcelManager.GetCellFontName },

                // Get Alignment
                new LuaFunctionEntry { Name = "GetCellHorizontalAlignment", Function = ExcelManager.GetCellHorizontalAlignment },
                new LuaFunctionEntry { Name = "GetCellVerticalAlignment", Function = ExcelManager.GetCellVerticalAlignment },
                new LuaFunctionEntry { Name = "GetCellWrapText", Function = ExcelManager.GetCellWrapText },

                // Get Borders
                new LuaFunctionEntry { Name = "GetCellBorderTop", Function = ExcelManager.GetCellBorderTop },
                new LuaFunctionEntry { Name = "GetCellBorderBottom", Function = ExcelManager.GetCellBorderBottom },
                new LuaFunctionEntry { Name = "GetCellBorderLeft", Function = ExcelManager.GetCellBorderLeft },
                new LuaFunctionEntry { Name = "GetCellBorderRight", Function = ExcelManager.GetCellBorderRight },

                // Get Number Format
                new LuaFunctionEntry { Name = "GetCellNumberFormat", Function = ExcelManager.GetCellNumberFormat },



                // Get Merged
                new LuaFunctionEntry { Name = "IsCellMerged", Function = ExcelManager.IsCellMerged },
                new LuaFunctionEntry { Name = "GetCellMergedRange", Function = ExcelManager.GetCellMergedRange },



                // Recalculate formulas
                new LuaFunctionEntry { Name = "CalculateCell", Function = ExcelManager.CalculateCell },
                new LuaFunctionEntry { Name = "RecalculateSheet", Function = ExcelManager.RecalculateSheet },
                new LuaFunctionEntry { Name = "Recalculate", Function = ExcelManager.RecalculateWorkbook },


                new LuaValueEntry { Name = "Ver",  Value = "1.3" },

            });

        }
    }
}
