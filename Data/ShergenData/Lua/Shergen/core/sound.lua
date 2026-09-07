Sound = ListClass.Make();

VOLUME_DEFAULT = AUDIO.AddChannel();

Sound.opVID     = VOLUME_DEFAULT;

function Sound.Play(FILENAME,VOLUMEID,CALLBACK,LOOP,PITCH)
	Sound:Add({ID = AUDIO.LoadSound(FILENAME,VOLUMEID ~= nil and VOLUMEID or VOLUME_DEFAULT,true,LOOP,PITCH, Sound.Finish, Sound.COUNT+1), Callback = CALLBACK});
	AUDIO.PlaySound(Sound:GetLast().ID);
	return Sound:GetLast().ID;
end;

function Sound.Finish(ID)
	local s = Sound:Get(ID);
	AUDIO.FreeSound(s.ID);
	switch(type(s.Callback)):caseof{
		--"string" = function RunString(call); end,
		["function"] = function (x) s.Callback(); end,
		["table"] = function (x) s.Callback[1](table.unpack(s.Callback, 2)) end,
	};
	Sound:DeleteID(ID);
end;