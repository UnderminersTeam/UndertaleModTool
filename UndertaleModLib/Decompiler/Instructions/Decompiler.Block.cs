using System.Collections.Generic;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    /// <summary>
    /// Represents a block node of instructions from GML bytecode (for control flow). 
    /// </summary>
    public class Block
    {
        public uint? Address;
        public List<UndertaleInstruction> Instructions = new List<UndertaleInstruction>();
        public List<Statement> Statements = null;
        public Expression ConditionStatement = null;
        public bool conditionalExit;
        public Block nextBlockTrue;
        public Block nextBlockFalse;
        public List<Block> entryPoints = new List<Block>();
        internal List<TempVarReference> TempVarsOnEntry;

        public int _CachedIndex;

        public Block(uint? address)
        {
            Address = address;
        }

        public override string ToString()
        {
            return "Block " + Address;
        }
    }
}