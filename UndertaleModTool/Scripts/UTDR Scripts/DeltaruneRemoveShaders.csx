using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Compiler;

EnsureDataLoaded();

ScriptMessage("This script is for older devices that cannot handle shaders in DELTARUNE.");

Action deactivateAct;

#region Detect DELTARUNE version
string displayName = Data.GeneralInfo.DisplayName.Content;
if (!Data.IsVersionAtLeast(2, 3) && (displayName == "SURVEY_PROGRAM" || displayName == "DELTARUNE Chapter 1"))
{
    // DELTARUNE Chapter 1, before 1&2 demo
    deactivateAct = null;
}
else if (displayName == "DELTARUNE Chapter 1&2")
{
    // DELTARUNE (1&2 demo prior to LTS)
    deactivateAct = DeactivateForChapter2;
}
else if (displayName.ToUpper().Contains("DELTARUNE"))
{
    if (Data.GameObjects.ByName("obj_event_manager") is null)
    {
        // DELTARUNE (1&2 LTS demo)
        if (displayName == "DELTARUNE Chapter 1")
        {
            deactivateAct = null;
        }
        else if (displayName == "DELTARUNE Chapter 2")
        {
            deactivateAct = DeactivateForChapter2;
        }
        else
        {
            ScriptError("Unsupported game version.");
            return;
        }
    }
    else
    {
        // DELTARUNE (full release, handles all chapters)
        switch (displayName)
        {
            case "DELTARUNE Chapter 1":
                deactivateAct = null;
                break;

            case "DELTARUNE Chapter 2":
                deactivateAct = DeactivateForChapter2;
                break;

            case "DELTARUNE Chapter 3":
                deactivateAct = DeactivateForChapter3;
                break;

            case "DELTARUNE Chapter 4":
                deactivateAct = DeactivateForChapter4;
                break;

            case "DELTARUNE Chapter 5":
                deactivateAct = DeactivateForChapter5;
                break;

            default:
                ScriptError("Unsupported game version.");
                return;
        }
    }
}
else
{
    ScriptError("Unsupported game version.");
    return;
}
#endregion

if (deactivateAct is null)
{
    ScriptMessage("DELTARUNE Chapter 1 doesn't have shaders.");
    return;
}

#region Clear (recreate) all shaders
List<(string, UndertaleShader.ShaderType)> shadersNonExist = new();
for (var i = 0; i < Data.Shaders.Count; i++)
{
    var shader = Data.Shaders[i];
    shadersNonExist.Add((shader.Name.Content, shader.Type));
}

Data.Shaders.Clear();

foreach ((string str, UndertaleShader.ShaderType type) in shadersNonExist)
{
    UndertaleShader existingShader = new()
    {
        Type = type,
        Name = Data.Strings.MakeString(str),
        GLSL_ES_Fragment = Data.Strings.MakeString(""),
        GLSL_ES_Vertex = Data.Strings.MakeString(""),
        GLSL_Fragment = Data.Strings.MakeString(""),
        GLSL_Vertex = Data.Strings.MakeString(""),
        HLSL9_Fragment = Data.Strings.MakeString(""),
        HLSL9_Vertex = Data.Strings.MakeString("")
    };

    Data.Shaders.Add(existingShader);
}
#endregion

#region Code deactivation methods (for each chapter)
void DeactivateForChapter2()
{
    CodeImportGroup group = new(Data);
    /*group.QueueReplace("gml_GlobalScript_pal_swap_set", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_init_system", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_enable_layer", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_get_pal_count", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_draw_palette", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_reset", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_layer_reset", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_set_layer", "");
    group.QueueReplace("gml_GlobalScript_pal_swap_get_color_count", "");
    group.QueueReplace("gml_GlobalScript__pal_swap_layer_start", "");
    group.QueueReplace("gml_GlobalScript__pal_swap_layer_end", "");*/
    group.Import();
}

void DeactivateForChapter3()
{

}

void DeactivateForChapter4()
{

}

void DeactivateForChapter5()
{

}
#endregion

deactivateAct();

ScriptMessage("Successfully cleared all shaders and deactivated all the shader GML code.");