using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

string exportFolder = PromptChooseDirectory();
if (exportFolder == null)
    throw new ScriptException("The export folder was not set.");

Directory.CreateDirectory(exportFolder + "/Shader_Data/");
File.WriteAllText(exportFolder + "/Shader_Data/" + "Import_Loc.txt", "Import location");

foreach(UndertaleShader shader in Data.Shaders)
{
    string exportBase = (exportFolder + "/Shader_Data/" + shader.Name.Content + "/");
    Directory.CreateDirectory(exportBase);
    File.WriteAllText(exportBase + "Type.txt", shader.Type.ToString());
    File.WriteAllText(exportBase + "GLSL_ES_Fragment.txt", shader.GLSL_ES_Fragment.Content);
    File.WriteAllText(exportBase + "GLSL_ES_Vertex.txt", shader.GLSL_ES_Vertex.Content);
    File.WriteAllText(exportBase + "GLSL_Fragment.txt", shader.GLSL_Fragment.Content);
    File.WriteAllText(exportBase + "GLSL_Vertex.txt", shader.GLSL_Vertex.Content);
    File.WriteAllText(exportBase + "HLSL9_Fragment.txt", shader.HLSL9_Fragment.Content);
    File.WriteAllText(exportBase + "HLSL9_Vertex.txt", shader.HLSL9_Vertex.Content);
    if (shader.HLSL11_VertexData.IsNull == false)
        File.WriteAllBytes(exportBase + "HLSL11_VertexData.bin", shader.HLSL11_VertexData.Data);
    if (shader.HLSL11_PixelData.IsNull == false)
        File.WriteAllBytes(exportBase + "HLSL11_PixelData.bin", shader.HLSL11_PixelData.Data);
    if (shader.PSSL_VertexData.IsNull == false)
        File.WriteAllBytes(exportBase + "PSSL_VertexData.bin", shader.PSSL_VertexData.Data);
    if (shader.PSSL_PixelData.IsNull == false)
        File.WriteAllBytes(exportBase + "PSSL_PixelData.bin", shader.PSSL_PixelData.Data);
    if (shader.Cg_PSVita_VertexData.IsNull == false)
        File.WriteAllBytes(exportBase + "Cg_PSVita_VertexData.bin", shader.Cg_PSVita_VertexData.Data);
    if (shader.Cg_PSVita_PixelData.IsNull == false)
        File.WriteAllBytes(exportBase + "Cg_PSVita_PixelData.bin", shader.Cg_PSVita_PixelData.Data);
    if (shader.Cg_PS3_VertexData.IsNull == false)
        File.WriteAllBytes(exportBase + "Cg_PS3_VertexData.bin", shader.Cg_PS3_VertexData.Data);
    if (shader.Cg_PS3_PixelData.IsNull == false)
        File.WriteAllBytes(exportBase + "Cg_PS3_PixelData.bin", shader.Cg_PS3_PixelData.Data);
    string vertex = null;
    for (var i = 0; i < shader.VertexShaderAttributes.Count; i++)
    {
        if (vertex == null)
            vertex = "";
        vertex += shader.VertexShaderAttributes[i].Name.Content;
        vertex += "\n";
    }
    File.WriteAllText(exportBase + "VertexShaderAttributes.txt", ((vertex != null) ? vertex : ""));
}

