--[[
        This unit holds all the functions to create elements
--]]

MOUSE = eReg:Get(1);
MOUSE.SetTexture = SetTexture;

FPSCounter = eReg:Get(2);
setmetatable(FPSCounter, { __index = LabelClass });

function NewElement(PARENT, ANCHOR, POSSIZE, VISIBLE, ELEMENT, CLASS)
	if ELEMENT.type == nil then
		ELEMENT.type = TYPE.ELEMENT;
	end;

	ELEMENT.parent  = PARENT;
	ELEMENT.anchor  = ANCHOR;
	ELEMENT.x       = POSSIZE.X;
	ELEMENT.y       = POSSIZE.Y;
	ELEMENT.width   = POSSIZE.W;
	if POSSIZE.H ~= 0 then
		ELEMENT.height  = POSSIZE.H;
	end;
	if ELEMENT.visible == nil then
		ELEMENT.visible = VISIBLE;
	end;
	ELEMENT = AddElement(ELEMENT);

	if ELEMENT.SKIN ~= nil then
		ELEMENT = AddSkinnedElement(ELEMENT, ELEMENT.SKIN);
	end;

	if POSSIZE.H == 0 then
		ELEMENT.height = 0;
	end;

	setmetatable(ELEMENT, { __index = CLASS or ElementClass });

	return ELEMENT;
end;


function NewLabel(PARENT, ANCHOR, POSSIZE, FONT, TEXT, ELEMENT)
	ELEMENT.type = TYPE.LABEL;

	-- FONT může být string (název fontu) nebo tabulka {NAME, SIZE, RANGE={MIN,MAX}, CHARSPACING, LINESPACING}
	if type(FONT) == "string" then
		if ELEMENT.font_name == nil then
			ELEMENT.font_name = FONT;
		end;
	elseif type(FONT) == "table" then
		if ELEMENT.font_name == nil and FONT.NAME ~= nil then
			ELEMENT.font_name = FONT.NAME;
		end;
		if ELEMENT.font_size == nil and FONT.SIZE ~= nil then
			ELEMENT.font_size = FONT.SIZE;
		end;
		if (ELEMENT.range_min == nil or ELEMENT.range_max == nil)  and FONT.RANGE ~= nil then
			if ELEMENT.range_min == nil then ELEMENT.range_min = FONT.RANGE.MIN; end;
			if ELEMENT.range_max == nil then ELEMENT.range_max = FONT.RANGE.MAX; end;
		end;
		if ELEMENT.char_spacing== nil and FONT.CHARSPACING ~= nil then
			ELEMENT.char_spacing = FONT.CHARSPACING;
		end;
		if ELEMENT.line_spacing== nil and FONT.LINESPACING ~= nil then
			ELEMENT.line_spacing = FONT.LINESPACING;
		end;
	end;

	ELEMENT.text = TEXT;

	return NewElement(PARENT, ANCHOR, POSSIZE, true, ELEMENT, LabelClass);
end;


function NewScrollbox(PARENT,ANCHOR,POSSIZE,ELEMENT)
	ELEMENT.type = TYPE.SCROLLBOX;
	ELEMENT.scissor = true;
	return NewElement(PARENT,ANCHOR,POSSIZE,true,ELEMENT,ScrollboxClass);
end;

function NewScrollbar(PARENT,ANCHOR,POSSIZE,BINDTO,ELEMENT)
        ELEMENT.type     = TYPE.SCROLLBAR;
        if ELEMENT.texture2 == nil then
                ELEMENT.texture2 = 'scrollbar.png';
        end;

		if ELEMENT.texture ~= nil then
			ELEMENT.color = WHITE();
		end;

		if ELEMENT.vertical == nil then
			ELEMENT.vertical = true;
		else
			ELEMENT.vertical = ELEMENT.vertical;
		end;

        ELEMENT = NewElement(PARENT,ANCHOR,POSSIZE,true,ELEMENT, ScrollbarClass);

        if BINDTO ~= nil then
        	if ELEMENT.vertical then
                	SetBarToScrollbox(BINDTO,PROP.SCROLLBAR,ELEMENT);
                else
                	SetBarToScrollbox(BINDTO,PROP.SCROLLBAR2,ELEMENT);
                end;
        end;
		ELEMENT.text = TEXT;
        return ELEMENT;
end;

function NewScrollbar_RightOf(BINDTO,ANCHOR,WIDTH,ELEMENT)
        return NewScrollbar(GetParent(BINDTO),ANCHOR,XYWH(BINDTO.x+BINDTO.width+1,BINDTO.y,WIDTH,BINDTO.height),BINDTO,ELEMENT);
end;

function NewScrollbar_DownOf(BINDTO,ANCHOR,HEIGHT,ELEMENT)
		ELEMENT.vertical = false;
        return NewScrollbar(GetParent(BINDTO),ANCHOR,XYWH(BINDTO.x,BINDTO.y+BINDTO.height+1,BINDTO.width,HEIGHT),BINDTO,ELEMENT);
end;


function NewTextlog(PARENT,ANCHOR,POSSIZE,FONT,TEXT,ELEMENT)
	ELEMENT.type = TYPE.TEXTLOG;
	ELEMENT.scissor = true;
	ELEMENT.text_valign = ALIGN.TOP;

	-- FONT může být string (název fontu) nebo tabulka {NAME, SIZE, RANGE={MIN,MAX}, CHARSPACING, LINESPACING}
	if type(FONT) == "string" then
		if ELEMENT.font_name == nil then
			ELEMENT.font_name = FONT;
		end;
	elseif type(FONT) == "table" then
		if ELEMENT.font_name == nil and FONT.NAME ~= nil then
			ELEMENT.font_name = FONT.NAME;
		end;
		if ELEMENT.font_size == nil and FONT.SIZE ~= nil then
			ELEMENT.font_size = FONT.SIZE;
		end;
		if (ELEMENT.range_min == nil or ELEMENT.range_max == nil)  and FONT.RANGE ~= nil then
			if ELEMENT.range_min == nil then ELEMENT.range_min = FONT.RANGE.MIN; end;
			if ELEMENT.range_max == nil then ELEMENT.range_max = FONT.RANGE.MAX; end;
		end;
		if ELEMENT.char_spacing== nil and FONT.CHARSPACING ~= nil then
			ELEMENT.char_spacing = FONT.CHARSPACING;
		end;
		if ELEMENT.line_spacing== nil and FONT.LINESPACING ~= nil then
			ELEMENT.line_spacing = FONT.LINESPACING;
		end;
	end;
	ELEMENT.text = TEXT;
	return NewElement(PARENT,ANCHOR,POSSIZE,true,ELEMENT,TextlogClass);
end;


function NewTextField(PARENT, ANCHOR, POSSIZE, FONT, ELEMENT)
	ELEMENT.type = TYPE.TEXTFIELD;

	-- FONT může být string (název fontu) nebo tabulka {NAME, SIZE, RANGE={MIN,MAX}, CHARSPACING, LINESPACING}
	if type(FONT) == "string" then
		if ELEMENT.font_name == nil then
			ELEMENT.font_name = FONT;
		end;
	elseif type(FONT) == "table" then
		if ELEMENT.font_name == nil and FONT.NAME ~= nil then
			ELEMENT.font_name = FONT.NAME;
		end;
		if ELEMENT.font_size == nil and FONT.SIZE ~= nil then
			ELEMENT.font_size = FONT.SIZE;
		end;
		if (ELEMENT.range_min == nil or ELEMENT.range_max == nil)  and FONT.RANGE ~= nil then
			if ELEMENT.range_min == nil then ELEMENT.range_min = FONT.RANGE.MIN; end;
			if ELEMENT.range_max == nil then ELEMENT.range_max = FONT.RANGE.MAX; end;
		end;
		if ELEMENT.char_spacing== nil and FONT.CHARSPACING ~= nil then
			ELEMENT.char_spacing = FONT.CHARSPACING;
		end;
		if ELEMENT.line_spacing== nil and FONT.LINESPACING ~= nil then
			ELEMENT.line_spacing = FONT.LINESPACING;
		end;
	end;

	return NewElement(PARENT, ANCHOR, POSSIZE, true, ELEMENT, TextFieldClass);
end;
