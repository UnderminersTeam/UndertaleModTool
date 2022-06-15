using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;

EnsureDataLoaded();

string codeFolder = GetFolder(FilePath) + "Export_Code" + Path.DirectorySeparatorChar;
ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));
Directory.CreateDirectory(codeFolder);

string line;
string path_error = Path.Combine(codeFolder, "Status.txt");
string path_error2 = Path.Combine(codeFolder, "Errored_Code_Entries.txt");
string errored_code = "";
ArrayList errored_code_arr = new ArrayList();
bool errors_recorded = false;
bool errored = false;
bool skip = false;
bool write = true;
bool isErrorCodeEntry = false;
ScriptMessage("If UndertaleModTool crashes during code export, or another serious error of that nature occurs, this script will record it. Please reload the game into the tool in the event the tool crashes and re-run this script until it completes successfully without crashing. A full record of code entries with fatal decompilation problems (if they exist) will be recorded by the end in \"Errored_Code_Entries.txt\".");

SetProgressBar(null, "Code Entries", 0, Data.Code.Count);
StartProgressBarUpdater();

if (File.Exists(path_error))
{
    System.IO.StreamReader file = new System.IO.StreamReader(path_error);
    while ((line = file.ReadLine()) != null)
    {
        if (line == "No errors.")
        {
            ScriptMessage("All clear.");
            break;
        }
        else
        {
            errored = true;
            errored_code = file.ReadLine();
            if (ScriptQuestion("It appears that an error occurred at " + errored_code + ". Would you like to skip this code entry during decompilation?"))
            {
                skip = true;
            }
            break;
        }

    }
    file.Close();
}
else
{
    write = true;
}

if (errored)
{
    using (StreamWriter sw = File.AppendText(path_error2))
    {
        sw.WriteLine(errored_code);
    }
}

if (File.Exists(path_error2))
{
    errors_recorded = true;
    if (!errored)
    {
        if (ScriptQuestion("It appears that one or more errors occurred during decompilation. Would you like to skip these code entries?"))
        {
            skip = true;
        }
    }
    System.IO.StreamReader file = new System.IO.StreamReader(path_error2);
    for (int i = 0; ((line = file.ReadLine()) != null); i++)
    {
        //ScriptMessage(line);
        errored_code_arr.Add(line);
    }
    file.Close();
}

await Task.Run(() => {
    foreach (UndertaleCode code in Data.Code)
    {
        if (write)
        {
            try
            {
                File.WriteAllText(path_error, "An error in decompilation occurred in: \n" + code.Name.Content);
            }
            catch (Exception e)
            {
                File.WriteAllText(path_error, "Unknown.");
            }
        }
        if (errored_code_arr.Count > 0)
        {
            for (int i = 0; i < errored_code_arr.Count; i++)
            {
                if (errored_code_arr[i].ToString() == code.Name.Content)
                {
                    isErrorCodeEntry = true;
                }
            }
            if ((!isErrorCodeEntry) || (!skip))
            {
                string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
                try
                {
                    File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
                }
                catch (Exception e)
                {
                    File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                }
            }
            isErrorCodeEntry = false;
        }
        else
        {
            string path = Path.Combine(codeFolder, code.Name.Content + ".gml");
            try
            {
                File.WriteAllText(path, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
            }
            catch (Exception e)
            {
                File.WriteAllText(path, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
            }
        }

        IncrementProgress();
    }
});
if (write)
{
    try
    {
        File.WriteAllText(path_error, "No errors.");
    }
    catch (Exception e)
    {
        File.WriteAllText(path_error, "Unknown.");
    }
}

await StopProgressBarUpdater();
HideProgressBar();
ScriptMessage("Export Complete.\n\nLocation: " + codeFolder);
if (File.Exists(path_error2))
{
    string asmFolder = GetFolder(FilePath) + "Error_Assembly" + Path.DirectorySeparatorChar;
    Directory.CreateDirectory(asmFolder);
    if (errored_code_arr.Count > 0)
    {
        for (int i = 0; i < errored_code_arr.Count; i++)
        {
            string codename = errored_code_arr[i].ToString();
            UndertaleCode code = Data.Code.ByName(codename);
            string asmPath = Path.Combine(asmFolder, code.Name.Content + ".asm");
            try
            {
                File.WriteAllText(asmPath, (code != null ? code.Disassemble(Data.Variables, Data.CodeLocals.For(code)) : ""));
            }
            catch (Exception e)
            {
                File.WriteAllText(asmPath, "/*\nDISASSEMBLY FAILED!\n\n" + e.ToString() + "\n*/"); // Please don't
            }
        }
    }
    ScriptMessage("Please place the \"Error_Assembly\" folder into a zip file and send it to Grossley#2869 on Discord, along with what game you were playing, where you got it from, and any other pertinent information, so that these errors may be corrected.");
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}