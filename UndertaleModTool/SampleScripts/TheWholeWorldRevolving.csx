EnsureDataLoaded();

if (Data.GeneralInfo.Major < 2)
	throw new Exception("GMS1.4 is not supported... yet!");

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Step, EventSubtypeStep.BeginStep, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
; 4 = view_angle
pushi.e 0
conv.i.v
pushi.e 4
conv.i.v
call.i __view_get(argc=2)
pushglb.v global.time
push.d 15
div.d.v
call.i sin(argc=1)
push.d 2
div.d.v
add.v.v
push.d 1
add.d.v
pushi.e 0
conv.i.v
pushi.e 4
conv.i.v
call.i __view_set(argc=3)
popz.v

; 11 = view_xport
pushi.e 0
conv.i.v
pushi.e 11
conv.i.v
call.i __view_get(argc=2)
pushglb.v global.time
push.d 10
div.d.v
call.i cos(argc=1)
push.d 1.5
mul.d.v
add.v.v
pushi.e 0
conv.i.v
pushi.e 11
conv.i.v
call.i __view_set(argc=3)
popz.v

; 12 = view_yport
pushi.e 0
conv.i.v
pushi.e 12
conv.i.v
call.i __view_get(argc=2)
pushglb.v global.time
push.d 10
div.d.v
call.i sin(argc=1)
push.d 1.5
mul.d.v
add.v.v
pushi.e 0
conv.i.v
pushi.e 12
conv.i.v
call.i __view_set(argc=3)
popz.v
", Data));

ScriptMessage("* The whole world is spinning, spinning\n\nJEVIL HARDMODE CHAOS LOADED\nI CAN DO ANYTHING\nby krzys_h");