using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

EnsureDataLoaded();

// "Select 'Import_Loc.txt' file in 'Shader_Data'"
string importFolder = PromptChooseDirectory();
if (importFolder == null)
    throw new ScriptException("The import folder was not set.");

string[] dirFiles = Directory.GetFiles(importFolder, "*.*", SearchOption.AllDirectories);
List<string> shadersToModify = new List<string>();
List<string> shadersExisting = new List<string>();
List<string> shadersNonExist = new List<string>();
foreach (string file in dirFiles)
{
    if (Path.GetFileName(file) == "Import_Loc.txt")
        continue;
    else
    {
        shadersToModify.Add(Path.GetDirectoryName(file).Replace(importFolder, ""));
    }
}
List<string> currentList = new List<string>();
string res = "";

for (var i = 0; i < shadersToModify.Count; i++)
{
    currentList.Clear();
    for (int j = 0; j < Data.Shaders.Count; j++)
    {
        string x = Data.Shaders[j].Name.Content;
        res += (x + "\n");
        currentList.Add(x);
    }
    if (Data.Shaders.ByName(shadersToModify[i]) != null)
    {
        Data.Shaders.Remove(Data.Shaders.ByName(shadersToModify[i]));
        AddShader(shadersToModify[i]);
        Reorganize<UndertaleShader>(Data.Shaders, currentList);
    }
    else
        AddShader(shadersToModify[i]);
}


void ImportShader(UndertaleShader existing_shader)
{
    string localImportDir = importFolder + "/" + existing_shader.Name.Content + "/";
    if (File.Exists(localImportDir + "Type.txt"))
    {
        string shader_type = File.ReadAllText(localImportDir + "Type.txt");
        if (shader_type.Contains("GLSL_ES"))
            existing_shader.Type = UndertaleShader.ShaderType.GLSL_ES;
        else if (shader_type.Contains("GLSL"))
            existing_shader.Type = UndertaleShader.ShaderType.GLSL;
        else if (shader_type.Contains("HLSL9"))
            existing_shader.Type = UndertaleShader.ShaderType.HLSL9;
        else if (shader_type.Contains("HLSL11"))
            existing_shader.Type = UndertaleShader.ShaderType.HLSL11;
        else if (shader_type.Contains("PSSL"))
            existing_shader.Type = UndertaleShader.ShaderType.PSSL;
        else if (shader_type.Contains("Cg_PSVita"))
            existing_shader.Type = UndertaleShader.ShaderType.Cg_PSVita;
        else if (shader_type.Contains("Cg_PS3"))
            existing_shader.Type = UndertaleShader.ShaderType.Cg_PS3;
    }
    if (File.Exists(localImportDir + "GLSL_ES_Fragment.txt"))
        existing_shader.GLSL_ES_Fragment.Content = File.ReadAllText(localImportDir + "GLSL_ES_Fragment.txt");
    if (File.Exists(localImportDir + "GLSL_ES_Vertex.txt"))
        existing_shader.GLSL_ES_Vertex.Content = File.ReadAllText(localImportDir + "GLSL_ES_Vertex.txt");
    if (File.Exists(localImportDir + "GLSL_Fragment.txt"))
        existing_shader.GLSL_Fragment.Content = File.ReadAllText(localImportDir + "GLSL_Fragment.txt");
    if (File.Exists(localImportDir + "GLSL_Vertex.txt"))
        existing_shader.GLSL_Vertex.Content = File.ReadAllText(localImportDir + "GLSL_Vertex.txt");
    if (File.Exists(localImportDir + "HLSL9_Fragment.txt"))
        existing_shader.HLSL9_Fragment.Content = File.ReadAllText(localImportDir + "HLSL9_Fragment.txt");
    if (File.Exists(localImportDir + "HLSL9_Vertex.txt"))
        existing_shader.HLSL9_Vertex.Content = File.ReadAllText(localImportDir + "HLSL9_Vertex.txt");
    if (File.Exists(localImportDir + "HLSL11_VertexData.bin"))
    {
        if (existing_shader.HLSL11_VertexData == null)
        {
            existing_shader.HLSL11_VertexData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.HLSL11_VertexData.Data = File.ReadAllBytes(localImportDir + "HLSL11_VertexData.bin");
        existing_shader.HLSL11_VertexData.IsNull = false;
    }
    if (File.Exists(localImportDir + "HLSL11_PixelData.bin"))
    {
        if (existing_shader.HLSL11_PixelData == null)
        {
            existing_shader.HLSL11_PixelData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.HLSL11_PixelData.IsNull = false;
        existing_shader.HLSL11_PixelData.Data = File.ReadAllBytes(localImportDir + "HLSL11_PixelData.bin");
    }
    if (File.Exists(localImportDir + "PSSL_VertexData.bin"))
    {
        if (existing_shader.PSSL_VertexData == null)
        {
            existing_shader.PSSL_VertexData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.PSSL_VertexData.IsNull = false;
        existing_shader.PSSL_VertexData.Data = File.ReadAllBytes(localImportDir + "PSSL_VertexData.bin");
    }
    if (File.Exists(localImportDir + "PSSL_PixelData.bin"))
    {
        if (existing_shader.PSSL_PixelData == null)
        {
            existing_shader.PSSL_PixelData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.PSSL_PixelData.IsNull = false;
        existing_shader.PSSL_PixelData.Data = File.ReadAllBytes(localImportDir + "PSSL_PixelData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PSVita_VertexData.bin"))
    {
        if (existing_shader.Cg_PSVita_VertexData == null)
        {
            existing_shader.Cg_PSVita_VertexData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.Cg_PSVita_VertexData.IsNull = false;
        existing_shader.Cg_PSVita_VertexData.Data = File.ReadAllBytes(localImportDir + "Cg_PSVita_VertexData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PSVita_PixelData.bin"))
    {
        if (existing_shader.Cg_PSVita_PixelData == null)
        {
            existing_shader.Cg_PSVita_PixelData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.Cg_PSVita_PixelData.IsNull = false;
        existing_shader.Cg_PSVita_PixelData.Data = File.ReadAllBytes(localImportDir + "Cg_PSVita_PixelData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PS3_VertexData.bin"))
    {
        if (existing_shader.Cg_PS3_VertexData == null)
        {
            existing_shader.Cg_PS3_VertexData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.Cg_PS3_VertexData.IsNull = false;
        existing_shader.Cg_PS3_VertexData.Data = File.ReadAllBytes(localImportDir + "Cg_PS3_VertexData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PS3_PixelData.bin"))
    {
        if (existing_shader.Cg_PS3_PixelData == null)
        {
            existing_shader.Cg_PS3_PixelData = new UndertaleShader.UndertaleRawShaderData();
        }
        existing_shader.Cg_PS3_PixelData.IsNull = false;
        existing_shader.Cg_PS3_PixelData.Data = File.ReadAllBytes(localImportDir + "Cg_PS3_PixelData.bin");
    }
}

void AddShader(string shader_name)
{
    UndertaleShader new_shader = new UndertaleShader();
    new_shader.Name = Data.Strings.MakeString(shader_name);
    string localImportDir = importFolder + "/" + shader_name + "/";
    if (File.Exists(localImportDir + "Type.txt"))
    {
        string shader_type = File.ReadAllText(localImportDir + "Type.txt");
        if (shader_type.Contains("GLSL_ES"))
            new_shader.Type = UndertaleShader.ShaderType.GLSL_ES;
        else if (shader_type.Contains("GLSL"))
            new_shader.Type = UndertaleShader.ShaderType.GLSL;
        else if (shader_type.Contains("HLSL9"))
            new_shader.Type = UndertaleShader.ShaderType.HLSL9;
        else if (shader_type.Contains("HLSL11"))
            new_shader.Type = UndertaleShader.ShaderType.HLSL11;
        else if (shader_type.Contains("PSSL"))
            new_shader.Type = UndertaleShader.ShaderType.PSSL;
        else if (shader_type.Contains("Cg_PSVita"))
            new_shader.Type = UndertaleShader.ShaderType.Cg_PSVita;
        else if (shader_type.Contains("Cg_PS3"))
            new_shader.Type = UndertaleShader.ShaderType.Cg_PS3;
        else
            new_shader.Type = UndertaleShader.ShaderType.GLSL_ES;
    }
    else
        new_shader.Type = UndertaleShader.ShaderType.GLSL_ES;
    if (File.Exists(localImportDir + "GLSL_ES_Fragment.txt"))
        new_shader.GLSL_ES_Fragment = Data.Strings.MakeString(File.ReadAllText(localImportDir + "GLSL_ES_Fragment.txt"));
    else
        new_shader.GLSL_ES_Fragment = Data.Strings.MakeString("");
    if (File.Exists(localImportDir + "GLSL_ES_Vertex.txt"))
        new_shader.GLSL_ES_Vertex = Data.Strings.MakeString(File.ReadAllText(localImportDir + "GLSL_ES_Vertex.txt"));
    else
        new_shader.GLSL_ES_Vertex = Data.Strings.MakeString("");
    if (File.Exists(localImportDir + "GLSL_Fragment.txt"))
        new_shader.GLSL_Fragment = Data.Strings.MakeString(File.ReadAllText(localImportDir + "GLSL_Fragment.txt"));
    else
        new_shader.GLSL_Fragment = Data.Strings.MakeString("");
    if (File.Exists(localImportDir + "GLSL_Vertex.txt"))
        new_shader.GLSL_Vertex = Data.Strings.MakeString(File.ReadAllText(localImportDir + "GLSL_Vertex.txt"));
    else
        new_shader.GLSL_Vertex = Data.Strings.MakeString("");
    if (File.Exists(localImportDir + "HLSL9_Fragment.txt"))
        new_shader.HLSL9_Fragment = Data.Strings.MakeString(File.ReadAllText(localImportDir + "HLSL9_Fragment.txt"));
    else
        new_shader.HLSL9_Fragment = Data.Strings.MakeString("");
    if (File.Exists(localImportDir + "HLSL9_Vertex.txt"))
        new_shader.HLSL9_Vertex = Data.Strings.MakeString(File.ReadAllText(localImportDir + "HLSL9_Vertex.txt"));
    else
        new_shader.HLSL9_Vertex = Data.Strings.MakeString("");
    if (File.Exists(localImportDir + "HLSL11_VertexData.bin"))
    {
        new_shader.HLSL11_VertexData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.HLSL11_VertexData.Data = File.ReadAllBytes(localImportDir + "HLSL11_VertexData.bin");
        new_shader.HLSL11_VertexData.IsNull = false;
    }
    if (File.Exists(localImportDir + "HLSL11_PixelData.bin"))
    {
        new_shader.HLSL11_PixelData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.HLSL11_PixelData.IsNull = false;
        new_shader.HLSL11_PixelData.Data = File.ReadAllBytes(localImportDir + "HLSL11_PixelData.bin");
    }
    if (File.Exists(localImportDir + "PSSL_VertexData.bin"))
    {
        new_shader.PSSL_VertexData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.PSSL_VertexData.IsNull = false;
        new_shader.PSSL_VertexData.Data = File.ReadAllBytes(localImportDir + "PSSL_VertexData.bin");
    }
    if (File.Exists(localImportDir + "PSSL_PixelData.bin"))
    {
        new_shader.PSSL_PixelData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.PSSL_PixelData.IsNull = false;
        new_shader.PSSL_PixelData.Data = File.ReadAllBytes(localImportDir + "PSSL_PixelData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PSVita_VertexData.bin"))
    {
        new_shader.Cg_PSVita_VertexData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.Cg_PSVita_VertexData.IsNull = false;
        new_shader.Cg_PSVita_VertexData.Data = File.ReadAllBytes(localImportDir + "Cg_PSVita_VertexData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PSVita_PixelData.bin"))
    {
        new_shader.Cg_PSVita_PixelData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.Cg_PSVita_PixelData.IsNull = false;
        new_shader.Cg_PSVita_PixelData.Data = File.ReadAllBytes(localImportDir + "Cg_PSVita_PixelData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PS3_VertexData.bin"))
    {
        new_shader.Cg_PS3_VertexData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.Cg_PS3_VertexData.IsNull = false;
        new_shader.Cg_PS3_VertexData.Data = File.ReadAllBytes(localImportDir + "Cg_PS3_VertexData.bin");
    }
    if (File.Exists(localImportDir + "Cg_PS3_PixelData.bin"))
    {
        new_shader.Cg_PS3_PixelData = new UndertaleShader.UndertaleRawShaderData();
        new_shader.Cg_PS3_PixelData.IsNull = false;
        new_shader.Cg_PS3_PixelData.Data = File.ReadAllBytes(localImportDir + "Cg_PS3_PixelData.bin");
    }
    if (File.Exists(localImportDir + "VertexShaderAttributes.txt"))
    {
        string line;
        // Read the file and display it line by line.
        System.IO.StreamReader file = new System.IO.StreamReader(localImportDir + "VertexShaderAttributes.txt");
        while((line = file.ReadLine()) != null)
        {
            if (line != "")
            {
                UndertaleShader.VertexShaderAttribute vertex_x = new UndertaleShader.VertexShaderAttribute();
                vertex_x.Name = Data.Strings.MakeString(line);
                new_shader.VertexShaderAttributes.Add(vertex_x);
            }
        }
        file.Close();
    }
    Data.Shaders.Add(new_shader);
}

void Reorganize<T>(IList<T> list, List<string> order) where T : UndertaleNamedResource, new()
{
    Dictionary<string, T> temp = new Dictionary<string, T>();
    for (int i = 0; i < list.Count; i++)
    {
        T asset = list[i];
        string assetName = asset.Name?.Content;
        if (order.Contains(assetName))
        {
            temp[assetName] = asset;
        }
    }

    List<T> addOrder = new List<T>();
    for (int i = order.Count - 1; i >= 0; i--)
    {
        T asset;
        try
        {
            asset = temp[order[i]];
        } catch (Exception e)
        {
            throw new ScriptException("Missing asset with name \"" + order[i] + "\"");
        }
        addOrder.Add(asset);
    }

    foreach (T asset in addOrder)
        list.Remove(asset);
    foreach (T asset in addOrder)
        list.Insert(0, asset);
}
