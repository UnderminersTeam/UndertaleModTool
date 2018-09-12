using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.ModelsDebug
{
    // The index of this object matches index in CODE in main data file
    public class UndertaleScriptSource : UndertaleResource
    {
        public UndertaleString SourceCode { get; set; }

        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(SourceCode);
        }

        public void Unserialize(UndertaleReader reader)
        {
            SourceCode = reader.ReadUndertaleString();
        }
    }
}
