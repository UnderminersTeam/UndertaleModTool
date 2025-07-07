/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer;
using Underanalyzer.Mock;

namespace UnderanalyzerTest;

public class VMAssembly_ParseInstructions
{
    [Fact]
    public void TestBasic()
    {
        string text =
        """
        :[0]

        # Conv as well as data types
        conv.i.v
        conv.v.i
        conv.b.v
        conv.s.v
        conv.l.v
        conv.d.i
        conv.i.d

        # Misc. instructions
        mul.i.i
        div.i.i
        rem.i.i
        mod.i.i
        add.i.i
        sub.i.i
        and.b.b
        or.b.b
        xor.b.b
        neg.b
        not.b
        shl.i.i
        shr.i.i

        # Comparison
        cmp.i.i lt
        cmp.i.i leq
        cmp.i.i eq
        cmp.i.i neq
        cmp.i.i geq
        cmp.i.i gt

        # Pop instruction
        pop.i.v self.a
        pop.i.v local.a
        pop.v.v [array]self.a
        pop.v.v [stacktop]self.a
        pop.v.v [stacktop]self.a
        pop.v.v [instance]self.a

        # Duplication
        dup.i 0
        dup.l 0
        dup.i 1

        # Duplication (swap mode)
        dup.i 4 5
        
        # Return/exit
        ret.v
        ret.i
        exit.i

        # Discard pop
        popz.v
        popz.i

        # Branching
        b [0]
        bt [end]
        bf [0]
        pushenv [0]
        popenv [0]
        popenv <drop>

        # Push instructions
        push.d 3.1415926535
        push.i 123456
        push.l 5000000000
        push.b true
        push.v self.a
        push.v local.a
        push.v [array]self.a
        push.v [stacktop]self.a
        push.v [instance]self.a
        push.s "Test string!"
        push.s "\"Test\nescaped\nstring!\""
        push.e 123
        pushi.e 123

        # Call instruction
        call.i test_function 5
        
        # Extended instructions
        chkindex.e
        pushaf.e
        popaf.e
        pushac.e
        setowner.e
        isstaticok.e
        setstatic.e
        savearef.e
        restorearef.e
        isnullish.e
        pushref.i 1234 Sprite

        # Function index push
        push.i [function]test_function_reference
        pushref.i test_function_reference

        # Variable index push
        push.i [variable]test_variable_reference
        
        :[end]
        """;
        string[] lines = text.Split('\n');

        GMCode code = VMAssembly.ParseAssemblyFromLines(lines, null);
        List<GMInstruction> list = code.Instructions;

        Assert.Equal(75, list.Count);

        Assert.Equal(IGMInstruction.Opcode.Convert, list[0].Kind);
        Assert.Equal(IGMInstruction.DataType.Int32, list[0].Type1);
        Assert.Equal(IGMInstruction.DataType.Variable, list[0].Type2);

        Assert.Equal(IGMInstruction.Opcode.Convert, list[1].Kind);
        Assert.Equal(IGMInstruction.DataType.Variable, list[1].Type1);
        Assert.Equal(IGMInstruction.DataType.Int32, list[1].Type2);

        Assert.Equal(IGMInstruction.Opcode.Pop, list[26].Kind);
        Assert.Equal(IGMInstruction.DataType.Int32, list[26].Type1);
        Assert.Equal(IGMInstruction.DataType.Variable, list[26].Type2);
        Assert.Equal(IGMInstruction.InstanceType.Self, list[26].InstType);
        Assert.Equal(IGMInstruction.VariableType.Normal, list[26].ReferenceVarType);
        Assert.Equal("a", list[26].ResolvedVariable!.Name.Content);

        Assert.Equal(IGMInstruction.Opcode.Pop, list[29].Kind);
        Assert.Equal(IGMInstruction.DataType.Variable, list[29].Type1);
        Assert.Equal(IGMInstruction.DataType.Variable, list[29].Type2);
        Assert.Equal(IGMInstruction.InstanceType.Self, list[29].InstType);
        Assert.Equal(IGMInstruction.VariableType.StackTop, list[29].ReferenceVarType);
        Assert.Equal("a", list[29].ResolvedVariable!.Name.Content);

        Assert.Equal(IGMInstruction.Opcode.Branch, list[41].Kind);
        Assert.Equal(-list[41].Address, list[41].BranchOffset);

        Assert.Equal(IGMInstruction.Opcode.BranchTrue, list[42].Kind);
        Assert.Equal(8 + (list[^1].Address - list[42].Address), list[42].BranchOffset);

        Assert.Equal(IGMInstruction.Opcode.Push, list[56].Kind);
        Assert.Equal(IGMInstruction.DataType.String, list[56].Type1);
        Assert.Equal("Test string!", list[56].ValueString!.Content);

        Assert.Equal(IGMInstruction.Opcode.Push, list[57].Kind);
        Assert.Equal(IGMInstruction.DataType.String, list[57].Type1);
        Assert.Equal("\"Test\nescaped\nstring!\"", list[57].ValueString!.Content);

        Assert.Equal(IGMInstruction.Opcode.Push, list[72].Kind);
        Assert.Equal(IGMInstruction.DataType.Int32, list[72].Type1);
        Assert.Equal("test_function_reference", list[72].ResolvedFunction!.Name.Content);

        Assert.Equal(IGMInstruction.Opcode.Extended, list[73].Kind);
        Assert.Equal(IGMInstruction.ExtendedOpcode.PushReference, list[73].ExtKind);
        Assert.Equal(IGMInstruction.DataType.Int32, list[73].Type1);
        Assert.NotNull(list[73].ResolvedFunction);
        Assert.Equal("test_function_reference", list[73].ResolvedFunction!.Name.Content);

        Assert.Equal(IGMInstruction.Opcode.Push, list[74].Kind);
        Assert.Equal(IGMInstruction.DataType.Int32, list[74].Type1);
        Assert.Equal("test_variable_reference", list[74].ResolvedVariable!.Name.Content);
    }

    [Fact]
    public void TestSubEntries()
    {
        string text =
        """
        > test_root (locals=5)
        pushi.e 0

        > test_sub_entry_1 (locals=20, args=5)
        :[0]
        pushi.e 1

        :[1]
        pushi.e 2

        > test_sub_entry_2 (args=15, locals=10)
        :[2]
        pushi.e 3

        > test_sub_entry_no_params

        """;
        string[] lines = text.Split('\n');

        GMCode code = VMAssembly.ParseAssemblyFromLines(lines, null, "test_root");
        List<GMInstruction> list = code.Instructions;

        Assert.Equal("test_root", code.Name.Content);
        Assert.Equal(3, code.Children.Count);
        Assert.Equal(1, code.ArgumentCount);
        Assert.Equal(5, code.LocalCount);

        Assert.Equal("test_sub_entry_1", code.Children[0].Name.Content);
        Assert.Equal(code, code.Children[0].Parent);
        Assert.Equal(list[1].Address, code.Children[0].StartOffset);
        Assert.Equal(5, code.Children[0].ArgumentCount);
        Assert.Equal(20, code.Children[0].LocalCount);

        Assert.Equal("test_sub_entry_2", code.Children[1].Name.Content);
        Assert.Equal(code, code.Children[1].Parent);
        Assert.Equal(list[3].Address, code.Children[1].StartOffset);
        Assert.Equal(15, code.Children[1].ArgumentCount);
        Assert.Equal(10, code.Children[1].LocalCount);

        Assert.Equal("test_sub_entry_no_params", code.Children[2].Name.Content);
        Assert.Equal(code, code.Children[2].Parent);
        Assert.Equal(code.Length, code.Children[2].StartOffset);
        Assert.Equal(0, code.Children[2].ArgumentCount);
        Assert.Equal(0, code.Children[2].LocalCount);
    }
}