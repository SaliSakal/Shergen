menu_fade_time = 0.125;


------------- Nová paleta 
GoldR,    GoldG,    GoldB    = 200, 158, 80;    -- hlavní zlatá 
DisGoldR, DisGoldG, DisGoldB = 40,  30,  8;     -- ztlumená/disabled zlatá 
CreamR,   CreamG,   CreamB   = 245, 238, 224;   -- světlá krémová 
NearWR,   NearWG,   NearWB   = 250, 244, 228;   -- skoro bílá, teplá 
BorderR,  BorderG,  BorderB  = 18,  14,  10;    -- tmavý teplý border/pozadí 
BorderLR, BorderLG, BorderLB = 25,  20,  14;    -- světlejší border/gradient 

------------- New menu colors
MenuColor_Base = RGBA(GoldR,GoldG,GoldB);
MenuColor_BaseDissable = RGB(DisGoldR,DisGoldG,DisGoldB);
MenuColor_Lighter = RGB(CreamR,CreamG,CreamB);
MenuColor_NearbyWhite = RGB(NearWR,NearWG,NearWB);
MenuColor_Quit = RGB(230,112,76);
MenuColor_Back = RGB(GoldR*0.7,GoldG*0.7,GoldB*0.7);
MenuColor_White = RGB(255,255,255);
MenuColor_Gray = RGB(175,175,175);
MenuColor_Green = RGB(102,255,102);
MenuColor_Blue = RGB(102,178,255);
MenuColor_Red = RGB(255,102,102);
MenuColor_Easy = RGB(0,150,0);
MenuColor_Medium = RGB(150,150,0);
MenuColor_Hard = RGB(150,0,0);
MenuColor_Dissable = RGB(GoldR/2,GoldG/2,GoldB/2);
MenuColor_Delete = RGB(255,0,0);
MenuColor_Rename = RGB(0,255,0);
MenuColor_Time = RGB(200,200,200);
MenuColor_LightLine = RGB(GoldR+120,GoldG+120,GoldB+120);
MenuFontColor_Base = RGB(214,200,168); -- ztlumená teplá krémová (dřív 204,198,147)
MenuFontColor_lightRed = RGBA(200,150,150,255);
MenuFontColor_Dissable = RGB(255,150,150);
MenuFontColor_Red = RGB(255,50,50);
MenuFontColor_Green = RGB(50,255,50);
MenuFontColor_Yellow = RGB(255,255,50);
MenuFontColor_SteamName = RGB(204,198,247);
MenuFontColor_CanoTree = RGB(163,230,170);
MenuFontColor_LobbyVersion = RGB(1,199,199);
MenuColor_AddComp = RGB(105,35,105);
MenuColor_TechLevels = RGB(220,220,220);
MenuColor_PlayerUnReady = RGB(GoldR*0.5,GoldG*0.5,GoldB*0.5);
MenuColor_PlayerUnReadyBorder = RGB(GoldR,GoldG,GoldB);
MenuColor_PlayerReady = RGB(28,53,41);
MenuColor_PlayerReadyBorder = RGB(49,97,66);
MenuColor_PlayerServer = RGB(29,41,55);
MenuColor_PlayerServerBorder = RGB(46,70,96);
MenuColor_Bot = RGB(105*0.7,35*0.7,105*0.7);
MenuColor_BotBorder = RGB(105,35,105);
MenuColor_Border = RGB(BorderR,BorderG,BorderB);
MenuColor_Background1 = RGBA(GoldR*0.5,GoldG*0.5,GoldB*0.5,100);
MenuColor_Background2 = RGBA(36,30,22,220);
MenuColor_Background3 = RGBA(26,21,15,180);
MenuColor_Background4 = RGBA(27,22,16,150);
MenuColor_Background5 = RGBA(48,39,26,200);
MenuColor_Background6 = RGBA(27,22,16,180);
MenuColor_Background7 = RGBA(48,39,26,220);
MenuColor_Background8 = RGBA(27,22,16,200);
MenuColor_Background9 = RGBA(30,24,17,200);
MenuColor_Background10 = RGBA(14,11,8,180);
MenuColor_Background11 = RGBA(48,39,26,10);
MenuColor_SettingsBackground = BLACKA(80);
MenuColor_SettingsSelectedL = RGBA(GoldR,GoldG,GoldB,100);
MenuColor_SettingsSelectedLDisabled = RGBA(DisGoldR,DisGoldG,DisGoldB,100);
MenuColor_SettingsSelectedO = RGB(GoldR,GoldG,GoldB);
MenuColor_SettingsSelectedODisabled = RGB(DisGoldR,DisGoldG,DisGoldB);
MenuColor_SettingsBorder = MenuColor_Base;
MenuColor_SettingsBorderLighter = RGB(CreamR,CreamG,CreamB);
MenuColor_SettingsBorderDisabled = GRAY(90);
MenuColor_SettingsBorderLighterDisabled = MenuColor_Back;
MenuColor_SettingsRangeL= RGBA(GoldR,GoldG,GoldB,0);
MenuColor_SettingsRangeLDisabled= RGBA(DisGoldR,DisGoldG,DisGoldB,0);
MenuColor_SettingsRangeO = RGBA(GoldR,GoldG,GoldB,255);
MenuColor_SettingsRangeODisabled = RGBA(DisGoldR,DisGoldG,DisGoldB,255);
MenuColor_SettingsRangeArrowL = RGBA(CreamR,CreamG,CreamB,0);
MenuColor_SettingsRangeArrowLDisabled = RGBA(DisGoldR,DisGoldG,DisGoldB,0);
MenuColor_SettingsRangeArrowO = RGBA(CreamR,CreamG,CreamB,255);
MenuColor_SettingsRangeArrowODisabled = RGBA(DisGoldR,DisGoldG,DisGoldB,255);
MenuColor_SettingsRangeArrowSO = MenuColor_SettingsRangeO;
MenuColor_SettingsRangeArrowSODisabled = MenuColor_SettingsRangeODisabled;
MenuColor_SettingsBoolBoxL = RGBA(GoldR,GoldG,9,50);
MenuColor_SettingsBoolBoxLDisabled  = RGBA(DisGoldR,DisGoldG,DisGoldB,80);
MenuColor_SettingsBoolBoxO = RGBA(CreamR,CreamG,CreamB,100);
MenuColor_SettingsBoolBoxODisabled = RGBA(DisGoldR,DisGoldG,DisGoldB,255);
MenuColor_SettingsBoolL = RGBA(CreamR,CreamG,CreamB,200);
MenuColor_SettingsBoolLDisabled  = RGBA(DisGoldR,DisGoldG,DisGoldB,200);
MenuColor_SettingsBoolO = RGBA(CreamR,CreamG,CreamB,255);
MenuColor_SettingsBoolODisabled = RGBA(DisGoldR,DisGoldG,DisGoldB,255);


MenuFontColor_SettingsSelected = GRAY(255);

MenuColors_Edit = {
	color1=RGBA(BorderR,BorderG,BorderB,220),
	bevel=true,
	bevel_color1=MenuColor_Background6,
	bevel_color2=MenuColor_Background5,
};

COLOURS_DIALOG_EDIT      = MenuColors_Edit
COLOURS_DIALOG_LISTBOX   = {color1=GRAYA(10,150), bevel_color1=GRAY(22), bevel_color2=GRAY(30),};
COLOURS_DIALOG_RADIO     = {color1=RGBA(BorderLR,BorderLG,BorderLB,157),   color2=RGBA(CreamR,CreamG,CreamB,150),};
COLOURS_DIALOG_SCROLLBAR = {color1=GRAYA(60,127), color2=WHITE(),};


COLOURS_LOBBY_EDIT       = {

	bevel=true,
	bevel_color1=MenuColor_Background6,
	bevel_color2=MenuColor_Background5,
	gradient = true,
	gradient_color1=RGBA(BorderLR,BorderLG,BorderLB,200),
	gradient_color2=RGBA(BorderR,BorderG,BorderB,220),
	};

Lobby_Edit = {
	type=TYPE_EDIT,
	color1=IRCBackCol,
	autosize=false,
	bevel=true,
	bevel_color1=MenuColor_Background6,
	bevel_color2=MenuColor_Background5,
	gradient=true,
	gradient_color1=RGBA(BorderLR,BorderLG,BorderLB,200),
	gradient_color2=RGBA(BorderR,BorderG,BorderB,220),
}
-------------
Bevel_Highlight=MenuColor_Background6;
Bevel_Shadow=MenuColor_Background5;
Button_Color=RGB(GoldR,GoldG,GoldB);--GRAY(45);
Window_Color=MenuColor_Background2;
CheckBox_Color1=GRAYA(20,150);
CheckBox_Color2=GRAY(150);
CheckBox_Gradient_Color1=MenuColor_Background2;--GRAY(70);
CheckBox_Gradient_Color2=MenuColor_Background1;--GRAY(150);
ProgressBar_Color1=RGBA(BorderR,BorderG,BorderB,220);
ProgressBar_Color2=RGB(GoldR,GoldG,GoldB);--GRAY(150);
ProgressBar_Gradient_Color1=MenuColor_Background2;--GRAY(70);
ProgressBar_Gradient_Color2=MenuColor_Background1;--GRAY(150);
Edit_Color1=RGBA(BorderR,BorderG,BorderB,220);
Edit_Color2=RGBA(BorderLR,BorderLG,BorderLB,200);

Scrollbar_Color1=GRAYA(60,127);
Scrollbar_Color2=WHITE();
ListBox_Color1=GRAYA(20,200);

Window_Back={bevel=true,
	bevel_color1=GRAY(10),
	bevel_color2=GRAY(10),
	bevel = true,
	gradient_color1=MenuColor_Background9;--GRAYA(50,240),
	gradient_color2=MenuColor_Background10;--GRAYA(50,240),
	gradient=true};

Window_Skirmish={bevel=true,
	bevel_color1=GRAYA(0,0),
	bevel_color2=GRAYA(0,0),
	gradient_color1=MenuColor_Background7,
	gradient_color2=MenuColor_Background8,
	gradient=true};

WINDOW_BACKGROUND={bevel=true,
	bevel_color1=GRAY(10),
	bevel_color2=GRAY(10),
	gradient_color1=MenuColor_Background7,
	gradient_color2=MenuColor_Background8,
	gradient=true};

Window_Back2={bevel=true,
	bevel_color1=MenuColor_Background1,
	bevel_color2=MenuColor_Background1,
	gradient_color1=MenuColor_Background6,
	gradient_color2=MenuColor_Background5,
	gradient=true};

Window_Light = {
	highlight1=GRAY(106),
	highlight2=GRAY(63),
	col1=GRAY(86),
	col2=GRAY(43),
	b_highlight1=GRAY(60),
	b_highlight2=GRAY(90),
	b_shadow1=GRAY(30),
	b_shadow2=GRAY(50),
}

progressbar_merge={
	bevel_color1=GRAY(10),
	bevel_color2=GRAY(40),
	color1=ProgressBar_Color1,
	color2=ProgressBar_Color2,
	gradient=true,
	gradient_color1=ProgressBar_Gradient_Color1,
	gradient_color2=ProgressBar_Gradient_Color2,
};

checkbox_merge={
	bevel_color1=GRAY(10),
	bevel_color2=GRAY(40),
	color1=CheckBox_Color1,
	color2=CheckBox_Color2,
	gradient=true,
	gradient_color1=CheckBox_Gradient_Color1,
	gradient_color2=CheckBox_Gradient_Color2,
};

GradButton_Green = {
	highlight1=RGB(28,52,31),
	highlight2=RGB(22,40,24),
	col1=RGB(20,38,23),
	col2=RGB(16,29,18),
	b_highlight1=RGB(23,40,22),
	b_highlight2=RGB(32,55,30),
	b_shadow1=RGB(14,24,13),
	b_shadow2=RGB(19,33,18),
}

GradButton_Grey_Light = {
	highlight1=GRAY(106),
	highlight2=GRAY(63),
	col1=GRAY(86),
	col2=GRAY(43),
	b_highlight1=GRAY(60),
	b_highlight2=GRAY(90),
	b_shadow1=GRAY(30),
	b_shadow2=GRAY(50),
}

GradButton_Grey_Dark = {
	highlight1=GRAY(86/2),
	highlight1=GRAY(106/2),
	highlight2=GRAY(63/2),
	col1=GRAY(86/2),
	col2=GRAY(43/2),
	b_highlight1=GRAY(60/2),
	b_highlight2=GRAY(90/2),
	b_shadow1=GRAY(30/2),
	b_shadow2=GRAY(50/2),
}



----- FONTS ------
	menuButtonsFont = Tahoma_18;
