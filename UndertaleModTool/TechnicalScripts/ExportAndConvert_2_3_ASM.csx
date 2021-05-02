using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

if (Data.ProfileMode)
{
    ScriptMessage("This script is incompatible with profile mode.");
    return;
}

int progress = 0;
string codeFolder = GetFolder(FilePath) + "Export_Assembly2" + Path.DirectorySeparatorChar;
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

if (Directory.Exists(codeFolder)) 
{
    codeFolder = GetFolder(FilePath) + "Export_Assembly2_2" + Path.DirectorySeparatorChar;
}

Directory.CreateDirectory(codeFolder);

UpdateProgress();
DumpCode();
HideProgressBar();
ScriptMessage("Conversion Complete.\n\nLocation: " + codeFolder);

void UpdateProgress()
{
    UpdateProgressBar(null, "Code Entries", progress++, Data.Code.Count);
}

string GetFolder(string path) 
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}


void DumpCode()
{
    foreach (UndertaleCode code_orig in Data.Code) 
    {
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
                    argsLocal.Index = localvar.Index;
                    //localvar.Name = Data.Strings.MakeString("arguments");
                    //localvar.Index = 0;
                    locals.Locals.Add(argsLocal);
                    codeLocalsCount += 1;
                }
                code_orig.LocalsCount = codeLocalsCount;
                code_orig.GenerateLocalVarDefinitions(code_orig.FindReferencedLocalVars(), locals); // Dunno if we actually need this line, but it seems to work?
                code_orig.DuplicateEntry = false;
            }
            else
            {
                UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                argsLocal.Name = Data.Strings.MakeString("arguments");
                argsLocal.Index = 0;
                locals.Locals.Add(argsLocal);
                code_orig.LocalsCount = 1;
                code_orig.GenerateLocalVarDefinitions(code_orig.FindReferencedLocalVars(), locals); // Dunno if we actually need this line, but it seems to work?
            }
            Data.CodeLocals.Add(locals);
        }
        string path = Path.Combine(codeFolder, code_orig.Name.Content + ".asm");
        if (!(code_orig.DuplicateEntry))
        {
            string x = "";
            try 
            {
                string disasm_code = code_orig.Disassemble(Data.Variables, Data.CodeLocals.For(code_orig));
                //ScriptMessage(code_orig.Name.Content);
                //ScriptMessage("1 " + disasm_code);
                int ix = disasm_code.IndexOf("00000: b ");
                string code = "";
                if (ix != -1) 
                {
                    code = disasm_code.Substring(ix + 9, 5);
                    //ScriptMessage("2 " + code);
                    //Console.WriteLine(code);
                    string toBeSearched2 = disasm_code.Replace("00000: b " + code, "");
                    //ScriptMessage("3 " + toBeSearched2);
                    //Console.WriteLine(toBeSearched2);
                    ix = toBeSearched2.IndexOf(code + ": ");
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
                File.WriteAllText(((path.Length > 150) ? path.Substring(0, 150) + ".asm" : Path.Combine(codeFolder, code_orig.Name.Content + ".asm")), (code_orig != null ? code_orig.Disassemble(Data.Variables, Data.CodeLocals.For(code_orig)) : ""));
            }
            catch (Exception e) 
            {
                ScriptMessage("Error " + code_orig.Name.Content + ": " + e.ToString());
                SetUMTConsoleText(x);
                SetFinishedMessage(false);
                return;
            }

            UpdateProgress();
        }
        else
        {
            if (!(Directory.Exists(codeFolder + "/Duplicates/")))
            {
                Directory.CreateDirectory(codeFolder + "/Duplicates/");
            }
            try 
            {
                File.WriteAllText(((path.Length > 150) ? path.Substring(0, 150) + ".asm" : Path.Combine(codeFolder + "/Duplicates/", code_orig.Name.Content + ".asm")), (code_orig != null ? code_orig.Disassemble(Data.Variables, Data.CodeLocals.For(code_orig)) : ""));
            }
            catch (Exception e) 
            {
                File.WriteAllText(((path.Length > 150) ? path.Substring(0, 150) + ".asm" : Path.Combine(codeFolder + "/Duplicates/", code_orig.Name.Content + ".asm")), "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
            }
            UpdateProgress();
        }
    }
}

