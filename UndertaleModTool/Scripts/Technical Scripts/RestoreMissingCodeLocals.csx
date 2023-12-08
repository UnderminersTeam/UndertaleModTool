if (Data?.GeneralInfo.BytecodeVersion < 15)
{
    ScriptMessage("Cannot run on this game, bytecode >= 15 required!");
    return;
}
int newCount = 0;
foreach (UndertaleCode code in Data.Code)
{
    if (Data.CodeLocals.ByName(code.Name.Content) == null)
    {
        UndertaleCodeLocals locals = new UndertaleCodeLocals();
        locals.Name = code.Name;
        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
        argsLocal.Name = Data.Strings.MakeString("arguments");
        argsLocal.Index = 0;
        locals.Locals.Add(argsLocal);
        code.LocalsCount = 1;
        Data.CodeLocals.Add(locals);
        newCount += 1;
    }
}
ScriptMessage("Added code locals for " + newCount.ToString() + " codes successfully");
return;
