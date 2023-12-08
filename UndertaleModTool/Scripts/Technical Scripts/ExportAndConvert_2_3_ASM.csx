using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

if (Data.ToolInfo.ProfileMode)
{
    ScriptMessage("This script is incompatible with profile mode.");
    return;
}

string codeFolder = GetFolder(FilePath) + "Export_Assembly2" + Path.DirectorySeparatorChar;
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

if (Directory.Exists(codeFolder))
{
    codeFolder = GetFolder(FilePath) + "Export_Assembly2_2" + Path.DirectorySeparatorChar;
}

Directory.CreateDirectory(codeFolder);

SetProgressBar(null, "Code Entries", 0, Data.Code.Count);
StartProgressBarUpdater();

SyncBinding("Strings, CodeLocals, Variables, Functions", true);
await Task.Run(DumpCode);
DisableAllSyncBindings();

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Conversion Complete.\n\nLocation: " + codeFolder);

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

string ReplaceFirst(string text, string search, string replace)
{
    int pos = text.IndexOf(search);
    if (pos < 0)
    {
        return text;
    }
    return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
}

void DumpCode()
{
    foreach (UndertaleCode code_orig in Data.Code)
    {
        code_orig.Offset = 0;
        if (Data.CodeLocals.ByName(code_orig.Name.Content) == null)
        {
            UndertaleCodeLocals locals = new UndertaleCodeLocals();
            locals.Name = code_orig.Name;
            uint codeLocalsCount = 0;
            string equivalentGlobalScript = (code_orig.Name.Content).Replace("gml_Script_", "gml_GlobalScript_");
            if (Data.CodeLocals.ByName(equivalentGlobalScript) != null)
            {
                foreach (UndertaleCodeLocals.LocalVar localvar in Data.CodeLocals.ByName(equivalentGlobalScript).Locals)
                {
                    UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                    argsLocal.Name = localvar.Name;
                    //(localvar.Name != null ? localvar.Name : Data.Strings.MakeString("arguments"));
                    argsLocal.Index = localvar.Index;
                    //localvar.Name = Data.Strings.MakeString("arguments");
                    //localvar.Index = 0;
                    locals.Locals.Add(argsLocal);
                    codeLocalsCount += 1;
                }
                code_orig.LocalsCount = codeLocalsCount;
                code_orig.ParentEntry = null;
            }
            else
            {
                UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                argsLocal.Name = Data.Strings.MakeString("arguments");
                argsLocal.Index = 0;
                locals.Locals.Add(argsLocal);
                code_orig.LocalsCount = 1;
            }
            Data.CodeLocals.Add(locals);
        }
        string path = Path.Combine(codeFolder, code_orig.Name.Content + ".asm");
        if (code_orig.ParentEntry == null)
        {
            string x = "";
            try
            {
                string disasm_code = code_orig.Disassemble(Data.Variables, Data.CodeLocals.For(code_orig));
                //ScriptMessage(code_orig.Name.Content);
                //ScriptMessage("1 " + disasm_code);
                int ix = -1;
                if (code_orig.Instructions.Count > 0 && code_orig.Instructions[0].Kind == UndertaleInstruction.Opcode.B)
                    ix = disasm_code.IndexOf("b [");
                string code = "";
                if (ix != -1)
                {
                    code = disasm_code.Substring(ix + 2, disasm_code.IndexOf(']', ix) - (ix + 1));
                    //ScriptMessage("2 " + code);
                    //Console.WriteLine(code);
                    string toBeSearched2 = ReplaceFirst(disasm_code, "b " + code, "");
                    //ScriptMessage("3 " + toBeSearched2);
                    //Console.WriteLine(toBeSearched2);
                    ix = toBeSearched2.IndexOf(":" + code);
                    x = "";
                    if (ix != -1)
                    {
                        code = toBeSearched2.Substring(ix, toBeSearched2.Length - (ix));
                        //Console.WriteLine(code);
                        x = toBeSearched2.Replace(code, "");
                        //ScriptMessage("4 " + x);
                        //Console.WriteLine(x);
                    }
                    code_orig.Replace(Assembler.Assemble(x, Data));
                }
                string str_path_to_use = Path.Combine(codeFolder, code_orig.Name.Content + ".asm");
                string code_output = "";
                if (code_orig != null)
                    code_output = code_orig.Disassemble(Data.Variables, Data.CodeLocals.For(code_orig));
                File.WriteAllText(str_path_to_use, code_output);
            }
            catch (Exception e)
            {
                ScriptMessage("Error " + code_orig.Name.Content + ": " + e.ToString());
                SetUMTConsoleText(x);
                SetFinishedMessage(false);
                return;
            }

            IncrementProgress();
        }
        else
        {
            if (!(Directory.Exists(Path.Combine(codeFolder, "Duplicates"))))
            {
                Directory.CreateDirectory(Path.Combine(codeFolder, "Duplicates"));
            }
            try
            {
                string str_path_to_use = Path.Combine(codeFolder, "Duplicates", code_orig.Name.Content + ".asm");
                string code_output = "";
                if (code_orig != null)
                    code_output = code_orig.Disassemble(Data.Variables, Data.CodeLocals.For(code_orig));
                File.WriteAllText(str_path_to_use, code_output);
            }
            catch (Exception e)
            {
                string str_path_to_use = Path.Combine(codeFolder, "Duplicates", code_orig.Name.Content + ".asm");
                File.WriteAllText(str_path_to_use, "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
            }

            IncrementProgress();
        }
    }
}

