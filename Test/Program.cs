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
            UndertaleData data = UndertaleIO.Read(new FileStream(@"C:\Program Files (x86)\Steam\steamapps\common\undertale\data.win", FileMode.Open));
            
            /*foreach (var obj in data.GameObjects)
            {
                for (int i = 0; i < obj.Events.Count; i++)
                {
                    foreach(var evnt in obj.Events[i])
                    {
                        foreach(var scpt in evnt.CodeBlock)
                        {
                            Debug.WriteLine(i + "." + evnt.EventSubtype + ": " + scpt.CodeId.Name.Content);
                        }
                    }
                }
            }*/

            //Directory.CreateDirectory("code");
            Directory.CreateDirectory("decomp");
            foreach (var code in data.Code)
            {
                //if (code.Name.Content == "gml_Object_obj_sans_shift_tester_Keyboard_32")
                //if (code.Name.Content == "gml_Object_obj_statuedrop_Collision_1365")
                //if (code.Name.Content == "gml_Object_obj_encount_core1_Step_0")
                /*if (code.Name.Content == "gml_Object_obj_lastsans_trigger_Step_0")
                {
                    Debug.Write(Decompiler.Decompile(code));
                }*/

                Debug.WriteLine(code.Name.Content);
                using (StreamWriter writer = new StreamWriter("decomp/" + code.Name.Content + ".txt"))
                {
                    try
                    {
                        writer.Write(Decompiler.Decompile(code));
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        writer.WriteLine("EXCEPTION!");
                        writer.Write(e.ToString());
                    }
                }


                //Debug.WriteLine(code.Name.Content);
                /*Dictionary<uint, Decompiler.Block> blocks = Decompiler.DecompileFlowGraph(code);
                using (StreamWriter writer = new StreamWriter("code/" + code.Name.Content + ".dot"))
                {
                    Decompiler.ExportFlowGraph(writer, blocks);
                }*/
                //Printer(rootBlock[0]);
            }

            /*foreach(var room in data.Rooms)
            {
                if (room.Name.Content.StartsWith("room_intro"))
                {
                    Console.WriteLine(room.Name);
                    Console.WriteLine("=============");
                    foreach (var bg in room.Backgrounds)
                        if(bg.BackgroundDefinition != null)
                            Console.WriteLine(bg.BackgroundDefinition.Name);
                    foreach (var go in room.GameObjects)
                        Console.WriteLine(go.ObjectDefinition.Name);
                    Console.WriteLine();
                }
            }*/

            /*data.Strings.Add(new UndertaleString("This is my test added string"));
            data.Strings.Add(new UndertaleString("I want the world to move"));
            data.Strings.Add(new UndertaleString("So I add a ton of stuff"));
            data.Strings.Add(new UndertaleString("To cause the padding to shift"));
            data.Strings.Add(new UndertaleString("And modify all the offsets"));
            for(int i = 0; i < 100; i++)
                data.Strings.Add(new UndertaleString("This is junkstring " + i));

            data.Strings.Insert(0, new UndertaleString("This is seriously going to break"));*/

            /*UndertaleString nam = new UndertaleString("MyNewScript");
            data.Strings.Add(nam);
            var scr = new UndertaleScript()
            {
                Name = nam,
            };
            scr.Code = data.CodeDefinitions[10];
            data.Scripts.Add(scr);*/

            /*UndertaleString roomname = new UndertaleString("room_addedjustnow");
            data.Strings.Add(roomname);
            data.Rooms.Add(new UndertaleRoom() {
                Name = roomname,
                Caption = roomname,
            });*/
            /*for(int i = 0; i < 10; i++)
            {
                UndertaleString funcname = new UndertaleString("functestteeheeanewvar"+i);
                data.Strings.Add(funcname);
                data.Variables.Insert(0, new UndertaleVariable()
                {
                    Name = funcname,
                    FirstAddressCode = data.Variables[0].FirstAddressCode,
                    FirstAddressOffset = data.Variables[0].FirstAddressOffset,
                    Occurrences = 0
                });
            }*/
            /*int ii = 0;
            foreach (UndertaleVariable var in data.Variables)
            {
                var.Name = new UndertaleString("randomname" + (ii++));
                data.Strings.Add(var.Name);
            }*/
            /*UndertaleString funcname = new UndertaleString("functest");
            data.Strings.Add(funcname);
            data.Functions.Insert(0, new UndertaleFunction()
            {
                Name = funcname
            });*/
            /*data.Functions.Add(new UndertaleFunction()
            {
                Name = data.Functions[0].Name
            });*/

            /*UndertaleTexturePageItem texpage = new UndertaleTexturePageItem();
            texpage.X = data.TexturePage[0].X;
            texpage.Y = data.TexturePage[0].Y;
            texpage.Width = data.TexturePage[0].Width;
            texpage.Height = data.TexturePage[0].Height;
            texpage.RenderX = data.TexturePage[0].RenderX;
            texpage.RenderY = data.TexturePage[0].RenderY;
            texpage.BoundingX = data.TexturePage[0].BoundingX;
            texpage.BoundingY = data.TexturePage[0].BoundingY;
            texpage.BoundingWidth = data.TexturePage[0].BoundingWidth;
            texpage.BoundingHeight = data.TexturePage[0].BoundingHeight;
            texpage.TexturePage = data.TexturePage[0].TexturePage;
            data.TexturePage.Add(texpage);*/

            /*UndertaleString name = new UndertaleString("NewAddedSound");
            data.Strings.Add(name);
            UndertaleString type = new UndertaleString(".wav");
            data.Strings.Add(type);
            UndertaleString file = new UndertaleString("NewAddedSound.wav");
            data.Strings.Add(file);
            UndertaleSound snd = new UndertaleSound();
            snd.Name = name;
            snd.Type = type;
            snd.File = file;
            snd.Pitch = 1.0f;
            snd.Volume = 1.0f;
            snd.GroupID = 0;
            snd.Flags = UndertaleSound.AudioEntryFlags.Regular;
            snd.AudioID = null;
            data.Sounds.Add(snd);*/

            UndertaleIO.Write(new FileStream("newdata.win", FileMode.Create), data);
        }
    }
}
