// Adds a text on the main menu! :D

EnsureDataLoaded();

var code = Data.GameObjects.ByName("obj_intromenu").EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data.Strings, Data.Code, Data.CodeLocals);
code.Append(Assembler.Assemble(@"
push.s ""This is a test#I can put whatever I want here now#hehe, this thing is working!""
conv.s.v
pushi.e 50
conv.i.v
pushi.e 200
conv.i.v
call.i scr_drawtext_centered(argc=3)
popz.v
", Data.Functions, Data.Variables, Data.Strings));

ChangeSelection(code);