-----------------------------------------------------------------------------
---  Orig. File : /lua/window_mainmenulua
---  Version    : 1
---
---  Summary    : Main Menu 
---
---  Created    : Petr 'Sali' Salak, Freya Group
------------------------------------------------------------------------------
wMP =  (1/1920)*ScrWidth ;
hMP =  (1/1080)*ScrHeight ;
LayoutWidth = ScrWidth;
LayoutHeight = ScrHeight;
menuButtonsFontName = wMP < 0.83 and Tahoma_18 or (wMP < 1.78 and Tahoma_22 or Tahoma_30);

menu = NewElement(nil,anchorLTRB,XYWH(0,0,LayoutWidth,LayoutHeight),true,{texture='interface/shergen_background.png',color=WHITE() });

version = NewLabel(menu,anchorLRB,XYWH(0,LayoutHeight-24*2,LayoutWidth-5,24),Tahoma_12B,"Version: " .. VERSION, {nomouseevent=true, text_halign=ALIGN.RIGHT});


menu.side =	NewElement(menu,anchorLTB,XYWH(0,0,290,LayoutHeight),true,{ gradient=true, color=MenuColor_Background1, gradient_color=MenuColor_Background1, gradient_color2=MenuColor_Background2, scissor=true });--BLACKA(153)});
menu.side.main = NewElement(menu.side,anchorLTB,XYWH(0,0,menu.side.width-4,menu.side.height),true,{color=WHITEA(0)});
menu.side.grad = NewElement(menu.side,anchorLTB,XYWH(menu.side.width-3,-2,2,LayoutHeight+2),true,{color=MenuColor_Base,border_type=BORDER_TYPE.OUTER, border_color=MenuColor_Border,border_size=1});



menu.side.main.quit = NewMainMenuButton(menu.side.main,anchorL,(menu.side.main.height/2)+50+4*41,"QUIT",GUI.Exit,MenuColor_Quit);

