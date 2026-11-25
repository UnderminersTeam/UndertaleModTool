using CommunityToolkit.Mvvm.ComponentModel;
using UndertaleModLib;
using static UndertaleModLib.UndertaleChunkEXTN;

namespace UndertaleModToolAvalonia;

public partial class UndertaleExtensionChunkViewModel : ObservableObject, ITabContent
{
    public UndertaleChunkEXTN ExtensionChunk { get; }

    public UndertaleExtensionChunkViewModel(UndertaleChunkEXTN extensionChunk)
    {
        ExtensionChunk = extensionChunk;
    }

    public static ByteArrayWrapper CreateByteArray() => new byte[16];
}
