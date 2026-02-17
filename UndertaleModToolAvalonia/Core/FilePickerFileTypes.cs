using System.Collections.Generic;
using Avalonia.Platform.Storage;

namespace UndertaleModToolAvalonia;

public static class FilePickerFileTypes
{
    static readonly FilePickerFileType AllSingle = new("All files")
    {
        Patterns = ["*"],
    };

    static readonly FilePickerFileType BINSingle = new("BIN files (.bin)")
    {
        Patterns = ["*.bin"],
    };

    static readonly FilePickerFileType DataSingle = new("GameMaker data files (.win, .unx, .ios, .droid, audiogroup*.dat)")
    {
        Patterns = ["*.win", "*.unx", "*.ios", "*.droid", "audiogroup*.dat"],
    };

    static readonly FilePickerFileType PNGSingle = new("PNG files (.png)")
    {
        Patterns = ["*.png"],
    };

    static readonly FilePickerFileType QOISingle = new("QOI files (.qoi)")
    {
        Patterns = ["*.qoi"],
    };

    static readonly FilePickerFileType BZ2Single = new("BZ2 files (.bz2)")
    {
        Patterns = ["*.bz2"],
    };

    static readonly FilePickerFileType WAVSingle = new("WAV files (.wav)")
    {
        Patterns = ["*.wav"],
    };

    static readonly FilePickerFileType CSSingle = new("C# scripts (.csx)")
    {
        Patterns = ["*.csx"],
    };

    static readonly FilePickerFileType EXESingle = new("Executable files (.exe)")
    {
        Patterns = ["*.exe"],
    };

    public static readonly IReadOnlyList<FilePickerFileType> All = [AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> BIN = [BINSingle, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> Data = [DataSingle, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> Image = [PNGSingle, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> PNG = [PNGSingle, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> QOI = [QOISingle, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> BZ2 = [BZ2Single, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> WAV = [WAVSingle, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> CS = [CSSingle, AllSingle];
    public static readonly IReadOnlyList<FilePickerFileType> EXE = [EXESingle, AllSingle];
}
