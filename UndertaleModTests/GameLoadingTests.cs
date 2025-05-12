using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Underanalyzer.Decompiler;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModTests
{
    public abstract class GameLoadingTestBase : GameTestBase
    {
        public GameLoadingTestBase(string path, string md5) : base(path, md5)
        {
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
            GlobalDecompileContext context = new GlobalDecompileContext(data);
            Parallel.ForEach(data.Code, (code) =>
            {
                if (code is null)
                    return;
                //Console.WriteLine(code.Name.Content);
                try
                {
                    new DecompileContext(context, code, data.ToolInfo.DecompilerSettings).DecompileToString();
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to decompile script " + code.Name.Content, e);
                }
            });
        }

        [TestMethod]
        public void DisassembleAndReassembleAllScripts()
        {
            Parallel.ForEach(data.Code, (code) =>
            {
                if (code is null)
                    return;
                //Console.WriteLine(code.Name.Content);

                bool knownBug = false;
                foreach(var instr in code.Instructions)
                {
                    if (instr.ValueString is not null)
                    {
                        UndertaleString str = instr.ValueString.Resource;
                        if (str.Content.Contains('\n') || str.Content.Contains('"')) // see #28
                            knownBug = true;
                    }
                }
                if (knownBug)
                {
                    Console.WriteLine("SKIPPING " + code.Name.Content + ", known bug");
                    return;
                }

                string disasm;
                try
                {
                    disasm = code.Disassemble(data.Variables, data.CodeLocals?.For(code), data.CodeLocals is null);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to disassemble script " + code.Name.Content, e);
                }

                IList<UndertaleInstruction> reasm = Assembler.Assemble(disasm, data.Functions, data.Variables, data.Strings);
                Assert.AreEqual(code.Instructions.Count, reasm.Count, "Reassembled instruction count didn't match the disassembly for script " + code.Name.Content);
                uint address = 0;
                for(int i = 0; i < code.Instructions.Count; i++)
                {
                    UndertaleInstruction instr = code.Instructions[i];

                    string errMsg = "Instruction at " + address.ToString("D5") + " didn't match for script: " + code.Name.Content;
                    Assert.AreEqual(instr.Kind, reasm[i].Kind, errMsg);
                    Assert.AreEqual(instr.ExtendedKind, reasm[i].ExtendedKind, errMsg);
                    Assert.AreEqual(instr.ComparisonKind, reasm[i].ComparisonKind, errMsg);
                    Assert.AreEqual(instr.Type1, reasm[i].Type1, errMsg);
                    Assert.AreEqual(instr.Type2, reasm[i].Type2, errMsg);
                    Assert.AreEqual(instr.TypeInst, reasm[i].TypeInst, errMsg);
                    Assert.AreEqual(instr.Extra, reasm[i].Extra, errMsg);
                    Assert.AreEqual(instr.SwapExtra, reasm[i].SwapExtra, errMsg);
                    Assert.AreEqual(instr.ArgumentsCount, reasm[i].ArgumentsCount, errMsg);
                    Assert.AreEqual(instr.JumpOffsetPopenvExitMagic, reasm[i].JumpOffsetPopenvExitMagic, errMsg);
                    if (!instr.JumpOffsetPopenvExitMagic)
                        Assert.AreEqual(instr.JumpOffset, reasm[i].JumpOffset, errMsg); // note: also handles IntArgument implicitly
                    Assert.AreSame(instr.ValueVariable, reasm[i].ValueVariable, errMsg);
                    Assert.AreSame(instr.ValueFunction, reasm[i].ValueFunction, errMsg);
                    Assert.AreEqual(instr.ReferenceType, reasm[i].ReferenceType, errMsg);

                    if (instr.Kind == UndertaleInstruction.Opcode.Push && instr.Type1 == UndertaleInstruction.DataType.Double)
                        Assert.AreEqual(instr.ValueDouble, reasm[i].ValueDouble, Math.Abs(instr.ValueDouble) * (1e-5), errMsg); // see issue #53
                    else if (instr.ValueString is not null)
                        Assert.AreSame(instr.ValueString.Resource, reasm[i].ValueString?.Resource, errMsg);
                    else
                    {
                        Assert.AreEqual(instr.ValueShort, reasm[i].ValueShort, errMsg);
                        Assert.AreEqual(instr.ValueInt, reasm[i].ValueInt, errMsg);
                        Assert.AreEqual(instr.ValueLong, reasm[i].ValueLong, errMsg);
                    }

                    address += instr.CalculateInstructionSize();
                }
            });
        }
    }

    [TestClass]
    public class UndertaleLoadingTest : GameLoadingTestBase
    {
        public UndertaleLoadingTest() : base(GamePaths.UNDERTALE_PATH, GamePaths.UNDERTALE_MD5)
        {
        }
    }

    [TestClass]
    public class UndertaleSwitchLoadingTest : GameLoadingTestBase
    {
        public UndertaleSwitchLoadingTest() : base(GamePaths.UNDERTALE_SWITCH_PATH, GamePaths.UNDERTALE_SWITCH_MD5)
        {
        }
    }

    [TestClass]
    public class DeltaruneLoadingTest : GameLoadingTestBase
    {
        public DeltaruneLoadingTest() : base(GamePaths.DELTARUNE_PATH, GamePaths.DELTARUNE_MD5)
        {
        }
    }



    [TestClass]
    public class EmptyGameTest
    {
        [TestMethod]
        public void CreateAndSaveEmptyGame()
        {
            UndertaleData data = UndertaleData.CreateNew();
            using (MemoryStream ms = new MemoryStream())
            {
                UndertaleIO.Write(ms, data);
            }
        }
    }

}
