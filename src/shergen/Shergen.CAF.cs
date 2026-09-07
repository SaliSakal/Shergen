using Shergen.Lua;


namespace Shergen
{
    public partial class Shergen
    {
        private void RegisterGUIFunctionsAConstants()
        {
            //  Registrace funkcí...
            Console.WriteLine("🔗 Registering GUI functions and Constants...");

            // Běžné funkce
            //mainLua.RegisterNewFunction("Shergen_reset", ResetShergen);

            RegisterLuaFTable("GUI", new LuaTableEntry[]
            {
                //UI funkce
                new LuaFunctionEntry { Name = "NewElement", Function = NewElement },
                new LuaFunctionEntry { Name = "DebugListOfElements", Function = UIElement.DebugListOfElements },
                new LuaFunctionEntry { Name = "GetChildrenIDs", Function = GetChildrenIDs },
                new LuaFunctionEntry { Name = "DestroyElement", Function = DestroyElement },
                new LuaFunctionEntry { Name = "DestroyChildren", Function = DestroyChildren },



                // Elements property
                new LuaFunctionEntry { Name = "SetProperty", Function = SetProperty },

                new LuaFunctionEntry { Name = "SetAnchor", Function = SetAnchor },

                new LuaFunctionEntry { Name = "GetProperty", Function = GetProperty },

                new LuaFunctionEntry { Name = "GetParentID", Function = GetParentID },
                new LuaFunctionEntry { Name = "SetParent", Function = SetParent },
                new LuaFunctionEntry { Name = "BringToFront", Function = BringToFront },
                new LuaFunctionEntry { Name = "BringToBack", Function = BringToBack },
                new LuaFunctionEntry { Name = "BringBy", Function = BringBy },

                // Font defaults & fallback
                // GUI.SetDefaultFont("DFF", "MyFont.DFF")  or  GUI.SetDefaultFont("TTF", "MyFont.ttf")
                new LuaFunctionEntry { Name = "SetDefaultFont",   Function = SetDefaultFont   },
                // GUI.SetFontFallback("MyFont.DFF", "□")
                new LuaFunctionEntry { Name = "SetFontFallback",  Function = SetFontFallback  },

                new LuaFunctionEntry { Name = "RegisterIcon", Function = RegisterIcon },
                new LuaFunctionEntry { Name = "UnregisterIcon", Function = UnregisterIcon },
                // Input
                new LuaFunctionEntry { Name = "SetCallback", Function = SetCallback },
                new LuaFunctionEntry { Name = "SetFocus",    Function = SetFocus    },

                // Cursor
                new LuaFunctionEntry { Name = "GetCursorID",       Function = GetCursorID       },
                new LuaFunctionEntry { Name = "SetCursor",         Function = SetCursor         },
                new LuaFunctionEntry { Name = "SetCursorTexture",  Function = SetCursorTexture  },
                new LuaFunctionEntry { Name = "ResetCursor",       Function = ResetCursor       },
                new LuaFunctionEntry { Name = "GetCursorHotSpot",  Function = GetCursorHotSpot  },
                new LuaFunctionEntry { Name = "SetCursorPos",      Function = SetCursorPos      },
                new LuaFunctionEntry { Name = "SetCursorLocked",   Function = SetCursorLocked   },
                new LuaFunctionEntry { Name = "SetCursorHidden",   Function = SetCursorHidden   },
                new LuaFunctionEntry { Name = "SetCursorCaptured", Function = SetCursorCaptured },

                // Event data getters — call inside a callback to get current event values
                new LuaFunctionEntry { Name = "GetMouseX",      Function = GetMouseX      },
                new LuaFunctionEntry { Name = "GetMouseY",      Function = GetMouseY      },
                new LuaFunctionEntry { Name = "GetMouseDeltaX", Function = GetMouseDeltaX },
                new LuaFunctionEntry { Name = "GetMouseDeltaY", Function = GetMouseDeltaY },
                new LuaFunctionEntry { Name = "GetMouseLocalX", Function = GetMouseLocalX },
                new LuaFunctionEntry { Name = "GetMouseLocalY", Function = GetMouseLocalY },

                new LuaFunctionEntry { Name = "GetButton",  Function = GetButton  },
                new LuaFunctionEntry { Name = "GetScroll",  Function = GetScroll  },
                new LuaFunctionEntry { Name = "GetKey",     Function = GetKey     },
                new LuaFunctionEntry { Name = "GetChar",    Function = GetChar    },

                new LuaFunctionEntry { Name = "IsKeyPressed", Function = IsKeyPressed},
                new LuaFunctionEntry { Name = "IsMousePressed", Function = IsMousePressed},

                // Tick & resize callbacks
                new LuaFunctionEntry { Name = "RegisterTickCallback",   Function = RegisterTickCallback   },
                new LuaFunctionEntry { Name = "UnregisterTickCallback", Function = UnregisterTickCallback },
                new LuaFunctionEntry { Name = "SetResizeCallback",      Function = SetResizeCallback      },

                // App
                new LuaFunctionEntry { Name = "Exit", Function = Exit },

                // TextLog
                new LuaFunctionEntry { Name = "AddLine",    Function = AddLine    },
                new LuaFunctionEntry { Name = "ClearLines", Function = ClearLines },

            });

            RegisterLuaFTable("File", new LuaTableEntry[]
            {
                new LuaFunctionEntry { Name = "Save", Function = FileManager.SaveFile },
                new LuaFunctionEntry { Name = "Load", Function = FileManager.LoadFile },
                new LuaFunctionEntry { Name = "Exists", Function = FileManager.FileExists },
                new LuaFunctionEntry { Name = "GetFiles", Function = FileManager.GetFilesLua },
                new LuaFunctionEntry { Name = "GetDirs", Function = FileManager.GetDirsLua },
            });

            // Typy elemetů -  UIElementType

            RegisterLuaFTable("TYPE", new LuaTableEntry[]
            {
                new LuaValueEntry { Name = "ELEMENT", Value = UIElementType.Element },
                new LuaValueEntry { Name = "LABEL", Value = UIElementType.Label },
                new LuaValueEntry { Name = "SCROLLBOX", Value = UIElementType.ScrollBox },
                new LuaValueEntry { Name = "SCROLLBAR", Value = UIElementType.ScrollBar },
                new LuaValueEntry { Name = "TEXTLOG",   Value = UIElementType.TextLog   },
                new LuaValueEntry { Name = "TEXTFIELD", Value = UIElementType.TextField},
                //new LuaValueEntry { Name = "TEXTBOX", Value = UIElementType.ScrollText },

            });

            RegisterLuaFTable("ALIGN", new LuaTableEntry[]
            {
                new LuaValueEntry { Name = "TOP", Value = UIAling.Top },
                new LuaValueEntry { Name = "LEFT", Value = UIAling.Left },
                new LuaValueEntry { Name = "MIDDLE", Value = UIAling.Middle },
                new LuaValueEntry { Name = "RIGHT", Value = UIAling.Right },
                new LuaValueEntry { Name = "BOTTOM", Value = UIAling.Bottom },
            });

            RegisterLuaFTable("BORDER_TYPE", new LuaTableEntry[]
            {
                new LuaValueEntry { Name = "NONE", Value = UIBorder.None },
                new LuaValueEntry { Name = "INNER", Value = UIBorder.Inner },
                new LuaValueEntry { Name = "OUTER", Value = UIBorder.Outer },
            });



            RegisterLuaFTable("PROP", new LuaTableEntry[]
            {
                // Base
                new LuaValueEntry { Name = "X", Value = UIProperty.X },
                new LuaValueEntry { Name = "Y", Value = UIProperty.Y },
                new LuaValueEntry { Name = "WIDTH", Value = UIProperty.Width },
                new LuaValueEntry { Name = "HEIGHT", Value = UIProperty.Height },
                new LuaValueEntry { Name = "VISIBLE", Value = UIProperty.Visible },
                new LuaValueEntry { Name = "SCISSOR", Value = UIProperty.Scissor },
                new LuaValueEntry { Name = "SCISSORMASK", Value = UIProperty.ScissorMask },
                new LuaValueEntry { Name = "SCISSORMASK_THRESHOLD", Value = UIProperty.ScissorMaskThreshold },
                new LuaValueEntry { Name = "CANFOCUS", Value = UIProperty.CanFocus },
                new LuaValueEntry { Name = "FADE", Value = UIProperty.Fade },
                new LuaValueEntry { Name = "PASSTHROUGH", Value = UIProperty.PassThrough },
                new LuaValueEntry { Name = "EVENTCAPTURE", Value = UIProperty.EventCapture },
                new LuaValueEntry { Name = "HITMASK", Value = UIProperty.HitMask },
                new LuaValueEntry { Name = "COLOR1", Value = UIProperty.Color1 },
                new LuaValueEntry { Name = "COLOR2", Value = UIProperty.Color2 },
                new LuaValueEntry { Name = "COLOR3", Value = UIProperty.Color3 },
                new LuaValueEntry { Name = "BORDER_SIZE", Value = UIProperty.Border_Size },
                new LuaValueEntry { Name = "BORDER_TYPE", Value = UIProperty.Border_Type },
                new LuaValueEntry { Name = "BORDER_COLOR", Value = UIProperty.Border_Color },
                new LuaValueEntry { Name = "FALLBACK_TEXTURE", Value = UIProperty.Texture_Fallback },
                new LuaValueEntry { Name = "FALLBACK_TEXTURE2", Value = UIProperty.Texture2_Fallback },
                new LuaValueEntry { Name = "FALLBACK_TEXTURE3", Value = UIProperty.Texture3_Fallback },
                new LuaValueEntry { Name = "TEXTURE_ID", Value = UIProperty.TextureID },
                new LuaValueEntry { Name = "TEXTURE2_ID", Value = UIProperty.Texture2ID },
                new LuaValueEntry { Name = "TEXTURE3_ID", Value = UIProperty.Texture3ID },
                new LuaValueEntry { Name = "TEXTURE", Value = UIProperty.Texture },
                new LuaValueEntry { Name = "TEXTURE2", Value = UIProperty.Texture2 },
                new LuaValueEntry { Name = "TEXTURE3", Value = UIProperty.Texture3 },
                new LuaValueEntry { Name = "SUBCOORDS", Value = UIProperty.SubCoords},
                new LuaValueEntry { Name = "TEXTURE_WIDTH", Value = UIProperty.TextureWidth},
                new LuaValueEntry { Name = "TEXTURE_HEIGHT", Value = UIProperty.TextureHeight},
                new LuaValueEntry { Name = "TEXTURE2_WIDTH", Value = UIProperty.Texture2Width},
                new LuaValueEntry { Name = "TEXTURE2_HEIGHT", Value = UIProperty.Texture2Height},
                new LuaValueEntry { Name = "TEXTURE3_WIDTH", Value = UIProperty.Texture3Width},
                new LuaValueEntry { Name = "TEXTURE3_HEIGHT", Value = UIProperty.Texture3Height},
                new LuaValueEntry { Name = "GRAYSCALE" , Value = UIProperty.Grayscale},
                // Font
                new LuaValueEntry { Name = "FONT_COLOR", Value = UIProperty.Font_Color },
                new LuaValueEntry { Name = "FONT_COLOR_BACK", Value = UIProperty.Font_Col_Back },
                new LuaValueEntry { Name = "FONT_SIZE", Value = UIProperty.Font_Size },
                new LuaValueEntry { Name = "AUTO_SIZE_HEIGHT", Value = UIProperty.AutoSizeHeight},
                new LuaValueEntry { Name = "AUTO_SIZE_WIDTH", Value = UIProperty.AutoSizeWidth},
                new LuaValueEntry { Name = "AUTO_SIZE_HEIGHT_MAX", Value = UIProperty.AutoSizeHeight_Max},
                new LuaValueEntry { Name = "AUTO_SIZE_WIDTH_MAX", Value = UIProperty.AutoSizeWidth_Max},

                new LuaValueEntry { Name = "TEXT", Value = UIProperty.Text },
                new LuaValueEntry { Name = "HINT", Value = UIProperty.Hint },
                new LuaValueEntry { Name = "HALIGN", Value = UIProperty.HAling },
                new LuaValueEntry { Name = "VALIGN", Value = UIProperty.VAling },
                new LuaValueEntry { Name = "ANCHOR", Value = UIProperty.Anchor },
                new LuaValueEntry { Name = "FONT_NAME", Value = UIProperty.Font },
                new LuaValueEntry { Name = "RANGE_MIN", Value = UIProperty.RangeMin },
                new LuaValueEntry { Name = "RANGE_MAX", Value = UIProperty.RangeMax },
                new LuaValueEntry { Name = "CHAR_SCALE_X", Value = UIProperty.CharScaleX },
                new LuaValueEntry { Name = "CHAR_SCALE_Y", Value = UIProperty.CharScaleY },
                new LuaValueEntry { Name = "CHAR_SPACING", Value = UIProperty.CharSpacing },
                new LuaValueEntry { Name = "LINE_SPACING", Value = UIProperty.LineSpacing },
                new LuaValueEntry { Name = "WORDWRAP", Value = UIProperty.WordWrap },
                new LuaValueEntry { Name = "SHADOW", Value = UIProperty.ShadowEnabled },
                new LuaValueEntry { Name = "SHADOW_OFFSET_X", Value = UIProperty.ShadowOffsetX },
                new LuaValueEntry { Name = "SHADOW_OFFSET_Y", Value = UIProperty.ShadowOffsetY },
                new LuaValueEntry { Name = "SHADOW_COLOR", Value = UIProperty.ShadowColor },
                new LuaValueEntry { Name = "OUTLINE", Value = UIProperty.OutlineEnabled },
                new LuaValueEntry { Name = "OUTLINE_WIDTH", Value = UIProperty.OutlineWidth },
                new LuaValueEntry { Name = "OUTLINE_COLOR", Value = UIProperty.OutlineColor },
                new LuaValueEntry { Name = "SCROLLTEXT", Value = UIProperty.ScrollText },
                new LuaValueEntry { Name = "SCROLLTEXT_SPEED", Value = UIProperty.ScrollText_Speed },
                new LuaValueEntry { Name = "SCROLLTEXT_DELAY", Value = UIProperty.ScrollText_Delay },
                
                // scrollbox properties
                new LuaValueEntry { Name = "SCROLLOFFSET_X",  Value = UIProperty.ScrollOffsetX  },
                new LuaValueEntry { Name = "SCROLLOFFSET_Y",  Value = UIProperty.ScrollOffsetY  },
                new LuaValueEntry { Name = "SCROLLBAR",     Value = UIProperty.ScrollBar     },
                new LuaValueEntry { Name = "SCROLLBAR2",     Value = UIProperty.ScrollBar2     },
                new LuaValueEntry { Name = "SCROLLBAR_TYPE",  Value = UIProperty.ScrollBarType  },
                new LuaValueEntry { Name = "HORIZONTALSCROLL", Value = UIProperty.ScrollBoxHorizontal },

                // textlog properties
                new LuaValueEntry { Name = "ADDATBOTTOM", Value = UIProperty.AddAtBottom },
                new LuaValueEntry { Name = "MAXLINES",    Value = UIProperty.MaxLines    },

                // textfield properties
                new LuaValueEntry { Name = "PLACEHOLDER", Value = UIProperty.PlaceHolder },
                new LuaValueEntry { Name = "SHOWRICH", Value = UIProperty.ShowRich },
                new LuaValueEntry { Name = "CURSORPOSITION", Value = UIProperty.CursorPosition },
                new LuaValueEntry { Name = "CURSORCOLOR", Value = UIProperty.CursorColor },
                new LuaValueEntry { Name = "CURSORSPEED", Value = UIProperty.CursorSpeed },
                new LuaValueEntry { Name = "READONLY", Value = UIProperty.ReadOnly },
                new LuaValueEntry { Name = "MAXLENGHT", Value = UIProperty.MaxLength },
                new LuaValueEntry { Name = "PASSWORDMODE", Value = UIProperty.PasswordMode },

                //new LuaValueEntry { Name = "", Value = UIProperty. },
                // cursor — hotspot se nastavuje přes GUI.SetCursor(path, x, y), ne přes PROP

            });

            // Mouse button indices (0-based, matches OpenTK MouseButton enum)
            RegisterLuaFTable("BUTTON", new LuaTableEntry[]
            {
                new LuaValueEntry { Name = "LEFT",    Value = 0 },
                new LuaValueEntry { Name = "RIGHT",   Value = 1 },
                new LuaValueEntry { Name = "MIDDLE",  Value = 2 },
                new LuaValueEntry { Name = "BUTTON4", Value = 3 },
                new LuaValueEntry { Name = "BUTTON5", Value = 4 },
            });

            // Callback type constants for GUI.SetCallback
            RegisterLuaFTable("CALLBACK", new LuaTableEntry[]
            {
                // Mouse hover / move
                new LuaValueEntry { Name = "MOUSEOVER",    Value = UICallback.MouseOver  },
                new LuaValueEntry { Name = "MOUSELEAVE",   Value = UICallback.MouseLeave },
                new LuaValueEntry { Name = "MOUSEMOVE",    Value = UICallback.MouseMove  },
                new LuaValueEntry { Name = "MOUSEWHEEL",   Value = UICallback.MouseWheel },

                // Left button
                new LuaValueEntry { Name = "LDOWN",        Value = UICallback.LDown      },
                new LuaValueEntry { Name = "LUP",          Value = UICallback.LUp        },
                new LuaValueEntry { Name = "LCLICK",       Value = UICallback.LClick     },
                new LuaValueEntry { Name = "LDBLCLICK",    Value = UICallback.LDblClick  },
                new LuaValueEntry { Name = "LDRAG",        Value = UICallback.LDrag      },

                // Right button
                new LuaValueEntry { Name = "RDOWN",        Value = UICallback.RDown      },
                new LuaValueEntry { Name = "RUP",          Value = UICallback.RUp        },
                new LuaValueEntry { Name = "RCLICK",       Value = UICallback.RClick     },
                new LuaValueEntry { Name = "RDBLCLICK",    Value = UICallback.RDblClick  },

                // Middle button
                new LuaValueEntry { Name = "MDOWN",        Value = UICallback.MDown      },
                new LuaValueEntry { Name = "MUP",          Value = UICallback.MUp        },
                new LuaValueEntry { Name = "MCLICK",       Value = UICallback.MClick     },
                new LuaValueEntry { Name = "MDBLCLICK",    Value = UICallback.MDblClick  },

                // Button 4
                new LuaValueEntry { Name = "B4DOWN",       Value = UICallback.B4Down     },
                new LuaValueEntry { Name = "B4UP",         Value = UICallback.B4Up       },
                new LuaValueEntry { Name = "B4CLICK",      Value = UICallback.B4Click    },
                new LuaValueEntry { Name = "B4DBLCLICK",   Value = UICallback.B4DblClick },

                // Button 5
                new LuaValueEntry { Name = "B5DOWN",       Value = UICallback.B5Down     },
                new LuaValueEntry { Name = "B5UP",         Value = UICallback.B5Up       },
                new LuaValueEntry { Name = "B5CLICK",      Value = UICallback.B5Click    },
                new LuaValueEntry { Name = "B5DBLCLICK",   Value = UICallback.B5DblClick },

                // Keyboard
                new LuaValueEntry { Name = "KEYDOWN",      Value = UICallback.KeyDown    },
                new LuaValueEntry { Name = "KEYUP",        Value = UICallback.KeyUp      },
                new LuaValueEntry { Name = "KEYPRESS",     Value = UICallback.KeyPress   },

                // Element state
                new LuaValueEntry { Name = "FOCUS",        Value = UICallback.Focus    },
                new LuaValueEntry { Name = "RESIZED",      Value = UICallback.Resized    },
                new LuaValueEntry { Name = "VISIBILITY",   Value = UICallback.Visibility },
                new LuaValueEntry { Name = "BLUR",         Value = UICallback.Blur     },
                new LuaValueEntry { Name = "ANIMEND",      Value = UICallback.AnimEnd    },

                // Touch events (if supported)
                new LuaValueEntry { Name = "TOUCHDOWN",   Value = UICallback.TouchDown   },
                new LuaValueEntry { Name = "TOUCHUP",     Value = UICallback.TouchUp     },
                new LuaValueEntry { Name = "TOUCHMOVE",   Value = UICallback.TouchMove   },
                new LuaValueEntry { Name = "TOUCHTAP",    Value = UICallback.TouchTap    },
                new LuaValueEntry { Name = "TOUCHDBLTAP", Value = UICallback.TouchDblTap },

                // Combined click/tap events
                new LuaValueEntry { Name = "CLICKTAP",     Value = UICallback.ClickTap    },
                new LuaValueEntry { Name = "DBLCLICKTAP",  Value = UICallback.DblClickTap },
            });

            RegisterLuaFTable("SCROLLBAR_TYPE", new LuaTableEntry[]
            {
                new LuaValueEntry { Name = "VERTICAL",   Value = UIScrollBarType.Vertical   },
                new LuaValueEntry { Name = "HORIZONTAL", Value = UIScrollBarType.Horizontal },
            });

            RegisterLuaConstant("LUA_VERSION", "5.5");
            RegisterLuaConstant("SHERGEN_VERSION", "0.7");


            RegisterLuaFTable("AUDIO", new LuaTableEntry[]
            {
                new LuaFunctionEntry { Name = "AddChannel",         Function = Audio.AddChannel },
                new LuaFunctionEntry { Name = "SetChannelVolume",   Function = Audio.SetChannelVolume },
                new LuaFunctionEntry { Name = "GetChannelVolume",   Function = Audio.GetChannelVolume },
                new LuaFunctionEntry { Name = "SetChannel3D",       Function = Audio.SetChannel3D },
                new LuaFunctionEntry { Name = "GetChannel3D",       Function = Audio.GetChannel3D },
                new LuaFunctionEntry { Name = "SetChannelDistance", Function = Audio.SetChannelDistance },
                new LuaFunctionEntry { Name = "GetChannelDistance", Function = Audio.GetChannelDistance },
                new LuaFunctionEntry { Name = "LoadSound",          Function = Audio.LoadSound },
                new LuaFunctionEntry { Name = "PlaySound",          Function = Audio.PlaySound },
                new LuaFunctionEntry { Name = "PauseSound",         Function = Audio.PauseSound },
                new LuaFunctionEntry { Name = "StopSound",          Function = Audio.StopSound },
                new LuaFunctionEntry { Name = "FreeSound",          Function = Audio.FreeSound },
                new LuaFunctionEntry { Name = "IsPlaying",          Function = Audio.IsPlaying },
                new LuaFunctionEntry { Name = "GetSoundTime",       Function = Audio.GetSoundTime },
                new LuaFunctionEntry { Name = "SetSoundTime",       Function = Audio.SetSoundTime },
                new LuaFunctionEntry { Name = "SetSoundPosition",   Function = Audio.SetSoundPosition },
                new LuaFunctionEntry { Name = "SetSoundVelocity",   Function = Audio.SetSoundVelocity },
                new LuaFunctionEntry { Name = "SetSoundPitch",      Function = Audio.SetSoundPitch },
                new LuaFunctionEntry { Name = "SetSoundLoop",       Function = Audio.SetSoundLoop },
                new LuaValueEntry { Name = "VERSION", Value= "1.0" },

            });



            string platform = OperatingSystem.IsAndroid() ? "ANDROID"
                            : OperatingSystem.IsWindows() ? "WINDOWS"
                            : OperatingSystem.IsMacOS()   ? "MACOS"
                            : OperatingSystem.IsLinux()   ? "LINUX"
                            : "UNKNOWN";
            bool isMobile = OperatingSystem.IsAndroid();
            string arch   = System.Runtime.InteropServices.RuntimeInformation
                                .ProcessArchitecture.ToString().ToUpperInvariant();

            RegisterLuaFTable("PLATFORM", new LuaTableEntry[]
            {
                new LuaValueEntry { Name = "OS",         Value = platform },
                new LuaValueEntry { Name = "IS_MOBILE",  Value = isMobile },
                new LuaValueEntry { Name = "IS_DESKTOP", Value = !isMobile },
                new LuaValueEntry { Name = "ARCH",       Value = arch },
                new LuaValueEntry { Name = "OS_VERSION", Value = Environment.OSVersion.VersionString },
            });


        }
    }
}


