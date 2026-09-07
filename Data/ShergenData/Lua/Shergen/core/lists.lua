----------------------------------------------
--  Name:      ListClass
--	Version:   1.0
--  Author:    Stucuk
--  Category:  Utils
----------------------------------------------
ListClass = {};

function ListClass.Make(useShift)
	local list = {LIST={}, COUNT=0, USESHIFT = useShift}
	
	setmetatable(list, { __index = ListClass });
	
	return list;
end;

function ListClass:Add(DATA)
	self.COUNT=self.COUNT+1;
	DATA.inList=self;
	DATA.inListID = self.COUNT;
	self.LIST[self.COUNT] = DATA;

	return DATA;
end;

function ListClass:Set(ID,DATA)
	if ID > self.COUNT then
        	self.COUNT=ID;
        end;
	DATA.inList   = self;
	DATA.inListID = ID;
	self.LIST[ID] = DATA;

	return DATA;
end;

function ListClass:Delete(DATA)
	if DATA.inList==self then
		self:DeleteID(DATA.inListID);
		DATA.inList = nil;
		DATA.inListID = nil;
	end;
end;

function ListClass:DeleteID(ID)
	self.LIST[ID] = nil;
	self:Trim();
end;

function ListClass:Get(ID)
	return self.LIST[ID];
end;

function ListClass:GetLast()
	return self.LIST[self.COUNT];
end;

function ListClass:FindFree()
	for i=1,self.COUNT do
		if self:get(i) == nil then
                	return i;
                end;
	end;
        return -1;
end;

function ListClass:FindUsed(START)
	for i=START,self.COUNT do
		if self:get(i) ~= nil then
                	return i;
                end;
	end;
        return -1;
end;

function ListClass:Trim()
	if self.USESHIFT then
	        local ff,fu = self:FindFree(),-1;
        	fu = self:FindUsed(ff+1);

	        while (ff > -1) and (fu > -1) do
        	        self.LIST[ff] = self.LIST[fu];
	                self.LIST[fu] = nil;

        		ff = self:FindFree();
	                if ff > -1 then
                		fu = self:FindUsed(ff+1);
        	        end;
	        end;
        end;

	while (self.COUNT > 0) and (self.LIST[self.COUNT] == nil) do
		self.COUNT = self.COUNT-1;
	end;
end;
