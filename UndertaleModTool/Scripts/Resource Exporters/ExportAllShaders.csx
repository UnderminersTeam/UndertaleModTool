using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

string exportFolder = PromptChooseDirectory();
if (exportFolder is null)
{
    return;
}

foreach (UndertaleShader shader in Data.Shaders)
{
    if (shader is null)
    {
        continue;
    }

    string exportBase = Path.Combine(exportFolder, shader.Name.Content);
    Directory.CreateDirectory(exportBase);

    File.WriteAllText(Path.Combine(exportBase, "Type.txt"), shader.Type.ToString());
    File.WriteAllText(Path.Combine(exportBase, "GLSL_ES_Fragment.txt"), shader.GLSL_ES_Fragment.Content);
    File.WriteAllText(Path.Combine(exportBase, "GLSL_ES_Vertex.txt"), shader.GLSL_ES_Vertex.Content);
    File.WriteAllText(Path.Combine(exportBase, "GLSL_Fragment.txt"), shader.GLSL_Fragment.Content);
    File.WriteAllText(Path.Combine(exportBase, "GLSL_Vertex.txt"), shader.GLSL_Vertex.Content);
    File.WriteAllText(Path.Combine(exportBase, "HLSL9_Fragment.txt"), shader.HLSL9_Fragment.Content);
    File.WriteAllText(Path.Combine(exportBase, "HLSL9_Vertex.txt"), shader.HLSL9_Vertex.Content);
    if (!shader.HLSL11_VertexData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "HLSL11_VertexData.bin"), shader.HLSL11_VertexData.Data);
    if (!shader.HLSL11_PixelData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "HLSL11_PixelData.bin"), shader.HLSL11_PixelData.Data);
    if (!shader.PSSL_VertexData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "PSSL_VertexData.bin"), shader.PSSL_VertexData.Data);
    if (!shader.PSSL_PixelData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "PSSL_PixelData.bin"), shader.PSSL_PixelData.Data);
    if (!shader.Cg_PSVita_VertexData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "Cg_PSVita_VertexData.bin"), shader.Cg_PSVita_VertexData.Data);
    if (!shader.Cg_PSVita_PixelData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "Cg_PSVita_PixelData.bin"), shader.Cg_PSVita_PixelData.Data);
    if (!shader.Cg_PS3_VertexData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "Cg_PS3_VertexData.bin"), shader.Cg_PS3_VertexData.Data);
    if (!shader.Cg_PS3_PixelData.IsNull)
        File.WriteAllBytes(Path.Combine(exportBase, "Cg_PS3_PixelData.bin"), shader.Cg_PS3_PixelData.Data);

    StringBuilder vertexSb = new();
    for (var i = 0; i < shader.VertexShaderAttributes.Count; i++)
    {
        vertexSb.AppendLine(shader.VertexShaderAttributes[i].Name.Content);
    }
    File.WriteAllText(Path.Combine(exportBase, "VertexShaderAttributes.txt"), vertexSb.ToString());
}

