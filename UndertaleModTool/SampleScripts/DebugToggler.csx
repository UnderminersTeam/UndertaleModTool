// Makes it possible to switch debug mode on and off using F12

EnsureDataLoaded();

ScriptMessage("Toggle debug mode with F12\nby krzys_h");

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f12, Data.Strings, Data.Code, Data.CodeLocals);

// i could just use NOT... but I wanted to test branches in the assembler
code.Append(Assembler.Assemble(@"
pushglb.v global.debug
pushi.e 1
cmp.i.v EQ
bf go_enable

pushi.e 0
pop.v.i global.debug
b func_end

go_enable: pushi.e 1
pop.v.i global.debug
", Data));

var scr_debug = Data.Scripts.ByName("scr_debug")?.Code;
if (scr_debug != null) // Deltarune
{
    scr_debug.Replace(Assembler.Assemble(@"
pushglb.v global.debug
ret.v
", Data));
}

ChangeSelection(code);
ScriptMessage("Patched!");