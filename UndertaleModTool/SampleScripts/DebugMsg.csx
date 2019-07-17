EnsureDataLoaded();

var code = Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals);

CompileContext compileContext = Compiler.CompileGMLText(@"
if (global.debug == 1) {
    draw_set_color(65535);
    for (var i = 0; i < 100; i++)
        draw_text(50, (i * 15) + 50, global.msg[i]);
    
    with (OBJ_WRITER)
        draw_text(70, 30, mystring);
}", Data, code);

if (!compileContext.SuccessfulCompile || compileContext.HasError)
{
    ScriptError(compileContext.ResultError, "Compiler failed");
    return;
}

try
{
    var instructions = Assembler.Assemble(compileContext.ResultAssembly, Data);
    code.Replace(instructions);
    ChangeSelection(code);
}
catch (Exception ex)
{
    ScriptError(ex.ToString(), "Assembler error");
    return;
}

ScriptMessage("DebugMsg - Finished.");