eReg = ListClass.Make();
eReg:Set(0,{ID=0}); -- Desktop
eReg:Set(1,{ID=1}); -- Mouse
eReg:Set(2,{ID=2}); -- FPS Counter
eReg:Set(3,{ID=3}); -- Profiler

--[[
   AddElement(Input), GUI.SetProperty, GUI.SetProperty, GUI.SetCallback

   PŘÍKLAD:
      local btn = AddElement({
         -- Typ a rodič
         type    = TYPE.ELEMENT,         -- TYPE.ELEMENT | TYPE.LABEL | TYPE.SCROLLBOX | TYPE.SCROLLBAR | TYPE.TEXTLOG | TYPE.TEXTFIELD  (default: TYPE.ELEMENT)
         parent  = someElement,       -- reference na rodičovský element (default: Desktop)
         special = false,             -- speciální element (vždy vykreslen navrchu, neklikatelný, muže být dítětem pouze dalšího speciálu nebo desktopu)

         -- Pozice a velikost
         x = 0,  y = 0,
         width = 100,  height = 30,

         -- Viditelnost a průhlednost
         visible = true,
         fade    = 1.0,               -- průhlednost elementu i dětí (0.0 až 1.0)

         -- Ořez dětí
         scissor                  = false,   -- ořezávat děti obdélníkem tohoto elementu
         scissor_mask             = nil,     -- cesta k PNG (grayscale) pro tvarový ořez dětí
         scissor_mask_threshold   = 50,      -- práh průhlednosti pro scissor masku (0–255) - ořezává se co je mín než danná hodníota

         -- HitTest maska
         hit_mask               = nil,       -- cesta k textuře; alfa kanál určuje klikatelnou plochu
         hit_mask_use_grayscale = false,     -- zda hit_mask používá alfu (false) nebo grayscale (true)
         hit_mask_threshold     = 50,        -- práh hit testu pro hit masku (0–255) - ignoruje se co je mín než danná hodníota

         -- Kotvení  { left, right, top, bottom }
         anchor = { left=true, right=false, top=true, bottom=false },
                   zkratky - anchorLB, anchorLT, anchorL, anchorLR, anchorRB, anchorTR, 
                             anchorR, anchorT, anchorB, anchorTB, anchorNone, anchorLTRB, anchorLTB, 
                             nchorTRB, anchorLTR, anchorLRB 

         -- Focus
         can_focus = false,           -- zda element může přijímat focus (callback klávesnice)

         -- Myš / průchod událostí
         pass_through   = false,      -- události procházejí skrze element na elementy pod ním
         event_capture  = false,      -- zastaví průchod událostí z pass-through rodiče

         -- Barvy  { R, G, B, A}  (0–255)
         color  = RGBA(255, 255,255, 255 ),  -- hlavní barva / multiplikátor textury
         color2 = RGBA(255, 255,255, 0   ),  -- sekundární barva (texture2)
         color3 = RGBA(255, 255,255, 0   ),  -- terciální barva  (texture3)

         -- Ohraničení
         border_size   = 0,
         border_type   = BORDER_TYPE.NONE,  -- BORDER_TYPE.NONE | BORDER_TYPE.INNER | BORDER_TYPE.OUTER
         border_color  = RGBA(0, 0,0, 255 ),

         -- Textury
         texture           = "background.png",  -- hlavní textura (násobena color)
         texture2          = nil,               -- progress / check textura (násobena color2)
         texture3          = nil,               -- doplňková textura (násobena color3)
         texture_fallback  = nil,               -- fallback pokud texture neexistuje
         texture2_fallback = nil,               -- fallback pro texture2
         texture3_fallback = nil,               -- fallback pro texture3
         subCoords         = nil,               -- výřez textury { X, Y, W, H } v normalizovaných souřadnicích
         grayscale         = 0,                 -- úroveň šedé (0 = barva, 255 = zcela šedé; záporné = jas)

         -- Text
         text        = "Hello",
         hint        = "Tooltip text",          -- text tooltipu při najetí myší
         font_name   = "MyFont.DFF",
         font_size   = 14,
         -- DFF font práh vykreslování (0.0 až 1.0; -1 = automaticky dle velikosti fontu)
         range_min = 0.38,   -- minimální práh (hrana písma)
         range_max = 0.6,    -- maximální práh (střed písma)
         -- Škálování a mezery znaků
         char_scale_x = 1.0,
         char_scale_y = 1.0,
         char_spacing = 0.0,
         line_spacing = 0.0,


         text_halign = ALIGN.LEFT,    -- ALIGN.LEFT | ALIGN.MIDDLE | ALIGN.RIGHT
         text_valign = ALIGN.TOP,     -- ALIGN.TOP  | ALIGN.MIDDLE | ALIGN.BOTTOM
         wordwrap    = false,

         -- Automatická velikost dle textu
         auto_size_width      = false,   -- šířka se přizpůsobí obsahu
         auto_size_height     = false,   -- výška se přizpůsobí obsahu
         auto_size_width_max  = 0,       -- maximální šířka při auto size (0 = neomezeno)
         auto_size_height_max = 0,       -- maximální výška při auto size (0 = neomezeno)

         -- Barvy fontu
         font_color = RGBA(255, 255,255, 255 ),



         -- Stín textu
         shadow          = false,
         shadow_offset_x = 1.0,
         shadow_offset_y = 1.0,
         shadow_color    = RGBA(0, 0,0, 200 ),

         -- Obrys textu
         outline       = false,
         outline_width = 1.0,
         outline_color = RGBA(0, 0,0, 255 ),

         -- Scrollování textu (pouze TYPE.LABEL)
         scrolltext       = false,   -- posouvat text pokud je delší než element
         scrolltext_speed = 30,      -- rychlost posouvání v pixelech za sekundu
         scrolltext_delay = 1000,    -- prodleva před spuštěním posouvání v ms

         -- ScrollBox
         scroll_offset_x  = 0,                       -- horizontální scroll offset
         scroll_offset_y  = 0,                       -- vertikální scroll offset
         horizontal_scroll = false,                  -- hlavní je horizontální scrollování

         -- ScrollBar (pouze TYPE.SCROLLBAR)
         vertical = true,                            -- SCROLLBAR_TYPE.VERTICAL nebo HORIZONTAL

         -- TextLog (pouze TYPE.TEXTLOG)
         max_line     = 0,       -- maximální počet řádků (0 = neomezeno. při přesahu se začnou nejstarší vymazávat)
         add_at_bottom = false,  -- přidávat nové řádky na konec (jinak na začátek)

         -- Callbacky
         --   hodnota může být:
         --     function            – funkce bez extra argumentů
         --     { func, ... }       – funkce + extra argumenty
         --     "lua kód"           – string volaný v globálu
         --
         --   Placeholdery v extra argumentech (nahrazeny hodnotou při každém eventu):
         --     %id    – ID elementu (číslo)
         --     %x     – pozice myši X  lokálně  (relativně k elementu)
         --     %y     – pozice myši Y  lokálně  (relativně k elementu)
         --     %ax    – pozice myši X  absolutně (na obrazovce)
         --     %ay    – pozice myši Y  absolutně (na obrazovce)
         --     %b     – index tlačítka myši  (0=levé, 1=pravé, 2=střední, 3, 4)
         --     %d     – delta kolečka myši  (kladné = nahoru, záporné = dolů)
         --     %dh    - delta druhého kolečka myši (kladne = doleva , zaporné = doprava )
         --     %dx    – pohyb myši delta X  (drag / move)
         --     %dy    – pohyb myši delta Y  (drag / move)
         --     %k     – kód klávesy  (int)
         --     %sc    – scan kód klávesy (fyzická pozice, int)
         --     %c     – znak jako string  (pro KEYPRESS eventy)
         --     %shift %shiftl %shiftr  – stav Shift modifikátoru (bool)
         --     %ctrl  %ctrll  %ctrlr   – stav Ctrl modifikátoru  (bool)
         --     %alt   %altl   %altr    – stav Alt modifikátoru   (bool)
         --
         --     %vis     visibility  true/false
         --     %w %h    width height
         --
         callbacks = {
            -- Myš – pohyb / hover
            [CALLBACK.MOUSEOVER]    = { onHover,  "%id", "%ax", "%ay", "%x", "%y" },  -- kurzor vstoupil na element
            [CALLBACK.MOUSELEAVE]   = { onLeave,  "%id" },                             -- kurzor opustil element
            [CALLBACK.MOUSEMOVE]    = { onMove,   "%id", "%ax", "%ay", "%dx", "%dy" }, -- pohyb myši nad elementem
            [CALLBACK.MOUSEWHEEL]   = { onScroll, "%id", "%d" "dh" },                       -- kolečko myši

            -- Levé tlačítko
            [CALLBACK.LDOWN]        = { onLDown,   "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.LUP]          = { onLUp,     "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.LCLICK]       = { onClick,   "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.LDBLCLICK]    = { onDblClick,"%id","%ax", "%ay", "%x", "%y" },
            [CALLBACK.LDRAG]        = { onDrag,    "%id", "%ax", "%ay", "%dx", "%dy" },

            -- Pravé tlačítko
            [CALLBACK.RDOWN]        = { onRDown,  "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.RUP]          = { onRUp,    "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.RCLICK]       = { onRClick, "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.RDBLCLICK]    = { onRDbl,   "%id", "%ax", "%ay", "%x", "%y" },

            -- Střední tlačítko
            [CALLBACK.MDOWN]        = { onMDown,  "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.MUP]          = { onMUp,    "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.MCLICK]       = { onMClick, "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.MDBLCLICK]    = { onMDbl,   "%id", "%ax", "%ay", "%x", "%y" },

            -- Tlačítka 4
            [CALLBACK.B4DOWN]       = { onB4Down, "%id", "%ax", "%ay", "%x", "%y" },  
            [CALLBACK.B4UP]         = { onB4Up,   "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.B4CLICK]      = { onB4,      "%id", "%ax", "%ay", "%x", "%y" }, 
            [CALLBACK.B4DBLCLICK]   = { onB4Dbl,  "%id", "%ax", "%ay", "%x", "%y" },
            -- Tlačítka 5
            [CALLBACK.B5DOWN]       = { onB5Down,  "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.B5UP]         = { onB5Up,    "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.B5CLICK]      = { onB5,      "%id", "%ax", "%ay", "%x", "%y" }, 
            [CALLBACK.B5DBLCLICK]   = { onB5Dbl,   "%id", "%ax", "%ay", "%x", "%y" },

            -- Klávesnice
            [CALLBACK.KEYDOWN]      = { onKeyDown,  "%id", "%k", "%sc", "%shift", "%ctrl", "%alt", "%shiftl", "%ctrll", "%altl", "%shiftr", "%ctrlr", "%altr" },
            [CALLBACK.KEYUP]        = { onKeyUp,    "%id", "%k", "%sc", "%shift", "%ctrl", "%alt", "%shiftl", "%ctrll", "%altl", "%shiftr", "%ctrlr", "%altr" },
            [CALLBACK.KEYPRESS]     = { onKeyPress, "%id", "%c" },  -- Unicode znak (text input)

            -- Stav elementu

            [CALLBACK.RESIZED]      = { onResized,    "%id", "%h", "%w% },   -- element změnil velikost - pouze auto resize
            [CALLBACK.VISIBILITY]   = { onVisibility, "%id", "%vis" },   -- viditelnost se změnila

            //[CALLBACK.ANIMEND]      = { onAnimEnd,    "%id" },   -- animace Events skončila


            -- Dotyk
            [CALLBACK.TOUCHDOWN]    = { onTouchDown, "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.TOUCHUP]      = { onTouchUp,   "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.TOUCHMOVE]    = { onTouchMove, "%id", "%ax", "%ay", "%dx", "%dy" },
            [CALLBACK.TOUCHTAP]     = { onTouchTap,  "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.TOUCHDBLTAP]  = { onTouchDbl,  "%id", "%ax", "%ay", "%x", "%y" },

            -- Kombinované click + tap (funguje pro myš i dotyk)
            [CALLBACK.CLICKTAP]     = { onClickTap,  "%id", "%ax", "%ay", "%x", "%y" },
            [CALLBACK.DBLCLICKTAP]  = { onDblClickTap, "%id", "%ax", "%ay", "%x", "%y" },

            -- Focus (zatím neimplementováno)
            [CALLBACK.FOCUS]     = {onFocus, "%id" },   -- element získal focus
            [CALLBACK.BLUR]      = {onBlur, "%id" },    -- element ztratil focus
         },
      });
--]]

--    Vytvoří nový UI element a nastaví mu vlastnosti z tabulky Input.
function AddElement(Input)
	if (Input.type == nil) then
		Input.type = TYPE.ELEMENT;
	end;

	if (Input.special == nil) then
		Input.special = false;
	end;

	if (Input.parent == nil) then
		Input.ID = GUI.NewElement(Input.type, 0, Input.special);
	else
		Input.ID = GUI.NewElement(Input.type, Input.parent.ID, Input.special);
	end;

	if (Input.ID > 0) then

		-- Pozice a velikost
		GUI.SetProperty(Input.ID, PROP.X,      Input.x);
		GUI.SetProperty(Input.ID, PROP.Y,      Input.y);
		GUI.SetProperty(Input.ID, PROP.WIDTH,  Input.width);
		GUI.SetProperty(Input.ID, PROP.HEIGHT, Input.height);


		-- Viditelnost a ořez
		GUI.SetProperty(Input.ID, PROP.VISIBLE, Input.visible);
		GUI.SetProperty(Input.ID, PROP.CanFocus, Input.can_focus);
		GUI.SetProperty(Input.ID, PROP.SCISSOR, Input.scissor);
		GUI.SetProperty(Input.ID, PROP.SCISSORMASK, Input.scissor_mask);
		GUI.SetProperty(Input.ID, PROP.SCISSORMASK_THRESHOLD, Input.scissor_mask_threshold);
		GUI.SetProperty(Input.ID, PROP.FADE,    Input.fade);

		-- Mouse Events
		GUI.SetProperty(Input.ID, PROP.PASSTHROUGH, Input.pass_through);
		GUI.SetProperty(Input.ID, PROP.EVENTCAPTURE, Input.event_capture);
		GUI.SetProperty(Input.ID, PROP.HITMASK, Input.hit_mask);
        GUI.SetProperty(Input.ID, PROP.HITMASK_GRAYSCALE, Input.hit_mask_use_grayscale); 
        GUI.SetProperty(Input.ID, PROP.HITMASK_THRESHOLD, Input.hit_mask_threshold);

		-- Kotvení
		GUI.SetAnchor(Input.ID, Input.anchor);

		-- Barvy
		GUI.SetProperty(Input.ID, PROP.COLOR1, Input.color);
		GUI.SetProperty(Input.ID, PROP.COLOR2, Input.color2);
		GUI.SetProperty(Input.ID, PROP.COLOR3, Input.color3);

		-- Ohraničení
		GUI.SetProperty(Input.ID, PROP.BORDER_SIZE,   Input.border_size);
		GUI.SetProperty(Input.ID, PROP.BORDER_TYPE,   Input.border_type);
		GUI.SetProperty(Input.ID, PROP.BORDER_COLOR, Input.border_color);

		-- Textury
		GUI.SetProperty(Input.ID, PROP.FALLBACK_TEXTURE, Input.texture_fallback);
		GUI.SetProperty(Input.ID, PROP.FALLBACK_TEXTURE2, Input.texture2_fallback);
		GUI.SetProperty(Input.ID, PROP.FALLBACK_TEXTURE3, Input.texture3_fallback);
		GUI.SetProperty(Input.ID, PROP.TEXTURE,      Input.texture);
		GUI.SetProperty(Input.ID, PROP.TEXTURE2,     Input.texture2);
		GUI.SetProperty(Input.ID, PROP.TEXTURE3,     Input.texture3);
		GUI.SetProperty(Input.ID, PROP.SUBCOORDS,    Input.subCoords);

		GUI.SetProperty(Input.ID, PROP.GRAYSCALE, Input.grayscale);

		-- Text
		GUI.SetProperty(Input.ID, PROP.TEXT,       Input.text);
		GUI.SetProperty(Input.ID, PROP.HINT,       Input.hint);
		GUI.SetProperty(Input.ID, PROP.SHADOWTEXT, Input.shadowtext);
		GUI.SetProperty(Input.ID, PROP.FONT_NAME,  Input.font_name);
		GUI.SetProperty(Input.ID, PROP.FONT_SIZE,  Input.font_size);
		GUI.SetProperty(Input.ID, PROP.AUTO_SIZE_WIDTH,  Input.auto_size_width);
		GUI.SetProperty(Input.ID, PROP.AUTO_SIZE_HEIGHT, Input.auto_size_height);
	    GUI.SetProperty(Input.ID, PROP.AUTO_SIZE_WIDTH_MAX,  Input.auto_size_width_max);
		GUI.SetProperty(Input.ID, PROP.AUTO_SIZE_HEIGHT_MAX, Input.auto_size_height_max);
		GUI.SetProperty(Input.ID, PROP.HALIGN,     Input.text_halign);
		GUI.SetProperty(Input.ID, PROP.VALIGN,     Input.text_valign);
		GUI.SetProperty(Input.ID, PROP.WORDWRAP,   Input.wordwrap);
        GUI.SetProperty(Input.ID, PROP.SHOWRICH, Input.show_rich);

		-- Barvy fontu
		GUI.SetProperty(Input.ID, PROP.FONT_COLOR,          Input.font_color);
		--GUI.SetProperty(Input.ID, PROP.FONT_COLOR_BACK,      Input.font_color_background);

		-- Škálování a mezery znaků
		GUI.SetProperty(Input.ID, PROP.CHAR_SCALE_X, Input.char_scale_x);
		GUI.SetProperty(Input.ID, PROP.CHAR_SCALE_Y, Input.char_scale_y);
		GUI.SetProperty(Input.ID, PROP.CHAR_SPACING, Input.char_spacing);
		GUI.SetProperty(Input.ID, PROP.LINE_SPACING,  Input.line_spacing);

		-- Stín textu
		GUI.SetProperty(Input.ID, PROP.SHADOW,          Input.shadow);
		GUI.SetProperty(Input.ID, PROP.SHADOW_OFFSET_X, Input.shadow_offset_x);
		GUI.SetProperty(Input.ID, PROP.SHADOW_OFFSET_Y, Input.shadow_offset_y);
		GUI.SetProperty(Input.ID, PROP.SHADOW_COLOR,    Input.shadow_color);

		-- Obrys textu
		GUI.SetProperty(Input.ID, PROP.OUTLINE,       Input.outline);
		GUI.SetProperty(Input.ID, PROP.OUTLINE_WIDTH,  Input.outline_width);
		GUI.SetProperty(Input.ID, PROP.OUTLINE_COLOR,  Input.outline_color);

		-- Rozsah
		GUI.SetProperty(Input.ID, PROP.RANGE_MIN, Input.range_min);
		GUI.SetProperty(Input.ID, PROP.RANGE_MAX, Input.range_max);

		-- Scroll textu
		GUI.SetProperty(Input.ID, PROP.SCROLLTEXT, Input.scrolltext);
		GUI.SetProperty(Input.ID, PROP.SCROLLTEXT_SPEED, Input.scrolltext_speed);
		GUI.SetProperty(Input.ID, PROP.SCROLLTEXT_DELAY, Input.scrolltext_delay);

		-- Callbacky
		if Input.callbacks then
			for cbType, cbDef in pairs(Input.callbacks) do
				if type(cbDef) == "function" then
					GUI.SetCallback(Input.ID, cbType, cbDef);
				elseif type(cbDef) == "table" then
					GUI.SetCallback(Input.ID, cbType, table.unpack(cbDef));
				elseif type(cbDef) == "string" then
					GUI.SetCallback(Input.ID, cbType, cbDef);
				end;
			end;
		end;

		-- Scrollbox a scrollbar
		GUI.SetProperty(Input.ID, PROP.SCROLLOFFSET_X, Input.scroll_offset_x);
		GUI.SetProperty(Input.ID, PROP.SCROLLOFFSET_Y, Input.scroll_offset_y);
		GUI.SetProperty(Input.ID, PROP.SCROLLBAR_TYPE, Input.vertical and SCROLLBAR_TYPE.VERTICAL or SCROLLBAR_TYPE.HORIZONTAL ); -- SCROLLBAR_TYPE.VERTICAL | SCROLLBAR_TYPE.HORIZONTAL
		GUI.SetProperty(Input.ID, PROP.HORIZONTALSCROLL, Input.horizontal_scroll);

		-- TextLog
		GUI.SetProperty(Input.ID, PROP.MAXLINES, Input.max_line);      
		GUI.SetProperty(Input.ID, PROP.ADDATBOTTOM, Input.add_at_bottom); 
	end;

	


	return eReg:Set(Input.ID, Input);
end;

-- ── Helpers pro nastavení jednotlivých vlastností ─────────────────────────────

function SetParent(element, parent)    GUI.SetParentID(element.ID, parent.ID); return element; end;
-- Position
function SetX(element, value)          GUI.SetProperty(element.ID, PROP.X, value); return element; end;
function SetY(element, value)          GUI.SetProperty(element.ID, PROP.Y, value); return element; end;
function SetPos(element, x, y)         GUI.SetProperty(element.ID, PROP.X, x); GUI.SetProperty(element.ID, PROP.Y, y); return element; end;

-- Size
function SetWidth(element, value)      GUI.SetProperty(element.ID, PROP.WIDTH,  value); return element; end;
function SetHeight(element, value)     GUI.SetProperty(element.ID, PROP.HEIGHT, value); return element; end;
function SetSize(element, width, height)         GUI.SetProperty(element.ID, PROP.WIDTH, width); GUI.SetProperty(element.ID, PROP.HEIGHT, height); return element; end;
-- {X, Y, W, H }
function SetXYWH(element, vtable)   
	GUI.SetProperty(element.ID, PROP.X, vtable.X); 
	GUI.SetProperty(element.ID, PROP.Y, vtable.Y); 
	GUI.SetProperty(element.ID, PROP.WIDTH, vtable.W); 
	GUI.SetProperty(element.ID, PROP.HEIGHT, vtable.H); 
	return element; 
end;
function SetPosSize(element, x, y, width, height) SetPos(element, x, y); SetSize(element, width, height); return element; end;

function BringToFront(element)		GUI.BringToFront(element.ID);					return element; end;
function BringToBack(element)		GUI.BringToBack(element.ID);					return element; end;
function BringBy(element, offset)	GUI.BringBy(element.ID, offset);				return element; end;

-- Visibility and fading
function SetVisible(element, value)    GUI.SetProperty(element.ID, PROP.VISIBLE, value); return element; end;
function Show(element)                 GUI.SetProperty(element.ID, PROP.VISIBLE, true);	 return element; end;
function Hide(element)                 GUI.SetProperty(element.ID, PROP.VISIBLE, false); return element; end;
function SetFade(element, value)       GUI.SetProperty(element.ID, PROP.FADE, value);    return element; end;

 -- Only element with CanFocus can by focused, note invalid un unfocusable element will set Desktop
function SetFocus(element)             GUI.SetFocus(element.ID); return element; end;
function SetCanFocus(element, value)   GUI.SetProperty(element.ID, PROP.CANFOCUS, value); return element; end;

-- Ořez (děti se nebudou vykreslovat mimo hranice tohoto elementu)
-- scissor (childrens will not be drawn outside the bounds of this element)
function SetScissor(element, value)    GUI.SetProperty(element.ID, PROP.SCISSOR, value); return element; end;
function SetScissorMask(element, value) GUI.SetProperty(element.ID, PROP.SCISSORMASK, value); return element; end;
function SetScissorMaskThreshold(element, value) GUI.SetProperty(element.ID, PROP.SCISSORMASK_THRESHOLD, value); return element; end;
-- anchor
function SetAnchor(element, anchor)    GUI.SetAnchor(element.ID, anchor); return element; end;

-- colors  { R, G, B, A }
function SetColor(element, color)       GUI.SetProperty(element.ID, PROP.COLOR1,        color); return element; end;
function SetColor2(element, color)       GUI.SetProperty(element.ID, PROP.COLOR2,        color); return element; end;
function SetColor3(element, color)       GUI.SetProperty(element.ID, PROP.COLOR3,        color); return element; end;
function SetFontColor(element, color)    GUI.SetProperty(element.ID, PROP.FONT_COLOR,    color); return element; end;
function SetBorderColor(element, color)  GUI.SetProperty(element.ID, PROP.BORDER_COLOR,  color); return element; end;
function SetShadowColor(element, color)  GUI.SetProperty(element.ID, PROP.SHADOW_COLOR,  color); return element; end;
function SetOutlineColor(element, color) GUI.SetProperty(element.ID, PROP.OUTLINE_COLOR, color); return element; end;

-- Border
function SetBorder(element, type, size, color)	
	GUI.SetProperty(element.ID, PROP.BORDER_SIZE, type); 
	GUI.SetProperty(element.ID, PROP.BORDER_COLOR, color); 
	GUI.SetProperty(element.ID, PROP.BORDER_TYPE, size); 
	return element;
end;
function SetBorderSize(element, value)  GUI.SetProperty(element.ID, PROP.BORDER_SIZE, value); return element; end;
function SetBorderType(element, value)  GUI.SetProperty(element.ID, PROP.BORDER_TYPE, value); return element; end;

-- Textures
function SetTexture(element, value)          GUI.SetProperty(element.ID, PROP.TEXTURE,      value);      return element; end;
function SetTexture2(element, value)         GUI.SetProperty(element.ID, PROP.TEXTURE2,     value);      return element; end;
function SetTexture3(element, value)         GUI.SetProperty(element.ID, PROP.TEXTURE3,     value);      return element; end;
function SetTextureFallback(element, value)  GUI.SetProperty(element.ID, PROP.FALLBACK_TEXTURE, value);  return element; end;
function SetTexture2Fallback(element, value) GUI.SetProperty(element.ID, PROP.FALLBACK_TEXTURE2, value); return element; end;
function SetTexture3Fallback(element, value) GUI.SetProperty(element.ID, PROP.FALLBACK_TEXTURE3, value); return element; end;
function SetSubCoords(element, value)        GUI.SetProperty(element.ID, PROP.SUBCOORDS, value);         return element; end;

-- 0-255 level of grayscale (255 is totaly gray), and bellow of 0 is brightness
function SetGrayscale(element, value)        GUI.SetProperty(element.ID, PROP.GRAYSCALE, value);         return element; end;

-- Texts
function SetText(element, value)       GUI.SetProperty(element.ID, PROP.TEXT,      value); return element; end;
function SetHint(element, value)       GUI.SetProperty(element.ID, PROP.HINT,      value); return element; end;
function SetFont(element, value)       GUI.SetProperty(element.ID, PROP.FONT_NAME, value); return element; end;
function SetFontSize(element, value)   GUI.SetProperty(element.ID, PROP.FONT_SIZE, value); return element; end;
function SetAutoSizeWidth(element, value)  GUI.SetProperty(element.ID, PROP.AUTO_SIZE_WIDTH,  value); return element; end;
function SetAutoSizeHeight(element, value) GUI.SetProperty(element.ID, PROP.AUTO_SIZE_HEIGHT, value); return element; end;
function SetAutoSizeWidthMax(element, value) GUI.SetProperty(element.ID, PROP.AUTO_SIZE_WIDTH_MAX, value); return element; end;
function SetAutoSizeHeightMax(element, value) GUI.SetProperty(element.ID, PROP.AUTO_SIZE_HEIGHT_MAX, value); return element; end;
function SetHAlign(element, value)     GUI.SetProperty(element.ID, PROP.HALIGN,    value); return element; end;
function SetVAlign(element, value)     GUI.SetProperty(element.ID, PROP.VALIGN,    value); return element; end;
function SetWordWrap(element, value)   GUI.SetProperty(element.ID, PROP.WORDWRAP,  value); return element; end;
function SetShowRich(element, value)   GUI.SetProperty(element.ID, PROP.SHOWRICH,  value); return element; end;

-- shadow text
function SetShadow(element, value)         GUI.SetProperty(element.ID, PROP.SHADOW,          value); return element; end;
function SetShadowOffsetX(element, value)  GUI.SetProperty(element.ID, PROP.SHADOW_OFFSET_X, value); return element; end;
function SetShadowOffsetY(element, value)  GUI.SetProperty(element.ID, PROP.SHADOW_OFFSET_Y, value); return element; end;

-- Outline text
function SetOutline(element, value)        GUI.SetProperty(element.ID, PROP.OUTLINE,       value); return element; end;
function SetOutlineWidth(element, value)   GUI.SetProperty(element.ID, PROP.OUTLINE_WIDTH, value); return element; end;

-- Scaling text
function SetCharScaleX(element, value)   GUI.SetProperty(element.ID, PROP.CHAR_SCALE_X, value);  return element; end;
function SetCharScaleY(element, value)   GUI.SetProperty(element.ID, PROP.CHAR_SCALE_Y, value);  return element; end;
function SetCharSpacing(element, value)  GUI.SetProperty(element.ID, PROP.CHAR_SPACING,  value); return element; end;
function SetLineSpacing(element, value)  GUI.SetProperty(element.ID, PROP.LINE_SPACING,  value); return element; end;

-- Scrolling text
function SetScrollText(element, value)        GUI.SetProperty(element.ID, PROP.SCROLLTEXT,        value);  return element; end;
function SetScrollTextSpeed(element, value)   GUI.SetProperty(element.ID, PROP.SCROLLTEXT_SPEED,   value); return element; end;
function SetScrollTextDelay(element, value)   GUI.SetProperty(element.ID, PROP.SCROLLTEXT_DELAY,   value); return element; end;

-- Mouse Events
function SetPassThrough(element, value)    GUI.SetProperty(element.ID, PROP.PASSTHROUGH, value); return element; end;
function SetEventCapture(element, value)   GUI.SetProperty(element.ID, PROP.EVENTCAPTURE, value); return element; end;
function SetHitMask(element, value)        GUI.SetProperty(element.ID, PROP.HITMASK, value); return element; end;

-- Callbacks
--   SetCallback(element, CALLBACK.LCLICK, func, arg1, arg2, ...)
--   SetCallback(element, CALLBACK.LCLICK, {func, arg1, arg2, ...})
--   SetCallback(element, CALLBACK.LCLICK, stringCode)
--   SetCallback(element, CALLBACK.LCLICK, nil)   -- smaže callback
function SetCallback(element, callbackType, funcOrTable, ...)
	if type(funcOrTable) == "table" then
		GUI.SetCallback(element.ID, callbackType, table.unpack(funcOrTable));
	elseif type(funcOrTable) == "function" or type(funcOrTable) == "string" or type(funcOrTable) == "nil" then  -- function, string or erase
		GUI.SetCallback(element.ID, callbackType, funcOrTable, ...);
	end;
	return element;
end;

-- Scrollbox a scrollbar
function SetScrollOffset(element, offsetX, offsetY)   
	GUI.SetProperty(element.ID, PROP.SCROLLOFFSET_X, offsetX); 
	GUI.SetProperty(element.ID, PROP.SCROLLOFFSET_Y, offsetY); 
	return element;
end;
function SetScrollOffsetY(element) 	GUI.SetProperty(element.ID, PROP.SCROLLOFFSET_Y, offsetY); return element; end;
function SetScrollOffsetX(element)	GUI.SetProperty(element.ID, PROP.SCROLLOFFSET_X, offsetX); return element; end;

function SetBarToScrollbox(boxElement,typeScrollbar, barElement) GUI.SetProperty(boxElement.ID, typeScrollbar,         barElement.ID);  return element;  end;
function SetScrollbarType(element, scrollbarType)                GUI.SetProperty(element.ID,    PROP.SCROLLBAR_TYPE,   scrollbarType);  return element;  end;
function SetHorizontalScroll(element, value)                     GUI.SetProperty(element.ID,    PROP.HORIZONTALSCROLL, value);          return element;  end;

-- TextLog

function AddLine(element, text) 	GUI.AddLine(element.ID, text); return element; end;
function CleanLines(element)        GUI.CleanLines(element.ID);    return element; end;
  
function SetMaxLines(element, int)     GUI.SetProperty(element.ID, PROP.MAXLINES, int);     return element; end;
function SetAddAtBottom(element, bool) GUI.SetProperty(element.ID, PROP.ADDATBOTTOM, bool);  return element; end;


--// Geters
function GetParent(element)     local pID = GUI.GetParentID(element.ID); if pID then return eReg:Get(pID); end; end;

-- Position
function GetX(element)          return GUI.GetProperty(element.ID, PROP.X); end;
function GetY(element)          return GUI.GetProperty(element.ID, PROP.Y); end;
function GetPos(element)        return GUI.GetProperty(element.ID, PROP.X), GUI.GetProperty(element.ID, PROP.Y); end;

-- Size
function GetWidth(element)      return GUI.GetProperty(element.ID, PROP.WIDTH); end;
function GetHeight(element)     return GUI.GetProperty(element.ID, PROP.HEIGHT); end;
function GetSize(element)       return  GUI.GetProperty(element.ID, PROP.WIDTH), GUI.GetProperty(element.ID, PROP.HEIGHT); end;

function GetXYWH(element)       return XYWH(GUI.GetProperty(element.ID, PROP.X), GUI.GetProperty(element.ID, PROP.Y), GUI.GetProperty(element.ID, PROP.WIDTH), GUI.GetProperty(element.ID, PROP.HEIGHT)); end;


-- Visibility and fading
function GetVisible(element)    return GUI.GetProperty(element.ID, PROP.VISIBLE); end;
function GetFade(element)       return GUI.GetProperty(element.ID, PROP.FADE); end;
function GetCanFocus(element)   return GUI.GetProperty(element.ID, PROP.CANFOCUS); end;

-- scissor
function GetScissor(element)     return GUI.GetProperty(element.ID, PROP.SCISSOR); end;
function GetScissorMask(element) return GUI.GetProperty(element.ID, PROP.SCISSORMASK); end;
function GetScissorMaskThreshold(element) return GUI.GetProperty(element.ID, PROP.SCISSORMASK_THRESHOLD); end;

-- colors
function GetColor(element)     return GUI.GetProperty(element.ID, PROP.COLOR1); end;
function GetColor2(element)     return GUI.GetProperty(element.ID, PROP.COLOR2); end;
function GetColor3(element)     return GUI.GetProperty(element.ID, PROP.COLOR3); end;
function GetFontColor(element)  return GUI.GetProperty(element.ID, PROP.FONT_COLOR); end;
function GetBorderColor(element)  return GUI.GetProperty(element.ID, PROP.BORDER_COLOR); end;
function GetShadowColor(element)  return GUI.GetProperty(element.ID, PROP.SHADOW_COLOR); end;
function GetOutlineColor(element) return GUI.GetProperty(element.ID, PROP.OUTLINE_COLOR); end;

-- Border
function GetBorderSize(element) return GUI.GetProperty(element.ID, PROP.BORDER_SIZE); end;
function GetBorderType(element) return GUI.GetProperty(element.ID, PROP.BORDER_TYPE); end;

-- Textures
function GetTexture(element)              return GUI.GetProperty(element.ID, PROP.TEXTURE);       end;
function GetTexture2(element)             return GUI.GetProperty(element.ID, PROP.TEXTURE2);      end;
function GetTexture3(element)             return GUI.GetProperty(element.ID, PROP.TEXTURE3);      end;
function GetTextureFallback(element)      return GUI.GetProperty(element.ID, PROP.FALLBTEXTURE);  end;
function GetTexture2Fallback(element)     return GUI.GetProperty(element.ID, PROP.FALLBTEXTURE2); end;
function GetTexture3Fallback(element)     return GUI.GetProperty(element.ID, PROP.FALLBTEXTURE3); end;
function GetTextureID(element)            return GUI.GetProperty(element.ID, PROP.TEXTURE_ID);    end;
function GetTexture2ID(element)           return GUI.GetProperty(element.ID, PROP.TEXTURE2_ID);   end;
function GetTexture3ID(element)           return GUI.GetProperty(element.ID, PROP.TEXTURE3_ID);   end;
function GetSubCoords(element, value)     return GUI.GetProperty(element.ID, PROP.SUBCOORDS)      end;
function GetTextureWidth(element)   return GUI.GetProperty(element.ID, PROP.TEXTURE_WIDTH)  end;
function GetTextureHeight(element)  return GUI.GetProperty(element.ID, PROP.TEXTURE_HEIGHT) end;
function GetTextureSize(element)    return GUI.GetProperty(element.ID, PROP.TEXTURE_WIDTH), GUI.GetProperty(element.ID, PROP.TEXTURE_HEIGHT); end;
function GetTexture2Width(element)  return GUI.GetProperty(element.ID, PROP.TEXTURE2_WIDTH)  end;
function GetTexture2Height(element) return GUI.GetProperty(element.ID, PROP.TEXTURE2_HEIGHT) end;
function GetTexture2Size(element)   return GUI.GetProperty(element.ID, PROP.TEXTURE2_WIDTH), GUI.GetProperty(element.ID, PROP.TEXTURE2_HEIGHT); end;
function GetTexture3Width(element)  return GUI.GetProperty(element.ID, PROP.TEXTURE3_WIDTH)  end;
function GetTexture3Height(element) return GUI.GetProperty(element.ID, PROP.TEXTURE3_HEIGHT) end;
function GetTexture3Size(element)   return GUI.GetProperty(element.ID, PROP.TEXTURE3_WIDTH), GUI.GetProperty(element.ID, PROP.TEXTURE3_HEIGHT); end;

function GetGrayscale(element)      return GUI.GetProperty(element.ID, PROP.GRAYSCALE) end;
-- Texts

function GetText(element)       return GUI.GetProperty(element.ID, PROP.TEXT); end;
function GetHint(element)       return GUI.GetProperty(element.ID, PROP.HINT); end;
function GetFont(element)       return GUI.GetProperty(element.ID, PROP.FONT_NAME); end;
function GetFontSize(element)   return GUI.GetProperty(element.ID, PROP.FONT_SIZE); end;
function GetAutoSizeWidth(element)  return GUI.GetProperty(element.ID, PROP.AUTO_SIZE_WIDTH); end;
function GetAutoSizeHeight(element) return GUI.GetProperty(element.ID, PROP.AUTO_SIZE_HEIGHT); end;
function GetAutoSizeWidthMax(element)  return GUI.GetProperty(element.ID, PROP.AUTO_SIZE_WIDTH_MAX); end;
function GetAutoSizeHeightMax(element) return GUI.GetProperty(element.ID, PROP.AUTO_SIZE_HEIGHT_MAX); end;
function GetHAlign(element)     return GUI.GetProperty(element.ID, PROP.HALIGN); end;
function GetVAlign(element)     return GUI.GetProperty(element.ID, PROP.VALIGN); end
function GetWordWrap(element)   return GUI.GetProperty(element.ID, PROP.WORDWRAP); end;
function GetShowRich(element)   return GUI.GetProperty(element.ID, PROP.SHOWRICH); end;

-- Shadow text
function GetShadow(element)     return GUI.GetProperty(element.ID, PROP.SHADOW); end;
function GetShadowOffsetX(element) return GUI.GetProperty(element.ID, PROP.SHADOW_OFFSET_X); end;
function GetShadowOffsetY(element) return GUI.GetProperty(element.ID, PROP.SHADOW_OFFSET_Y); end;

-- Outline text
function GetOutline(element)     return GUI.GetProperty(element.ID, PROP.OUTLINE); end;
function GetOutlineWidth(element) return GUI.GetProperty(element.ID, PROP.OUTLINE_WIDTH); end

-- Scaling text
function GetCharScaleX(element)  return GUI.GetProperty(element.ID, PROP.CHAR_SCALE); end;
function GetCharScaleY(element)  return GUI.GetProperty(element.ID, PROP.CHAR_SCALE); end;
function GetCharSpacing(element) return GUI.GetProperty(element.ID, PROP.CHAR_SPACING); end;
function GetLineSpacing(element) return GUI.GetProperty(element.ID, PROP.LINE_SPACING); end;

-- Mouse Events
function GetPassThrough(element)  return GUI.GetProperty(element.ID, PROP.PASSTHROUGH); end;
function GetEventCapture(element) return GUI.GetProperty(element.ID, PROP.EVENTCAPTURE); end
function GetHitMask(element)      return GUI.GetProperty(element.ID, PROP.HITMASK); end;

-- Scrolling text
function GetScrollText(element) return GUI.GetProperty(element.ID, PROP.SCROLLTEXT); end;
function GetScrollTextSpeed(element) return GUI.GetProperty(element.ID, PROP.SCROLLTEXT_SPEED); end;
function GetScrollTextDelay(element) return GUI.GetProperty(element.ID, PROP.SCROLLTEXT_DELAY); end;

-- Scrollbox & scrollbar
function GetScrollOffset(element) return GUI.GetProperty(element.ID, PROP.SCROLLOFFSET_X), GUI.GetProperty(element.ID, PROP.SCROLLOFFSET_Y); end;
function GetScrollbarType(element) return GUI.GetProperty(element.ID, PROP.SCROLLBAR_TYPE); end;
function GetHorizontalScroll(element) return GUI.GetProperty(element.ID, PROP.HORIZONTALSCROLL); end;

function GetScrollbar(element)  return GUI.GetProperty(element.ID, PROP.SCROLLBAR); end;
function GetScrollbar2(element) return GUI.GetProperty(element.ID, PROP.SCROLLBAR2); end;

function GetMaxLines(element)    GUI.GetProperty(element.ID, PROP.MAXLINES, int);      end;
function GetAddAtBottom(element) GUI.GetProperty(element.ID, PROP.ADDATBOTTOM, bool);  end;





--- 

function DestroyElement(element)
    if element and element.ID then

        local elementsToDestroy = {};

        local function childsToDestroy(ID)
            local childs = GUI.GetChildrenIDs(ID);
            for _, childID in ipairs(childs) do
                table.insert(elementsToDestroy, childID);
                childsToDestroy(childID);
            end;
        end;

        childsToDestroy(element.ID);

        GUI.DestroyElement(element.ID);

        eReg:DeleteID(element.ID);
        for i = 1, #elementsToDestroy do
            eReg:DeleteID(elementsToDestroy[i]);
        end;

    end;
end;

function DestroyChildren(element)
	if element and element.ID then
	    local elementsToDestroy = {};

        local function childsToDestroy(ID)
            local childs = GUI.GetChildrenIDs(ID);
            for _, childID in ipairs(childs) do
                table.insert(elementsToDestroy, childID);
                childsToDestroy(childID);
            end;
        end;

        childsToDestroy(element.ID);

		GUI.DestroyChildren(element.ID);

		for i = 1, #elementsToDestroy do
            eReg:DeleteID(elementsToDestroy[i]);
        end;
	end;
end;




-- ── Třídy (přes metatabulky) ──────────────────────────────────────────────
--   Nastavují se v elements.utils.lua po vytvoření elementu.
--   element:SetX(100)  je stejné jako  SetX(element, 100)

---@class ElementClass
--   Základ pro všechny elementy — pozice, velikost, viditelnost, barvy, textury.
ElementClass = {};

ElementClass.SetParent = SetParent;
ElementClass.GetParent = GetParent;
-- Pozice

ElementClass.SetX       = SetX;
ElementClass.SetY       = SetY;
ElementClass.SetPos     = SetPos;


ElementClass.GetX       = GetX;
ElementClass.GetY       = GetY;
ElementClass.GetPos     = GetPos;


-- Velikost

ElementClass.SetWidth   = SetWidth;
ElementClass.SetHeight  = SetHeight;
ElementClass.SetSize    = SetSize;
ElementClass.SetPosSize = SetPosSize;

ElementClass.SetXYWH = SetXYWH;

ElementClass.GetWidth   = GetWidth;
ElementClass.GetHeight  = GetHeight;
ElementClass.GetSize    = GetSize;

ElementClass.GetXYWH = GetXYWH;

ElementClass.BringToFront = BringToFront;
ElementClass.BringToBack  = BringToBack;
ElementClass.BringBy	  = BringBy;
-- Viditelnost

ElementClass.SetVisible = SetVisible;
ElementClass.Show       = Show;
ElementClass.Hide       = Hide;
ElementClass.SetFade    = SetFade;

ElementClass.GetVisible = GetVisible;
ElementClass.GetFade    = GetFade;

ElementClass.SetFocus   = SetFocus;
ElementClass.SetCanFocus = SetCanFocus;

ElementClass.GetCanFocus = GetCanFocus;
-- Ořez

ElementClass.SetScissor = SetScissor;
ElementClass.SetScissorMask = SetScissorMask;
ElementClass.SetScissorMaskThreshold = SetScissorMaskThreshold;

ElementClass.GetScissor = GetScissor;
ElementClass.GetScissorMask = GetScissorMask;
ElementClass.GetScissorMaskThreshold = GetScissorMaskThreshold;

-- Kotvení

ElementClass.SetAnchor  = SetAnchor;

-- Barvy elementu  { R, G, B, A }

ElementClass.SetColor       = SetColor;
ElementClass.SetColor2      = SetColor2;
ElementClass.SetColor3      = SetColor3;
ElementClass.SetBorderColor = SetBorderColor;


ElementClass.GetColor       = GetColor;
ElementClass.GetColor2      = GetColor2;
ElementClass.GetColor3      = GetColor3;
ElementClass.GetBorderColor = GetBorderColor;



-- Ohraničení
ElementClass.SetBorder = SetBorder;
ElementClass.SetBorderSize = SetBorderSize;
ElementClass.SetBorderType = SetBorderType;

ElementClass.GetBorderSize = GetBorderSize;
ElementClass.GetBorderType = GetBorderType;


-- Textury

ElementClass.SetTexture         = SetTexture;
ElementClass.SetTexture2        = SetTexture2;
ElementClass.SetTexture3        = SetTexture3;
ElementClass.SetTextureFallback = SetTextureFallback;
ElementClass.SetTexture2Fallback = SetTexture2Fallback;
ElementClass.SetTexture3Fallback = SetTexture3Fallback;
ElementClass.SetSubCoords       = SetSubCoords;
ElementClass.SetGrayscale       = SetGrayscale;

ElementClass.GetTexture         = GetTexture;
ElementClass.GetTexture2        = GetTexture2;
ElementClass.GetTexture3        = GetTexture3;
ElementClass.GetTextureFallback = GetTextureFallback;
ElementClass.GetTexture2Fallback = GetTexture2Fallback;
ElementClass.GetTexture3Fallback = GetTexture3Fallback;
ElementClass.GetTextureID       = GetTextureID;
ElementClass.GetTexture2ID      = GetTexture2ID;
ElementClass.GetTexture3ID      = GetTexture3ID;
ElementClass.GetSubCoords       = GetSubCoords;
ElementClass.GetTextureWidth = GetTextureWidth;
ElementClass.GetTextureHeight = GetTextureHeight;
ElementClass.GetTextureSize = GetTextureSize
ElementClass.GetTexture2Width = GetTexture2Width;
ElementClass.GetTexture2Height = GetTexture2Height;
ElementClass.GetTexture2Size = GetTexture2Size
ElementClass.GetTexture3Width = GetTexture3Width;
ElementClass.GetTexture3Height = GetTexture3Height;
ElementClass.GetTexture3Size = GetTexture3Size;
ElementClass.GetGrayscale    = GetGrayscale;

-- Mouse Events
ElementClass.SetPassThrough = SetPassThrough;
ElementClass.SetEventCapture = SetEventCapture;
ElementClass.SetHitMask = SetHitMask;

ElementClass.GetPassThrough = GetPassThrough;
ElementClass.GetEventCapture = GetEventCapture;
ElementClass.GetHitMask = GetHitMask;

-- Callbacky

ElementClass.SetCallback = SetCallback


-- Destroys

ElementClass.Destroy = DestroyElement;
ElementClass.DestroyChildren = DestroyChildren;

-- hints

ElementClass.SetHint = SetHint;
ElementClass.GetHint = GetHint;


---@class LabelClass : ElementClass
--   Textové elementy (Label) — font, zarovnání, stín, obrys, škálování.
LabelClass = {};
setmetatable(LabelClass, { __index = ElementClass });

-- Barvy textu

LabelClass.SetFontColor    = SetFontColor;
LabelClass.SetShadowColor  = SetShadowColor;
LabelClass.SetOutlineColor = SetOutlineColor;

LabelClass.GetFontColor    = GetFontColor;
LabelClass.GetShadowColor  = GetShadowColor;
LabelClass.GetOutlineColor = GetOutlineColor;

-- Text

LabelClass.SetText     = SetText;
LabelClass.SetFont     = SetFont;
LabelClass.SetFontSize = SetFontSize;
LabelClass.SetAutoSizeWidth = SetAutoSizeWidth;
LabelClass.SetAutoSizeHeight = SetAutoSizeHeight;
LabelClass.SetAutoSizeWidthMax = SetAutoSizeWidthMax;
LabelClass.SetAutoSizeHeightMax = SetAutoSizeHeightMax;
LabelClass.SetHAlign   = SetHAlign;
LabelClass.SetVAlign   = SetVAlign;
LabelClass.SetWordWrap = SetWordWrap;
LabelClass.SetShowRich = SetShowRich;

LabelClass.GetText     = GetText;
LabelClass.GetFont     = GetFont;
LabelClass.GetFontSize = GetFontSize;
LabelClass.GetAutoSizeWidth = GetAutoSizeWidth;
LabelClass.GetAutoSizeHeight = GetAutoSizeHeight;
LabelClass.GetAutoSizeWidthMax = GetAutoSizeWidthMax;
LabelClass.GetAutoSizeHeightMax = GetAutoSizeHeightMax;
LabelClass.GetHAlign   = GetHAlign;
LabelClass.GetVAlign   = GetVAlign;
LabelClass.GetWordWrap = GetWordWrap;
LabelClass.GetShowRich = GetShowRich;

-- Stín textu

LabelClass.SetShadow        = SetShadow;
LabelClass.SetShadowOffsetX = SetShadowOffsetX;
LabelClass.SetShadowOffsetY = SetShadowOffsetY;

LabelClass.GetShadow        = GetShadow;
LabelClass.GetShadowOffsetX = GetShadowOffsetX;
LabelClass.GetShadowOffsetY = GetShadowOffsetY;

-- Obrys textu

LabelClass.SetOutline      = SetOutline;
LabelClass.SetOutlineWidth = SetOutlineWidth;

LabelClass.GetOutline      = GetOutline;
LabelClass.GetOutlineWidth = GetOutlineWidth;


-- Škálování znaků

LabelClass.SetCharScaleX  = SetCharScaleX;
LabelClass.SetCharScaleY  = SetCharScaleY;
LabelClass.SetCharSpacing = SetCharSpacing;
LabelClass.SetLineSpacing = SetLineSpacing;

LabelClass.GetCharScaleX  = GetCharScaleX;
LabelClass.GetCharScaleY  = GetCharScaleY;
LabelClass.GetCharSpacing = GetCharSpacing;
LabelClass.GetLineSpacing = GetLineSpacing;

-- Scrolování textu
LabelClass.SetScrollText      = SetScrollText;
LabelClass.SetScrollTextSpeed = SetScrollTextSpeed;
LabelClass.SetScrollTextDelay = SetScrollTextDelay;

LabelClass.GetScrollText      = GetScrollText;
LabelClass.GetScrollTextSpeed = GetScrollTextSpeed;
LabelClass.GetScrollTextDelay = GetScrollTextDelay;



-- Scrollbox
ScrollboxClass = {};
setmetatable(ScrollboxClass, { __index = ElementClass });
ScrollboxClass.SetScrollOffset = SetScrollOffset;
ScrollboxClass.SetScrollOffsetX = SetScrollOffsetX;
ScrollboxClass.SetScrollOffsetY = SetScrollOffsetY;
ScrollboxClass.SetBarToScrollbox = SetBarToScrollbox;
ScrollboxClass.SetHorizontalScroll = SetHorizontalScroll;

ScrollboxClass.GetScrollOffset = GetScrollOffset;
ScrollboxClass.GetHorizontalScroll = GetHorizontalScroll;

ScrollboxClass.GetScrollbar = GetScrollbar;
ScrollboxClass.GetScrollbar2 = GetScrollbar2;

-- TextLog
TextlogClass = {}
setmetatable(TextlogClass, { __index = LabelClass});
TextlogClass.SetScrollOffsetY = SetScrollOffsetY;
TextlogClass.SetBarToScrollbox = SetBarToScrollbox;
TextlogClass.GetScrollOffset = GetScrollOffset;
TextlogClass.GetScrollbar = GetScrollbar;
TextlogClass.AddLine = AddLine;
TextlogClass.CleanLines = CleanLines;
TextlogClass.SetMaxLines = SetMaxLines;
TextlogClass.SetAddAtBottom = GetAddAtBottom;
TextlogClass.GetMaxLines = SetMaxLines;
TextlogClass.GetAddAtBottom = GetAddAtBottom;


-- Scrallbar
ScrollbarClass = {};
setmetatable(ScrollbarClass, { __index = ElementClass });
ScrollbarClass.GetScrollbarType = GetScrollbarType;

TextFieldClass = {};
setmetatable(TextFieldClass, { __index = LabelClass});


-- Special elements
Desktop = eReg:Get(0);
Mouse = eReg:Get(1);
FPSCounter = eReg:Get(2);
Profiler = eReg:Get(3);


Mouse.GetTexture = GetTexture;
-- return X, Y
function Mouse:GetHotSpot()             return GUI.GetCursorHotSpot() end;
-- set texture and hotspot, x and y are optional (default 0)
function Mouse:SetCursor(texture, x, y) GUI.SetCursor(texture, x or 0, y or 0); end;
-- set texture only, keeps previous hotspot
function Mouse:SetTexture(texture)      GUI.SetCursorTexture(texture); end;
-- reset cursor to system default
function Mouse:ResetCursor()            GUI.ResetCursor() end;
-- mouse position on screen
function Mouse:GetPos()                 return GUI.GetMouseX(), GUI.GetMouseY(); end;
-- position relative to element under mouse, best used inside callbacks
function Mouse:GetRelativePos()         return GUI.GetMouseLocalX(), GUI.GetMouseLocalY(); end;
-- move mouse to position
function Mouse:SetPos(x, y)             GUI.SetCursorPos(x, y); end;
-- lock mouse inside the window
function Mouse:SetLock(bool)            GUI.SetCursorLocked(bool); end;
-- FPS mode: hidden + locked against movement, raw delta
function Mouse:SetCaptured(bool)        GUI.SetCursorCaptured(bool) end;
-- hidden but free to move (SetCaptured overrides this)
function Mouse:SetHidden(bool)          GUI.SetCursorHidden(bool) end;
-- movement delta since last frame, best called every frame
function Mouse:GetDelta()               return GUI.GetMouseDeltaX(), GUI.GetMouseDeltaY() end;



setmetatable(Desktop, { __index = ElementClass });
setmetatable(FPSCounter, { __index = LabelClass });
setmetatable(Profiler, { __index = LabelClass });


