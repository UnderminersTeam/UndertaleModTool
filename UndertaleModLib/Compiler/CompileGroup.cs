using System;
using System.Collections.Generic;
using Underanalyzer;
using Underanalyzer.Compiler;
using Underanalyzer.Compiler.Bytecode;
using Underanalyzer.Compiler.Errors;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Compiler;

/// <summary>
/// Context for managing groups of GML code compilation operations.
/// </summary>
public sealed class CompileGroup
{
    /// <summary>
    /// Associated game data for this compile context.
    /// </summary>
    public UndertaleData Data { get; }

    /// <summary>
    /// Associated global decompile context for this compile context.
    /// </summary>
    public GlobalDecompileContext GlobalContext { get; }

    /// <summary>
    /// This action will be called when main-thread operations should occur, and can be changed.
    /// </summary>
    public Action<Action> MainThreadAction { get; set; } = static (f) => f();

    /// <summary>
    /// Whether certain linking lookups (for strings, assets, etc.) should persist between multiple compiles.
    /// This should not be preferred: generally, multiple code operations should be queued instead.
    /// </summary>
    /// <remarks>
    /// This should be used with care. When enabled, no game data should be modified between each compile; otherwise,
    /// string or asset duplication may occur, which can corrupt the data permanently.
    /// </remarks>
    public bool PersistLinkingLookups { get; set; } = false;

    /// <summary>
    /// Stores a code entry and corresponding GML code to be compiled during an operation.
    /// </summary>
    private readonly record struct QueuedOperation(UndertaleCode CodeEntry, string Code);

    /// <summary>
    /// Stores a code entry, script, and function for a newly-created child code entry during linking.
    /// </summary>
    /// <remarks>
    /// Each component (code, script, and function) may individually be <see langword="null"/>.
    /// </remarks>
    private readonly record struct ChildCodeEntryData(
        string Name, FunctionEntry FunctionEntry,
        UndertaleCode Code, UndertaleScript Script, UndertaleFunction Function,
        bool ExistingCode, bool ExistingScript, bool ExistingFunction);

    // Queued list of code to replace the contents of
    private List<QueuedOperation> _queuedCodeReplacements = null;

    // During linking, a lookup of string contents to string IDs.
    private Dictionary<string, int> _linkingStringIdLookup = null;

    // During linking, a lookup of function names to functions.
    private Dictionary<string, UndertaleFunction> _linkingFunctionLookup = null;

    // During linking, a lookup of script names to scripts.
    private Dictionary<string, UndertaleScript> _linkingScriptLookup = null;

    // During linking, a unique number to use for struct variables.
    private int _linkingStructCounter = -1;

    // During linking, a lookup of all variable names that have been patched so far,
    // to the index they appear in the order of variable patches.
    private Dictionary<string, int> _linkingVariableOrderLookup = null;

    // During linking, a list of all variable names that have been patched so far,
    // along with whether they have ever been used as a local variable.
    private List<(string Name, bool IsEverLocal)> _linkingVariableOrder = null;

    // During linking, a lookup of local variable names to references of those local variables.
    private Dictionary<string, List<UndertaleInstruction.Reference<UndertaleVariable>>> _linkingLocalReferences = null;

    /// <summary>
    /// Initializes a new compile context.
    /// </summary>
    /// <remarks>
    /// If <paramref name="globalContext"/> is not provided, a new global context will be created.
    /// </remarks>
    public CompileGroup(UndertaleData data, GlobalDecompileContext globalContext = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        Data = data;
        GlobalContext = globalContext ?? new(data);
    }

    /// <summary>
    /// Queues a code replace operation on this compile context.
    /// </summary>
    /// <param name="codeToModify">Existing (root) code entry to modify.</param>
    /// <param name="gmlCode">GML source code to compile as replacement code.</param>
    public void QueueCodeReplace(UndertaleCode codeToModify, string gmlCode)
    {
        ArgumentNullException.ThrowIfNull(codeToModify);
        ArgumentNullException.ThrowIfNull(gmlCode);

        _queuedCodeReplacements ??= new();
        _queuedCodeReplacements.Add(new(codeToModify, gmlCode));
    }

    /// <summary>
    /// Looks up a string ID during linking.
    /// </summary>
    /// <returns><see langword="true"/> if an ID was successfully found; <see langword="false"/> otherwise.</returns>
    internal bool LookupStringId(string contents, out int id)
    {
        return _linkingStringIdLookup.TryGetValue(contents, out id);
    }

    /// <summary>
    /// Looks up a string, or creates one, during linking.
    /// </summary>
    internal UndertaleString MakeString(string contents)
    {
        // Try to look up existing string first
        if (_linkingStringIdLookup.TryGetValue(contents, out int id))
        {
            return Data.Strings[id];
        }

        // Create new string, and update lookup
        UndertaleString str = new(contents);
        int strIndex = Data.Strings.Count;
        _linkingStringIdLookup[contents] = strIndex;
        Data.Strings.Add(str);
        return str;
    }

    /// <summary>
    /// Looks up a string, or creates one, during linking. Also outputs the ID of the string.
    /// </summary>
    internal UndertaleString MakeString(string contents, out int id)
    {
        // Try to look up existing string first
        if (_linkingStringIdLookup.TryGetValue(contents, out id))
        {
            return Data.Strings[id];
        }

        // Create new string, and update lookup
        UndertaleString str = new(contents);
        id = Data.Strings.Count;
        _linkingStringIdLookup[contents] = id;
        Data.Strings.Add(str);
        return str;
    }

    /// <summary>
    /// Registers a non-local variable during linking.
    /// </summary>
    internal void RegisterNonLocalVariable(string name)
    {
        if (!_linkingVariableOrderLookup.ContainsKey(name))
        {
            // Add to variable order list, but not as a local
            _linkingVariableOrderLookup.Add(name, _linkingVariableOrder.Count);
            _linkingVariableOrder.Add((name, false));
        }
    }

    /// <summary>
    /// Registers a local variable during linking, queuing the reference to be patched later.
    /// </summary>
    internal void RegisterLocalVariable(UndertaleInstruction.Reference<UndertaleVariable> reference, string name)
    {
        // Queue reference to be patched later
        if (!_linkingLocalReferences.TryGetValue(name, out List<UndertaleInstruction.Reference<UndertaleVariable>> referenceList))
        {
            referenceList = new(16);
            _linkingLocalReferences[name] = referenceList;
        }
        referenceList.Add(reference);

        // Update variable order
        if (_linkingVariableOrderLookup.TryGetValue(name, out int existingIndex))
        {
            // If not already marked as "ever local," mark it as such now
            if (!_linkingVariableOrder[existingIndex].IsEverLocal)
            {
                _linkingVariableOrder[existingIndex] = (name, true);
            }
        }
        else
        {
            // Add to variable order list as a local
            _linkingVariableOrderLookup.Add(name, _linkingVariableOrder.Count);
            _linkingVariableOrder.Add((name, true));
        }
    }

    /// <summary>
    /// Defines a single code local variable.
    /// </summary>
    private void DefineCodeLocal(UndertaleCodeLocals locals, bool linkingModern, string name, uint currentLocalIndex)
    {
        UndertaleString str = MakeString(name, out int stringId);
        if (linkingModern)
        {
            // In 2.3+, local variables use variable ID rather than local index
            locals.Locals.Add(new UndertaleCodeLocals.LocalVar()
            {
                Index = (uint)stringId, // variable ID is based on string index
                Name = str
            });
        }
        else
        {
            // Prior to 2.3, local variables simply use local index
            locals.Locals.Add(new UndertaleCodeLocals.LocalVar()
            {
                Index = currentLocalIndex,
                Name = str
            });
        }
    }

    /// <summary>
    /// Returns whether the two code entry names are similar (that is, the original code entry
    /// name is identical aside from some numbers that the new name does not exactly match).
    /// </summary>
    private static bool SimilarCodeEntryNames(string originalName, string newNameNoNumbers)
    {
        int originalPos = 0;
        for (int i = 0; i < newNameNoNumbers.Length; i++)
        {
            if (newNameNoNumbers[i] == '%')
            {
                // Special control character: read any amount of digits from original name
                while (originalPos < originalName.Length && char.IsAsciiDigit(originalName[originalPos]))
                {
                    originalPos++;
                }
                continue;
            }
            if (newNameNoNumbers[i] == '#')
            {
                // Special control character: read any amount of hex digits from original name
                while (originalPos < originalName.Length && char.IsAsciiHexDigitUpper(originalName[originalPos]))
                {
                    originalPos++;
                }
                continue;
            }
            if (originalPos >= originalName.Length)
            {
                // New name is too long
                return false;
            }
            if (originalName[originalPos] != newNameNoNumbers[i])
            {
                // New name doesn't match somewhere other than through numbers
                return false;
            }
            originalPos++;
        }
        if (originalPos != originalName.Length)
        {
            // New name is too short
            return false;
        }

        // Everything seems to be similar enough!
        return true;
    }

    /// <summary>
    /// After verifying that an original code entry name and a new code entry name are similar,
    /// this method can be used to find the original short name from the original code entry name,
    /// being supplied the new short name without numbers.
    /// </summary>
    private static string FindOriginalShortName(string originalName, string newShortNameNoNumbers)
    {
        // Start reading after prefix
        int startOriginalPos = "gml_Script_".Length;
        int originalPos = startOriginalPos;
        for (int i = 0; i < newShortNameNoNumbers.Length; i++)
        {
            if (newShortNameNoNumbers[i] == '%')
            {
                // Special control character: read any amount of digits from original name
                while (originalPos < originalName.Length && char.IsAsciiDigit(originalName[originalPos]))
                {
                    originalPos++;
                }
                continue;
            }
            if (newShortNameNoNumbers[i] == '#')
            {
                // Special control character: read any amount of hex digits from original name
                while (originalPos < originalName.Length && char.IsAsciiHexDigitUpper(originalName[originalPos]))
                {
                    originalPos++;
                }
                continue;
            }
            if (originalPos < originalName.Length)
            {
                originalPos++;
            }
        }

        // Now, originalPos is placed at the end of the short name. Return substring from that location!
        return originalName[startOriginalPos..originalPos];
    }

    /// <summary>
    /// Performs all compilation operations that have been queued on this context.
    /// </summary>
    /// <remarks>
    /// Code replacement operations will be performed first, followed by code append operations.
    /// </remarks>
    public CompileResult Compile()
    {
        // Ensure global context is prepared for compilation
        GlobalContext.PrepareForCompilation(!PersistLinkingLookups);

        // List to use for any errors generated, but don't allocate memory unless needed
        List<CompileError> errors = null;

        // Version checks:
        // In 2.3 and above, new processes are used for linking in general.
        // In 2023.11 and above, a new naming process is used for code entries.
        // This was updated a bit in 2024.2 and 2024.4 as well.
        bool linkingModern = Data.IsVersionAtLeast(2, 3);
        bool newNamingProcess = Data.IsVersionAtLeast(2023, 11);
        bool staticAnonymousNames = Data.IsVersionAtLeast(2024, 2);
        bool childFunctionNameFix = Data.IsVersionAtLeast(2024, 4);

        // Work through replacement queue
        foreach (QueuedOperation operation in _queuedCodeReplacements)
        {
            // Guess script kind and global script name, based on code entry name
            (CompileScriptKind scriptKind, string globalScriptName) = GuessScriptKindFromName(operation.CodeEntry.Name?.Content);

            // Perform initial compile step
            CompileContext context = new(operation.Code, scriptKind, globalScriptName, GlobalContext);
            try
            {
                context.Compile();

                // Check for compile errors
                if (context.HasErrors)
                {
                    errors ??= new(context.Errors.Count);
                    foreach (ICompileError error in context.Errors)
                    {
                        errors.Add(new CompileError(operation.CodeEntry, error));
                    }
                }
            }
            catch (Exception e)
            {
                // Exception thrown; make compile error for it (and also get rid of context)
                context = null;
                errors ??= new();
                errors.Add(new CompileError(operation.CodeEntry, e));
            }

            // If any errors have occurred in general (even in other operations), don't proceed to linking
            if (errors is not null)
            {
                continue;
            }

            // Perform linking on main thread
            MainThreadAction(() =>
            {
                // Setup for linking
                InitializeLinkingLookups();
                GlobalContext.LinkingCompileGroup = this;

                // Make list of reusable child code entry names (and set of child entries remaining)
                List<string> originalChildEntryNames = new(operation.CodeEntry.ChildEntries.Count);
                Dictionary<string, UndertaleCode> remainingChildEntries = new(operation.CodeEntry.ChildEntries.Count);
                foreach (UndertaleCode childEntry in operation.CodeEntry.ChildEntries)
                {
                    string currentChildName = childEntry.Name.Content;
                    originalChildEntryNames.Add(currentChildName);
                    remainingChildEntries[currentChildName] = childEntry;
                }

                // Resolve function entries. Either pair up with existing child code entries, or make new ones.
                List<ChildCodeEntryData> childDataList = new(context.OutputFunctionEntries.Count);
                HashSet<string> usedChildEntryNames = new(operation.CodeEntry.ChildEntries.Count);
                List<(string Name, IGMFunction NewFunction, IGMFunction OldFunction)> newlyDefinedGlobalFunctions = new();
                string rootCodeEntryName = operation.CodeEntry.Name.Content;
                int anonCounter = 0;
                foreach (FunctionEntry functionEntry in context.OutputFunctionEntries)
                {
                    // Determine name of parent function, or null if none exists
                    string parentFunctionName = functionEntry.Parent?.ChildFunctionName;
                    if (scriptKind != CompileScriptKind.GlobalScript)
                    {
                        // Non-global scripts always use root code entry name as a base
                        parentFunctionName ??= rootCodeEntryName;
                    }

                    // Determine short name
                    string shortName, shortNameNoNumbers;
                    if (functionEntry.FunctionName is not null)
                    {
                        // Named function; trivial
                        shortName = functionEntry.FunctionName;
                        shortNameNoNumbers = shortName;
                    }
                    else if (functionEntry.Kind == FunctionEntryKind.StructInstantiation)
                    {
                        // Struct instantiation; create name
                        shortName = $"___struct___{_linkingStructCounter}";
                        shortNameNoNumbers = "___struct___%";
                    }
                    else
                    {
                        // Anonymous function; determine first part
                        string anonName;
                        if (staticAnonymousNames && functionEntry.StaticVariableName is not null)
                        {
                            anonName = $"{functionEntry.StaticVariableName}@anon";
                        }
                        else
                        {
                            anonName = "anon";
                        }

                        // Use other arbitrary information to differentiate this function from others
                        if (parentFunctionName is null)
                        {
                            if (newNamingProcess)
                            {
                                string rootWithoutGlobalScript = rootCodeEntryName.Replace("gml_GlobalScript_", "");
                                shortName = $"{anonName}@{anonCounter++}@{rootWithoutGlobalScript}";
                                shortNameNoNumbers = $"{anonName}@%@{rootWithoutGlobalScript}";
                            }
                            else
                            {
                                shortName = $"{anonName}_{operation.Code.GetHashCode():X8}_{anonCounter++}";
                                shortNameNoNumbers = $"{anonName}_#_%";
                            }
                        }
                        else
                        {
                            if (newNamingProcess)
                            {
                                shortName = $"{anonName}@{anonCounter++}";
                                shortNameNoNumbers = $"{anonName}@%";
                            }
                            else
                            {
                                shortName = $"{anonName}_{parentFunctionName}_{anonCounter++}";
                                shortNameNoNumbers = $"{anonName}_{parentFunctionName}_%";
                            }
                        }
                    }

                    // Determine full code entry name
                    string codeEntryName, codeEntryNameNoNumbers;
                    if (parentFunctionName is null)
                    {
                        codeEntryName = $"gml_Script_{shortName}";
                        codeEntryNameNoNumbers = $"gml_Script_{shortNameNoNumbers}";
                    }
                    else
                    {
                        if (newNamingProcess)
                        {
                            string parentWithoutGlobalScript = parentFunctionName.Replace("gml_GlobalScript_", "");
                            codeEntryName = $"gml_Script_{shortName}@{parentWithoutGlobalScript}";
                            codeEntryNameNoNumbers = $"gml_Script_{shortNameNoNumbers}@{parentWithoutGlobalScript}";
                        }
                        else
                        {
                            codeEntryName = $"gml_Script_{shortName}_{parentFunctionName}";
                            codeEntryNameNoNumbers = $"gml_Script_{shortNameNoNumbers}_{parentFunctionName}";
                        }
                    }

                    // Try to pair up with an existing child code entry, or otherwise create a new one
                    UndertaleCode existingCodeEntry = null, newCodeEntry = null;
                    UndertaleScript existingScript = null, newScript = null;
                    UndertaleFunction existingFunction = null, newFunction = null;
                    for (int i = 0; i < originalChildEntryNames.Count; i++)
                    {
                        string originalEntryName = originalChildEntryNames[i];
                        if (remainingChildEntries.TryGetValue(originalEntryName, out UndertaleCode originalCodeEntry) &&
                            SimilarCodeEntryNames(originalEntryName, codeEntryNameNoNumbers))
                        {
                            // Names are similar enough - use this existing child
                            codeEntryName = originalEntryName;
                            shortName = FindOriginalShortName(originalEntryName, shortNameNoNumbers);
                            existingCodeEntry = newCodeEntry = originalCodeEntry;
                            existingScript = newScript = _linkingScriptLookup[originalEntryName];
                            existingFunction = newFunction = _linkingFunctionLookup[originalEntryName];
                            remainingChildEntries.Remove(originalEntryName);
                            break;
                        }
                    }
                    if (existingCodeEntry is null)
                    {
                        // Verify that code entry name isn't used; if it is, as a contingency, add a suffix
                        if (usedChildEntryNames.Contains(codeEntryName))
                        {
                            int suffixId = 0;
                            string baseCodeEntryName = codeEntryName;
                            do
                            {
                                codeEntryName = $"{baseCodeEntryName}_{suffixId++}";
                            }
                            while (usedChildEntryNames.Contains(codeEntryName));
                        }

                        // Need to make a new code entry
                        newCodeEntry = new()
                        {
                            Name = MakeString(codeEntryName),
                            ParentEntry = operation.CodeEntry
                        };
                    }
                    if (existingScript is null)
                    {
                        // Need to make a new script entry
                        newScript = new()
                        {
                            Name = MakeString(codeEntryName),
                            Code = newCodeEntry
                        };
                    }
                    if (existingFunction is null)
                    {
                        // Need to make a new function entry
                        newFunction = new()
                        {
                            Name = MakeString(codeEntryName, out int id),
                            NameStringID = id
                        };
                    }

                    // Resolve function and child function name
                    string childFunctionName;
                    if (childFunctionNameFix)
                    {
                        childFunctionName = $"{shortName}@{parentFunctionName ?? rootCodeEntryName}";
                    }
                    else
                    {
                        childFunctionName = $"{shortName}_{parentFunctionName ?? rootCodeEntryName}";
                    }
                    functionEntry.ResolveFunction(newFunction, childFunctionName);

                    // Resolve final struct name
                    if (functionEntry.Kind == FunctionEntryKind.StructInstantiation)
                    {
                        functionEntry.ResolveStructName(shortName);

                        // If not using existing name, increment struct counter
                        if (existingCodeEntry is null)
                        {
                            _linkingStructCounter++;
                        }
                    }

                    // If this is a global function, define function name globally
                    if (scriptKind == CompileScriptKind.GlobalScript &&
                        functionEntry is { DeclaredInRootScope: true, FunctionName: not null })
                    {
                        Data.GlobalFunctions.TryGetFunction(shortName, out IGMFunction oldFunction);
                        Data.GlobalFunctions.DefineFunction(shortName, newFunction);
                        newlyDefinedGlobalFunctions.Add((shortName, newFunction, oldFunction));
                    }

                    // Add data to list for later addition to main data
                    usedChildEntryNames.Add(codeEntryName);
                    childDataList.Add(
                        new(
                            codeEntryName,
                            functionEntry,
                            newCodeEntry,
                            newScript,
                            newFunction,
                            existingCodeEntry is not null,
                            existingScript is not null,
                            existingFunction is not null
                        )
                    );
                }

                // TODO: maybe throw an error if there's any remaining child code entries that are also global functions

                // Create structures for linking local variables
                _linkingVariableOrderLookup ??= new(16);
                _linkingVariableOrder ??= new(16);
                _linkingLocalReferences ??= new(16);

                // Perform main link
                try
                {
                    context.Link();

                    // Check for link errors
                    if (context.HasErrors)
                    {
                        errors ??= new(context.Errors.Count);
                        foreach (ICompileError error in context.Errors)
                        {
                            errors.Add(new CompileError(operation.CodeEntry, error));
                        }
                    }
                }
                catch (Exception e)
                {
                    // Exception thrown; make compile error for it (and also get rid of context)
                    context = null;
                    errors ??= new();
                    errors.Add(new CompileError(operation.CodeEntry, e));
                }

                // Collect all local variable names, and resolve references (if not errored)
                List<string> localsOrder = null;
                if (errors is null)
                {
                    localsOrder = new(_linkingLocalReferences.Count);
                    ISet<UndertaleVariable> originalReferencedLocals = operation.CodeEntry.FindReferencedLocalVars();
                    foreach ((string name, bool isEverLocal) in _linkingVariableOrder)
                    {
                        if (isEverLocal)
                        {
                            // Add to local order
                            localsOrder.Add(name);

                            // Ensure local name string is generated
                            UndertaleString localString = MakeString(name, out int nameStringIndex);

                            // Get variable ID for this local (GM 2.3+ uses string index, prior uses local index)
                            int varId = linkingModern ? nameStringIndex : localsOrder.Count;

                            // Use existing registered variables if possible
                            UndertaleVariable variable = null;
                            foreach (UndertaleVariable referenced in originalReferencedLocals)
                            {
                                if (referenced.Name == localString && referenced.VarID == varId)
                                {
                                    variable = referenced;
                                    break;
                                }
                            }

                            // If not already referenced, define a new variable, and store in lookup
                            variable ??= Data.Variables.DefineLocal(Data, varId, localString, nameStringIndex);

                            // Update all references
                            foreach (UndertaleInstruction.Reference<UndertaleVariable> reference in _linkingLocalReferences[name])
                            {
                                reference.Target = variable;
                            }
                        }
                    }
                }

                // Clear out local variable structures for any further operations
                _linkingVariableOrderLookup.Clear();
                _linkingVariableOrder.Clear();
                _linkingLocalReferences.Clear();

                // Undo setup for linking
                GlobalContext.LinkingCompileGroup = null;

                // If any errors occurred, don't commit any further modifications to main data (avoid too much corruption)
                if (errors is not null)
                {
                    // Undefine global functions that were newly-defined (so they don't cause consistency issues)
                    foreach ((string functionName, IGMFunction newFunction, IGMFunction oldFunction) in newlyDefinedGlobalFunctions)
                    {
                        Data.GlobalFunctions.UndefineFunction(functionName, newFunction);

                        // Also restore old function if possible
                        if (oldFunction is not null)
                        {
                            Data.GlobalFunctions.DefineFunction(functionName, oldFunction);
                        }
                    }
                    return;
                }

                // Set max local variable count, if applicable
                uint codeLocalsCount = (uint)(1 + localsOrder.Count);
                if (codeLocalsCount > Data.MaxLocalVarCount)
                {
                    Data.MaxLocalVarCount = codeLocalsCount;
                }

                // Update code locals, if they exist
                if (Data.CodeLocals?.For(operation.CodeEntry) is UndertaleCodeLocals locals)
                {
                    // Clear out all existing locals
                    locals.Locals.Clear();

                    // Create "arguments" local
                    DefineCodeLocal(locals, linkingModern, "arguments", 0);

                    // Create new locals
                    uint codeLocalsIndex = 1;
                    foreach (string local in localsOrder)
                    {
                        DefineCodeLocal(locals, linkingModern, local, codeLocalsIndex);
                        codeLocalsIndex++;
                    }
                }

                // Remove old child code entries/scripts/functions
                foreach ((string name, UndertaleCode code) in remainingChildEntries)
                {
                    // Remove code entry
                    Data.Code.Remove(code);
                    operation.CodeEntry.ChildEntries.Remove(code);

                    // Remove script
                    if (_linkingScriptLookup.TryGetValue(name, out UndertaleScript script))
                    {
                        Data.Scripts.Remove(script);
                        _linkingScriptLookup.Remove(name);
                    }

                    // Remove function, as long as it's not a global function (since it could still be referenced)
                    if (_linkingFunctionLookup.TryGetValue(name, out UndertaleFunction function) &&
                        !Data.GlobalFunctions.FunctionExists(function))
                    {
                        Data.Functions.Remove(function);
                        _linkingFunctionLookup.Remove(name);
                    }
                }

                // Commit new child code entries
                int childIndex = 0;
                int rootCodeEntryIndex = -1;
                foreach (ChildCodeEntryData childData in childDataList)
                {
                    // Attach latest information to code entry
                    childData.Code.Length = (uint)context.OutputLength;
                    childData.Code.Offset = (uint)childData.FunctionEntry.BytecodeOffset;
                    childData.Code.ArgumentsCount = (ushort)childData.FunctionEntry.ArgumentCount;
                    childData.Code.LocalsCount = (uint)childData.FunctionEntry.Scope.LocalCount;

                    // Attach latest information to script
                    childData.Script.IsConstructor = childData.FunctionEntry.IsConstructor;

                    // Define code entry
                    if (!childData.ExistingCode)
                    {
                        operation.CodeEntry.ChildEntries.Insert(childIndex, childData.Code);
                        if (rootCodeEntryIndex == -1)
                        {
                            // Get index of root code entry only when required
                            rootCodeEntryIndex = Data.Code.IndexOf(operation.CodeEntry);
                        }
                        // TODO: this currently puts sub-functions below parent functions, but official behavior is the other way around
                        Data.Code.Insert(rootCodeEntryIndex + childIndex + 1, childData.Code);
                    }

                    // Define script
                    if (!childData.ExistingScript)
                    {
                        _linkingScriptLookup[childData.Name] = childData.Script;
                        Data.Scripts.Add(childData.Script);
                    }

                    // Define function
                    if (!childData.ExistingFunction)
                    {
                        _linkingFunctionLookup[childData.Name] = childData.Function;
                        Data.Functions.Add(childData.Function);
                    }

                    childIndex++;
                }

                // Commit instructions and other data to main code entry
                static IEnumerable<UndertaleInstruction> EnumerateInstructions(IReadOnlyList<IGMInstruction> instructionList)
                {
                    foreach (IGMInstruction instruction in instructionList)
                    {
                        yield return (UndertaleInstruction)instruction;
                    }
                }
                operation.CodeEntry.Replace(EnumerateInstructions(context.OutputInstructions));
                operation.CodeEntry.Length = (uint)context.OutputLength;
                operation.CodeEntry.Offset = 0;
                operation.CodeEntry.ArgumentsCount = 0;
                operation.CodeEntry.LocalsCount = (uint)(1 + context.OutputRootScope.LocalCount);
            });
        }

        // Clear queues
        _queuedCodeReplacements?.Clear();

        // Unreference other structures
        UnreferenceLinkingLookups();

        // Return result
        if (errors is null)
        {
            return CompileResult.SuccessfulResult;
        }
        return new CompileResult(false, errors);
    }

    /// <summary>
    /// Initializes linking lookup structures, if they have not already been.
    /// </summary>
    private void InitializeLinkingLookups()
    {
        // Create string -> ID lookup
        if (_linkingStringIdLookup is null)
        {
            _linkingStringIdLookup = new(Data.Strings.Count);
            for (int i = 0; i < Data.Strings.Count; i++)
            {
                if (Data.Strings[i]?.Content is string content)
                {
                    _linkingStringIdLookup[content] = i;
                }
            }
        }

        // Create function name -> function lookup
        if (_linkingFunctionLookup is null)
        {
            _linkingFunctionLookup = new(Data.Functions.Count);
            for (int i = 0; i < Data.Functions.Count; i++)
            {
                UndertaleFunction function = Data.Functions[i];
                if (function.Name?.Content is string name)
                {
                    _linkingFunctionLookup[name] = function;
                }
            }    
        }

        // Create script name -> script lookup
        if (_linkingScriptLookup is null)
        {
            _linkingScriptLookup = new(Data.Scripts.Count);
            for (int i = 0; i < Data.Scripts.Count; i++)
            {
                UndertaleScript script = Data.Scripts[i];
                if (script.Name?.Content is string name)
                {
                    _linkingScriptLookup[name] = script;
                }
            }
        }

        // Check for next usable struct ID
        if (_linkingStructCounter == -1)
        {
            _linkingStructCounter = 0;
            foreach (UndertaleVariable variable in Data.Variables)
            {
                if (variable.Name?.Content is string name)
                {
                    const string structPrefix = "___struct___";
                    if (name.StartsWith(structPrefix, StringComparison.Ordinal) && 
                        int.TryParse(name.AsSpan(structPrefix.Length), out int id))
                    {
                        _linkingStructCounter = Math.Max(_linkingStructCounter, id + 1);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Dereferences all internal linking structures to reduce potential memory usage.
    /// </summary>
    private void UnreferenceLinkingLookups()
    {
        if (!PersistLinkingLookups)
        {
            _linkingStringIdLookup = null;
            _linkingFunctionLookup = null;
            _linkingStructCounter = -1;
        }
        _linkingVariableOrderLookup = null;
        _linkingVariableOrder = null;
        _linkingLocalReferences = null;
    }

    /// <summary>
    /// Guesses the type of a script from a code entry's name.
    /// </summary>
    private static (CompileScriptKind ScriptKind, string GlobalScriptName) GuessScriptKindFromName(string codeName)
    {
        // If null, just assume script
        if (codeName is null)
        {
            return (CompileScriptKind.Script, null);
        }

        // Compare prefixes against known ones
        const string globalScriptPrefix = "gml_GlobalScript_";
        if (codeName.StartsWith(globalScriptPrefix, StringComparison.Ordinal))
        {
            // Output global script name as well
            return (CompileScriptKind.GlobalScript, codeName[globalScriptPrefix.Length..]);
        }
        if (codeName.StartsWith("gml_Script", StringComparison.Ordinal))
        {
            return (CompileScriptKind.Script, null);
        }
        if (codeName.StartsWith("gml_Object", StringComparison.Ordinal))
        {
            return (CompileScriptKind.ObjectEvent, null);
        }
        if (codeName.StartsWith("gml_Room", StringComparison.Ordinal))
        {
            return (CompileScriptKind.RoomCreationCode, null);
        }
        if (codeName.StartsWith("Timeline", StringComparison.Ordinal))
        {
            return (CompileScriptKind.Timeline, null);
        }

        // Unknown; default to script
        return (CompileScriptKind.Script, null);
    }
}
