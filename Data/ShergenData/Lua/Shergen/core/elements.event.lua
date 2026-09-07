Events = ListClass.Make();

function Events.AddEvent(ID,NEWVALUE,TIME,CALLBACK,ETYPE,HANDLE,MAKE)
    Events.CancelForID(ID, ETYPE); 
	local event = MAKE(ID,NEWVALUE,TIME,CALLBACK,ETYPE);
        event.handleEvent = HANDLE;
        Events:Add(event);
end;

-------------------[ CANCEL EVENT ]-------------------
-- Remove all pending events for a given element ID. If ETYPE is passed, only
-- events of that type are cancelled. Useful when replacing an in-flight animation 
function Events.Cancel(element,ETYPE)
    Events.CancelForID(element.ID,ETYPE)
end;

function Events.CancelForID(ID,ETYPE)
    local i = 1;
    while i <= Events.COUNT do
        local e = Events:Get(i);
        if (e ~= nil) and (e.id == ID) and ((ETYPE == nil) or (e.type == ETYPE)) then
            Events:Delete(e);
        else
            i = i + 1;
        end;
    end;
end;

---------------------[ RUNTIME ]----------------------

function Events.Tick(FRAMETIME)
	local event = {};
        for i = 1,Events.COUNT do
		event = Events:Get(i);
                if event ~= nil then
					event.handleEvent(event,FRAMETIME);
                end;
        end;
end;

GUI.RegisterTickCallback(Events.Tick);

-----------------[ PROP EVENTS ]-------------------

function Events.MakeEvent(ID,NEWVALUE,TIME,CALLBACK,ETYPE)
   local curValue = GUI.GetProperty(ID, ETYPE);
   local diff = NEWVALUE-curValue;
   local event = {
       type = ETYPE,
       id = ID,
       diff = math.abs(diff),
       sign = math.sign(diff),
       value = NEWVALUE,
       curvalue =curValue,
       time = TIME,
       callback= CALLBACK,
       timepassed=0
   }
   return event;
end;

function Events.HandleEvent(EVENT,FRAMETIME)
    EVENT.timepassed = math.min (EVENT.timepassed + FRAMETIME, EVENT.time);
    if (EVENT.timepassed >= EVENT.time) then
        GUI.SetProperty(EVENT.id, EVENT.type,EVENT.value);
        local call = EVENT.callback;

        Events:Delete(EVENT);

        switch(type(call)):caseof{
            --"string" = function RunString(call); end,
            ["function"] = function (x) call(); end,
            ["table"] = function (x) call[1](table.unpack(call, 2)) end,
        };
        return;
    end;

    GUI.SetProperty(EVENT.id, EVENT.type, EVENT.curvalue + ( (EVENT.diff/EVENT.time) * EVENT.timepassed * EVENT.sign)  );
end;

function Events.AddEvent_Default(ID,NEWVALUE,TIME,CALLBACK,ETYPE)
	Events.AddEvent(ID,NEWVALUE,TIME,CALLBACK,ETYPE,Events.HandleEvent,Events.MakeEvent);
end;

-----------------[ COLOR EVENTS ]-------------------
function RGBSubtract(COL_A,COL_B)
	return RGBA(COL_A.R-COL_B.R,COL_A.G-COL_B.G,COL_A.B-COL_B.B,COL_A.A-COL_B.A);
end;

function AbsColor(COL)
	return RGBA(math.abs(COL.R),math.abs(COL.G),math.abs(COL.B),math.abs(COL.A));
end;

function SignColor(COL)
	return RGBA(math.sign(COL.R),math.sign(COL.G),math.sign(COL.B),math.sign(COL.A));
end;


function Events.MakeEvent_Color(ID,NEWVALUE,TIME,CALLBACK,ETYPE)
    local curValue = GUI.GetProperty(ID, ETYPE);
    local diff = RGBSubtract(NEWVALUE,curValue);
    local event = {
        type=ETYPE,
        id=ID,
        diff=AbsColor(diff),
        sign=SignColor(diff),
        value=NEWVALUE,
        curvalue=curValue,
        time=TIME,
        callback=CALLBACK,
        timepassed=0,
    }
    return event;
end;

function Events.HandleEvent_Color(EVENT,FRAMETIME)
    EVENT.timepassed = math.min(EVENT.timepassed+FRAMETIME,EVENT.time);
    if (EVENT.timepassed >= EVENT.time) then
        GUI.SetProperty(EVENT.id,EVENT.type,EVENT.value);
        local call = EVENT.callback;

        Events:Delete(EVENT);

        switch(type(call)):caseof{
            --"string" = function RunString(call); end,
            ["function"] = function (x) call(); end,
            ["table"] = function (x) local c = call[1]; table.removeValue(call, c); c(table.unpack(call)); end,
        };
        return;
    end;

    local r = EVENT.curvalue.R   + (math.min(EVENT.diff.R,  (EVENT.diff.R/EVENT.time)  *EVENT.timepassed)*EVENT.sign.R);
    local g = EVENT.curvalue.G + (math.min(EVENT.diff.G,(EVENT.diff.G/EVENT.time)*EVENT.timepassed)*EVENT.sign.G);
    local b = EVENT.curvalue.B  + (math.min(EVENT.diff.B, (EVENT.diff.B/EVENT.time) *EVENT.timepassed)*EVENT.sign.B);
    local a = EVENT.curvalue.A + (math.min(EVENT.diff.A,(EVENT.diff.A/EVENT.time)*EVENT.timepassed)*EVENT.sign.A);

    GUI.SetProperty(EVENT.id,EVENT.type,RGBA(r,g,b,a));
end;

function Events.AddEvent_Color(ID,NEWVALUE,TIME,CALLBACK,ETYPE)
	Events.AddEvent(ID,NEWVALUE,TIME,CALLBACK,ETYPE,Events.HandleEvent_Color,Events.MakeEvent_Color);
end;



----------------- [Shortcuts events] -----------------


function AddEventSlideX(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Default(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.X);
end;

function AddEventSlideY(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Default(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.Y);
end;

function AddEventWidth(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Default(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.WIDTH);
end;

function AddEventHeight(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Default(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.HEIGHT);
end;

function AddEventFontSize(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Default(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.FONT_SIZE);
end;

function AddEventFade(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Default(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.FADE);
end;

function AddEventGrayscale(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Default(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.GRAYSCALE);
end;

function AddEventColor1(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Color(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.COLOR1);
end;

function AddEventColor2(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Color(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.COLOR2);
end;

function AddEventColor3(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Color(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.COLOR3);
end;

function AddEventBorderColor(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Color(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.BORDER_COLOR);
end;

function AddEventFontColor(ELEMENT,NEWVALUE,TIME,CALLBACK)
	Events.AddEvent_Color(ELEMENT.ID,NEWVALUE,TIME,CALLBACK,PROP.FONT_COLOR);
end;
