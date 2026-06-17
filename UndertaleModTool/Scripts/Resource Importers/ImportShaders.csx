using System.Text;
using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UndertaleModLib.Util;

EnsureDataLoaded();

string importFolder = PromptChooseDirectory();
if (importFolder is null)
{
    throw new ScriptCancelledException("The import folder was not set.");
}

var shadersToModify = Directory.GetDirectories(importFolder).Select(x => Path.GetFileName(x));
foreach (string shaderName in shadersToModify)
{
    if (Data.Shaders.ByName(shaderName) is UndertaleShader existingShader)
    {
        ImportShader(existingShader);
    }
    else
    {
        AddShader(shaderName);
    }
}

void ImportShaderPlaintextFile(Action<UndertaleString> stringSetter, string importDirectory, string name)
{
    string path = Path.Join(importDirectory, $"{name}.txt");
    if (!File.Exists(path))
    {
        stringSetter(Data.Strings.MakeString(""));
        return;
    }
    stringSetter(Data.Strings.MakeString(File.ReadAllText(path)));
}

void ImportShaderBinaryFile(Action<UndertaleShader.UndertaleRawShaderData> dataSetter, string importDirectory, string name)
{
    string path = Path.Join(importDirectory, $"{name}.bin");
    if (!File.Exists(path))
    {
        dataSetter(new());
        return;
    }
    dataSetter(new()
    {
        Data = File.ReadAllBytes(path),
        IsNull = false
    });
}

void ImportShader(UndertaleShader existingShader, string existingImportDir = null)
{
    string localImportDir = existingImportDir ?? Paths.JoinVerifyWithinDirectory(importFolder, existingShader.Name.Content);
    string shaderTypePath = Path.Join(localImportDir, "Type.txt");
    if (File.Exists(shaderTypePath))
    {
        string shaderType = File.ReadAllText(shaderTypePath);
        if (shaderType.Contains("GLSL_ES"))
            existingShader.Type = UndertaleShader.ShaderType.GLSL_ES;
        else if (shaderType.Contains("GLSL"))
            existingShader.Type = UndertaleShader.ShaderType.GLSL;
        else if (shaderType.Contains("HLSL9"))
            existingShader.Type = UndertaleShader.ShaderType.HLSL9;
        else if (shaderType.Contains("HLSL11"))
            existingShader.Type = UndertaleShader.ShaderType.HLSL11;
        else if (shaderType.Contains("PSSL"))
            existingShader.Type = UndertaleShader.ShaderType.PSSL;
        else if (shaderType.Contains("Cg_PSVita"))
            existingShader.Type = UndertaleShader.ShaderType.Cg_PSVita;
        else if (shaderType.Contains("Cg_PS3"))
            existingShader.Type = UndertaleShader.ShaderType.Cg_PS3;
        else
            throw new ScriptException($"Failed to determine shader type for shader {existingShader.Name.Content}");
    }
    else
    {
        existingShader.Type = UndertaleShader.ShaderType.GLSL_ES;
    }

    ImportShaderPlaintextFile((str) => existingShader.GLSL_ES_Fragment = str, localImportDir, "GLSL_ES_Fragment");
    ImportShaderPlaintextFile((str) => existingShader.GLSL_ES_Vertex = str, localImportDir, "GLSL_ES_Vertex");
    ImportShaderPlaintextFile((str) => existingShader.GLSL_Fragment = str, localImportDir, "GLSL_Fragment");
    ImportShaderPlaintextFile((str) => existingShader.GLSL_Vertex = str, localImportDir, "GLSL_Vertex");
    ImportShaderPlaintextFile((str) => existingShader.HLSL9_Fragment = str, localImportDir, "HLSL9_Fragment");
    ImportShaderPlaintextFile((str) => existingShader.HLSL9_Vertex = str, localImportDir, "HLSL9_Vertex");
    ImportShaderBinaryFile((data) => existingShader.HLSL11_VertexData = data, localImportDir, "HLSL11_VertexData");
    ImportShaderBinaryFile((data) => existingShader.HLSL11_PixelData = data, localImportDir, "HLSL11_PixelData");
    ImportShaderBinaryFile((data) => existingShader.PSSL_VertexData = data, localImportDir, "PSSL_VertexData");
    ImportShaderBinaryFile((data) => existingShader.PSSL_PixelData = data, localImportDir, "PSSL_PixelData");
    ImportShaderBinaryFile((data) => existingShader.Cg_PSVita_VertexData = data, localImportDir, "Cg_PSVita_VertexData");
    ImportShaderBinaryFile((data) => existingShader.Cg_PSVita_PixelData = data, localImportDir, "Cg_PSVita_PixelData");
    ImportShaderBinaryFile((data) => existingShader.Cg_PS3_VertexData = data, localImportDir, "Cg_PS3_VertexData");
    ImportShaderBinaryFile((data) => existingShader.Cg_PS3_PixelData = data, localImportDir, "Cg_PS3_PixelData");

    existingShader.VertexShaderAttributes.Clear();
    string vertexShaderAttributesPath = Path.Join(localImportDir, "VertexShaderAttributes.txt");
    if (File.Exists(vertexShaderAttributesPath))
    {
        string line;
        using StreamReader file = new(vertexShaderAttributesPath);
        while ((line = file.ReadLine()) is not null)
        {
            line = line.Trim();
            if (line.Length == 0)
            {
                continue;
            }
            existingShader.VertexShaderAttributes.Add(new()
            {
                Name = Data.Strings.MakeString(line)
            });
        }
    }

    Project?.MarkAssetForExport(existingShader);
}

void AddShader(string shaderName)
{
    UndertaleShader newShader = new()
    {
        Name = Data.Strings.MakeString(shaderName)
    };
    string localImportDir = Paths.JoinVerifyWithinDirectory(importFolder, shaderName);
    ImportShader(newShader, localImportDir);
    Data.Shaders.Add(newShader);
}
