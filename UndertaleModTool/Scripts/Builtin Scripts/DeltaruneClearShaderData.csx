using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

ScriptMessage("For older devices that cannot handle Deltarune's shaders.");

List<string> shadersNonExist = new List<string>();
for (var i = 0; i < Data.Shaders.Count; i++)
    shadersNonExist.Add(Data.Shaders[i].Name.Content);
Data.Shaders.Clear();
foreach (string str in shadersNonExist)
{
    UndertaleShader existing_shader = new UndertaleShader();
    existing_shader.Name = Data.Strings.MakeString(str);
    existing_shader.GLSL_ES_Fragment = Data.Strings.MakeString("");
    existing_shader.GLSL_ES_Vertex = Data.Strings.MakeString("");
    existing_shader.GLSL_Fragment = Data.Strings.MakeString("");
    existing_shader.GLSL_Vertex = Data.Strings.MakeString("");
    existing_shader.HLSL9_Fragment = Data.Strings.MakeString("");
    existing_shader.HLSL9_Vertex = Data.Strings.MakeString("");
    Data.Shaders.Add(existing_shader);
}
//Delete all places where shaders are probably called/referenced
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

/*
for (var i = 0; i < Data.Shaders.Count; i++)
{
    Data.Shaders[i].VertexShaderAttributes.Clear();
    if (Data.Shaders[i].HLSL11_VertexData != null)
    {
        Data.Shaders[i].HLSL11_VertexData.IsNull = true;
        Data.Shaders[i].HLSL11_VertexData.Data = null;
        Data.Shaders[i].HLSL11_VertexData = null;
    }
    if (Data.Shaders[i].HLSL11_PixelData != null)
    {
        Data.Shaders[i].HLSL11_PixelData.IsNull = true;
        Data.Shaders[i].HLSL11_PixelData.Data = null;
        Data.Shaders[i].HLSL11_PixelData = null;
    }
    if (Data.Shaders[i].PSSL_VertexData != null)
    {
        Data.Shaders[i].PSSL_VertexData.IsNull = true;
        Data.Shaders[i].PSSL_VertexData.Data = null;
        Data.Shaders[i].PSSL_VertexData = null;
    }
    if (Data.Shaders[i].PSSL_PixelData != null)
    {
        Data.Shaders[i].PSSL_PixelData.IsNull = true;
        Data.Shaders[i].PSSL_PixelData.Data = null;
        Data.Shaders[i].PSSL_PixelData = null;
    }
    if (Data.Shaders[i].Cg_PSVita_VertexData != null)
    {
        Data.Shaders[i].Cg_PSVita_VertexData.IsNull = true;
        Data.Shaders[i].Cg_PSVita_VertexData.Data = null;
        Data.Shaders[i].Cg_PSVita_VertexData = null;
    }
    if (Data.Shaders[i].Cg_PSVita_PixelData != null)
    {
        Data.Shaders[i].Cg_PSVita_PixelData.IsNull = true;
        Data.Shaders[i].Cg_PSVita_PixelData.Data = null;
        Data.Shaders[i].Cg_PSVita_PixelData = null;
    }
    if (Data.Shaders[i].Cg_PS3_VertexData != null)
    {
        Data.Shaders[i].Cg_PS3_VertexData.IsNull = true;
        Data.Shaders[i].Cg_PS3_VertexData.Data = null;
        Data.Shaders[i].Cg_PS3_VertexData = null;
    }
    if (Data.Shaders[i].Cg_PS3_PixelData != null)
    {
        Data.Shaders[i].Cg_PS3_PixelData.IsNull = true;
        Data.Shaders[i].Cg_PS3_PixelData.Data = null;
        Data.Shaders[i].Cg_PS3_PixelData = null;
    }
}
*/