using System.Collections.Generic;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    // Represents a high-level function or script call.
    public abstract class FunctionCall : Expression
    {
        internal UndertaleInstruction.DataType ReturnType;
        internal List<Expression> Arguments;

        protected FunctionCall(UndertaleInstruction.DataType returnType, List<Expression> args)
        {
            this.ReturnType = returnType;
            this.Arguments = args;
        }

        internal override bool IsDuplicationSafe()
        {
            // Function calls are never duplication safe - they can have side effects
            return false;
        }
    }
}