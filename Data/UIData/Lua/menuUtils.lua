-----------------------------------------------------------------------------
---  Orig. File : /lua/menu/menuUtils.lua
---  Version    : 6
---
---  Summary    : Utils for modern main menu.
---
---  Created    : Petr 'Sali' Salak, Freya Group
------------------------------------------------------------------------------
VOLUME_EFFECTS = AUDIO.AddChannel();
ClickSoundID = AUDIO.LoadSound("sounds/click.ogg");


function NewMainMenuButton(parent,anchor,Y,name,callback,color,fontName,fontColor);
	return NewMainMenuButtonEX(parent,anchor,XYWH(0,Y,parent.width,40),name,callback,color,fontName,fontColor);

end;

function NewMainMenuButtonEX(parent,anchor,coors,name,callback,color,font,fontColor);
	if not font then
		font = menuButtonsFont;
	end;
	if not fontColor then
		fontColor = MenuFontColor_Base;
	end;
	if not color then
		color = MenuColor_Base;
	end;
	
	
	local L = NewElement(parent,anchor,coors,true,{color=BLACKA(50)});
	L:SetBorder(BORDER_TYPE.OUTER,1,MenuColor_Border );
	
	L.lighter = NewElement(L,anchorLT,XYWH(0,0,L.width,L.height),true,{color=color, fade = 0, pass_through=true,texture = 'interface/menuButton.png'}); 
	
	L.label = NewLabel(L,anchorLT,XYWH(0,0,L.width,L.height),font,name,{pass_through=true,font_color = fontColor, text_halign=ALIGN.MIDDLE, text_valign=ALIGN.MIDDLE,shadow=true, shadow_offset_x = 2,shadow_offset_y = 2,scissor=true});
	L.hovered = false;

	function L:SetText(text)
		L.label:SetText(text);
	end;

	L.normalDark  = function () 
		--print("Dark called for " .. L.label.text); 
		L.hovered = false;
		AddEventFade(L.lighter,0,menu_fade_time);   
		AddEventFontColor(L.label, fontColor ,menu_fade_time); 
		AddEventFontSize(L.label,font.SIZE, menu_fade_time); 
	end;
	L.normalClick = function () 
		AUDIO.PlaySound(ClickSoundID); 
		AddEventFade(L.lighter,120,menu_fade_time); 
		AddEventFontColor(L.label, fontColor ,menu_fade_time); 
		AddEventFontSize(L.label,font.SIZE +2,menu_fade_time); 
	end;
	L.normalHover = function () 
		--print("Hover called for " .. L.label.text); 
		L.hovered = true;
		AddEventFade(L.lighter,255,menu_fade_time); 
		AddEventFontColor(L.label, WHITE() ,menu_fade_time);   
		AddEventFontSize(L.label,font.SIZE +2,menu_fade_time); 
	end; 
	L.nomralLUP = function ()
		if L.hovered then L.normalHover(); end; 
	end;

	L:SetCallback (CALLBACK.MOUSEOVER,L.normalHover);
	L:SetCallback (CALLBACK.MOUSELEAVE,L.normalDark);
	L:SetCallback (CALLBACK.CLICKTAP,callback);
	L:SetCallback (CALLBACK.LDOWN,L.normalClick);
	L:SetCallback (CALLBACK.LUP,L.nomralLUP);

	return L;
end;
