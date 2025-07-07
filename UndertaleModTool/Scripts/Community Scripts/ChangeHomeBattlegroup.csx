using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

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


UndertaleCode code = Data.Code.ByName("gml_Object_obj_mainchara_KeyPress_36");
if (code == null)
{
    ScriptError("Cannot apply, \"gml_Object_obj_mainchara_KeyPress_36\" does not exist!");
    return;
}

GlobalDecompileContext globalDecompileContext = new(Data);
Underanalyzer.Decompiler.IDecompileSettings decompilerSettings = new Underanalyzer.Decompiler.DecompileSettings();

bool case_sensitive = true;
bool multiline = false;
bool isRegex = true;

string preValue = GetPreviousValue();
if (!ScriptQuestion("Change the battlegroup in \"gml_Object_obj_mainchara_KeyPress_36\" (when you press \"HOME\" in debug mode)?" + ((preValue == "None") ? "" : " The current battlegroup value is: " + preValue)))
{
    ScriptError("Cancelled!");
    return;
}
if (GetPreviousValue() == "None")
{
    String replacement = SimpleTextInput("Enter new battle group value for when you press \"HOME\"", "New battle group value", GetDecompiledText("gml_Object_obj_mainchara_KeyPress_36", globalDecompileContext, decompilerSettings), true);
    ImportGMLString("gml_Object_obj_mainchara_KeyPress_36", replacement);
    ScriptMessage("Completed");
    return;
}

//Group 1: "global.battlegroup = ("
//Group 2: Original value of battlegroup
//Group 3: " + nnn)"
const string keyword = @"(global\.battlegroup ?= ?\(?)(\d+)( ?\+ ?nnn\)?);?";
bool success = false;
int number;
while (!success)
{
    success = Int32.TryParse(SimpleTextInput("Enter new battle group value for when you press \"HOME\"", "New battle group value", "", multiline), out number);
}

//Substitute in group 1, the new value, and group 3
//And the groups are specified using curly brackets to prevent the regex from misinterpreting the request.
ReplaceTextInGML(code.Name.Content, keyword, ("${1}" + number.ToString() + "${3}"), case_sensitive, isRegex, globalDecompileContext, decompilerSettings);

ScriptMessage("Completed");

string GetPreviousValue()
{
    var line_number = 1;
    string decompiled_text = GetDecompiledText("gml_Object_obj_mainchara_KeyPress_36", globalDecompileContext, decompilerSettings);
    string results = "";
    string[] splitted = decompiled_text.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
    bool exists = false;
    foreach (string lineInt in splitted)
    {
        if (System.Text.RegularExpressions.Regex.IsMatch(lineInt, keyword))
        {
            results = Regex.Replace(lineInt, keyword, "${2}", RegexOptions.None);
            exists = true;
            break;
        }
    }
    if (exists)
        return results;
    else
        return "None";
}