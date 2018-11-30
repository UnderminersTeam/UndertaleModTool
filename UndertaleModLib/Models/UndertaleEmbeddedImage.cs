using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    // Not to be confused with the other "embedded" resources, this is a bit separate.
    // GMS2 only, see https://github.com/krzys-h/UndertaleModTool/issues/4#issuecomment-421844420 for rough structure, but doesn't appear commonly used
    public class UndertaleEmbeddedImage : UndertaleNamedResource
    {
        public UndertaleString Name { get; set; }
        public UndertaleTexturePageItem TextureEntry;

        public UndertaleEmbeddedImage()
        {
        }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleObjectPointer(TextureEntry);
        }

        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            TextureEntry = reader.ReadUndertaleObjectPointer<UndertaleTexturePageItem>();
        }

        public override string ToString()
        {
            return Name.Content + " (" + GetType().Name + ")";
        }
    }
}
