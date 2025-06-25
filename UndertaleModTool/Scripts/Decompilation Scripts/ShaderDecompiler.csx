// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

EnsureDataLoaded();

string shaderFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "shaders" + Path.DirectorySeparatorChar;

if (Directory.Exists(shaderFolder))
{
    Directory.Delete(shaderFolder, true);
}

Directory.CreateDirectory(shaderFolder);

public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }
}

// data.win shader types are different from .yy shader types
public enum GMShaderType {
    GLSL_ES = 1,
    GLSL = 2,
    HLSL_11 = 3,
    PSSL = 4,
}

public class GMShader {
    public string resourceType = "GMShader";
    public string resourceVersion = "1.0";
    public string name = "";

    public GMShaderType type = GMShaderType.GLSL_ES;

    public List<string> tags = new List<string>();
    public AssetReference parent;
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

string CutOffAt(string str, string cutOffAt, int trimExtra) {
    var pos = str.IndexOf(cutOffAt);
    if (pos == -1) return str;
    return str.Substring(pos + cutOffAt.Length + trimExtra);
}

foreach (UndertaleShader shader in Data.Shaders) {
    string folderName = Path.Combine(shaderFolder, shader.Name.Content);
    Directory.CreateDirectory(folderName);
    string vertSrc = "";
    string vertPath = Path.Combine(folderName, shader.Name.Content + ".vsh");
    string fragSrc = "";
    string fragPath = Path.Combine(folderName, shader.Name.Content + ".fsh");
    string yyPath = Path.Combine(folderName, shader.Name.Content + ".yy");

    var shaderData = new GMShader{
        name = shader.Name.Content,
        parent = new AssetReference{
            name = "Shaders",
            path = "folders/Shaders.yy",
        },
    };

    switch (shader.Type) {
        case UndertaleShader.ShaderType.GLSL_ES:
            shaderData.type = GMShaderType.GLSL_ES;
            vertSrc = CutOffAt(shader.GLSL_ES_Vertex.Content, "#define _YY_GLSLES_ 1", 1);
            fragSrc = CutOffAt(shader.GLSL_ES_Fragment.Content, "#define _YY_GLSLES_ 1", 1);
            break;
        case UndertaleShader.ShaderType.GLSL:
            shaderData.type = GMShaderType.GLSL;
            vertSrc = CutOffAt(shader.GLSL_Vertex.Content, "#define _YY_GLSL_ 1", 1);
            fragSrc = CutOffAt(shader.GLSL_Fragment.Content, "#define _YY_GLSL_ 1", 1);
            break;
        default: throw new Exception($"Unsupported shader type: {shader.Type}");
    };

    string exportedyy = JsonConvert.SerializeObject(shaderData, Formatting.Indented);
    File.WriteAllText(yyPath, exportedyy);
    File.WriteAllText(fragPath, fragSrc);
    File.WriteAllText(vertPath, vertSrc);
}