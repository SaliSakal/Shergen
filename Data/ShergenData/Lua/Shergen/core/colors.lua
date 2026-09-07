----------------------------------------------
--  Name:      GUI Color Helpers
--  Category:  Utils
----------------------------------------------

-- ── Předdefinované barvy (RGB) ─────────────────────────────────────────────

--- create RGBA table with same RGB and different A
---@param gray number 0-255
---@param alpha number 0-255
---@return RGBA
function GRAYA(gray,alpha)
	return RGBA(gray,gray,gray,alpha);
end;

--- create RGBA table with same RGB and 255 Alpha
---@param gray number 0-255
---@return RGBA
function GRAY(gray)
	return GRAYA(gray,255);
end;
--- create RGBA table with black color with custom Alpha
---@param alpha number 0-255
---@return RGBA
function BLACKA(alpha)
	return RGBA(0,0,0,alpha);
end;
--- create RGBA table with black color with 255 Alpha
---@return RGBA
function BLACK()
	return BLACKA(255);
end;
--- create RGBA table with white color with custom Alpha
---@param alpha number 0-255
---@return RGBA
function WHITEA(alpha)
	return RGBA(255,255,255,alpha);
end;
--- create RGBA table with white color with 255 Alpha
---@return RGBA
function WHITE()
	return WHITEA(255);
end;


-- Standard terminal 16 + rozšířené
-- Standard 16 terminal colours + extended named colours
COLOR = {
    -- Standard terminal colors
    Black         = RGB(  0,   0,   0),
    Red           = RGB(128,   0,   0),
    Green         = RGB(  0, 128,   0),
    Yellow        = RGB(128, 128,   0),
    Blue          = RGB(  0,   0, 128),
    Magenta       = RGB(128,   0, 128),
    Cyan          = RGB(  0, 128, 128),
    White         = RGB(192, 192, 192),

    -- Bright variants
    BrightBlack   = RGB(128, 128, 128),
    BrightRed     = RGB(255,  85,  85),
    BrightGreen   = RGB( 85, 255,  85),
    BrightYellow  = RGB(255, 255,  85),
    BrightBlue    = RGB( 85,  85, 255),
    BrightMagenta = RGB(255,  85, 255),
    BrightCyan    = RGB( 85, 255, 255),
    BrightWhite   = RGB(255, 255, 255),

    -- Dark variants (~60 % of base terminal colour)
    DarkGray      = RGB( 64,  64,  64),
    DarkRed       = RGB( 75,   0,   0),
    DarkGreen     = RGB(  0,  75,   0),
    DarkBlue      = RGB(  0,   0,  75),
    DarkCyan      = RGB(  0,  75,  75),

    -- Pure / maximum-saturation variants
    PureRed       = RGB(255,   0,   0),
    PureGreen     = RGB(  0, 255,   0),
    PureBlue      = RGB(  0,   0, 255),
    PureYellow    = RGB(255, 255,   0),
    PureMagenta   = RGB(255,   0, 255),
    PureCyan      = RGB(  0, 255, 255),

    -- Grays
    Gray          = RGB(128, 128, 128),  -- alias: BrightBlack
    LightGray     = RGB(192, 192, 192),  -- alias: White
    Silver        = RGB(192, 192, 192),
    Gainsboro     = RGB(220, 220, 220),

    -- Reds & pinks
    Crimson       = RGB(220,  20,  60),
    Tomato        = RGB(255,  99,  71),
    Coral         = RGB(255, 127,  80),
    Salmon        = RGB(250, 128, 114),
    Pink          = RGB(255, 192, 203),
    HotPink       = RGB(255, 105, 180),
    DeepPink      = RGB(255,  20, 147),

    -- Oranges & browns
    OrangeRed     = RGB(255,  69,   0),
    Orange        = RGB(255, 165,   0),
    Gold          = RGB(255, 215,   0),
    Chocolate     = RGB(210, 105,  30),
    SaddleBrown   = RGB(139,  69,  19),
    Brown         = RGB(165,  42,  42),
    Tan           = RGB(210, 180, 140),

    -- Greens
    ForestGreen   = RGB( 34, 139,  34),
    Olive         = RGB(128, 128,   0),  -- alias: Yellow
    LimeGreen     = RGB( 50, 205,  50),
    Lime          = RGB(  0, 255,   0),  -- alias: PureGreen
    SpringGreen   = RGB(  0, 255, 127),

    -- Blues
    Navy          = RGB(  0,   0, 128),  -- alias: Blue
    RoyalBlue     = RGB( 65, 105, 225),
    SteelBlue     = RGB( 70, 130, 180),
    DodgerBlue    = RGB( 30, 144, 255),
    SkyBlue       = RGB(135, 206, 235),
    LightBlue     = RGB(173, 216, 230),

    -- Purples
    Indigo        = RGB( 75,   0, 130),
    DarkViolet    = RGB(148,   0, 211),
    Purple        = RGB(128,   0, 128),  -- alias: Magenta
    MediumPurple  = RGB(147, 112, 219),
    Violet        = RGB(238, 130, 238),
    Orchid        = RGB(218, 112, 214),
    Plum          = RGB(221, 160, 221),

    -- Teals & cyans
    Teal          = RGB(  0, 128, 128),  -- alias: Cyan
    Turquoise     = RGB( 64, 224, 208),
    Aquamarine    = RGB(127, 255, 212),
};

-- ── Interní helper: rozbalí RGB/RGBA table → r, g, b, a ──────────────────────
local function unpackRGBA(c)
    return c.red, c.green, c.blue, c.alpha;
end;

local function unpackRGB(c)
    return c.red, c.green, c.blue;
end;


-- ── Interní helper: rozbalí RGB/RGBA table → r, g, b, a ──────────────────────
local function unpackRGBA(c)
    return c.red, c.green, c.blue, c.alpha;
end;

local function unpackRGB(c)
    return c.red, c.green, c.blue;
end;