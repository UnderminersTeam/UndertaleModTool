using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleShader;

namespace UndertaleModLib.Project.SerializableAssets;

/// <summary>
/// A serializable version of <see cref="UndertaleShader"/>.
/// </summary>
internal sealed class SerializableShader : ISerializableProjectAsset
{
    /// <inheritdoc/>
    public string DataName { get; set; }

    /// <inheritdoc/>
    [JsonIgnore]
    public SerializableAssetType AssetType => SerializableAssetType.Shader;

    /// <inheritdoc/>
    [JsonIgnore]
    public bool IndividualDirectory => true;

    /// <inheritdoc/>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public int OverrideOrder { get; set; }

    /// <inheritdoc cref="UndertaleShader.Type"/>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ShaderType Type { get; set; } = ShaderType.GLSL_ES;

    /// <inheritdoc cref="UndertaleShader.Version"/>
    public int Version { get; set; } = 2;

    /// <inheritdoc cref="UndertaleShader.VertexShaderAttributes"/>
    public List<string> VertexShaderAttributes { get; set; } = [];

    // Data asset that was located during pre-import.
    private UndertaleShader _dataAsset = null;

    /// <summary>
    /// Populates this serializable script with data from an actual script.
    /// </summary>
    internal void PopulateFromData(ProjectContext projectContext, UndertaleShader shader)
    {
        // Update all main properties
        DataName = shader.Name.Content;
        Type = shader.Type;
        Version = shader.Version;
        VertexShaderAttributes.Clear();
        VertexShaderAttributes.Capacity = shader.VertexShaderAttributes.Count;
        foreach (var attr in shader.VertexShaderAttributes)
        {
            if (attr.Name.Content is string str)
            {
                VertexShaderAttributes.Add(str);
            }
        }

        _dataAsset = shader;
    }

    /// <summary>
    /// Serializes an individual plaintext shader file.
    /// </summary>
    private static void SerializeShaderPlaintext(string destinationDirectory, UndertaleString data, string name)
    {
        if (data.Content is not string content)
        {
            return;
        }
        File.WriteAllText(Path.Join(destinationDirectory, $"{name}.txt"), content);
    }

    /// <summary>
    /// Serializes an individual binary shader file.
    /// </summary>
    private static void SerializeShaderBinary(string destinationDirectory, UndertaleRawShaderData data, string name)
    {
        if (data is null)
        {
            return;
        }
        if (data.IsNull || data.Data is null)
        {
            return;
        }
        File.WriteAllBytes(Path.Join(destinationDirectory, $"{name}.bin"), data.Data);
    }

    /// <inheritdoc/>
    public void Serialize(ProjectContext projectContext, string destinationFile)
    {
        // Write main JSON
        using (FileStream fs = new(destinationFile, FileMode.Create))
        {
            JsonSerializer.Serialize<ISerializableProjectAsset>(fs, this, ProjectContext.JsonOptions);
        }
        
        // Write plaintext and data files
        string directory = Path.GetDirectoryName(destinationFile);
        SerializeShaderPlaintext(directory, _dataAsset.GLSL_ES_Fragment, "GLSL_ES_Fragment");
        SerializeShaderPlaintext(directory, _dataAsset.GLSL_ES_Vertex, "GLSL_ES_Vertex");
        SerializeShaderPlaintext(directory, _dataAsset.GLSL_Fragment, "GLSL_Fragment");
        SerializeShaderPlaintext(directory, _dataAsset.GLSL_Vertex, "GLSL_Vertex");
        SerializeShaderPlaintext(directory, _dataAsset.HLSL9_Fragment, "HLSL9_Fragment");
        SerializeShaderPlaintext(directory, _dataAsset.HLSL9_Vertex, "HLSL9_Vertex");
        SerializeShaderBinary(directory, _dataAsset.HLSL11_VertexData, "HLSL11_VertexData");
        SerializeShaderBinary(directory, _dataAsset.HLSL11_PixelData, "HLSL11_PixelData");
        SerializeShaderBinary(directory, _dataAsset.PSSL_VertexData, "PSSL_VertexData");
        SerializeShaderBinary(directory, _dataAsset.PSSL_PixelData, "PSSL_PixelData");
        SerializeShaderBinary(directory, _dataAsset.Cg_PSVita_VertexData, "Cg_PSVita_VertexData");
        SerializeShaderBinary(directory, _dataAsset.Cg_PSVita_PixelData, "Cg_PSVita_PixelData");
        SerializeShaderBinary(directory, _dataAsset.Cg_PS3_VertexData, "Cg_PS3_VertexData");
        SerializeShaderBinary(directory, _dataAsset.Cg_PS3_PixelData, "Cg_PS3_PixelData");
    }

    /// <inheritdoc/>
    public void PreImport(ProjectContext projectContext)
    {
        if (projectContext.Data.Shaders.ByName(DataName) is UndertaleShader existing)
        {
            // Shader found
            _dataAsset = existing;
        }
        else
        {
            // No shader found; create new one
            _dataAsset = new()
            {
                Name = projectContext.MakeString(DataName)
            };
            projectContext.Data.Shaders.Add(_dataAsset);
        }
    }

    /// <summary>
    /// Imports an individual plaintext shader file.
    /// </summary>
    private static void ImportShaderPlaintextFile(string importDirectory, UndertaleData data, Action<UndertaleString> stringSetter, string name)
    {
        string path = Path.Join(importDirectory, $"{name}.txt");
        if (!File.Exists(path))
        {
            stringSetter(data.Strings.MakeString(""));
            return;
        }
        stringSetter(data.Strings.MakeString(File.ReadAllText(path)));
    }

    /// <summary>
    /// Imports an individual binary shader file.
    /// </summary>
    private static void ImportShaderBinaryFile(string importDirectory, Action<UndertaleRawShaderData> dataSetter, string name)
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

    /// <inheritdoc/>
    public IProjectAsset Import(ProjectContext projectContext)
    {
        UndertaleShader shader = _dataAsset;

        // Update all main properties
        shader.Type = Type;
        shader.Version = Version;
        shader.VertexShaderAttributes.Clear();
        foreach (string attr in VertexShaderAttributes)
        {
            shader.VertexShaderAttributes.Add(new()
            {
                Name = projectContext.Data.Strings.MakeString(attr)
            });
        }

        // Import plaintext and data files
        if (!projectContext.AssetDataNamesToPaths.TryGetValue(new(DataName, AssetType), out string jsonFilename))
        {
            throw new ProjectException("Failed to get shader asset path");
        }
        string directory = Path.GetDirectoryName(jsonFilename);
        ImportShaderPlaintextFile(directory, projectContext.Data, (str) => shader.GLSL_ES_Fragment = str, "GLSL_ES_Fragment");
        ImportShaderPlaintextFile(directory, projectContext.Data, (str) => shader.GLSL_ES_Vertex = str, "GLSL_ES_Vertex");
        ImportShaderPlaintextFile(directory, projectContext.Data, (str) => shader.GLSL_Fragment = str, "GLSL_Fragment");
        ImportShaderPlaintextFile(directory, projectContext.Data, (str) => shader.GLSL_Vertex = str, "GLSL_Vertex");
        ImportShaderPlaintextFile(directory, projectContext.Data, (str) => shader.HLSL9_Fragment = str, "HLSL9_Fragment");
        ImportShaderPlaintextFile(directory, projectContext.Data, (str) => shader.HLSL9_Vertex = str, "HLSL9_Vertex");
        ImportShaderBinaryFile(directory, (data) => shader.HLSL11_VertexData = data, "HLSL11_VertexData");
        ImportShaderBinaryFile(directory, (data) => shader.HLSL11_PixelData = data, "HLSL11_PixelData");
        ImportShaderBinaryFile(directory, (data) => shader.PSSL_VertexData = data, "PSSL_VertexData");
        ImportShaderBinaryFile(directory, (data) => shader.PSSL_PixelData = data, "PSSL_PixelData");
        ImportShaderBinaryFile(directory, (data) => shader.Cg_PSVita_VertexData = data, "Cg_PSVita_VertexData");
        ImportShaderBinaryFile(directory, (data) => shader.Cg_PSVita_PixelData = data, "Cg_PSVita_PixelData");
        ImportShaderBinaryFile(directory, (data) => shader.Cg_PS3_VertexData = data, "Cg_PS3_VertexData");
        ImportShaderBinaryFile(directory, (data) => shader.Cg_PS3_PixelData = data, "Cg_PS3_PixelData");

        return shader;
    }
}
