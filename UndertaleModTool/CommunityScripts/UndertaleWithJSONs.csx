using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

//Made by Grossley with the help of Colinator

string currentDialog;
int saveProgress = 0;
int maxCount = 1;

EnsureDataLoaded();

string langFolder = GetFolder(FilePath) + "lang" + Path.DirectorySeparatorChar;
ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(Data, false));

if (Directory.Exists(langFolder)) 
{
    ScriptError("The lang files already exist.", "Error");
    return;
}

Directory.CreateDirectory(langFolder);

ScriptMessage("JSONifies Undertale versions 1.05+");
ScriptMessage(@"Switch languages using F11.
Reload text for curent language from JSON on command using F12.
Note: reloading from JSON may take about 10 seconds.
");
SetupProgress("Dumping the language files", 2);
DumpJSON("en");
DumpJSON("ja");
SetupProgress("Making the JSONs", 2);
MakeJSON("en");
MakeJSON("ja");
SetupProgress("Setting up code to JSONify Undertale Steam 1.08", 6);

void UpdateProgress() {
    if (saveProgress < maxCount)
        UpdateProgressBar(null, currentDialog, saveProgress++, maxCount);
}

void SetupProgress(string name, int totalCount) {
    currentDialog = name;
    maxCount = totalCount;
    saveProgress = 0;
    UpdateProgressBar(null, currentDialog, saveProgress, maxCount);
}

string GetFolder(string path) {
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

ImportGMLString("gml_Script_scr_change_language", @"");
ImportGMLString("gml_Script_scr_84_load_map_json", @"");
ImportGMLString("gml_Script_textdata_en", @"
if (variable_global_exists(""text_data_en""))
    ds_map_destroy(global.text_data_en);
global.text_data_en = scr_84_load_map_json(program_directory + ""\lang\\"" + ""lang_en.json"");");
ImportGMLString("gml_Script_textdata_ja", @"
if (variable_global_exists(""text_data_ja""))
    ds_map_destroy(global.text_data_ja);
global.text_data_ja = scr_84_load_map_json(program_directory + ""\lang\\"" + ""lang_ja.json"");");

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f11, Data.Strings, Data.Code, Data.CodeLocals).ReplaceGML(@"
scr_change_language()
", Data);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f12, Data.Strings, Data.Code, Data.CodeLocals).ReplaceGML(@"
if (global.language == ""en"")
    textdata_en()
else
    textdata_ja()
", Data);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    ImportGMLString("gml_Script_scr_change_language", @"
//Read the language from the INI file
if (global.language == ""en"")
    global.language = ""ja""
else
    global.language = ""en""
ossafe_ini_open(""config.ini"")
ini_write_string(""General"", ""lang"", global.language)
ossafe_ini_close()
");

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    ImportGMLString("gml_Script_scr_84_load_map_json", @"
var filename = argument0
var file = file_text_open_read(filename)
var json = """"
while (file_text_eof(file) == 0)
    json += file_text_readln(file)
file_text_close(file)
return json_decode(json);
");

HideProgressBar();
ScriptMessage("Complete.");

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void DumpJSON(string language)
{
    var lang_file = Data.Code.ByName("gml_Script_textdata_" + language);
    try 
    {
        File.WriteAllText(Path.Combine(langFolder, "lang_" + language + ".json"), (lang_file != null ? Decompiler.Decompile(lang_file, DECOMPILE_CONTEXT.Value) : ""));
    } 
    catch (Exception e) 
    {
        throw new System.Exception("gml_Script_textdata_" + language + " has an error that prevents creation of JSONs.");
    }
    UpdateProgress();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void MakeJSON(string language)
{
    if (Data.GeneralInfo.Major < 2) // Undertale PC (GMS1)
    {
        string input = File.ReadAllText(Path.Combine(langFolder, "lang_" + language + ".json"));

        string pattern = ".*ds_map_create\\(\\)";
        string replacement = "{";
        input = Regex.Replace(input, pattern, replacement);

        pattern = @"\\";
        replacement = @"\\";
        input = Regex.Replace(input, pattern, replacement);

        pattern = @""" \+ chr\(34\) \+ """;
        replacement = @"\""";
        input = Regex.Replace(input, pattern, replacement);

        pattern = @"ds_map_add\(global\.text_data_.., ("".*""), ("".*"")\)";
        replacement = @"  $1: $2,";
        input = Regex.Replace(input, pattern, replacement);

        pattern = @",\n\Z";
        replacement = "\n}";
        input = Regex.Replace(input, pattern, replacement);

        File.WriteAllText(Path.Combine(langFolder, "lang_" + language + ".json"), input);
        UpdateProgress();
    }
    else // Undertale Switch/Probs other consoles (GMS2)
    {
        string input = File.ReadAllText(Path.Combine(langFolder, "lang_" + language + ".json"));

        string pattern = ".*ds_map_create\\(\\)";
        string replacement = "{";
        input = Regex.Replace(input, pattern, replacement);

        pattern = @"ds_map_add\(global\.text_data_.., ("".*""), ("".*"")\)";
        replacement = @"  $1: $2,";
        input = Regex.Replace(input, pattern, replacement);

        pattern = @",\n\Z";
        replacement = "\n}";
        input = Regex.Replace(input, pattern, replacement);

        File.WriteAllText(Path.Combine(langFolder, "lang_" + language + ".json"), input);
        UpdateProgress();
    }
}