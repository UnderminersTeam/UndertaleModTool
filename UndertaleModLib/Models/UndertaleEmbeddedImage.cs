using System;

namespace UndertaleModLib.Models;

/// <summary>
/// An embedded image entry in a GameMaker data file. This is GMS2 only.<br/>
/// Not to be confused with the other "embedded" resources, this is a bit different.
/// </summary>
/// <remarks>Rough structure:<code>
/// EMBI - stands for "embedded images"?
/// Couldn't figure out how to fill the chunk with any data by editing a project and recompiling...
/// ---
/// &lt;normal FourCC and chunk length found in every chunk&gt;
///
/// Int32 - literally just the number 1, always (unknown)
///
/// Array of some kind:
/// Int32 - count of items in array
///
/// Each item:
/// 32-bit string pointer, some kind of texture page identifier?
/// 32-bit pointer to something relating to a texture page entry?
/// </code>
/// <see href="https://github.com/krzys-h/UndertaleModTool/issues/4#issuecomment-421844420"/>.</remarks>
public class UndertaleEmbeddedImage : UndertaleNamedResource, IStaticChildObjectsSize, IDisposable
{
    /// <inheritdoc cref="IStaticChildObjectsSize.ChildObjectsSize" />
    public static readonly uint ChildObjectsSize = 8;

    /// <summary>
    /// The name of the <see cref="UndertaleEmbeddedImage"/>.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// The <see cref="UndertaleTexturePageItem"/> of this <see cref="UndertaleEmbeddedImage"/>.
    /// </summary>
    public UndertaleTexturePageItem TextureEntry { get; set; }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.WriteUndertaleString(Name);
        writer.WriteUndertaleObjectPointer(TextureEntry);
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Name = reader.ReadUndertaleString();
        TextureEntry = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
    }

    /// <inheritdoc />
    public override string ToString()
    {
        return Name?.Content + " (" + GetType().Name + ")";
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        Name = null;
        TextureEntry = null;
    }
}