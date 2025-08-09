using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Platform.Storage;
using Microsoft.Extensions.DependencyInjection;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia.Views;

public partial class UndertaleShaderViewModel : IUndertaleResourceViewModel
{
    public MainViewModel MainVM;
    public UndertaleResource Resource => Shader;
    public UndertaleShader Shader { get; set; }

    IReadOnlyList<FilePickerFileType> binFileTypes = [
        new FilePickerFileType("BIN files (.bin)")
        {
            Patterns = ["*.bin"],
        },
        new FilePickerFileType("All files")
        {
            Patterns = ["*"],
        },
    ];

    public UndertaleShaderViewModel(UndertaleShader shader, IServiceProvider? serviceProvider = null)
    {
        Shader = shader;

        MainVM = (serviceProvider ?? App.Services).GetRequiredService<MainViewModel>();
    }

    public static UndertaleShader.VertexShaderAttribute CreateVertexShaderAttribute() => new();

    UndertaleShader.UndertaleRawShaderData? GetRawShaderDataFromString(string parameter)
    {
        return parameter switch
        {
            "HLSL11_VertexData" => Shader.HLSL11_VertexData,
            "HLSL11_PixelData" => Shader.HLSL11_PixelData,
            "PSSL_VertexData" => Shader.PSSL_VertexData,
            "PSSL_PixelData" => Shader.PSSL_PixelData,
            "Cg_PSVita_VertexData" => Shader.Cg_PSVita_VertexData,
            "Cg_PSVita_PixelData" => Shader.Cg_PSVita_PixelData,
            "Cg_PS3_VertexData" => Shader.Cg_PS3_VertexData,
            "Cg_PS3_PixelData" => Shader.Cg_PS3_PixelData,
            _ => throw new NotImplementedException(),
        };
    }

    public async void ImportRawShaderData(string parameter)
    {
        IReadOnlyList<IStorageFile> files = await MainVM.View!.OpenFileDialog(new FilePickerOpenOptions
        {
            Title = "Import shader",
            FileTypeFilter = binFileTypes,
        });

        if (files.Count != 1)
            return;

        using Stream stream = await files[0].OpenReadAsync();
        byte[] bytes = new byte[stream.Length];
        await stream.ReadAsync(bytes);

        UndertaleShader.UndertaleRawShaderData? rawShaderData = GetRawShaderDataFromString(parameter);

        if (rawShaderData is null)
        {
            rawShaderData = new UndertaleShader.UndertaleRawShaderData();

            Action setRawShaderData = parameter switch
            {
                "HLSL11_VertexData" => () => Shader.HLSL11_VertexData = rawShaderData,
                "HLSL11_PixelData" => () => Shader.HLSL11_PixelData = rawShaderData,
                "PSSL_VertexData" => () => Shader.PSSL_VertexData = rawShaderData,
                "PSSL_PixelData" => () => Shader.PSSL_PixelData = rawShaderData,
                "Cg_PSVita_VertexData" => () => Shader.Cg_PSVita_VertexData = rawShaderData,
                "Cg_PSVita_PixelData" => () => Shader.Cg_PSVita_PixelData = rawShaderData,
                "Cg_PS3_VertexData" => () => Shader.Cg_PS3_VertexData = rawShaderData,
                "Cg_PS3_PixelData" => () => Shader.Cg_PS3_PixelData = rawShaderData,
                _ => throw new NotImplementedException(),
            };
            setRawShaderData();
        }

        rawShaderData.IsNull = false;
        rawShaderData.Data = bytes;
    }

    public async void ExportRawShaderData(string parameter)
    {
        UndertaleShader.UndertaleRawShaderData? rawShaderData = GetRawShaderDataFromString(parameter);

        if (rawShaderData is null)
            return;

        IStorageFile? file = await MainVM.View!.SaveFileDialog(new FilePickerSaveOptions()
        {
            Title = "Export shader",
            FileTypeChoices = binFileTypes,
            DefaultExtension = ".bin",
            SuggestedFileName = Shader.Name?.Content + "_" + parameter + ".bin",
        });

        if (file is null)
            return;

        using Stream stream = await file.OpenWriteAsync();

        stream.Write(rawShaderData.Data);
    }
}
