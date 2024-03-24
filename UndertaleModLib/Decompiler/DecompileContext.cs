using System;
using System.Collections.Generic;
using System.Text;
using UndertaleModLib.Models;

namespace UndertaleModLib.Decompiler;

/// <summary>
/// The DecompileContext is bound to the currently decompiled code block
/// </summary>
public class DecompileContext
{
    public GlobalDecompileContext GlobalContext;
    public UndertaleCode TargetCode;
    public UndertaleGameObject Object;
    public static bool GMS2_3;
    public bool AssetResolutionEnabled => !GlobalContext.Data.IsVersionAtLeast(2023, 8);

    public DecompileContext(GlobalDecompileContext globalContext, UndertaleCode code, bool computeObject = true)
    {
        GlobalContext = globalContext;
        TargetCode = code;

        if (code.ParentEntry != null)
            throw new InvalidOperationException("This code block represents a function nested inside " + code.ParentEntry.Name + " - decompile that instead");

        if (computeObject && globalContext.Data is not null)
        {
            // TODO: This is expensive, move it somewhere else as a dictionary
            // and have it update when events/objects are modified.
                
            // Currently using for loops on purpose, as foreach has memory issues due to IEnumerable
            for (int i = 0; i < globalContext.Data.GameObjects.Count; i++)
            {
                UndertaleGameObject obj = globalContext.Data.GameObjects[i];
                for (int j = 0; j < obj.Events.Count; j++)
                {
                    var eventList = obj.Events[j];
                    for (int k = 0; k < eventList.Count; k++)
                    {
                        UndertaleGameObject.Event subEvent = eventList[k];
                        for (int l = 0; l < subEvent.Actions.Count; l++)
                        {
                            UndertaleGameObject.EventAction ev = subEvent.Actions[l];
                            if (ev.CodeId != code) continue;
                            Object = obj;
                            return;
                        }
                    }
                }
            }
        }
    }

    #region Struct management
    public List<Decompiler.Expression> ArgumentReplacements;
    public bool DecompilingStruct;
    #endregion

    #region Indentation management
    public const string Indent = "    ";
    private int _indentationLevel = 0;
    private string _indentation = "";

    public int IndentationLevel
    {
        get
        {
            return _indentationLevel;
        }
        set
        {
            _indentationLevel = value;

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < IndentationLevel; i++)
            {
                sb.Append(Indent);
            }
            _indentation = sb.ToString();
        }
    }
    public string Indentation => _indentation;
    #endregion

    #region Temp var management
    /// <summary>
    /// Maps a temp var to a place where it was created
    /// </summary>
    public Dictionary<string, Decompiler.TempVarAssignmentStatement> TempVarMap = new Dictionary<string, Decompiler.TempVarAssignmentStatement>();
    /// <summary>
    /// If used for auto-naming temp vars
    /// </summary>
    public int TempVarId { get; private set; }
    public Decompiler.AssignmentStatement CompilerTempVar;

    public Decompiler.TempVar NewTempVar()
    {
        return new Decompiler.TempVar(++TempVarId);
    }
    #endregion

    #region Local var management
    public HashSet<string> LocalVarDefines = new HashSet<string>();
    #endregion

    #region GMS 2.3+ Function management
    /// <summary>
    /// Set containing already-decompiled child code entries.
    /// Used to prevent decompiling the same child entry multiple times.
    /// Only applies to function entries, struct and constructors are unaffected.
    /// </summary>
    public ISet<UndertaleCode> AlreadyProcessed = new HashSet<UndertaleCode>();
    #endregion

    #region Asset type resolution
    /// <summary>
    /// Contains the resolved asset type for every variable
    /// </summary>
    public Dictionary<UndertaleVariable, AssetIDType> assetTypes = new Dictionary<UndertaleVariable, AssetIDType>();
    public Decompiler.DirectFunctionCall currentFunction; // TODO: clean up this hack
    #endregion

    #region Decompilation results
    /// <summary>
    /// Contains the result of decompiling this code block.
    /// This is a map from an entry point address to a list of statements.
    /// Needs to be here to access it in ToString for inline function definitions.
    /// </summary>
    public Dictionary<uint, List<Decompiler.Statement>> Statements { get; internal set; }
    #endregion

    /// <summary>
    /// Allows to disable the anonymous code name resolution to prevent recursion
    /// </summary>
    public bool DisableAnonymousFunctionNameResolution = false;
}