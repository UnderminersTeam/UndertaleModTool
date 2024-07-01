using System.Collections.Generic;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

/// <summary>
/// The DecompileContext is global for the entire decompilation run, or possibly multiple runs. It caches the decompilation results which don't change often
/// to speedup decompilation.
/// </summary>
public class GlobalDecompileContext
{
    public UndertaleData Data;

    public bool EnableStringLabels;

    public List<string> DecompilerWarnings = new List<string>();

    /// <summary>
    /// A cache of resolved function argument types. This is kept here because decompiling is slow, and there is no need to do it every time
    /// unless the code has changed.
    /// </summary>
    public Dictionary<string, AssetIDType[]> ScriptArgsCache = new Dictionary<string, AssetIDType[]>();

    /// <summary>
    /// A cache of function to actual name mapping. GMS2.3+ sometimes (usually when dealing with global scripts) calls method functions
    /// using the legacy call operator, passing the anonymous function directly. This dictionary contains a map from UndertaleFunction
    /// to its actual name, obtained by decompiling the parent CodeObject and looking for the assignment to global variable with function
    /// name.
    /// </summary>
    public Dictionary<UndertaleFunction, string> AnonymousFunctionNameCache = new Dictionary<UndertaleFunction, string>();

    public GlobalDecompileContext(UndertaleData data, bool enableStringLabels)
    {
        this.Data = data;
        this.EnableStringLabels = enableStringLabels;
    }

    public void ClearDecompilationCache()
    {
        // This will not be done automatically, because it would cause significant slowdown having to recalculate this each time, and there's no reason to reset it if it's decompiling a bunch at once.
        // But, since it is possible to invalidate this data, we add this here so we'll be able to invalidate it if we need to.
        ScriptArgsCache.Clear();
        AnonymousFunctionNameCache.Clear();
    }
}