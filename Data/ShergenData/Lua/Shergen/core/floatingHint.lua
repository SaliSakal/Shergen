----------------------------------------------
--  Name:      Floating Hint
--	Version:   2
--  Author:    Sali
--  Category:  Utils
----------------------------------------------
FloatingHint = NewLabel(Desktop, anchorNone, XYWH(0,0,300,10), Tahoma_12, "", {
    visible      = false,
    auto_size_width_max = 300,
    auto_size_height = true,   -- PROP.AUTO_SIZE_HEIGHT
    auto_size_width = true,
    font_color=RGB(221,224,211),
    wordwrap     = true,
    color        = BLACKA(210),
    font_color   = C_TEXT,
    border_type  = BORDER_TYPE.OUTER,
    border_color = GRAYA(80, 200),
    border_size  = 1,
    scissor      = true,
    nomouseevent = true,
    special      = true,
});

FloatingHint.timer = nil;
FloatingHint.delay = 0.6;

function HINT_SHOW(HINT,X,Y)
    HINT_HIDE();
    FloatingHint.timer = { text=HINT, x=X, y=Y, showAt=os.clock() + FloatingHint.delay }
    

end;

function HINT_HIDE()
    SetVisible(FloatingHint,false);
    FloatingHint.timer = nil;
end;

function HINT_UPDATE(FRAMETIME)
    if FloatingHint.timer then
        if os.clock() >= FloatingHint.timer.showAt then
            HINT_DRAW(FloatingHint.timer.text,FloatingHint.timer.x,FloatingHint.timer.y);
            FloatingHint.timer = nil;
        end;
    end;  
end;

function HINT_DRAW(HINT, X, Y)
    X,Y = Mouse:GetPos();  -- for better positioning, we can use mouse position instead of passed X,Y
    SetPos(FloatingHint,X,Y);
    SetText(FloatingHint,HINT);

    local ny = Y+34;

    if ny+GetHeight(FloatingHint) > ScrHeight then
        ny = ScrHeight-34-GetHeight(FloatingHint);
    end;

    SetY(FloatingHint,ny);

    if (X+GetWidth(FloatingHint) > ScrWidth) then
        SetX(FloatingHint,ScrWidth-GetWidth(FloatingHint));
    end;

    SetVisible(FloatingHint,true);
end;

GUI.RegisterTickCallback(HINT_UPDATE);