EnsureDataLoaded();
ScriptMessage("DebugMsg - Displays dialogue messages\nwhile debug mode is enabled.\n\nAuthor: krzys-h, Kneesnap");

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals);

code.AppendGML(@"
if (global.debug == 1) {
    draw_set_color(0xFFFF);
    var trueI = 0
    for (var i = 0; i < array_length_1d(global.msg); i++)
    {
        if ((!(global.msg[i] == ""%%%"")) && (!(global.msg[i] == ""%%"")) && (!(global.msg[i] == ""%"")))
        {
            trueI++
            draw_text(50, (trueI * 15 + 50), global.msg[i])
        }
    }
    with (OBJ_WRITER)
        draw_text(70, 30, mystring);
}", Data);

ChangeSelection(code);
ScriptMessage("DebugMsg - Finished.");