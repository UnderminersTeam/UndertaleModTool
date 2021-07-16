ScriptError("This script is temporarily disabled as it doesn't work properly, causing the game to crash, sorry about that.");
return;

if (false)
{
    EnsureDataLoaded();
    ScriptMessage("DebugMsg - Displays dialogue messages\nwhile debug mode is enabled.\n\nAuthor: krzys-h, Kneesnap");

    var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals);

    code.ReplaceGML(@"
if (global.debug == 1) {
    draw_set_color(0xFFFF);
    for (var i = 0; i < 100; i++)
        draw_text(50, (i * 15) + 50, global.msg[i]);
    
    with (OBJ_WRITER)
        draw_text(70, 30, mystring);
}", Data);

    ChangeSelection(code);
    ScriptMessage("DebugMsg - Finished.");
}