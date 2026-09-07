-- This file is used to include all the necessary Lua files for the core functionality of the application. 
-- It includes localization files based on the selected language and utility files for various functionalities.
-- Must be included before any other Lua files to ensure that all necessary functions and constants are available.
-- Also includes other modules of Shergen 

include('loc/Shergen/Core_ENG');
if LANG and LANG ~= "ENG" then
	tryInclude('loc/Shergen/Core_' .. LANG);
end;

include("lua/shergen/core/lists", "lua/shergen/core/utils", "lua/shergen/core/colors", "lua/shergen/core/mainConstants", "lua/shergen/core/keyCode" )

local luaFiles = File.GetFiles("lua/shergen/core", "lua", true);

table.diff(luaFiles,{"lists","colors","utils", "keyCode", "mainConstants"});
for _, luaFile in ipairs(luaFiles) do
    include("lua/shergen/core/" .. luaFile);
end;


luaFiles = File.GetFiles("lua/shergen", "lua", true);
table.diff(luaFiles,{"core"});

for _, luaFile in ipairs(luaFiles) do
	tryInclude('loc/Shergen/' .. luaFile .. '_ENG');
	if LANG and LANG ~= "ENG" then
		tryInclude('loc/Shergen/' .. luaFile .. '_' .. LANG);
	end;
    include("lua/shergen/" .. luaFile);
end;






