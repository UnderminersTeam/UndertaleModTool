using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleInstruction;

namespace UndertaleModLib.Compiler
{
    public static partial class Compiler
    {
        public static class AssemblyWriter
        {
            private static int uuidCounter = 0;
            public class CodeWriter
            {
                public CompileContext compileContext;
                public List<UndertaleInstruction> instructions;
                public uint offset = 0;
                public Stack<DataType> typeStack = new Stack<DataType>();
                public Stack<LoopContext> loopContexts = new Stack<LoopContext>();
                public Stack<OtherContext> otherContexts = new Stack<OtherContext>();
                public List<string> ErrorMessages = new List<string>();
                public List<VariablePatch> varPatches = new List<VariablePatch>();
                public List<FunctionPatch> funcPatches = new List<FunctionPatch>();
                public List<StringPatch> stringPatches = new List<StringPatch>();

                public CodeWriter(CompileContext context)
                {
                    compileContext = context;
                    instructions = new List<UndertaleInstruction>(128);
                    offset = 0;
                }

                public UndertaleInstruction Emit(Opcode opcode)
                {
                    var res = new UndertaleInstruction()
                    {
                        Kind = opcode,
                        Address = offset
                    };
                    instructions.Add(res);
                    offset += res.CalculateInstructionSize();
                    return res;
                }

                public UndertaleInstruction Emit(Opcode opcode, DataType type1)
                {
                    var res = new UndertaleInstruction()
                    {
                        Kind = opcode,
                        Address = offset,
                        Type1 = type1
                    };
                    instructions.Add(res);
                    offset += res.CalculateInstructionSize();
                    return res;
                }

                public UndertaleInstruction Emit(Opcode opcode, DataType type1, DataType type2)
                {
                    var res = new UndertaleInstruction()
                    {
                        Kind = opcode,
                        Address = offset,
                        Type1 = type1,
                        Type2 = type2
                    };
                    instructions.Add(res);
                    offset += res.CalculateInstructionSize();
                    return res;
                }

                public UndertaleInstruction EmitRef(Opcode opcode, DataType type1)
                {
                    var res = new UndertaleInstruction()
                    {
                        Kind = opcode,
                        Address = offset,
                        Type1 = type1
                    };
                    instructions.Add(res);
                    offset += 2;
                    return res;
                }

                public UndertaleInstruction EmitRef(Opcode opcode, DataType type1, DataType type2)
                {
                    var res = new UndertaleInstruction()
                    {
                        Kind = opcode,
                        Address = offset,
                        Type1 = type1,
                        Type2 = type2
                    };
                    instructions.Add(res);
                    offset += 2;
                    return res;
                }

                public List<UndertaleInstruction> Finish()
                {
                    bool defineArguments = true;
                    if (compileContext.OriginalCode != null)
                    {
                        UndertaleCodeLocals locals = compileContext.Data?.CodeLocals.For(compileContext.OriginalCode);
                        if (locals != null)
                        {
                            // Update the code locals of the UndertaleCode
                            defineArguments = false;

                            // First, remove unnecessary locals
                            for (int i = 1; i < locals.Locals.Count; i++)
                            {
                                string localName = locals.Locals[i].Name.Content;
                                if (CompileContext.GMS2_3 != true)
                                    locals.Locals[i].Index = (uint)i;
                                if (!compileContext.LocalVars.ContainsKey(localName))
                                {
                                    locals.Locals.RemoveAt(i--);
                                    compileContext.OriginalCode.LocalsCount--;
                                }
                            }

                            // Now add in the ones we are actually using that don't already exist
                            bool hasLocal(string name)
                            {
                                foreach (var l in locals.Locals)
                                {
                                    if (name == l.Name.Content)
                                        return true;
                                }
                                return false;
                            }
                            compileContext.MainThreadDelegate.Invoke(() =>
                            {
                                var variables = compileContext.Data?.Variables;
                                foreach (var l in compileContext.LocalVars)
                                {
                                    string name = l.Key;
                                    if (!hasLocal(name))
                                    {
                                        if (variables != null && CompileContext.GMS2_3 == true)
                                        {
                                            UndertaleVariable def = variables.DefineLocal(compileContext.OriginalReferencedLocalVars, 0, name, compileContext.Data.Strings, compileContext.Data);
                                            if (def != null)
                                                compileContext.OriginalReferencedLocalVars.Add(def); // Add to the end, even if redundant (searches go from front to back anyway)
                                            locals.Locals.Add(new UndertaleCodeLocals.LocalVar() { Index = (uint)def.VarID, Name = compileContext.Data?.Strings?.MakeString(name) });
                                        }
                                        else
                                            locals.Locals.Add(new UndertaleCodeLocals.LocalVar() { Index = (uint)locals.Locals.Count, Name = compileContext.Data?.Strings?.MakeString(name) });
                                    }
                                }
                                compileContext.OriginalCode.LocalsCount = (uint)locals.Locals.Count;
                                if (compileContext.OriginalCode.LocalsCount > compileContext.Data.MaxLocalVarCount)
                                    compileContext.Data.MaxLocalVarCount = compileContext.OriginalCode.LocalsCount;
                            });
                        }
                    }

                    int localId = 0;
                    List<VariablePatch> localPatches = varPatches.FindAll(p => p.InstType == InstanceType.Local);
                    compileContext.MainThreadDelegate.Invoke(() =>
                    {
                        if (compileContext.ensureVariablesDefined)
                        {
                            var variables = compileContext.Data?.Variables;
                            if (variables != null)
                            {
                                foreach (KeyValuePair<string, string> v in compileContext.LocalVars)
                                {
                                    if (v.Key == "arguments")
                                    {
                                        if (!defineArguments)
                                        {
                                            localId++;
                                            continue;
                                        }
                                    }

                                    UndertaleVariable def = variables.DefineLocal(compileContext.OriginalReferencedLocalVars, localId++, v.Key, compileContext.Data.Strings, compileContext.Data);
                                    if (def != null)
                                    {
                                        foreach (var patch in localPatches.FindAll(p => p.Name == v.Key))
                                        {
                                            if (patch.Target.Kind == Opcode.Pop)
                                                patch.Target.Destination = new Reference<UndertaleVariable>(def, patch.VarType);
                                            else
                                                patch.Target.Value = new Reference<UndertaleVariable>(def, patch.VarType);
                                            
                                            if (patch.VarType == VariableType.Normal)
                                                patch.Target.TypeInst = InstanceType.Local;
                                            else if (CompileContext.GMS2_3)
                                                patch.InstType = InstanceType.Self;
                                        }
                                    }
                                }

                                foreach (var patch in varPatches)
                                {
                                    if (patch.InstType != InstanceType.Local)
                                    {
                                        var realInstType = patch.InstType;
                                        if (realInstType >= 0)
                                            realInstType = InstanceType.Self;
                                        else if (realInstType == InstanceType.Other)
                                            realInstType = InstanceType.Self;
                                        else if (realInstType == InstanceType.Arg)
                                            realInstType = InstanceType.Builtin;
                                        else if (realInstType == InstanceType.Builtin)
                                            realInstType = InstanceType.Self; // used with @@This@@
                                        else if (realInstType == InstanceType.Stacktop)
                                            realInstType = InstanceType.Self; // used with @@GetInstance@@

                                        // 2.3 variable fix
                                        // Definitely needs at least some change when ++/-- support is added,
                                        // since that does use instance type global
                                        if (CompileContext.GMS2_3 &&
                                            patch.VarType == VariableType.Array &&
                                            realInstType == InstanceType.Global)
                                            realInstType = InstanceType.Self;

                                        UndertaleVariable def = variables.EnsureDefined(patch.Name, realInstType,
                                                                 compileContext.BuiltInList.GlobalArray.ContainsKey(patch.Name) ||
                                                                 compileContext.BuiltInList.GlobalNotArray.ContainsKey(patch.Name) ||
                                                                 compileContext.BuiltInList.Instance.ContainsKey(patch.Name) ||
                                                                 compileContext.BuiltInList.InstanceLimitedEvent.ContainsKey(patch.Name), 
                                                                 compileContext.Data.Strings, compileContext.Data);
                                        if (patch.Target.Kind == Opcode.Pop)
                                            patch.Target.Destination = new Reference<UndertaleVariable>(def, patch.VarType);
                                        else
                                            patch.Target.Value = new Reference<UndertaleVariable>(def, patch.VarType);
                                        if (patch.VarType == VariableType.Normal)
                                            patch.Target.TypeInst = patch.InstType;
                                    }
                                }
                            }
                        }

                        // GMS2.3 totally changed how functions work
                        // The FUNC chunk contains references to builtin functions, and anonymous function definitions called gml_Script_...
                        // The anonymous functions are bound to names by code in Data.GlobalInit
                        // so to get an actual mapping from names to functions, you have to decompile all GlobalInit scripts...
                        Decompiler.Decompiler.BuildSubFunctionCache(compileContext.Data);
                        foreach (var patch in funcPatches)
                        {
                            if (patch.isNewFunc)
                            {
                                UndertaleString childName = new("gml_Script_" + patch.Name);
                                int childNameIndex = compileContext.Data.Strings.Count;
                                compileContext.Data.Strings.Add(childName);

                                UndertaleCode childEntry = new()
                                {
                                    Name = childName,
                                    Length = compileContext.OriginalCode.Length, // todo: get a more certainly up-to-date length
                                    ParentEntry = compileContext.OriginalCode,
                                    Offset = patch.Offset,
                                    ArgumentsCount = (ushort)patch.ArgCount,
                                    LocalsCount = compileContext.OriginalCode.LocalsCount // todo: use just the locals for the individual script
                                };
                                compileContext.OriginalCode.ChildEntries.Add(childEntry);
                                int childEntryIndex = compileContext.Data.Code.IndexOf(compileContext.OriginalCode) + compileContext.OriginalCode.ChildEntries.Count;
                                compileContext.Data.Code.Insert(childEntryIndex, childEntry);

                                UndertaleScript childScript = new()
                                {
                                    Name = childName,
                                    Code = childEntry,
                                    IsConstructor = patch.isConstructor
                                };
                                compileContext.Data.Scripts.Add(childScript);

                                UndertaleFunction childFunction = new()
                                {
                                    Name = childName,
                                    NameStringID = childNameIndex,
                                    Autogenerated = true
                                };
                                
                                compileContext.Data.Functions.Add(childFunction);

                                compileContext.Data.KnownSubFunctions.Add(patch.Name, childFunction);
                                
                                continue;
                            }

                            UndertaleFunction def;
                            if (patch.ArgCount >= 0)
                            {
                                patch.Target.ArgumentsCount = (ushort)patch.ArgCount;
                                def = compileContext.Data.Functions.ByName(patch.Name);
                                if (CompileContext.GMS2_3)
                                {
                                    if (def != null && def.Autogenerated)
                                        def = null;
                                    def ??= compileContext.Data.KnownSubFunctions.GetValueOrDefault(patch.Name);
                                }

                                if (compileContext.ensureFunctionsDefined)
                                    def ??= compileContext.Data.Functions.EnsureDefined(patch.Name, compileContext.Data.Strings, true);

                                if (def != null)
                                {
                                    patch.Target.Function = new Reference<UndertaleFunction>(def);
                                }
                                else
                                {
                                    throw new Exception("Unknown function: " + patch.Name);
                                }
                            }
                            else
                            {
                                def = compileContext.Data.Functions.ByName(patch.Name);
                                // This code is only reachable using a 2.3 function definition. ("push.i gml_Script_scr_stuff")
                                def ??= compileContext.Data.KnownSubFunctions.GetValueOrDefault(patch.Name);
                                if (compileContext.ensureFunctionsDefined)
                                    def ??= compileContext.Data.Functions.EnsureDefined(patch.Name, compileContext.Data.Strings, true);

                                if (def != null)
                                {
                                    patch.Target.Value = new Reference<UndertaleFunction>(def);
                                }
                                else
                                {
                                    throw new Exception("Unknown function: " + patch.Name);
                                }
                            }
                        }

                        if (stringPatches.Count >= 512)
                        {
                            // Kick in optimization by mapping all of them to indices
                            Dictionary<string, int> stringMap = new Dictionary<string, int>(compileContext.Data.Strings.Count);
                            int i = 0;
                            foreach (var s in compileContext.Data.Strings)
                                stringMap[s.Content] = i++;
                            foreach (var patch in stringPatches)
                            {
                                if (stringMap.TryGetValue(patch.Content, out int ind))
                                {
                                    patch.Target.Value = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>(
                                                                        compileContext.Data.Strings[ind], ind);
                                } 
                                else
                                {
                                    UndertaleString newString = new UndertaleString(patch.Content);
                                    patch.Target.Value = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>(
                                                                        newString, compileContext.Data.Strings.Count);
                                    compileContext.Data.Strings.Add(newString);
                                }
                            }
                        }
                        else
                        {
                            foreach (var patch in stringPatches)
                            {
                                int ind;
                                UndertaleString str = compileContext.Data.Strings.MakeString(patch.Content, out ind);
                                var def = new UndertaleResourceById<UndertaleString, UndertaleChunkSTRG>(str, ind);
                                patch.Target.Value = def;
                            }
                        }
                    });

                    return instructions;
                }
            }

            public class VariablePatch
            {
                public UndertaleInstruction Target;
                public string Name;
                public VariableType VarType;
                public InstanceType InstType;
            }

            public class FunctionPatch
            {
                public UndertaleInstruction Target;
                public string Name;
                public int ArgCount;
                public uint Offset;
                public bool isNewFunc = false;
                public bool isConstructor = false;
            }

            public class StringPatch
            {
                public UndertaleInstruction Target;
                public string Content;
            }

            public class Patch
            {
                public List<UndertaleInstruction> Patches;
                public UndertaleInstruction Target;
                public int TargetIndex = -1;

                public static Patch Start()
                {
                    return new Patch()
                    {
                        Patches = new List<UndertaleInstruction>()
                    };
                }

                public static Patch StartHere(CodeWriter cw)
                {
                    return new Patch()
                    {
                        TargetIndex = cw.instructions.Count,
                        Patches = new List<UndertaleInstruction>()
                    };
                }

                public static Patch Start(UndertaleInstruction target)
                {
                    return new Patch()
                    {
                        Target = target
                    };
                }

                public void Finish(CodeWriter cw)
                {
                    if (TargetIndex != -1)
                    {
                        var target = cw.instructions[TargetIndex];
                        foreach (UndertaleInstruction i in Patches)
                        {
                            i.JumpOffset = (int)target.Address - (int)i.Address;
                        }
                    }
                    else if (Target == null)
                    {
                        foreach (UndertaleInstruction i in Patches)
                        {
                            i.JumpOffset = (int)cw.offset - (int)i.Address;
                        }
                    }
                }

                public void Add(UndertaleInstruction instr)
                {
                    if (TargetIndex != -1 || Target == null)
                    {
                        Patches.Add(instr);
                    }
                    else
                    {
                        instr.JumpOffset = (int)Target.Address - (int)instr.Address;
                    }
                }
            }

            public class OtherContext
            {
                public enum ContextKind
                {
                    Switch,
                    With
                }

                public ContextKind Kind;
                public Patch Break;
                public Patch Continue;
                public DataType TypeToPop; // switch statements
                public bool BreakUsed = false;
                public bool ContinueUsed = false;

                public OtherContext(Patch @break, Patch @continue, DataType typeToPop)
                {
                    Kind = ContextKind.Switch;
                    Break = @break;
                    Continue = @continue;
                    TypeToPop = typeToPop;
                }

                public OtherContext(Patch @break, Patch @continue)
                {
                    Kind = ContextKind.With;
                    Break = @break;
                    Continue = @continue;
                }

                public Patch UseBreak()
                {
                    BreakUsed = true;
                    return Break;
                }

                public Patch UseContinue()
                {
                    ContinueUsed = true;
                    return Continue;
                }
            }

            public struct LoopContext
            {
                public Patch Break;
                public Patch Continue;
                public bool BreakUsed;
                public bool ContinueUsed;

                public LoopContext(Patch @break, Patch @continue)
                {
                    Break = @break;
                    Continue = @continue;
                    BreakUsed = false;
                    ContinueUsed = false;
                }

                public Patch UseBreak()
                {
                    BreakUsed = true;
                    return Break;
                }

                public Patch UseContinue()
                {
                    ContinueUsed = true;
                    return Continue;
                }
            }

            private static Patch HelpUseBreak(ref Stack<LoopContext> s)
            {
                LoopContext c = s.Pop();
                Patch res = c.UseBreak();
                s.Push(c);
                return res;
            }
            private static Patch HelpUseContinue(ref Stack<LoopContext> s)
            {
                LoopContext c = s.Pop();
                Patch res = c.UseContinue();
                s.Push(c);
                return res;
            }
            private static Patch HelpUseBreak(ref Stack<OtherContext> s)
            {
                OtherContext c = s.Pop();
                Patch res = c.UseBreak();
                s.Push(c);
                return res;
            }
            private static Patch HelpUseContinue(ref Stack<OtherContext> s)
            {
                OtherContext c = s.Pop();
                Patch res = c.UseContinue();
                s.Push(c);
                return res;
            }

            public static CodeWriter AssembleStatement(CompileContext compileContext, Parser.Statement s)
            {
                uuidCounter = 0;
                CodeWriter cw = new CodeWriter(compileContext);
                AssembleStatement(cw, s);
                return cw;
            }

            private static void AssembleStatement(CodeWriter cw, Parser.Statement s, int remaining = -1)
            {
                switch (s.Kind)
                {
                    case Parser.Statement.StatementKind.Discard:
                        break;
                    case Parser.Statement.StatementKind.Block:
                        {
                            for (int i = 0; i < s.Children.Count; i++)
                            {
                                AssembleStatement(cw, s.Children[i], s.Children.Count - i);
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.Assign:
                        {
                            if (s.Children.Count != 3)
                            {
                                AssemblyWriterError(cw, "Malformed assignment statement.", s.Token);
                                break;
                            }

                            switch (s.Children[1].Token.Kind)
                            {
                                case Lexer.Token.TokenKind.Assign:
                                    AssembleExpression(cw, s.Children[2], s.Children[0]); // value
                                    AssembleStoreVariable(cw, s.Children[0], cw.typeStack.Pop()); // variable reference
                                    break;
                                case Lexer.Token.TokenKind.AssignPlus:
                                    AssembleOperationAssign(cw, s, Opcode.Add);
                                    break;
                                case Lexer.Token.TokenKind.AssignMinus:
                                    AssembleOperationAssign(cw, s, Opcode.Sub);
                                    break;
                                case Lexer.Token.TokenKind.AssignTimes:
                                    AssembleOperationAssign(cw, s, Opcode.Mul);
                                    break;
                                case Lexer.Token.TokenKind.AssignDivide:
                                    AssembleOperationAssign(cw, s, Opcode.Div);
                                    break;
                                case Lexer.Token.TokenKind.AssignAnd:
                                    AssembleOperationAssign(cw, s, Opcode.And, true);
                                    break;
                                case Lexer.Token.TokenKind.AssignOr:
                                    AssembleOperationAssign(cw, s, Opcode.Or, true);
                                    break;
                                case Lexer.Token.TokenKind.AssignXor:
                                    AssembleOperationAssign(cw, s, Opcode.Xor, true);
                                    break;
                                case Lexer.Token.TokenKind.AssignMod:
                                    AssembleOperationAssign(cw, s, Opcode.Mod);
                                    break;
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.Pre:
                        AssemblePostOrPre(cw, s, false, false);
                        break;
                    case Parser.Statement.StatementKind.Post:
                        AssemblePostOrPre(cw, s, true, false);
                        break;
                    case Parser.Statement.StatementKind.TempVarDeclare:
                        foreach (Parser.Statement subDeclare in s.Children)
                        {
                            if (subDeclare.Children.Count != 0)
                            {
                                // Assignment
                                AssembleStatement(cw, subDeclare.Children[0]);
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.GlobalVarDeclare:
                        // Nothing?
                        break;
                    case Parser.Statement.StatementKind.If:
                        {
                            AssembleExpression(cw, s.Children[0]); // condition
                            DataType type = cw.typeStack.Pop();
                            if (type != DataType.Boolean)
                            {
                                cw.Emit(Opcode.Conv, type, DataType.Boolean);
                            }
                            var conditionPatch = Patch.Start();
                            conditionPatch.Add(cw.Emit(Opcode.Bf));

                            AssembleStatement(cw, s.Children[1]); // body
                            if (s.Children.Count == 3)
                            {
                                var elsePatch = Patch.Start();
                                elsePatch.Add(cw.Emit(Opcode.B));
                                conditionPatch.Finish(cw);
                                AssembleStatement(cw, s.Children[2]); // else statement
                                elsePatch.Finish(cw);
                            } 
                            else
                            {
                                conditionPatch.Finish(cw);
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.ForLoop:
                        {
                            if (s.Children.Count != 4)
                            {
                                AssemblyWriterError(cw, "Malformed for loop.", s.Token);
                                break;
                            }

                            AssembleStatement(cw, s.Children[0]); // initial set

                            var conditionPatch = Patch.StartHere(cw);
                            AssembleExpression(cw, s.Children[1]); // condition
                            DataType type = cw.typeStack.Pop();
                            if (type != DataType.Boolean)
                            {
                                cw.Emit(Opcode.Conv, type, DataType.Boolean);
                            }
                            var endLoopPatch = Patch.Start();
                            endLoopPatch.Add(cw.Emit(Opcode.Bf));

                            var continuePatch = Patch.Start();
                            cw.loopContexts.Push(new LoopContext(endLoopPatch, continuePatch));
                            AssembleStatement(cw, s.Children[3]); // body
                            cw.loopContexts.Pop();
                            continuePatch.Finish(cw);
                            AssembleStatement(cw, s.Children[2]); // code that runs each iteration, usually "i++" or something
                            conditionPatch.Add(cw.Emit(Opcode.B));
                            conditionPatch.Finish(cw);
                            endLoopPatch.Finish(cw);
                        }
                        break;
                    case Parser.Statement.StatementKind.WhileLoop:
                        {
                            if (s.Children.Count != 2)
                            {
                                AssemblyWriterError(cw, "Malformed while loop.", s.Token);
                                break;
                            }

                            Patch conditionPatch = Patch.StartHere(cw);
                            AssembleExpression(cw, s.Children[0]); // condition
                            DataType type = cw.typeStack.Pop();
                            if (type != DataType.Boolean)
                            {
                                cw.Emit(Opcode.Conv, type, DataType.Boolean);
                            }
                            Patch endLoopPatch = Patch.Start();
                            endLoopPatch.Add(cw.Emit(Opcode.Bf));
                            cw.loopContexts.Push(new LoopContext(endLoopPatch, conditionPatch));
                            AssembleStatement(cw, s.Children[1]); // body
                            cw.loopContexts.Pop();
                            conditionPatch.Add(cw.Emit(Opcode.B));
                            conditionPatch.Finish(cw);
                            endLoopPatch.Finish(cw);
                        }
                        break;
                    case Parser.Statement.StatementKind.RepeatLoop:
                        {
                            if (s.Children.Count != 2)
                            {
                                AssemblyWriterError(cw, "Malformed repeat loop.", s.Token);
                                break;
                            }
                            
                            // This loop keeps its counter on the stack

                            AssembleExpression(cw, s.Children[0]); // number of times to repeat
                            DataType type = cw.typeStack.Pop();
                            if (type != DataType.Int32)
                            {
                                cw.Emit(Opcode.Conv, type, DataType.Int32);
                            }

                            Patch endPatch = Patch.Start();
                            Patch repeatPatch = Patch.Start();

                            cw.Emit(Opcode.Dup, DataType.Int32).Extra = 0;
                            cw.Emit(Opcode.Push, DataType.Int32).Value = 0; // This is REALLY weird, but this happens, always- normally it's pushi.e
                            cw.Emit(Opcode.Cmp, DataType.Int32, DataType.Int32)
                                        .ComparisonKind = ComparisonType.LTE;
                            endPatch.Add(cw.Emit(Opcode.Bt));

                            cw.loopContexts.Push(new LoopContext(endPatch, repeatPatch));

                            Patch startPatch = Patch.StartHere(cw);
                            AssembleStatement(cw, s.Children[1]); // body

                            repeatPatch.Finish(cw);
                            cw.Emit(Opcode.Push, DataType.Int32).Value = 1; // This is also weird- normally it's pushi.e
                            cw.Emit(Opcode.Sub, DataType.Int32, DataType.Int32);
                            cw.Emit(Opcode.Dup, DataType.Int32).Extra = 0;
                            cw.Emit(Opcode.Conv, DataType.Int32, DataType.Boolean);
                            startPatch.Add(cw.Emit(Opcode.Bt));
                            startPatch.Finish(cw);

                            endPatch.Finish(cw);
                            cw.Emit(Opcode.Popz, DataType.Int32); // Cleans up the stack of the decrementing value, which at this point should be <= 0

                            cw.loopContexts.Pop();
                        }
                        break;
                    case Parser.Statement.StatementKind.DoUntilLoop:
                        {
                            if (s.Children.Count != 2)
                            {
                                AssemblyWriterError(cw, "Malformed do..until loop.", s.Token);
                                break;
                            }

                            Patch endPatch = Patch.Start();
                            Patch repeatPatch = Patch.Start();

                            cw.loopContexts.Push(new LoopContext(endPatch, repeatPatch));

                            Patch startPatch = Patch.StartHere(cw);
                            AssembleStatement(cw, s.Children[0]); // body

                            repeatPatch.Finish(cw);
                            AssembleExpression(cw, s.Children[1]); // condition
                            DataType type = cw.typeStack.Pop();
                            if (type != DataType.Boolean)
                            {
                                cw.Emit(Opcode.Conv, type, DataType.Boolean);
                            }
                            startPatch.Add(cw.Emit(Opcode.Bf));
                            startPatch.Finish(cw);

                            endPatch.Finish(cw);
                            cw.loopContexts.Pop();
                        }
                        break;
                    case Parser.Statement.StatementKind.Switch:
                        {
                            Patch endPatch = Patch.Start();
                            bool isEnclosingLoop = (cw.loopContexts.Count > 0);
                            Patch continueEndPatch = null;
                            LoopContext enclosingContext = default(LoopContext);
                            if (isEnclosingLoop)
                            {
                                continueEndPatch = Patch.Start();
                                enclosingContext = cw.loopContexts.Peek();
                            }

                            // Value to compare against
                            AssembleExpression(cw, s.Children[0]);
                            var compareType = cw.typeStack.Pop();

                            cw.otherContexts.Push(new OtherContext(endPatch, continueEndPatch, compareType));

                            List<Tuple<Parser.Statement, Patch, int /* index in s.Children */>> cases = new List<Tuple<Parser.Statement, Patch, int>>();
                            Patch defaultPatch = null;
                            bool isReadyForOtherStatements = false;

                            for (int i = 1; i < s.Children.Count; i++)
                            {
                                var s2 = s.Children[i];
                                switch (s2.Kind)
                                {
                                    case Parser.Statement.StatementKind.SwitchCase:
                                        {
                                            cw.Emit(Opcode.Dup, compareType).Extra = 0;
                                            AssembleExpression(cw, s2.Children[0]);
                                            cw.Emit(Opcode.Cmp, cw.typeStack.Pop(), compareType).ComparisonKind = ComparisonType.EQ;

                                            Patch patch = Patch.Start();
                                            patch.Add(cw.Emit(Opcode.Bt));

                                            cases.Add(new Tuple<Parser.Statement, Patch, int>(s2, patch, i));

                                            isReadyForOtherStatements = true;
                                        }
                                        break;
                                    case Parser.Statement.StatementKind.SwitchDefault:
                                        {
                                            defaultPatch = Patch.Start();
                                            cases.Add(new Tuple<Parser.Statement, Patch, int>(s2, defaultPatch, i));

                                            isReadyForOtherStatements = true;
                                        }
                                        break;
                                    default:
                                        if (!isReadyForOtherStatements)
                                        {
                                            AssemblyWriterError(cw, "Statements in switch statement must be after case or default.", s2.Token);
                                        }
                                        break;
                                }
                            }

                            if (defaultPatch != null)
                            {
                                defaultPatch.Add(cw.Emit(Opcode.B));
                            }
                            endPatch.Add(cw.Emit(Opcode.B)); // Even if the default exists, this happens...

                            // Search for duplicates
                            List<Parser.Statement> alreadyUsed = new List<Parser.Statement>();
                            for (int i = 0; i < cases.Count; i++)
                            {
                                var c = cases[i].Item1;
                                bool shouldAdd = true;
                                for (int j = 0; j < alreadyUsed.Count; j++)
                                {
                                    if (c == alreadyUsed[j])
                                    {
                                        shouldAdd = false;
                                        AssemblyWriterError(cw, "Found duplicate case statement.", c.Token);
                                        AssemblyWriterError(cw, "First occurrence:", alreadyUsed[j].Token);
                                    }
                                }
                                if (shouldAdd)
                                    alreadyUsed.Add(c);
                            }
                            
                            // Write each case statement's code!
                            for (int i = 0; i < cases.Count; i++)
                            {
                                // Figure out where to start/end
                                var c = cases[i];
                                var next = (i + 1 < cases.Count) ? cases[i + 1] : null;
                                int endIndex = (next?.Item3 ?? s.Children.Count) - 1;

                                c.Item2.Finish(cw);

                                for (int j = c.Item3 + 1; j <= endIndex; j++)
                                {
                                    AssembleStatement(cw, s.Children[j]);
                                }
                            }

                            // Write part at end in case a continue statement is used
                            OtherContext context = cw.otherContexts.Pop();
                            if (isEnclosingLoop && context.ContinueUsed)
                            {
                                endPatch.Add(cw.Emit(Opcode.B));

                                continueEndPatch.Finish(cw);
                                cw.Emit(Opcode.Popz, compareType);
                                enclosingContext.UseContinue().Add(cw.Emit(Opcode.B));
                            }

                            endPatch.Finish(cw);
                            cw.Emit(Opcode.Popz, compareType);
                        }
                        break;
                    case Parser.Statement.StatementKind.With:
                        {
                            Patch endPatch = Patch.Start();
                            Patch popEnvPatch = Patch.Start();
                            // Hacky override for @@Other@@ and @@This@@ usage- will likely expand to whatever other cases it turns out the compiler uses.
                            if (CompileContext.GMS2_3 &&
                               s.Children[0].Kind == Parser.Statement.StatementKind.ExprConstant &&
                               ((InstanceType)s.Children[0].Constant.valueNumber).In(InstanceType.Other, InstanceType.Self))
                            {
                                cw.funcPatches.Add(new FunctionPatch()
                                {
                                    Target = cw.EmitRef(Opcode.Call, DataType.Int32),
                                    Name = (InstanceType)s.Children[0].Constant.valueNumber == InstanceType.Other ? "@@Other@@" : "@@This@@",
                                    ArgCount = 0
                                });
                                cw.typeStack.Push(DataType.Variable);
                            }
                            else
                                AssembleExpression(cw, s.Children[0]); // new object/context
                            var type = cw.typeStack.Pop();
                            if (type != DataType.Int32)
                            {
                                if (CompileContext.GMS2_3 && type == DataType.Variable)
                                    cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)-9; // stacktop conversion
                                else
                                    cw.Emit(Opcode.Conv, type, DataType.Int32);
                            }

                            cw.otherContexts.Push(new OtherContext(endPatch, popEnvPatch));

                            popEnvPatch.Add(cw.Emit(Opcode.PushEnv));
                            Patch startPatch = Patch.StartHere(cw);

                            AssembleStatement(cw, s.Children[1]);

                            popEnvPatch.Finish(cw);
                            startPatch.Add(cw.Emit(Opcode.PopEnv));
                            startPatch.Finish(cw);

                            if (cw.otherContexts.Pop().BreakUsed)
                            {
                                Patch cleanUpEndPatch = Patch.Start();
                                cleanUpEndPatch.Add(cw.Emit(Opcode.B));

                                endPatch.Finish(cw);
                                var dropPopenv = cw.Emit(Opcode.PopEnv);
                                dropPopenv.JumpOffsetPopenvExitMagic = true;
                                if (cw.compileContext.Data?.GeneralInfo?.BytecodeVersion <= 14)
                                    dropPopenv.JumpOffset = -1048576; // magic for older versions

                                cleanUpEndPatch.Finish(cw);
                            }
                            else
                            {
                                endPatch.Finish(cw);
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.Continue:
                        if (cw.loopContexts.Count == 0 && (cw.otherContexts.Count == 0 || cw.otherContexts.Peek().Continue == null))
                        {
                            AssemblyWriterError(cw, "Continue statement placed outside of any loops.", s.Token);
                        }
                        else
                        {
                            if (cw.otherContexts.Count > 0 && cw.otherContexts.Peek().Continue != null)
                                HelpUseContinue(ref cw.otherContexts).Add(cw.Emit(Opcode.B));
                            else
                                HelpUseContinue(ref cw.loopContexts).Add(cw.Emit(Opcode.B));
                        }
                        break;
                    case Parser.Statement.StatementKind.Break:
                        if (cw.loopContexts.Count == 0 && cw.otherContexts.Count == 0)
                        {
                            AssemblyWriterError(cw, "Break statement placed outside of any loops.", s.Token);
                        }
                        else
                        {
                            if (cw.otherContexts.Count > 0 && cw.otherContexts.Peek().Break != null)
                                HelpUseBreak(ref cw.otherContexts).Add(cw.Emit(Opcode.B));
                            else
                                HelpUseBreak(ref cw.loopContexts).Add(cw.Emit(Opcode.B));
                        }
                        break;
                    case Parser.Statement.StatementKind.FunctionCall:
                        {
                            AssembleFunctionCall(cw, s);

                            // Since this is a statement, nothing's going to happen with
                            // the return value. So it must be discarded so it doesn't
                            // make a mess on the stack.
                            cw.Emit(Opcode.Popz, cw.typeStack.Pop());
                        }
                        break;
                    case Parser.Statement.StatementKind.Return:
                        if (s.Children.Count == 1)
                        {
                            // Returns a value to caller
                            AssembleExpression(cw, s.Children[0]);
                            var type = cw.typeStack.Pop();
                            if (type != DataType.Variable)
                            {
                                cw.Emit(Opcode.Conv, type, DataType.Variable);
                            }

                            // Clean up contexts if necessary
                            bool useLocalVar = (cw.otherContexts.Count != 0);
                            if (useLocalVar)
                            {
                                // Put the return value into a local variable (GM does this as well)
                                // so that it doesn't get cleared out of the stack??
                                // See here: https://github.com/krzys-h/UndertaleModTool/issues/164
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Pop, DataType.Variable, DataType.Variable),
                                    InstType = InstanceType.Local,
                                    Name = "$$$$temp$$$$",
                                    VarType = VariableType.Normal
                                });
                                cw.compileContext.LocalVars["$$$$temp$$$$"] = "$$$$temp$$$$";
                            }
                            foreach (OtherContext oc in cw.otherContexts)
                            {
                                if (oc.Kind == OtherContext.ContextKind.Switch)
                                {
                                    cw.Emit(Opcode.Popz, oc.TypeToPop);
                                } 
                                else
                                {
                                    // With
                                    var dropPopenv = cw.Emit(Opcode.PopEnv);
                                    dropPopenv.JumpOffsetPopenvExitMagic = true;
                                    if (cw.compileContext.Data?.GeneralInfo?.BytecodeVersion <= 14)
                                        dropPopenv.JumpOffset = -1048576; // magic for older versions
                                }
                            }
                            if (useLocalVar)
                            {
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                                    InstType = InstanceType.Local,
                                    Name = "$$$$temp$$$$",
                                    VarType = VariableType.Normal
                                });
                            }
                            cw.Emit(Opcode.Ret, DataType.Variable);
                        } 
                        else
                        {
                            // Returns nothing, basically the same as exit
                            AssembleExit(cw);
                        }
                        break;
                    case Parser.Statement.StatementKind.Exit:
                        AssembleExit(cw);
                        break;
                    default:
                        AssemblyWriterError(cw, "Expected a statement, none found", s.Token);
                        break;
                }
            }

            private static void AssembleOperationAssign(CodeWriter cw, Parser.Statement s, Opcode op, bool needsToBeIntOrLong = false)
            {
                // Variable to operate on, duplicated second-to-last variable if necessary
                bool isSingle;
                AssembleVariablePush(cw, s.Children[0], out isSingle, true, false, true /* seems like a GMAC bug */);
                if (cw.typeStack.Count == 0)
                {
                    AssemblyWriterError(cw, "Type stack empty (invalid syntax)", null);
                }
                else
                    cw.typeStack.Pop();

                // Right
                AssembleExpression(cw, s.Children[2]);
                var type = cw.typeStack.Pop();
                if ((needsToBeIntOrLong && type != DataType.Int32 && type != DataType.Int64) 
                    || (!needsToBeIntOrLong && type == DataType.Boolean))
                {
                    cw.Emit(Opcode.Conv, type, DataType.Int32);
                    type = DataType.Int32;
                }

                // Actual operation
                cw.Emit(op, type, DataType.Variable);
                
                // Store back, using duplicate reference if necessary
                AssembleStoreVariable(cw, s.Children[0], DataType.Variable, !isSingle, true);
            }

            private static void AssemblePostOrPre(CodeWriter cw, Parser.Statement s, bool isPost, bool isExpression)
            { 
                // Variable to operate on, duplicated second-to-last variable if necessary
                bool isSingle;
                bool isArray;
                AssembleVariablePush(cw, s.Children[0], out isSingle, out isArray, true, true, true /* seems like a GMAC bug */);
                if (cw.typeStack.Count == 0)
                {
                    AssemblyWriterError(cw, "Type stack empty (invalid syntax)", null);
                }
                else
                    cw.typeStack.Pop();

                // Do the operation... somewhat strangely for expressions...
                if (isExpression && isPost)
                    AssemblePostPreStackOperation(cw, isSingle, isArray);
                cw.Emit(Opcode.Push, DataType.Int16).Value = (short)1;
                cw.Emit((s.Token.Kind == Lexer.Token.TokenKind.Increment) ? Opcode.Add : Opcode.Sub, DataType.Int32, DataType.Variable);
                if (isExpression && !isPost)
                    AssemblePostPreStackOperation(cw, isSingle, isArray);

                // Store back, using duplicate reference if necessary
                AssembleStoreVariable(cw, s.Children[0], DataType.Variable, !isSingle);
            }

            private static void AssemblePostPreStackOperation(CodeWriter cw, bool isSingle, bool isArray)
            {
                cw.Emit(Opcode.Dup, DataType.Variable).Extra = 0;
                cw.typeStack.Push(DataType.Variable);
                if (!isSingle)
                    cw.Emit(Opcode.Pop, DataType.Int16, DataType.Variable).SwapExtra = (ushort)(isArray ? 6 : 5);
            }

            private static void AssembleExit(CodeWriter cw)
            {
                // First switch statements
                foreach (OtherContext oc in cw.otherContexts)
                {
                    if (oc.Kind == OtherContext.ContextKind.Switch)
                    {
                        cw.Emit(Opcode.Popz, oc.TypeToPop);
                    }
                }

                // Then with statements
                foreach (OtherContext oc in cw.otherContexts)
                {
                    if (oc.Kind == OtherContext.ContextKind.With)
                    {
                        var dropPopenv = cw.Emit(Opcode.PopEnv);
                        dropPopenv.JumpOffsetPopenvExitMagic = true;
                        if (cw.compileContext.Data?.GeneralInfo?.BytecodeVersion <= 14)
                            dropPopenv.JumpOffset = -1048576; // magic for older versions
                    }
                }

                cw.Emit(Opcode.Exit, DataType.Int32);
            }

            private static void AssembleFunctionCall(CodeWriter cw, Parser.Statement fc)
            {
                // Needs to push args onto stack backwards
                for (int i = fc.Children.Count - 1; i >= 0; i--)
                {
                    AssembleExpression(cw, fc.Children[i]);

                    // Convert to Variable data type
                    var typeToConvertFrom = cw.typeStack.Pop();
                    if (typeToConvertFrom != DataType.Variable)
                    {
                        cw.Emit(Opcode.Conv, typeToConvertFrom, DataType.Variable);
                    }
                }

                cw.funcPatches.Add(new FunctionPatch()
                {
                    Target = cw.EmitRef(Opcode.Call, DataType.Int32),
                    Name = fc.Text,
                    ArgCount = fc.Children.Count
                });
                cw.typeStack.Push(DataType.Variable);
            }

            private static void AssembleStructDef(CodeWriter cw, Parser.Statement str)
            {
                /* Example struct definition
                ----- AssembleStatement:
                push.i gml_Script____struct___utmt_test
                conv.i.v
                call.i @@NullObject@@(argc=0)
                call.i method(argc=2)
                dup.v 0
                pushi.e -16
                pop.v.v [stacktop]static.___struct___utmt_test
                ----- new FunctionPatch (this function):
                call.i @@NewGMLObject@@(argc=1)
                ----- Variable set:
                pop.v.v self.a
                */

                List<Parser.Statement> leaked = str.Children[0].Children;
                // Just push these leaked variables onto the stack
                // We need to do this in reverse since function arguments 
                // are parsed in reverse stack order
                for (int i = leaked.Count - 1; i >= 0; i--) {
                    Parser.Statement statement = leaked[i];
                    AssembleExpression(cw, statement);
                }

                AssembleStatement(cw, str.Children[1]);

                cw.funcPatches.Add(new FunctionPatch()
                {
                    Target = cw.EmitRef(Opcode.Call, DataType.Int32),
                    Name = "@@NewGMLObject@@",
                    Offset = cw.offset * 4,
                    ArgCount = leaked.Count + 1
                });
                cw.typeStack.Push(DataType.Variable);
            }

            private static void AssembleExpression(CodeWriter cw, Parser.Statement e, Parser.Statement funcDefName = null)
            {
                switch (e.Kind)
                {
                    case Parser.Statement.StatementKind.ExprConstant:
                        {
                            Parser.ExpressionConstant value = e.Constant;
                            if (value == null)
                            {
                                AssemblyWriterError(cw, "Invalid constant", e.Token);
                                cw.typeStack.Push(DataType.Variable);
                                break;
                            }

                            switch (value.kind)
                            {
                                case Parser.ExpressionConstant.Kind.Number:
                                    if (value.isBool)
                                    {
                                        cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)value.valueNumber;
                                        cw.typeStack.Push(DataType.Boolean);
                                    }
                                    else if ((double)((long)value.valueNumber) == value.valueNumber)
                                    {
                                        // It's an integer
                                        long valueAsInt = (long)value.valueNumber;
                                        if (valueAsInt <= Int32.MaxValue && valueAsInt >= Int32.MinValue)
                                        {
                                            if (valueAsInt <= Int16.MaxValue && valueAsInt >= Int16.MinValue)
                                            {
                                                // Int16
                                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)valueAsInt;
                                                cw.typeStack.Push(DataType.Int32); // apparently?
                                            }
                                            else
                                            {
                                                // Int32
                                                cw.Emit(Opcode.Push, DataType.Int32).Value = (int)valueAsInt;
                                                cw.typeStack.Push(DataType.Int32);
                                            }
                                        }
                                        else
                                        {
                                            // Int64
                                            cw.Emit(Opcode.Push, DataType.Int64).Value = valueAsInt;
                                            cw.typeStack.Push(DataType.Int64);
                                        }
                                    }
                                    else
                                    {
                                        // It's a double
                                        cw.Emit(Opcode.Push, DataType.Double).Value = value.valueNumber;
                                        cw.typeStack.Push(DataType.Double);
                                    }
                                    break;
                                case Parser.ExpressionConstant.Kind.String:
                                    cw.stringPatches.Add(new StringPatch()
                                    {
                                        Target = cw.EmitRef(Opcode.Push, DataType.String),
                                        Content = value.valueString
                                    });
                                    cw.typeStack.Push(DataType.String);
                                    break;
                                case Parser.ExpressionConstant.Kind.Int64:
                                    cw.Emit(Opcode.Push, DataType.Int64).Value = value.valueInt64;
                                    cw.typeStack.Push(DataType.Int64);
                                    break;
                                case Parser.ExpressionConstant.Kind.Reference:
                                    {
                                        var instr = cw.Emit(Opcode.Break, DataType.Int32);
                                        instr.Value = (short)-11; // pushref
                                        instr.IntArgument = (int)value.valueNumber;
                                        cw.typeStack.Push(DataType.Variable);
                                        break;
                                    }
                                default:
                                    cw.typeStack.Push(DataType.Variable);
                                    AssemblyWriterError(cw, "Invalid constant type.", e.Token);
                                    break;
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.ExprFunctionCall:
                        AssembleFunctionCall(cw, e); // the return value in this case must be used
                        break;
                    case Parser.Statement.StatementKind.ExprStruct:
                        AssembleStructDef(cw, e); 
                        break;
                    case Parser.Statement.StatementKind.ExprVariableRef:
                    case Parser.Statement.StatementKind.ExprSingleVariable:
                        AssembleVariablePush(cw, e);
                        break;
                    case Parser.Statement.StatementKind.FunctionDef:
                        {
                            if (e.Children.Count != 3)
                            {
                                AssemblyWriterError(cw, "Malformed function assignment.", e.Token);
                                break;
                            }

                            if (funcDefName == null) {
                                funcDefName = new Parser.Statement(e);
                                do {
                                    funcDefName.Text = "anon_utmt_" +
                                        cw.compileContext.OriginalCode.Name.Content + "__" + uuidCounter++.ToString();
                                } while (cw.compileContext.Data.Scripts.ByName(funcDefName.Text) != null);
                            }

                            bool isConstructor = e.Children[1].Text == "constructor";
                            bool isStructDef = funcDefName.Text.StartsWith("___struct___");
                            
                            Patch startPatch = Patch.StartHere(cw);
                            Patch endPatch = Patch.Start();
                            endPatch.Add(cw.Emit(Opcode.B));
                            // we're accessing a subfunction here, so build the cache if needed
                            Decompiler.Decompiler.BuildSubFunctionCache(cw.compileContext.Data);

                            //Attempt to find the function before rushing to create a new one
                            var func = cw.compileContext.Data.Functions.FirstOrDefault(f => f.Name.Content == "gml_Script_" + funcDefName.Text);
                            if (func != null)
                                cw.compileContext.Data.KnownSubFunctions.TryAdd(funcDefName.Text, func);
                            
                            if (cw.compileContext.Data.KnownSubFunctions.ContainsKey(funcDefName.Text))
                            {
                                string subFunctionName = cw.compileContext.Data.KnownSubFunctions[funcDefName.Text].Name.Content;
                                UndertaleCode childEntry = cw.compileContext.OriginalCode.ChildEntries.ByName(subFunctionName);
                                childEntry.Offset = cw.offset * 4;
                                childEntry.ArgumentsCount = (ushort)e.Children[0].Children.Count;
                                childEntry.LocalsCount = cw.compileContext.OriginalCode.LocalsCount; // todo: use just the locals for the individual script
                            }
                            else // we're making a new function baby
                            {
                                cw.funcPatches.Add(new FunctionPatch()
                                {
                                    Name = funcDefName.Text,
                                    Offset = cw.offset * 4,
                                    ArgCount = (ushort)e.Children[0].Children.Count,
                                    isNewFunc = true,
                                    isConstructor = isConstructor
                                });
                            }

                            cw.loopContexts.Push(new LoopContext(endPatch, startPatch));
                            AssembleStatement(cw, e.Children[2]); // body
                            AssembleExit(cw);
                            cw.loopContexts.Pop();
                            endPatch.Finish(cw);

                            cw.funcPatches.Add(new FunctionPatch()
                            {
                                Target = cw.EmitRef(Opcode.Push, DataType.Int32),
                                Name = funcDefName.Text,
                                ArgCount = -1
                            });
                            cw.Emit(Opcode.Conv, DataType.Int32, DataType.Variable);
                            if (isConstructor) {
                                cw.funcPatches.Add(new FunctionPatch()
                                {
                                    Target = cw.EmitRef(Opcode.Call, DataType.Int32),
                                    Name = "@@NullObject@@",
                                    ArgCount = 0
                                });
                            } else {
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)-1;
                                cw.Emit(Opcode.Conv, DataType.Int32, DataType.Variable);
                            }
                            cw.funcPatches.Add(new FunctionPatch()
                            {
                                Target = cw.EmitRef(Opcode.Call, DataType.Int32),
                                Name = "method",
                                ArgCount = 2
                            });
                            cw.typeStack.Push(DataType.Variable);
                            cw.Emit(Opcode.Dup, DataType.Variable).Extra = 0;
                            if (isStructDef) {
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)-16;
                            } else if (isConstructor) {
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)-6;
                            } else {
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)-1; // todo: -6 sometimes?
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.ExprBinaryOp:
                        {
                            // Push the left value onto the stack
                            AssembleExpression(cw, e.Children[0]);
                            ConvertTypeForBinaryOp(cw, e.Token.Kind);

                            if ((cw.compileContext?.Data?.ShortCircuit ?? true) &&
                                (e.Token.Kind == Lexer.Token.TokenKind.LogicalAnd || e.Token.Kind == Lexer.Token.TokenKind.LogicalOr))
                            {
                                // Short circuit
                                Patch endPatch = Patch.Start();
                                Patch branchPatch = Patch.Start();

                                bool isAnd = (e.Token.Kind == Lexer.Token.TokenKind.LogicalAnd);

                                for (int i = 1; i < e.Children.Count; i++)
                                {
                                    DataType type2 = cw.typeStack.Pop();
                                    if (type2 != DataType.Boolean)
                                    {
                                        cw.Emit(Opcode.Conv, type2, DataType.Boolean);
                                    }
                                    if (isAnd)
                                    {
                                        branchPatch.Add(cw.Emit(Opcode.Bf));
                                    } 
                                    else
                                    {
                                        branchPatch.Add(cw.Emit(Opcode.Bt));
                                    }
                                    AssembleExpression(cw, e.Children[i]);
                                }

                                DataType type = cw.typeStack.Pop();
                                if (type != DataType.Boolean)
                                {
                                    cw.Emit(Opcode.Conv, type, DataType.Boolean);
                                }
                                cw.typeStack.Push(DataType.Boolean);

                                endPatch.Add(cw.Emit(Opcode.B));
                                branchPatch.Finish(cw);
                                if (isAnd)
                                {
                                    cw.Emit(Opcode.Push, DataType.Int16).Value = (short)0;
                                } 
                                else
                                {
                                    cw.Emit(Opcode.Push, DataType.Int16).Value = (short)1;
                                }
                                endPatch.Finish(cw);

                                return;
                            }
                            
                            for (int i = 1; i < e.Children.Count; i++)
                            {
                                // Push the next value to the stack
                                AssembleExpression(cw, e.Children[i]);
                                ConvertTypeForBinaryOp(cw, e.Token.Kind);

                                // Decide what the resulting type will be after the operation
                                DataType type1 = cw.typeStack.Pop();
                                DataType type2 = cw.typeStack.Pop();
                                int type1Bias = DataTypeBias(type1);
                                int type2Bias = DataTypeBias(type2);
                                DataType resultingType;
                                if (type1Bias == type2Bias)
                                {
                                    resultingType = (DataType)Math.Min((byte)type1, (byte)type2);
                                }
                                else
                                {
                                    resultingType = (type1Bias > type2Bias) ? type1 : type2;
                                }

                                // Push the operation instructions
                                var instr = cw.Emit(Opcode.Add, type1, type2);
                                bool pushesABoolInstead = false;
                                switch (e.Token.Kind)
                                {
                                    case Lexer.Token.TokenKind.Plus:
                                        instr.Kind = Opcode.Add;
                                        break;
                                    case Lexer.Token.TokenKind.Minus:
                                        instr.Kind = Opcode.Sub;
                                        break;
                                    case Lexer.Token.TokenKind.Times:
                                        instr.Kind = Opcode.Mul;
                                        break;
                                    case Lexer.Token.TokenKind.Divide:
                                        instr.Kind = Opcode.Div;
                                        break;
                                    case Lexer.Token.TokenKind.Div:
                                        instr.Kind = Opcode.Rem;
                                        break;
                                    case Lexer.Token.TokenKind.Mod:
                                        instr.Kind = Opcode.Mod;
                                        break;
                                    case Lexer.Token.TokenKind.CompareEqual:
                                        instr.Kind = Opcode.Cmp;
                                        instr.ComparisonKind = ComparisonType.EQ;
                                        pushesABoolInstead = true;
                                        break;
                                    case Lexer.Token.TokenKind.CompareNotEqual:
                                        instr.Kind = Opcode.Cmp;
                                        instr.ComparisonKind = ComparisonType.NEQ;
                                        pushesABoolInstead = true;
                                        break;
                                    case Lexer.Token.TokenKind.CompareGreater:
                                        instr.Kind = Opcode.Cmp;
                                        instr.ComparisonKind = ComparisonType.GT;
                                        pushesABoolInstead = true;
                                        break;
                                    case Lexer.Token.TokenKind.CompareGreaterEqual:
                                        instr.Kind = Opcode.Cmp;
                                        instr.ComparisonKind = ComparisonType.GTE;
                                        pushesABoolInstead = true;
                                        break;
                                    case Lexer.Token.TokenKind.CompareLess:
                                        instr.Kind = Opcode.Cmp;
                                        instr.ComparisonKind = ComparisonType.LT;
                                        pushesABoolInstead = true;
                                        break;
                                    case Lexer.Token.TokenKind.CompareLessEqual:
                                        instr.Kind = Opcode.Cmp;
                                        instr.ComparisonKind = ComparisonType.LTE;
                                        pushesABoolInstead = true;
                                        break;
                                    case Lexer.Token.TokenKind.BitwiseShiftLeft:
                                        instr.Kind = Opcode.Shl;
                                        break;
                                    case Lexer.Token.TokenKind.BitwiseShiftRight:
                                        instr.Kind = Opcode.Shr;
                                        break;
                                    case Lexer.Token.TokenKind.BitwiseAnd:
                                        instr.Kind = Opcode.And;
                                        break;
                                    case Lexer.Token.TokenKind.BitwiseOr:
                                        instr.Kind = Opcode.Or;
                                        break;
                                    case Lexer.Token.TokenKind.BitwiseXor:
                                    case Lexer.Token.TokenKind.LogicalXor: // doesn't do short circuit
                                        instr.Kind = Opcode.Xor;
                                        break;

                                    // For when not short circuiting
                                    case Lexer.Token.TokenKind.LogicalAnd:
                                        instr.Kind = Opcode.And;
                                        break;
                                    case Lexer.Token.TokenKind.LogicalOr:
                                        instr.Kind = Opcode.Or;
                                        break;
                                }

                                cw.typeStack.Push(pushesABoolInstead ? DataType.Boolean : resultingType);
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.ExprUnary:
                        {
                            AssembleExpression(cw, e.Children[0]);
                            DataType type = cw.typeStack.Peek();

                            switch (e.Token.Kind)
                            {
                                case Lexer.Token.TokenKind.Not:
                                    if (type == DataType.String)
                                    {
                                        AssemblyWriterError(cw, "Cannot logically negate a string.", e.Token);
                                    }
                                    else if (type != DataType.Boolean)
                                    {
                                        cw.typeStack.Pop();
                                        cw.Emit(Opcode.Conv, type, DataType.Boolean);
                                        cw.typeStack.Push(DataType.Boolean);
                                    }
                                    cw.Emit(Opcode.Not, DataType.Boolean);
                                    break;
                                case Lexer.Token.TokenKind.BitwiseNegate:
                                    if (type == DataType.String)
                                    {
                                        AssemblyWriterError(cw, "Cannot bitwise negate a string.", e.Token);
                                    }
                                    else if (type == DataType.Double ||
                                             type == DataType.Float ||
                                             type == DataType.Variable)
                                    {
                                        cw.typeStack.Pop();
                                        cw.Emit(Opcode.Conv, type, DataType.Int32);
                                        cw.typeStack.Push(DataType.Int32);
                                        type = DataType.Int32;
                                    }
                                    cw.Emit(Opcode.Not, type);
                                    break;
                                case Lexer.Token.TokenKind.Minus:
                                    if (type == DataType.String)
                                    {
                                        AssemblyWriterError(cw, "Cannot negate a string.", e.Token);
                                    }
                                    else if (type == DataType.Boolean)
                                    {
                                        cw.typeStack.Pop();
                                        cw.Emit(Opcode.Conv, type, DataType.Int32);
                                        cw.typeStack.Push(DataType.Int32);
                                        type = DataType.Int32;
                                    }
                                    cw.Emit(Opcode.Neg, type);
                                    break;
                            }
                        }
                        break;
                    case Parser.Statement.StatementKind.Pre:
                        AssemblePostOrPre(cw, e, false, true);
                        break;
                    case Parser.Statement.StatementKind.Post:
                        AssemblePostOrPre(cw, e, true, true);
                        break;
                    case Parser.Statement.StatementKind.ExprConditional:
                        {
                            Patch falsePatch = Patch.Start();
                            Patch endPatch = Patch.Start();

                            // Condition
                            AssembleExpression(cw, e.Children[0]);
                            var t = cw.typeStack.Pop();
                            if (t != DataType.Boolean)
                            {
                                cw.Emit(Opcode.Conv, t, DataType.Boolean);
                            }
                            falsePatch.Add(cw.Emit(Opcode.Bf));

                            // True expression
                            AssembleExpression(cw, e.Children[1]);
                            t = cw.typeStack.Pop();
                            if (t != DataType.Variable)
                            {
                                cw.Emit(Opcode.Conv, t, DataType.Variable);
                            }
                            endPatch.Add(cw.Emit(Opcode.B));

                            // False expression
                            falsePatch.Finish(cw);
                            AssembleExpression(cw, e.Children[2]);
                            t = cw.typeStack.Pop();
                            if (t != DataType.Variable)
                            {
                                cw.Emit(Opcode.Conv, t, DataType.Variable);
                            }

                            endPatch.Finish(cw);
                            cw.typeStack.Push(DataType.Variable);
                        }
                        break;
                    case Parser.Statement.StatementKind.ExprFuncName:
                        {
                            cw.funcPatches.Add(new FunctionPatch()
                            {
                                Target = cw.EmitRef(Opcode.Push, DataType.Int32),
                                Name = e.Text,
                                ArgCount = -1
                            });
                            cw.Emit(Opcode.Conv, DataType.Int32, DataType.Variable);
                            cw.typeStack.Push(DataType.Variable);
                        }
                        break;
                    default:
                        AssemblyWriterError(cw, "Expected expression, none found.", e.Token);
                        cw.typeStack.Push(DataType.Variable);
                        break;
                }
            }

            // Used to determine which types have dominance over operations- the higher the more
            private static int DataTypeBias(DataType type)
            {
                switch (type)
                {
                    case DataType.Float:
                    case DataType.Int32:
                    case DataType.Boolean:
                    case DataType.String:
                        return 0;
                    case DataType.Double:
                    case DataType.Int64:
                        return 1;
                    case DataType.Variable:
                        return 2;
                    default:
                        return -1;
                }
            }

            // Used to convert types to their proper forms for binary operations
            private static void ConvertTypeForBinaryOp(CodeWriter cw, Lexer.Token.TokenKind kind)
            {
                var type = cw.typeStack.Peek();
                switch (kind)
                {
                    case Lexer.Token.TokenKind.Divide:
                        if (type != DataType.Double && type != DataType.Variable)
                        {
                            cw.typeStack.Pop();
                            cw.Emit(Opcode.Conv, type, DataType.Double);
                            cw.typeStack.Push(DataType.Double);
                        }
                        break;
                    case Lexer.Token.TokenKind.Plus:
                    case Lexer.Token.TokenKind.Minus:
                    case Lexer.Token.TokenKind.Times:
                    case Lexer.Token.TokenKind.Div:
                    case Lexer.Token.TokenKind.Mod:
                        if (type == DataType.Boolean)
                        {
                            cw.typeStack.Pop();
                            cw.Emit(Opcode.Conv, type, DataType.Int32);
                            cw.typeStack.Push(DataType.Double);
                        }
                        break;
                    case Lexer.Token.TokenKind.LogicalAnd:
                    case Lexer.Token.TokenKind.LogicalOr:
                    case Lexer.Token.TokenKind.LogicalXor:
                        if (type != DataType.Boolean)
                        {
                            cw.typeStack.Pop();
                            cw.Emit(Opcode.Conv, type, DataType.Boolean);
                            cw.typeStack.Push(DataType.Boolean);
                        }
                        break;
                    case Lexer.Token.TokenKind.BitwiseAnd:
                    case Lexer.Token.TokenKind.BitwiseOr:
                    case Lexer.Token.TokenKind.BitwiseXor:
                        if (type != DataType.Int32)
                        {
                            cw.typeStack.Pop();
                            if (type != DataType.Variable &&
                                type != DataType.Double &&
                                type != DataType.Int64)
                            {
                                cw.Emit(Opcode.Conv, type, DataType.Int32);
                                cw.typeStack.Push(DataType.Int32);
                            }
                            else
                            {
                                if (type != DataType.Int64)
                                {
                                    cw.Emit(Opcode.Conv, type, DataType.Int64);
                                }
                                cw.typeStack.Push(DataType.Int64);
                            }
                        }
                        break;
                    case Lexer.Token.TokenKind.BitwiseShiftLeft:
                    case Lexer.Token.TokenKind.BitwiseShiftRight:
                        if (type != DataType.Int64)
                        {
                            cw.typeStack.Pop();
                            cw.Emit(Opcode.Conv, type, DataType.Int64);
                            cw.typeStack.Push(DataType.Int64);
                        }
                        break;
                }
            }

            // Workaround for out parameters
            private static void AssembleVariablePush(CodeWriter cw, Parser.Statement e, bool duplicate = false, bool useLongDupForArray = false, bool useNoSpecificType = false)
            {
                AssembleVariablePush(cw, e, out _, out _, duplicate, useLongDupForArray, useNoSpecificType);
            }

            // Workaround for out parameters #2
            private static void AssembleVariablePush(CodeWriter cw, Parser.Statement e, out bool isSingle, bool duplicate = false, bool useLongDupForArray = false, bool useNoSpecificType = false)
            {
                AssembleVariablePush(cw, e, out isSingle, out _, duplicate, useLongDupForArray, useNoSpecificType);
            }

            private static void AssembleVariablePush(CodeWriter cw, Parser.Statement e, out bool isSingle, out bool isArray, bool duplicate = false, bool useLongDupForArray = false, bool useNoSpecificType = false)
            {
                isSingle = false;
                isArray = false;
                if (e.Kind == Parser.Statement.StatementKind.ExprVariableRef)
                {
                    if (e.Children.Count == 1)
                    {
                        if (e.Children[0].Children.Count != 0)
                        {
                            if (e.Children[0].Kind == Parser.Statement.StatementKind.ExprFunctionCall)
                            {
                                // Function call
                                AssembleFunctionCall(cw, e.Children[0]);
                                cw.typeStack.Push(DataType.Variable);
                                return;
                            }

                            // Special array access- instance type needs to be pushed beforehand
                            if (CompileContext.GMS2_3 && (cw.compileContext.BuiltInList.GlobalArray.ContainsKey(e.Children[0].Text) || cw.compileContext.BuiltInList.GlobalNotArray.ContainsKey(e.Children[0].Text)))
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)(e.Children[0].Text == "argument" ? InstanceType.Arg : InstanceType.Builtin); // hack x2
                            else
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)e.Children[0].ID;
                            // Pushing array (incl. 2D) but not popping
                            AssembleArrayPush(cw, e.Children[0], !duplicate);

                            if (CompileContext.GMS2_3 && e.Children[0].Children.Count > 2)
                            {
                                for (int i = 2; i < e.Children[0].Children.Count; i++)
                                {
                                    cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-4; // pushac
                                    AssembleExpression(cw, e.Children[0].Children[i]); // this needs error handling
                                }
                            }
                            if (duplicate)
                            {
                                if (CompileContext.GMS2_3 && e.Children[0].Children.Count != 1)
                                {
                                    cw.Emit(Opcode.Dup, DataType.Int32).Extra = 4;
                                    cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-8; // savearef
                                }
                                else
                                {
                                    if (useLongDupForArray)
                                        cw.Emit(Opcode.Dup, DataType.Int64).Extra = 0;
                                    else
                                        cw.Emit(Opcode.Dup, DataType.Int32).Extra = 1;
                                }
                            }
                            if (CompileContext.GMS2_3 && e.Children[0].Children.Count > 1)
                            {
                                cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-2; // pushaf
                            }
                            else
                            {
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                                    Name = e.Children[0].Text,
                                    InstType = GetIDPrefixSpecial(e.Children[0].ID),
                                    VarType = VariableType.Array
                                });
                            }
                            cw.typeStack.Push(DataType.Variable);
                            isArray = true;
                            return;
                        }
                        isSingle = true;
                        int id = e.Children[0].ID;
                        if (id >= 100000)
                            id -= 100000;
                        string name = e.Children[0].Text;
                        switch (id)
                        {
                            case -1:
                                if (cw.compileContext.BuiltInList.GlobalArray.ContainsKey(name) || cw.compileContext.BuiltInList.GlobalNotArray.ContainsKey(name))
                                {
                                    if (CompileContext.GMS2_3 &&
                                        name.In(
                                            "argument0",  "argument1",  "argument2",  "argument3",
                                            "argument4",  "argument5",  "argument6",  "argument7",
                                            "argument8",  "argument9",  "argument10", "argument11",
                                            "argument12", "argument13", "argument14", "argument15"))
                                    {
                                        // 2.3 argument (excuse the condition... the ID seems to be lost, so this is the easiest way to check)
                                        cw.varPatches.Add(new VariablePatch()
                                        {
                                            Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                                            Name = name,
                                            InstType = InstanceType.Arg,
                                            VarType = VariableType.Normal
                                        });
                                    }
                                    else
                                    {
                                        // Builtin global
                                        cw.varPatches.Add(new VariablePatch()
                                        {
                                            Target = cw.EmitRef(useNoSpecificType ? Opcode.Push : Opcode.PushBltn, DataType.Variable),
                                            Name = name,
                                            InstType = (CompileContext.GMS2_3 && !useNoSpecificType) ? InstanceType.Builtin : InstanceType.Self,
                                            VarType = VariableType.Normal
                                        });
                                    }
                                }
                                else
                                {
                                    cw.varPatches.Add(new VariablePatch()
                                    {
                                        Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                                        Name = name,
                                        InstType = InstanceType.Self,
                                        VarType = VariableType.Normal
                                    });
                                }
                                break;
                            case -5:
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(useNoSpecificType ? Opcode.Push : Opcode.PushGlb, DataType.Variable),
                                    Name = name,
                                    InstType = InstanceType.Global,
                                    VarType = VariableType.Normal
                                });
                                break;
                            case -7:
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(useNoSpecificType ? Opcode.Push : Opcode.PushLoc, DataType.Variable),
                                    Name = name,
                                    InstType = InstanceType.Local,
                                    VarType = VariableType.Normal
                                });
                                break;
                            default:
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                                    Name = name,
                                    InstType = (InstanceType)id,
                                    VarType = VariableType.Normal
                                });
                                break;
                        }
                        cw.typeStack.Push(DataType.Variable);
                    }
                    else
                    {
                        AssembleExpression(cw, e.Children[0]);
                        if (CompileContext.GMS2_3 && cw.typeStack.Peek() == DataType.Variable)
                        {
                            cw.typeStack.Pop();
                            cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)-9; // stacktop conversion
                        }
                        else if (cw.typeStack.Peek() != DataType.Int32) // apparently it converts to ints
                        {
                            cw.Emit(Opcode.Conv, cw.typeStack.Pop(), DataType.Int32);
                        }

                        for (int next = 1; next < e.Children.Count; next++)
                        {
                            if (e.Children[next].Children.Count != 0)
                            {
                                AssembleArrayPush(cw, e.Children[next]);
                                bool notLast = (next + 1 < e.Children.Count);
                                if (!notLast && duplicate) // ha ha, double negatives
                                {
                                    if (useLongDupForArray)
                                        cw.Emit(Opcode.Dup, DataType.Int64).Extra = 0;
                                    else
                                        cw.Emit(Opcode.Dup, DataType.Int32).Extra = 1;
                                }
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                                    Name = e.Children[next].Text,
                                    InstType = GetIDPrefixSpecial(e.Children[next].ID),
                                    VarType = VariableType.Array
                                });
                                cw.typeStack.Push(DataType.Variable);
                                if (notLast)
                                {
                                    cw.Emit(Opcode.Conv, cw.typeStack.Pop(), DataType.Int32);
                                }
                                else
                                    isArray = true;
                            }
                            else
                            {
                                if (duplicate && next + 1 >= e.Children.Count)
                                {
                                    cw.Emit(Opcode.Dup, DataType.Int32).Extra = (byte)(CompileContext.GMS2_3 ? 4 : 0);
                                }
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                                    Name = e.Children[next].Text,
                                    InstType = GetIDPrefixSpecial(e.Children[next].ID),
                                    VarType = VariableType.StackTop
                                });
                                cw.typeStack.Push(DataType.Variable);
                                if (next + 1 < e.Children.Count)
                                {
                                    cw.Emit(Opcode.Conv, cw.typeStack.Pop(), DataType.Int32);
                                }
                            }
                        }
                    }
                } 
                else if (e.Kind == Parser.Statement.StatementKind.ExprSingleVariable)
                {
                    // Assume local or self if necessary. Global doesn't apply here
                    Parser.Statement fix = new Parser.Statement(Parser.Statement.StatementKind.ExprVariableRef);
                    Parser.Statement fix2 = new Parser.Statement(e);
                    string variableName = e.Text;
                    if (!fix2.WasIDSet || fix2.ID >= 100000)
                    {
                        if (cw.compileContext.LocalVars.ContainsKey(variableName))
                        {
                            fix2.ID = -7; // local
                        }
                        else
                        {
                            fix2.ID = -1; // self
                        }
                    }
                    fix.Children.Add(fix2);
                    AssembleVariablePush(cw, fix, out isSingle, out isArray, duplicate, useLongDupForArray, useNoSpecificType);
                }
                else
                {
                    AssemblyWriterError(cw, "Malformed variable push.", e.Token);
                }
            }

            private static void AssembleArrayPush(CodeWriter cw, Parser.Statement a, bool arraypushaf = false)
            {
                // 1D index
                Parser.Statement index1d = a.Children[0];
                if (index1d.Kind == Parser.Statement.StatementKind.ExprConstant &&
                    ((index1d.Constant.kind == Parser.ExpressionConstant.Kind.Number && index1d.Constant.valueNumber < 0) ||
                     (index1d.Constant.kind == Parser.ExpressionConstant.Kind.Int64 && index1d.Constant.valueInt64 < 0)
                    ))
                    AssemblyWriterError(cw, "Array index should not be negative.", index1d.Token);

                AssembleExpression(cw, index1d);
                if (cw.typeStack.Peek() != DataType.Int32)
                {
                    cw.Emit(Opcode.Conv, cw.typeStack.Pop(), DataType.Int32);
                    cw.typeStack.Push(DataType.Int32);
                }

                // 2D index
                if (a.Children.Count != 1)
                {
                    if (!CompileContext.GMS2_3)
                    {
                        // These instructions are hardcoded. Honestly it seems pretty
                        // inefficient because these could be easily combined into
                        // one small instruction.
                        cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-1; // chkindex
                        cw.Emit(Opcode.Push, DataType.Int32).Value = 32000;
                        cw.Emit(Opcode.Mul, DataType.Int32, DataType.Int32);
                    }
                    else
                    {
                        // Surprise! One small instruction.
                        cw.varPatches.Add(new VariablePatch()
                        {
                            Target = cw.EmitRef(Opcode.Push, DataType.Variable),
                            Name = a.Text,
                            InstType = InstanceType.Self,
                            VarType = arraypushaf ? VariableType.ArrayPushAF : VariableType.ArrayPopAF
                        });
                    }

                    Parser.Statement index2d = a.Children[1];
                    if (index2d.Kind == Parser.Statement.StatementKind.ExprConstant &&
                        ((index2d.Constant.kind == Parser.ExpressionConstant.Kind.Number && index2d.Constant.valueNumber < 0) ||
                         (index2d.Constant.kind == Parser.ExpressionConstant.Kind.Int64 && index2d.Constant.valueInt64 < 0)
                        ))
                        AssemblyWriterError(cw, "Array index should not be negative.", index2d.Token);

                    AssembleExpression(cw, index2d);
                    if (cw.typeStack.Peek() != DataType.Int32)
                    {
                        cw.Emit(Opcode.Conv, cw.typeStack.Pop(), DataType.Int32);
                        cw.typeStack.Push(DataType.Int32);
                    }

                    if (!CompileContext.GMS2_3)
                    {
                        cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-1; // chkindex
                        cw.Emit(Opcode.Add, DataType.Int32, DataType.Int32);
                    }

                    cw.typeStack.Pop();
                }
                cw.typeStack.Pop();
            }

            private static InstanceType GetIDPrefixSpecial(int ID)
            {
                return ID switch
                {
                    -2 => InstanceType.Other,
                    -5 => InstanceType.Global,
                    -7 => InstanceType.Local,
                    _ => InstanceType.Self,
                };
            }

            private static void AssembleStoreVariable(CodeWriter cw, Parser.Statement s, DataType typeToStore, bool skip = false, bool duplicate = false)
            {
                if (s.Kind == Parser.Statement.StatementKind.ExprVariableRef)
                {
                    if (s.Children.Count == 1)
                    {
                        DataType popLocation = DataType.Variable;
                        if (skip)
                            popLocation = DataType.Int32;

                        if (s.Children[0].Children.Count != 0)
                        {
                            // Special array set- instance type needs to be pushed beforehand
                            if (!skip)
                            {
                                // Convert to variable in 2.3
                                if (CompileContext.GMS2_3 && typeToStore != DataType.Variable)
                                {
                                    cw.Emit(Opcode.Conv, typeToStore, DataType.Variable);
                                    typeToStore = DataType.Variable;
                                }
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)s.Children[0].ID;
                                AssembleArrayPush(cw, s.Children[0]);
                            }

                            if (CompileContext.GMS2_3 && s.Children[0].Children.Count != 1)
                            {
                                if (duplicate)
                                {
                                    cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-9; // restorearef
                                    UndertaleInstruction dupInst = cw.Emit(Opcode.Dup, DataType.Int32);
                                    dupInst.Extra = 4;
                                    dupInst.ComparisonKind = (ComparisonType)168; // idek what this is but it disassembles as 40
                                }
                                else if (s.Children[0].Children.Count > 2)
                                {
                                    for (int i = 2; i < s.Children[0].Children.Count; i++)
                                    {
                                        cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-4; // pushac
                                        AssembleExpression(cw, s.Children[0].Children[i]); // this needs error handling
                                    }
                                }
                                cw.Emit(Opcode.Break, DataType.Int16).Value = (short)-3; // popaf
                            }
                            else
                            {
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Pop, popLocation, typeToStore),
                                    Name = s.Children[0].Text,
                                    InstType = GetIDPrefixSpecial(s.Children[0].ID),
                                    VarType = VariableType.Array
                                });
                            }
                            return;
                        }

                        // Simple common assignment
                        int id = s.Children[0].ID;
                        if (id >= 100000)
                            id -= 100000;
                        if (CompileContext.GMS2_3 && (cw.compileContext.BuiltInList.GlobalArray.ContainsKey(s.Children[0].Text) || cw.compileContext.BuiltInList.GlobalNotArray.ContainsKey(s.Children[0].Text)))
                        {
                            if (s.Children[0].Text.In(
                                "argument0", "argument1", "argument2", "argument3",
                                "argument4", "argument5", "argument6", "argument7",
                                "argument8", "argument9", "argument10", "argument11",
                                "argument12", "argument13", "argument14", "argument15"))
                            {
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Pop, popLocation, typeToStore),
                                    Name = s.Children[0].Text,
                                    InstType = InstanceType.Arg,
                                    VarType = VariableType.Normal
                                });
                            }
                            else
                            {
                                cw.varPatches.Add(new VariablePatch()
                                {
                                    Target = cw.EmitRef(Opcode.Pop, popLocation, typeToStore),
                                    Name = s.Children[0].Text,
                                    InstType = InstanceType.Builtin,
                                    VarType = VariableType.Normal
                                });
                            }
                        }
                        else
                        {
                            cw.varPatches.Add(new VariablePatch()
                            {
                                Target = cw.EmitRef(Opcode.Pop, popLocation, typeToStore),
                                Name = s.Children[0].Text,
                                InstType = (InstanceType)id,
                                VarType = VariableType.Normal
                            });
                        }
                    }
                    else
                    {
                        if (!skip)
                        {
                            // Convert to variable in 2.3
                            if (CompileContext.GMS2_3 && typeToStore != DataType.Variable && s.Children.Last().Children.Count != 0)
                            {
                                cw.Emit(Opcode.Conv, typeToStore, DataType.Variable);
                                typeToStore = DataType.Variable;
                            }
                            AssembleExpression(cw, s.Children[0]);
                            if (CompileContext.GMS2_3 && cw.typeStack.Peek() == DataType.Variable)
                            {
                                cw.typeStack.Pop();
                                cw.Emit(Opcode.PushI, DataType.Int16).Value = (short)-9; // stacktop conversion
                                cw.typeStack.Push(DataType.Int32);
                            }
                            else if (cw.typeStack.Peek() != DataType.Int32) // apparently it converts to ints
                            {
                                cw.Emit(Opcode.Conv, cw.typeStack.Pop(), DataType.Int32);
                                cw.typeStack.Push(DataType.Int32);
                            }
                        }

                        DataType popLocation = DataType.Variable;
                        for (int next = 1; next < s.Children.Count; next++)
                        {
                            if (skip)
                            {
                                popLocation = DataType.Int32;
                                next = s.Children.Count - 1;
                            }
                            if (!skip && s.Children[next].Children.Count != 0) // don't push the index again
                                AssembleArrayPush(cw, s.Children[next]);
                            // Mind the ternaries
                            cw.varPatches.Add(new VariablePatch()
                            {
                                Target = next + 1 < s.Children.Count ? cw.EmitRef(Opcode.Push, DataType.Variable) : cw.EmitRef(Opcode.Pop, popLocation, typeToStore),
                                Name = s.Children[next].Text,
                                InstType = GetIDPrefixSpecial(s.Children[next].ID),
                                VarType = s.Children[next].Children.Count != 0 ? VariableType.Array : VariableType.StackTop
                            });
                            if (next + 1 < s.Children.Count)
                                cw.Emit(Opcode.Conv, DataType.Variable, DataType.Int32);
                        }
                        if (!skip)
                            cw.typeStack.Pop();
                    }
                }
                else if (s.Kind == Parser.Statement.StatementKind.ExprSingleVariable)
                {
                    // Assume local, global, or self if necessary
                    Parser.Statement fix = new Parser.Statement(Parser.Statement.StatementKind.ExprVariableRef);
                    Parser.Statement fix2 = new Parser.Statement(s);
                    string variableName = s.Text;
                    if (!fix2.WasIDSet || fix2.ID >= 100000)
                    {
                        if (cw.compileContext.LocalVars.ContainsKey(variableName))
                        {
                            fix2.ID = -7; // local
                        } 
                        else if (cw.compileContext.GlobalVars.ContainsKey(variableName))
                        {
                            fix2.ID = -5; // global
                        }
                        else
                        {
                            fix2.ID = -1; // self
                        }
                    }
                    fix.Children.Add(fix2);
                    AssembleStoreVariable(cw, fix, typeToStore, skip, duplicate);
                }
                else if (s.Kind == Parser.Statement.StatementKind.ExprFuncName)
                {
                    bool isStructDef = s.Text.StartsWith("___struct___");
                    // Until further notice, I'm assuming this only comes up in 2.3 script definition.
                    cw.varPatches.Add(new VariablePatch()
                    {
                        Target = cw.EmitRef(Opcode.Pop, DataType.Variable, DataType.Variable),
                        Name = s.Text,
                        InstType = isStructDef ? InstanceType.Static : InstanceType.Self,
                        VarType = VariableType.StackTop
                    });
                    if (!isStructDef) cw.Emit(Opcode.Popz, DataType.Variable);
                }
                else
                {
                    AssemblyWriterError(cw, "Malformed variable store.\n\nPlease note that editing GMS 2.3+ scripts is not yet fully supported.\n\n", s.Token);
                }
            }

            private static void AssemblyWriterError(CodeWriter cw, string msg, Lexer.Token context)
            {
                string finalMsg = msg;
                if (context?.Location != null)
                    finalMsg += string.Format(" Around line {0}, column {1}.", context.Location.Line, context.Location.Column);
                cw.ErrorMessages.Add(finalMsg);
            }
        }
    }
}
