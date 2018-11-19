using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTests
{
    public abstract class GameLoadingTestBase
    {
        private readonly string path;
        private readonly string expectedMD5;
        private UndertaleData data;

        private static string GenerateMD5(Stream stream)
        {
            using (var md5 = MD5.Create())
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        public GameLoadingTestBase(string path, string md5)
        {
            this.path = path;
            this.expectedMD5 = md5;
        }

        [TestInitialize]
        public void LoadData()
        {
            if (!File.Exists(path))
                Assert.Inconclusive("Unable to test, file not found: " + path);

            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                string fileMD5 = GenerateMD5(fs);
                if (fileMD5 != expectedMD5)
                    Assert.Inconclusive("Unable to test, incorrect file: got " + fileMD5 + " expected " + expectedMD5);
                fs.Position = 0;

                data = UndertaleIO.Read(fs);
            }
        }

        [TestMethod]
        public void SaveDataAndCompare()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                UndertaleIO.Write(ms, data);

                ms.Position = 0;
                string writtenMD5 = GenerateMD5(ms);
                Assert.AreEqual(expectedMD5, writtenMD5, "Written file doesn't match read file");
            }
        }

        [TestMethod]
        public void DecompileAllScripts()
        {
            foreach(var code in data.Code)
            {
                if (code.Name.Content == "gml_Object_obj_vaporized_new_Draw_0")
                {
                    Console.WriteLine("SKIPPING " + code.Name.Content + ", known bug"); // see #85
                    continue;
                }

                Console.WriteLine(code.Name.Content);
                try
                {
                    Decompiler.Decompile(code, data);
                }
                catch(Exception e)
                {
                    throw new Exception("Failed to decompile script " + code.Name.Content, e);
                }
            }
        }

        [TestMethod]
        public void DisassembleAndReassembleAllScripts()
        {
            foreach (var code in data.Code)
            {
                Console.WriteLine(code.Name.Content);

                bool knownBug = false;
                foreach(var instr in code.Instructions)
                {
                    if (instr.Value?.GetType() == typeof(UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>))
                    {
                        UndertaleString str = ((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)instr.Value).Resource;
                        if (str.Content.Contains("\n") || str.Content.Contains("\"")) // see #28
                            knownBug = true;
                    }
                }
                if (code.Name.Content == "gml_Object_obj_battlebomb_Alarm_3") // The script that contains the one and only popenv with a weirdly encoded offset
                    knownBug = true;
                if (knownBug)
                {
                    Console.WriteLine("SKIPPING " + code.Name.Content + ", known bug");
                    continue;
                }

                string disasm;
                try
                {
                    disasm = code.Disassemble(data.Variables, data.CodeLocals.For(code));
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to disassemble script " + code.Name.Content, e);
                }

                IList<UndertaleInstruction> reasm = Assembler.Assemble(disasm, data.Functions, data.Variables, data.Strings);
                Assert.AreEqual(code.Instructions.Count, reasm.Count, "Reassembled instruction count didn't match the disassembly for script " + code.Name.Content);
                for(int i = 0; i < code.Instructions.Count; i++)
                {
                    string errMsg = "Instruction at " + code.Instructions[i].Address.ToString("D5") + " didn't match for script: " + code.Name.Content;
                    Assert.AreEqual(code.Instructions[i].Kind, reasm[i].Kind, errMsg);
                    Assert.AreEqual(code.Instructions[i].ComparisonKind, reasm[i].ComparisonKind, errMsg);
                    Assert.AreEqual(code.Instructions[i].Type1, reasm[i].Type1, errMsg);
                    Assert.AreEqual(code.Instructions[i].Type2, reasm[i].Type2, errMsg);
                    Assert.AreEqual(code.Instructions[i].TypeInst, reasm[i].TypeInst, errMsg);
                    Assert.AreEqual(code.Instructions[i].DupExtra, reasm[i].DupExtra, errMsg);
                    Assert.AreEqual(code.Instructions[i].ArgumentsCount, reasm[i].ArgumentsCount, errMsg);
                    Assert.AreEqual(code.Instructions[i].JumpOffset, reasm[i].JumpOffset, errMsg);
                    Assert.AreEqual(code.Instructions[i].JumpOffsetIsWeird, reasm[i].JumpOffsetIsWeird, errMsg);
                    Assert.AreSame(code.Instructions[i].Destination?.Target, reasm[i].Destination?.Target, errMsg);
                    Assert.AreEqual(code.Instructions[i].Destination?.Type, reasm[i].Destination?.Type, errMsg);
                    Assert.AreSame(code.Instructions[i].Function?.Target, reasm[i].Function?.Target, errMsg);
                    Assert.AreEqual(code.Instructions[i].Function?.Type, reasm[i].Function?.Type, errMsg);

                    Assert.AreEqual(code.Instructions[i].Value?.GetType(), reasm[i].Value?.GetType(), errMsg);
                    if (code.Instructions[i].Value?.GetType() == typeof(double))
                        Assert.AreEqual((double)code.Instructions[i].Value, (double)reasm[i].Value, Math.Abs((double)code.Instructions[i].Value) * (1e-5), errMsg); // see issue #53
                    else if (code.Instructions[i].Value?.GetType() == typeof(float))
                        Assert.AreEqual((float)code.Instructions[i].Value, (float)reasm[i].Value, Math.Abs((float)code.Instructions[i].Value) * (1e-5), errMsg); // see issue #53
                    else if (code.Instructions[i].Value?.GetType() == typeof(UndertaleInstruction.Reference<UndertaleVariable>))
                        Assert.AreSame(((UndertaleInstruction.Reference<UndertaleVariable>)code.Instructions[i].Value).Target, ((UndertaleInstruction.Reference<UndertaleVariable>)reasm[i].Value).Target, errMsg);
                    else if (code.Instructions[i].Value?.GetType() == typeof(UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>))
                        Assert.AreSame(((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)code.Instructions[i].Value).Resource, ((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)reasm[i].Value).Resource, errMsg);
                    else
                        Assert.AreEqual(code.Instructions[i].Value, reasm[i].Value, errMsg);
                }
            }
        }
    }

    [TestClass]
    public class UndertaleLoadingTest : GameLoadingTestBase
    {
        public UndertaleLoadingTest() : base(@"C:\Program Files (x86)\Steam\steamapps\common\Undertale\data.win", "5903fc5cb042a728d4ad8ee9e949c6eb")
        {
        }
    }

    [TestClass]
    public class UndertaleSwitchLoadingTest : GameLoadingTestBase
    {
        public UndertaleSwitchLoadingTest() : base(@"..\..\..\Test\bin\Debug\switch\game.win", "427520a97db28c87da4220abb3a334c1")
        {
        }
    }

    [TestClass]
    public class DeltaruneLoadingTest : GameLoadingTestBase
    {
        public DeltaruneLoadingTest() : base(@"C:\Program Files (x86)\SURVEY_PROGRAM\data.win", "a88a2db3a68c714ca2b1ff57ac08a032")
        {
        }
    }
}
