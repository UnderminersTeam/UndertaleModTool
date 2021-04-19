// Adds a text on the main menu! :D
EnsureDataLoaded();

var code = Data.GameObjects.ByName("obj_intromenu").EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data.Strings, Data.Code, Data.CodeLocals);
code.AppendGML(@"scr_drawtext_centered(200, 50, ""This is a test\nI can put whatever I want here now\nhehe, this thing is working!"")", Data);
ChangeSelection(code);