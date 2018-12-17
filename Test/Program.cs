using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            UndertaleData data = UndertaleIO.Read(new FileStream(@"deltarune\data.win", FileMode.Open, FileAccess.Read));

            foreach(var code in data.Code)
            {
                Debug.WriteLine(code.Name.Content);
                code.Replace(Assembler.Assemble(code.Disassemble(data.Variables, data.CodeLocals.For(code)), data.Functions, data.Variables, data.Strings));
            }

            UndertaleIO.Write(new FileStream(@"deltarune\newdata.win", FileMode.Create), data);
        }
    }
}
