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
using UndertaleModLib.DebugData;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

// C:\Users\krzys\AppData\Roaming\GameMaker-Studio\GMDebug\GMDebug.exe -d=data.yydebug -t="127.0.0.1" -tp=6502 -p="C:\Users\krzys\Documents\GameMaker\Projects\Project4.gmx\Project4.project.gmx"

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            UndertaleData data = UndertaleIO.Read(new FileStream(@"C:\Program Files (x86)\Steam\steamapps\common\undertale\data.win", FileMode.Open));

            //UndertaleIO.Write(new FileStream("newdata.win", FileMode.Create), data);

            UndertaleDebugData debug = DebugDataGenerator.GenerateDebugData(data, DebugDataMode.Decompiled);
            using (FileStream stream = new FileStream("data.yydebug", FileMode.Create))
            {
                using (UndertaleWriter writer = new UndertaleWriter(stream))
                {
                    debug.FORM.Serialize(writer);
                }
            }
        }
    }
}
