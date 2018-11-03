EnsureDataLoaded();

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals);

var var_i = Data.Variables.IndexOf(Data.Variables.DefineLocal(1, "i", Data.Strings, Data));
code.Append(Assembler.Assemble(@"
.localvar 1 i " + var_i + @"
pushglb.v global.debug
pushi.e 1
cmp.i.v EQ
bf func_end

push.i " + 0x00FFFF.ToString() + @"
conv.i.v
call.i draw_set_color(argc=1)
popz.v

pushi.e 0
pop.v.i local.i

loop: pushi.e -5
pushloc.v local.i
conv.v.i
push.v [array]msg
pushloc.v local.i
pushi.e 15
mul.i.v
pushi.e 50
add.i.v
pushi.e 50
conv.i.v
call.i draw_text(argc=3)
popz.v

pushloc.v local.i
pushi.e 1
add.i.v
pop.v local.i

pushloc.v local.i
pushi.e 100
cmp.i.v LT
bf exit_loop
b loop

exit_loop: pushi.e " + Data.GameObjects.IndexOf(Data.GameObjects.ByName("obj_writer")) + @"
pushenv_start: pushenv func_end
push.v self.mystring
pushi.e 30
conv.i.v
pushi.e 70
conv.i.v
call.i draw_text(argc=3)
popz.v
popenv pushenv_start
", Data.Functions, Data.Variables, Data.Strings));

ChangeSelection(code);