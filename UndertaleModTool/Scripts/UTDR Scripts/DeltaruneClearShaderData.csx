using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

ScriptMessage("This script is for older devices that cannot handle shaders in DELTARUNE.");

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

// Delete all places where shaders are probably called/referenced
ImportASMString("gml_GlobalScript_pal_swap_set", "", false);
ImportASMString("gml_GlobalScript_pal_swap_init_system", "", false);
ImportASMString("gml_GlobalScript_pal_swap_enable_layer", "", false);
ImportASMString("gml_GlobalScript_pal_swap_get_pal_count", "", false);
ImportASMString("gml_GlobalScript_pal_swap_draw_palette", "", false);
ImportASMString("gml_GlobalScript_pal_swap_reset", "", false);
ImportASMString("gml_GlobalScript_pal_swap_layer_reset", "", false);
ImportASMString("gml_GlobalScript_pal_swap_set_layer", "", false);
ImportASMString("gml_GlobalScript_pal_swap_get_color_count", "", false);
ImportASMString("gml_GlobalScript__pal_swap_layer_start", "", false);
ImportASMString("gml_GlobalScript__pal_swap_layer_end", "", false);