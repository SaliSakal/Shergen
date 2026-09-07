namespace Shergen
{

    // Enum pro typy elementů
    public enum UIElementType
    {
        nil, Element, Mouse, Label, ScrollBox, ScrollText, ScrollBar, TextLog, TextField // TYPE_ELEMENT
    }

    /// <summary>
    /// Implemented by elements that can be scrolled (UIScrollBox, UITextLog).
    /// UIScrollBar.Target accepts any IScrollable.
    /// </summary>
    public interface IScrollable
    {
        float Height        { get; }
        float Width         { get; }
        float ContentHeight { get; }   // total scrollable content height
        float ContentWidth  { get; }   // total scrollable content width
        float ScrollOffsetY { get; set; }
        float ScrollOffsetX { get; set; }
        float MaxScrollY    { get; }
        float MaxScrollX    { get; }
        float ScrollTargetY { get; set; }
        float ScrollTargetX { get; set; }
    }

    public enum UIAling
    {
        nil, Top = 1, Left = 1, Middle = 2, Right = 3, Bottom = 3 // Aling
    }

    // Enum pro vlastnosti
    public enum UIProperty
    {
        nil, X, Y, Width, Height, CanFocus,
        Anchor,  Visible, Scissor, Border_Size, Border_Type, Border_Color,
        PassThrough, EventCapture, HitMask, ScissorMask, ScissorMaskThreshold,
        Texture, Texture2, Texture3, Texture_Fallback, Texture2_Fallback, Texture3_Fallback, TextureID, Texture2ID, Texture3ID, 
        SubCoords, TextureWidth, TextureHeight, Texture2Width, Texture2Height, Texture3Width, Texture3Height,
        Color1, Color2, Color3, Grayscale, Fade,

        Font, Font_Size, AutoSizeHeight, AutoSizeWidth, AutoSizeHeight_Max, AutoSizeWidth_Max,
        Font_Color, Text, Hint, Font_Col_Disabled, Font_Col_Over, Font_Col_Back, 
        HAling, VAling,
        RangeMin, RangeMax,
        CharScaleX, CharScaleY, CharSpacing, LineSpacing, WordWrap,
        ShadowEnabled, ShadowColor, ShadowOffsetX, ShadowOffsetY,
        OutlineEnabled, OutlineColor, OutlineWidth,

        ScrollText, ScrollText_Speed, ScrollText_Delay,

        ScrollOffsetX, ScrollOffsetY,    // pro scrollbox — interní offset obsahu
        ScrollBar, ScrollBar2,           // pro scrollbox — ID přiřazených scrollbarů
        ScrollBarType, ScrollBoxHorizontal,                  // pro scrollbar — horizontal/vertical

        // TextLog
        AddAtBottom, MaxLines,

        // Textfield
        PlaceHolder, ShowRich, CursorPosition, CursorColor, CursorSpeed, ReadOnly, MaxLength, PasswordMode,

        // mouse only
        Cursor_HotX, Cursor_HotY

    }

    public enum UIScrollBarType
    {
        Vertical, Horizontal
    }

    public enum UIBorder
    {
        None, Outer, Inner
    }

    /// <summary>
    /// Callback types for GUI.SetCallback(id, CALLBACK.*, func, ...).
    /// Per-button variants for click/down/up — no generic MOUSECLICK.
    /// </summary>
    public enum UICallback : int
    {
        // Mouse hover / move
        MouseOver   = 1,
        MouseLeave  = 2,
        MouseMove   = 3,
        MouseWheel  = 4,

        // Left button (0)
        LDown       = 10,
        LUp         = 11,
        LClick      = 12,
        LDblClick   = 13,
        LDrag       = 14,

        // Right button (1)
        RDown       = 20,
        RUp         = 21,
        RClick      = 22,
        RDblClick   = 23,

        // Middle button (2)
        MDown       = 30,
        MUp         = 31,
        MClick      = 32,
        MDblClick   = 33,

        // Button 4
        B4Down      = 40,
        B4Up        = 41,
        B4Click     = 42,
        B4DblClick  = 43,

        // Button 5
        B5Down      = 50,
        B5Up        = 51,
        B5Click     = 52,
        B5DblClick  = 53,

        // Keyboard
        KeyDown     = 60,
        KeyUp       = 61,
        KeyPress    = 62,   // Unicode text input

        // Element state
        Visibility  = 70,
        Resized = 71,
        Focus = 72,
        Blur        = 73,
        AnimEnd     = 75,

        Progress    = 76,

        // Touch-specific
        TouchDown = 80,
        TouchUp = 81,
        TouchMove = 82,
        TouchTap = 83,
        TouchDblTap = 84,

        // Universal — fires for LClick OR TouchTap
        ClickTap = 90,
        DblClickTap = 91,
    }




}


