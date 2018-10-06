using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Models
{
    public class UndertaleTimeline : UndertaleObject
    {
        public UndertaleTimeline()
        {
            throw new NotImplementedException();
        }

        public void Serialize(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Unserialize(UndertaleReader reader)
        {
            throw new NotImplementedException();
        }
    }

    // GMS2 only, see https://github.com/krzys-h/UndertaleModTool/issues/4#issuecomment-421844420 for rough structure, but doesn't appear commonly used
    public class UndertaleEmbeddedISomething : UndertaleObject
    {
        public UndertaleEmbeddedISomething()
        {
            throw new NotImplementedException();
        }

        public void Serialize(UndertaleWriter writer)
        {
            throw new NotImplementedException();
        }

        public void Unserialize(UndertaleReader reader)
        {
            throw new NotImplementedException();
        }
    }
}
