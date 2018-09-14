// Replaces the debug mode "Create system_information_962" option with "Go to room"

EnsureDataLoaded();

ScriptMessage("Add 'Go to room' dialog under F3\nby krzys_h");

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f3, Data.Strings);

Data.Functions.EnsureDefined("get_integer", Data.Strings);
code.Replace(Assembler.Assemble(@"
pushvar.v room@23
push.s ""Go to room""
conv.s.v
call.i get_integer(argc=2)
call.i room_goto(argc=1)
popz.i
", Data.Functions, Data.Variables, Data.Strings));

ChangeSelection(code);
ScriptMessage("Patched!");