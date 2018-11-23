EnsureDataLoaded();

// Remove all current fonts
// This is necessary because I need to add fonts under IDs that are normally used by these resources
// TODO: Japanese fonts won't work at all because I didn't add support for that
Data.Fonts.Clear();

var obj_time = Data.GameObjects.ByName("obj_time");

Data.Functions.EnsureDefined("font_add", Data.Strings);
obj_time.EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
; NOTE: According to GMS documentation the font ranges are ignored with ttf fonts, and that seems to be indeed the case
;font_add('wingding.ttf', 12, false, false, 32, 127);
;font_add('8bitoperator_jve.ttf', 24, false, false, 32, 127);
;font_add('8bitoperator_jve.ttf', 12, false, false, 32, 127);
;font_add('CryptOfTomorrow.ttf', 6, false, false, 32, 127);
;font_add('DotumChe.ttf', 12, true, false, 32, 127);
;font_add('DotumChe.ttf', 48, true, false, 32, 127);
;font_add('hachicro.ttf', 24, true, false, 32, 127);
;font_add('Mars Needs Cunnilingus.ttf', 18, false, false, 32, 127);
;font_add('comic.ttf', 10, true, false, 32, 127);
;font_add('PAPYRUS.TTF', 8, true, false, 32, 127);

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
pushi.e 12
conv.i.v
push.s ""wingding.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
pushi.e 24
conv.i.v
push.s ""8bitoperator_jve.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
pushi.e 12
conv.i.v
push.s ""8bitoperator_jve.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
pushi.e 6
conv.i.v
push.s ""CryptOfTomorrow.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 1
conv.i.v
pushi.e 12
conv.i.v
push.s ""DotumChe.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 1
conv.i.v
pushi.e 48
conv.i.v
push.s ""DotumChe.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 1
conv.i.v
pushi.e 24
conv.i.v
push.s ""hachicro.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
pushi.e 18
conv.i.v
push.s ""Mars Needs Cunnilingus.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 1
conv.i.v
pushi.e 10
conv.i.v
push.s ""comic.ttf""
conv.s.v
call.i font_add(argc=6)
popz.v

pushi.e 127
conv.i.v
pushi.e 32
conv.i.v
pushi.e 0
conv.i.v
pushi.e 1
conv.i.v
pushi.e 8
conv.i.v
push.s ""PAPYRUS.TTF""
conv.s.v
call.i font_add(argc=6)
popz.v
", Data));

ChangeSelection(obj_time);