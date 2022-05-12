namespace UndertaleModLib.Models;

// Not to be confused with the other "embedded" resources, this is a bit separate.
// GMS2 only, see https://github.com/krzys-h/UndertaleModTool/issues/4#issuecomment-421844420 for rough structure, but doesn't appear commonly used
public class UndertaleEmbeddedImage : UndertaleNamedResource
{
    public UndertaleString Name { get; set; }
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
        return Name.Content + " (" + GetType().Name + ")";
    }
}