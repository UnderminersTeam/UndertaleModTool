EnsureDataLoaded();
ScriptMessage("DebugMsg - Displays dialogue messages\nwhile debug mode is enabled.\n\nAuthor: krzys-h, Kneesnap");

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data);

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
importGroup.QueueAppend(code, @"
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
}");
importGroup.Import();

ChangeSelection(code);
ScriptMessage("DebugMsg - Finished.");