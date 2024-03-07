using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

public static partial class Decompiler
{
    /// <summary>
    /// Represents an unnamed value that gets passed around the stack.
    /// Theoretically, these should be cleaned up and removed by the end of decompilation.
    /// </summary>
    public class TempVar
    {
        public string Name;
        public UndertaleInstruction.DataType Type;
        internal AssetIDType AssetType;

        public TempVar(int id)
        {
            Name = MakeTemporaryVarName(id);
        }

        public static string MakeTemporaryVarName(int id)
        {
            return "_temp_local_var_" + id;
        }
    }
}