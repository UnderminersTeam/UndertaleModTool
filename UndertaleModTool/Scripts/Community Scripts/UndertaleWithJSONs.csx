using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.AST;

// Made by Grossley with the help of colinator27

int maxCount = 1;

EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}

string langFolder = Path.Combine(Path.GetDirectoryName(FilePath), "lang");
if (Directory.Exists(langFolder))
{
    ScriptError("The lang folder already exists.", "Error");
    return;
}

Directory.CreateDirectory(langFolder);

GlobalDecompileContext globalDecompileContext = new(Data);
IDecompileSettings decompilerSettings = new DecompileSettings();

ScriptMessage("JSONifies Undertale versions 1.05+");
ScriptMessage(@"Switch languages using F11.
Reload text for curent language from JSON on command using F12.
");

// this is one of the rare cases when it's better without "ProgressUpdater()"
await Task.Run(() =>
{
    maxCount = 2;
    SetProgressBar(null, "Decompiling and making the JSONs", 0, maxCount);
    MakeJSON("en");
    MakeJSON("ja");
    maxCount = 6;
    SetProgressBar(null, "Setting up code to JSONify Undertale Steam 1.08", 0, maxCount);
});

void IncProgressLocal()
{
    if (GetProgress() < maxCount)
        IncrementProgress();
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

ImportGMLString("gml_Script_scr_change_language", @"");
ImportGMLString("gml_Script_scr_84_load_map_json", @"");
if (Data.GeneralInfo.Major < 2) // Undertale PC (GMS1)
{
    ImportGMLString("gml_Script_textdata_en", @"
    if (variable_global_exists(""text_data_en""))
        ds_map_destroy(global.text_data_en);
    global.text_data_en = scr_84_load_map_json(program_directory + ""\lang\"" + ""lang_en.json"");");
    ImportGMLString("gml_Script_textdata_ja", @"
    if (variable_global_exists(""text_data_ja""))
        ds_map_destroy(global.text_data_ja);
    global.text_data_ja = scr_84_load_map_json(program_directory + ""\lang\"" + ""lang_ja.json"");");
}
else
{
    ImportGMLString("gml_Script_textdata_en", @"
    if (variable_global_exists(""text_data_en""))
        ds_map_destroy(global.text_data_en);
    global.text_data_en = scr_84_load_map_json(program_directory + ""\lang\\"" + ""lang_en.json"");");
    ImportGMLString("gml_Script_textdata_ja", @"
    if (variable_global_exists(""text_data_ja""))
        ds_map_destroy(global.text_data_ja);
    global.text_data_ja = scr_84_load_map_json(program_directory + ""\lang\\"" + ""lang_ja.json"");");
}

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f11, Data).ReplaceGML(@"
scr_change_language();
", Data);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, EventSubtypeKey.vk_f12, Data).ReplaceGML(@"
if (global.language == ""en"")
    textdata_en();
else
    textdata_ja();
", Data);

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    ImportGMLString("gml_Script_scr_change_language", @"
// Read the language from the INI file
if (global.language == ""en"")
    global.language = ""ja"";
else
    global.language = ""en"";
ossafe_ini_open(""config.ini"");
ini_write_string(""General"", ""lang"", global.language);
ossafe_ini_close();
");

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    ImportGMLString("gml_Script_scr_84_load_map_json", @"
var filename = argument0;
var file_buffer = buffer_load(filename);
var json = buffer_read(file_buffer, buffer_string);
buffer_delete(file_buffer);
return json_decode(json);
");

HideProgressBar();
ScriptMessage("Complete.");

////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

void MakeJSON(string language)
{
    UndertaleCode code = Data.Code.ByName("gml_Script_textdata_" + language);
    Dictionary<string, string> contents = new();

    var context = new DecompileContext(globalDecompileContext, code, decompilerSettings);
    BlockNode rootBlock = (BlockNode)context.DecompileToAST();
    foreach (IStatementNode stmt in rootBlock.Children)
    {
        if (stmt is FunctionCallNode { Function.Name.Content: "ds_map_add" } funcCall)
        {
            StringNode keyString = (StringNode)funcCall.Arguments[1];
            StringNode valueString = (StringNode)funcCall.Arguments[2];
            contents[keyString.Value.Content] = valueString.Value.Content;
        }
    }

    string outputPath = Path.Combine(langFolder, "lang_" + language + ".json");
    File.WriteAllText(outputPath, JsonConvert.SerializeObject(contents, Formatting.Indented));

    IncProgressLocal();
    UpdateProgressValue(GetProgress());
}
