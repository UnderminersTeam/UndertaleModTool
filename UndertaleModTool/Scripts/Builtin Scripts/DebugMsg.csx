EnsureDataLoaded();
ScriptMessage("DebugMsg - Displays dialogue messages\nwhile debug mode is enabled.\n\nAuthor: krzys-h, Kneesnap");

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals);

code.AppendGML(@"
if global.debug 
{
    draw_set_color(c_white);
    var drawYPosition = 0
    for (var i = 0; i < array_length_1d(global.msg); i++)
    {
        var currentMessage = global.msg[i]
        if ((!(currentMessage == ""%%%"")) && (!(currentMessage == ""%%"")) && (!(currentMessage == ""%"")))
        {
            drawYPosition++
            draw_text(10, (drawYPosition * 15 + 50), currentMessage)
        }
    }
}", Data);

ChangeSelection(code);
ScriptMessage("DebugMsg - Finished.");