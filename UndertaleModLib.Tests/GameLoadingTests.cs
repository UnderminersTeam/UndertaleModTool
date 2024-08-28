using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Tests
{
    public abstract class GameLoadingTestBase : GameTestBase
    {
        public GameLoadingTestBase(string path, string md5) : base(path, md5)
        {
        }

        [Fact]
        public void SaveDataAndCompare()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                UndertaleIO.Write(ms, data);

                ms.Position = 0;
                string writtenMD5 = GenerateMD5(ms);
                Assert.True(expectedMD5 == writtenMD5, "Written file doesn't match read file");
            }
        }

        [Fact]
        public void DecompileAllScripts()
        {
            GlobalDecompileContext context = new GlobalDecompileContext(data, true);
            Parallel.ForEach(data.Code, (code) =>
            {
                //Console.WriteLine(code.Name.Content);
                try
                {
                    Decompiler.Decompiler.Decompile(code, context);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to decompile script " + code.Name.Content, e);
                }
            });
        }

        [Fact]
        public void DisassembleAndReassembleAllScripts()
        {
            Parallel.ForEach(data.Code, (code) =>
            {
                //Console.WriteLine(code.Name.Content);

                bool knownBug = false;
                foreach (var instr in code.Instructions)
                {
                    if (instr.Value?.GetType() == typeof(UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>))
                    {
                        UndertaleString str = ((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)instr.Value).Resource;
                        if (str.Content.Contains("\n") || str.Content.Contains("\"")) // see #28
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
                    disasm = code.Disassemble(data.Variables, data.CodeLocals.For(code));
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to disassemble script " + code.Name.Content, e);
                }

                IList<UndertaleInstruction> reasm = Assembler.Assemble(disasm, data.Functions, data.Variables, data.Strings);

                Assert.True(code.Instructions.Count == reasm.Count, "Reassembled instruction count didn't match the disassembly for script " + code.Name.Content);

                for (int i = 0; i < code.Instructions.Count; i++)
                {
                    string errMsg = "Instruction at " + code.Instructions[i].Address.ToString("D5") + " didn't match for script: " + code.Name.Content;

                    Assert.True(code.Instructions[i].Kind == reasm[i].Kind, errMsg);
                    Assert.True(code.Instructions[i].ComparisonKind == reasm[i].ComparisonKind, errMsg);
                    Assert.True(code.Instructions[i].Type1 == reasm[i].Type1, errMsg);
                    Assert.True(code.Instructions[i].Type2 == reasm[i].Type2, errMsg);
                    Assert.True(code.Instructions[i].TypeInst == reasm[i].TypeInst, errMsg);
                    Assert.True(code.Instructions[i].Extra == reasm[i].Extra, errMsg);
                    Assert.True(code.Instructions[i].SwapExtra == reasm[i].SwapExtra, errMsg);
                    Assert.True(code.Instructions[i].ArgumentsCount == reasm[i].ArgumentsCount, errMsg);
                    Assert.True(code.Instructions[i].JumpOffsetPopenvExitMagic == reasm[i].JumpOffsetPopenvExitMagic, errMsg);

                    if (!code.Instructions[i].JumpOffsetPopenvExitMagic)
                        Assert.True(code.Instructions[i].JumpOffset == reasm[i].JumpOffset, errMsg); // note: also handles IntArgument implicitly

                    Assert.True(Object.ReferenceEquals(code.Instructions[i].Destination?.Target, reasm[i].Destination?.Target), errMsg);
                    Assert.True(code.Instructions[i].Destination?.Type == reasm[i].Destination?.Type, errMsg);
                    Assert.True(Object.ReferenceEquals(code.Instructions[i].Function?.Target, reasm[i].Function?.Target), errMsg);
                    Assert.True(code.Instructions[i].Function?.Type == reasm[i].Function?.Type, errMsg);

                    Assert.True(Object.ReferenceEquals(code.Instructions[i].Value?.GetType(), reasm[i].Value?.GetType()), errMsg);

                    if (code.Instructions[i].Value?.GetType() == typeof(double))
                        Assert.True(Math.Abs((double)code.Instructions[i].Value - (double)reasm[i].Value)
                            <= (Math.Abs((double)code.Instructions[i].Value) * (1e-5)), errMsg); // see issue #53
                    else if (code.Instructions[i].Value?.GetType() == typeof(float))
                        Assert.True(Math.Abs((float)code.Instructions[i].Value - (float)reasm[i].Value)
                            <= (Math.Abs((float)code.Instructions[i].Value) * (1e-5)), errMsg); // see issue #53
                    else if (code.Instructions[i].Value?.GetType() == typeof(UndertaleInstruction.Reference<UndertaleVariable>))
                        Assert.True(Object.ReferenceEquals(((UndertaleInstruction.Reference<UndertaleVariable>)code.Instructions[i].Value).Target, ((UndertaleInstruction.Reference<UndertaleVariable>)reasm[i].Value).Target), errMsg);
                    else if (code.Instructions[i].Value?.GetType() == typeof(UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>))
                        Assert.True(Object.ReferenceEquals(((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)code.Instructions[i].Value).Resource, ((UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>)reasm[i].Value).Resource), errMsg);
                    else
                        Assert.True(code.Instructions[i].Value == reasm[i].Value, errMsg);
                }
            });
        }
    }

    public class UndertaleLoadingTest : GameLoadingTestBase
    {
        public UndertaleLoadingTest() : base(GamePaths.UNDERTALE_PATH, GamePaths.UNDERTALE_MD5)
        {
        }
    }

    public class UndertaleSwitchLoadingTest : GameLoadingTestBase
    {
        public UndertaleSwitchLoadingTest() : base(GamePaths.UNDERTALE_SWITCH_PATH, GamePaths.UNDERTALE_SWITCH_MD5)
        {
        }
    }

    public class DeltaruneLoadingTest : GameLoadingTestBase
    {
        public DeltaruneLoadingTest() : base(GamePaths.DELTARUNE_PATH, GamePaths.DELTARUNE_MD5)
        {
        }
    }

    public class EmptyGameTest
    {
        [Fact]
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
