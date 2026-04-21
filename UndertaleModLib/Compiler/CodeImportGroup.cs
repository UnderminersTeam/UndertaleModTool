using System;
using System.Collections.Generic;
using Underanalyzer.Decompiler;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;

namespace UndertaleModLib.Compiler;

/// <summary>
/// Context for managing groups of GML code import operations. Higher level than <see cref="Compiler.CompileGroup"/>.
/// </summary>
/// <remarks>
/// This is mainly provided for basic scripting convenience. More complex operations may require more manual work.
/// </remarks>
public sealed class CodeImportGroup
{
    /// <summary>
    /// Associated game data for this import code context.
    /// </summary>
    public UndertaleData Data { get; }

    /// <summary>
    /// Associated global decompile context for this import code context.
    /// </summary>
    public GlobalDecompileContext GlobalContext { get; }

    /// <summary>
    /// Decompile settings to be used for any operations requiring decompilation.
    /// </summary>
    public IDecompileSettings DecompileSettings { get; }

    /// <summary>
    /// Whether this group will automatically create assets based on new code entry names, when necessary.
    /// </summary>
    /// <remarks>
    /// If a new code entry is created, and assets fail to be created due to ambiguity (particularly for
    /// object collision events), then an exception may be thrown. In those cases, code entries should 
    /// be manually resolved first, instead.
    /// </remarks>
    public bool AutoCreateAssets { get; set; } = true;

    /// <summary>
    /// Whether an exception will be thrown if a find and replace operation is a no-op.
    /// </summary>
    /// <remarks>
    /// <see langword="false"/> by default.
    /// </remarks>
    public bool ThrowOnNoOpFindReplace { get; set; } = false;

    /// <summary>
    /// This action will be called when main-thread operations should occur, and can be changed.
    /// </summary>
    public Action<Action> MainThreadAction { get; set; } = static (f) => f();

    /// <summary>
    /// Compile group being used by this context, if not null.
    /// </summary>
    internal CompileGroup CompileGroup { get; private set; } = null;

    /// <summary>
    /// Set of code entries currently queued for compile on <see cref="CompileGroup"/>.
    /// </summary>
    internal HashSet<UndertaleCode> CompileQueuedCodeEntries { get; } = new(8);

    /// <summary>
    /// List of operations queued for import.
    /// </summary>
    private readonly List<ICodeImportOperation> _queuedOperations = new(8);

    /// <summary>
    /// Initializes a new code import context.
    /// </summary>
    /// <remarks>
    /// If <paramref name="globalContext"/> is not provided, a new global context will be created.
    /// If <paramref name="decompileSettings"/> is not provided, default settings will be used.
    /// </remarks>
    public CodeImportGroup(UndertaleData data, GlobalDecompileContext globalContext = null, IDecompileSettings decompileSettings = null)
    {
        ArgumentNullException.ThrowIfNull(data);

        Data = data;
        GlobalContext = globalContext ?? new(data);
        DecompileSettings = decompileSettings ?? new DecompileSettings();
        CompileGroup = new(Data, GlobalContext)
        {
            MainThreadAction = MainThreadAction
        };
    }

    /// <summary>
    /// Helper to link an existing code entry to a specific object event, creating event/action structures as necessary.
    /// </summary>
    /// <param name="obj">Object to link to.</param>
    /// <param name="code">Code entry to link.</param>
    /// <param name="type">Main event type to link to.</param>
    /// <param name="subtype">Event subtype to link to.</param>
    /// <param name="mainThreadAction">Action to call when creating new data, which may require being changed on the main thread.</param>
    public static void LinkEvent(UndertaleGameObject obj, UndertaleCode code, EventType type, uint subtype, Action<Action> mainThreadAction)
    {
        UndertalePointerList<UndertaleGameObject.Event> eventList = obj.Events[(int)type];
        bool foundExisting = false;
        foreach (UndertaleGameObject.Event @event in eventList)
        {
            if (@event.EventSubtype != subtype)
            {
                // Not same subtype; skip
                continue;
            }

            if (@event.Actions is [UndertaleGameObject.EventAction action])
            {
                // Event already exists; reroute action to the new code entry.
                action.ActionName = code.Name;
                action.CodeId = code;
                foundExisting = true;
                break;
            }

            if (@event.Actions is [])
            {
                // Event already exists, but with no actions... create a new one.
                mainThreadAction(() => @event.Actions.Add(new()
                {
                    ActionName = code.Name,
                    CodeId = code
                }));
                foundExisting = true;
                break;
            }

            // This shouldn't be possible, normally...
            throw new Exception($"Multiple actions found for single event for \"{code.Name.Content}\"");
        }
        if (!foundExisting)
        {
            // Creating new event, as no existing one was found
            UndertaleGameObject.Event newEvent = new()
            {
                EventSubtype = subtype
            };
            mainThreadAction(() =>
            {
                newEvent.Actions.Add(new()
                {
                    ActionName = code.Name,
                    CodeId = code
                });
                eventList.Add(newEvent);
            });
        }
    }

    // Separators used for event name parsing.
    private const string EventScriptPrefix = "gml_Script_";
    private const string EventGlobalScriptPrefix = "gml_GlobalScript_";
    private const string EventObjectPrefix = "gml_Object_";
    private const string EventCollisionSeparator = "_Collision_";

    /// <summary>
    /// Attempts to parse the collision event subtype for the given code entry name.
    /// </summary>
    /// <returns>True if succeeded, false otherwise.</returns>
    private bool TryParseCollisionEvent(string codeEntryName, int collisionSeparatorIndex, out ReadOnlySpan<char> objectName, out uint subtype)
    {
        objectName = default;
        subtype = 0;

        // Only supports parsing in 2.3+, where objects are easily distinguished.
        if (!Data.IsVersionAtLeast(2, 3))
        {
            return false;
        }

        // Make sure only one separator exists.
        if (codeEntryName.IndexOf(EventCollisionSeparator, collisionSeparatorIndex + EventCollisionSeparator.Length - 1, StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        // Figure out object names, and make sure the one on the right-hand side already exists.
        objectName = codeEntryName.AsSpan(new Range(EventObjectPrefix.Length, collisionSeparatorIndex));
        if (objectName.Length <= 0)
        {
            return false;
        }
        ReadOnlySpan<char> otherObjectName = codeEntryName.AsSpan(collisionSeparatorIndex + EventCollisionSeparator.Length);
        int otherObjectIndex = Data.GameObjects.IndexOfName(otherObjectName);
        if (otherObjectIndex < 0)
        {
            return false;
        }

        // Parsed successfully - use object index as the event subtype.
        subtype = (uint)otherObjectIndex;
        return true;
    }

    /// <summary>
    /// Finds or creates a code entry, given a code entry name.
    /// </summary>
    private UndertaleCode FindOrCreateCodeEntry(string codeEntryName)
    {
        // Try to find existing
        if (Data.Code.ByName(codeEntryName) is UndertaleCode existing)
        {
            if (existing.ParentEntry is not null)
            {
                throw new Exception("Cannot import code into a child code entry");
            }
            return existing;
        }

        // Need to create a new entry!
        UndertaleString newEntryName = Data.Strings.MakeString(codeEntryName);
        UndertaleCode newEntry = UndertaleCode.CreateEmptyEntry(Data, newEntryName);

        // If auto-creating assets is disabled, exit early
        if (!AutoCreateAssets)
        {
            return newEntry;
        }

        // Also create assets based on code entry name
        if (codeEntryName.StartsWith(EventScriptPrefix, StringComparison.Ordinal))
        {
            MainThreadAction(() =>
            {
                ReadOnlySpan<char> scriptName = codeEntryName.AsSpan(EventScriptPrefix.Length);
                if (Data.Scripts.ByName(scriptName) is not UndertaleScript existingScript)
                {
                    // Create script, as one doesn't already exist
                    Data.Scripts.Add(new()
                    {
                        Name = Data.Strings.MakeString(scriptName.ToString()),
                        Code = newEntry
                    });
                }
                else
                {
                    // Attach to existing script asset (in case of corruption)
                    existingScript.Code = newEntry;
                }
            });
        }
        else if (codeEntryName.StartsWith(EventGlobalScriptPrefix, StringComparison.Ordinal))
        {
            // Scan to see if there's a global init script with a matching (and presumably, invalid) code reference
            UndertaleGlobalInit existingGlobalInit = null;
            foreach (UndertaleGlobalInit globalInit in Data.GlobalInitScripts)
            {
                if (globalInit.Code.Name.Content == codeEntryName)
                {
                    existingGlobalInit = globalInit;
                    break;
                }
            }
            if (existingGlobalInit is null)
            {
                // Create global init entry, if one doesn't already exist
                MainThreadAction(() =>
                {
                    Data.GlobalInitScripts.Add(new()
                    {
                        Code = newEntry
                    });
                });
            }
            else
            {
                // Attach to existing global init entry (in case of corruption)
                existingGlobalInit.Code = newEntry;
            }
        }
        else if (codeEntryName.StartsWith(EventObjectPrefix, StringComparison.Ordinal))
        {
            // Parse object event.
            // First, handle collision events, if applicable. (Only supported in GM 2.3+, when "_Collision_" is not in either object name.)
            int collisionSeparatorIndex = codeEntryName.IndexOf(EventCollisionSeparator, StringComparison.Ordinal);
            bool containsCollisionSeparator = collisionSeparatorIndex >= 0;
            if (containsCollisionSeparator && TryParseCollisionEvent(codeEntryName, collisionSeparatorIndex, out ReadOnlySpan<char> collisionObjectName, out uint collisionSubtype))
            {
                // Create new object if necessary.
                UndertaleGameObject collisionObj = Data.GameObjects.ByName(collisionObjectName);
                if (collisionObj is null)
                {
                    string collisionObjectNameStr = collisionObjectName.ToString();
                    MainThreadAction(() =>
                    {
                        collisionObj = new()
                        {
                            Name = Data.Strings.MakeString(collisionObjectNameStr)
                        };
                        Data.GameObjects.Add(collisionObj);
                    });
                }

                // Link code to object's event (and create one if necessary).
                LinkEvent(collisionObj, newEntry, EventType.Collision, collisionSubtype, MainThreadAction);

                // No more processing required.
                return newEntry;
            }

            // Not a collision event: Find positions of last two underscores in name.
            int lastUnderscore = codeEntryName.LastIndexOf('_');
            int secondLastUnderscore = codeEntryName.LastIndexOf('_', lastUnderscore - 1);
            if (lastUnderscore <= 0 || secondLastUnderscore <= 0)
            {
                if (containsCollisionSeparator)
                {
                    throw new Exception($"Collision event cannot be automatically resolved; must attach to object manually ({codeEntryName})");
                }
                throw new Exception($"Failed to parse object code entry name: \"{codeEntryName}\"");
            }

            // Extract object name, event type, and event subtype
            EventType eventType = codeEntryName.AsSpan(new Range(secondLastUnderscore + 1, lastUnderscore)) switch
            {
                "Create" => EventType.Create,
                "Destroy" => EventType.Destroy,
                "Alarm" => EventType.Alarm,
                "Step" => EventType.Step,
                "Collision" => throw new Exception($"Collision event cannot be automatically resolved; must attach to object manually ({codeEntryName})"),
                "Keyboard" => EventType.Keyboard,
                "Mouse" => EventType.Mouse,
                "Other" => EventType.Other,
                "Draw" => EventType.Draw,
                "KeyPress" => EventType.KeyPress,
                "KeyRelease" => EventType.KeyRelease,
                "Trigger" => EventType.Trigger,
                "CleanUp" => EventType.CleanUp,
                "Gesture" => EventType.Gesture,
                "PreCreate" => EventType.PreCreate,
                _ => containsCollisionSeparator ? 
                    throw new Exception($"Collision event cannot be automatically resolved; must attach to object manually ({codeEntryName})") :
                    throw new Exception($"Failed to parse object code entry name: \"{codeEntryName}\"")
            };
            if (!uint.TryParse(codeEntryName.AsSpan(lastUnderscore + 1), out uint eventSubtype))
            {
                throw new Exception($"Failed to parse object code entry name: \"{codeEntryName}\"");
            }

            // Create new object if necessary.
            ReadOnlySpan<char> objectName = codeEntryName.AsSpan(new Range(EventObjectPrefix.Length, secondLastUnderscore));
            UndertaleGameObject obj = Data.GameObjects.ByName(objectName);
            if (obj is null)
            {
                string objectNameStr = objectName.ToString();
                MainThreadAction(() =>
                {
                    obj = new()
                    {
                        Name = Data.Strings.MakeString(objectNameStr)
                    };
                    Data.GameObjects.Add(obj);
                });
            }

            // Link code to object's event (and create one if necessary).
            LinkEvent(obj, newEntry, eventType, eventSubtype, MainThreadAction);
        }

        return newEntry;
    }

    /// <summary>
    /// Decompiles an existing code entry to a string.
    /// </summary>
    internal string DecompileExistingCode(UndertaleCode code)
    {
        try
        {
            return new DecompileContext(GlobalContext, code, DecompileSettings).DecompileToString();
        }
        catch (DecompilerException ex)
        {
            throw new Exception($"Failed to decompile {code.Name?.Content}: {ex}");
        }
    }

    /// <summary>
    /// Queues a code replace operation on this code import context.
    /// </summary>
    /// <param name="codeToModify">Existing (root) code entry to modify.</param>
    /// <param name="gmlCode">GML source code to compile as replacement code.</param>
    public void QueueReplace(UndertaleCode codeToModify, string gmlCode)
    {
        ArgumentNullException.ThrowIfNull(codeToModify);
        ArgumentNullException.ThrowIfNull(gmlCode);

        _queuedOperations.Add(new CodeReplaceOperation(codeToModify, gmlCode));
    }

    /// <summary>
    /// Queues a code append operation on this code import context.
    /// </summary>
    /// <remarks>
    /// Upon importing, the original code entry will first be decompiled, 
    /// and then the code string will be appended (with a newline in between).
    /// </remarks>
    /// <param name="codeToModify">Existing (root) code entry to modify.</param>
    /// <param name="gmlCode">GML source code to append to a decompiled string of the original code.</param>
    public void QueueAppend(UndertaleCode codeToModify, string gmlCode)
    {
        ArgumentNullException.ThrowIfNull(codeToModify);
        ArgumentNullException.ThrowIfNull(gmlCode);

        _queuedOperations.Add(new CodeAppendOperation(codeToModify, gmlCode));
    }

    /// <summary>
    /// Queues a code prepend operation on this code import context.
    /// </summary>
    /// <remarks>
    /// Upon importing, the original code entry will first be decompiled, 
    /// and then the code string will be prepended (with a newline in between).
    /// </remarks>
    /// <param name="codeToModify">Existing (root) code entry to modify.</param>
    /// <param name="gmlCode">GML source code to prepend to a decompiled string of the original code.</param>
    public void QueuePrepend(UndertaleCode codeToModify, string gmlCode)
    {
        ArgumentNullException.ThrowIfNull(codeToModify);
        ArgumentNullException.ThrowIfNull(gmlCode);

        _queuedOperations.Add(new CodePrependOperation(codeToModify, gmlCode));
    }

    /// <summary>
    /// Queues a find and replace operation on this code import context.
    /// </summary>
    /// <param name="codeToModify">Existing (root) code entry to modify.</param>
    /// <param name="search">Code to search for in decompilation.</param>
    /// <param name="replacement">String to replace all occurrences of <paramref name="search"/> with.</param>
    /// <param name="caseSensitive">Whether the search should be case sensitive or not.</param>
    public void QueueFindReplace(UndertaleCode codeToModify, string search, string replacement, bool caseSensitive = true)
    {
        ArgumentNullException.ThrowIfNull(codeToModify);
        ArgumentException.ThrowIfNullOrEmpty(search);
        ArgumentNullException.ThrowIfNull(replacement);

        _queuedOperations.Add(new CodeFindReplaceOperation(codeToModify, search.ReplaceLineEndings("\n"), replacement, false, caseSensitive, false));
    }

    /// <summary>
    /// Queues a find and replace operation on this code import context, where string contents are compared on a line-by-line basis, 
    /// trimming whitespace at the beginning and end of each.
    /// </summary>
    /// <param name="codeToModify">Existing (root) code entry to modify.</param>
    /// <param name="search">Code to search for in decompilation.</param>
    /// <param name="replacement">String to replace all occurrences of <paramref name="search"/> with.</param>
    /// <param name="caseSensitive">Whether the search should be case sensitive or not.</param>
    public void QueueTrimmedLinesFindReplace(UndertaleCode codeToModify, string search, string replacement, bool caseSensitive = true)
    {
        ArgumentNullException.ThrowIfNull(codeToModify);
        ArgumentException.ThrowIfNullOrWhiteSpace(search);
        ArgumentNullException.ThrowIfNull(replacement);

        _queuedOperations.Add(new CodeFindReplaceOperation(codeToModify, search, replacement, false, caseSensitive, true));
    }

    /// <summary>
    /// Queues a regex find and replace operation on this code import context.
    /// </summary>
    /// <param name="codeToModify">Existing (root) code entry to modify.</param>
    /// <param name="search">Pattern to match against in decompilation.</param>
    /// <param name="replacement">String to replace all matches with.</param>
    /// <param name="caseSensitive">Whether the search should be case sensitive or not.</param>
    public void QueueRegexFindReplace(UndertaleCode codeToModify, string search, string replacement, bool caseSensitive = true)
    {
        ArgumentNullException.ThrowIfNull(codeToModify);
        ArgumentException.ThrowIfNullOrEmpty(search);
        ArgumentNullException.ThrowIfNull(replacement);

        _queuedOperations.Add(new CodeFindReplaceOperation(codeToModify, search, replacement, true, caseSensitive, false));
    }

    /// <summary>
    /// Queues a replace operation on this code import context.
    /// </summary>
    /// <param name="codeEntryName">Code entry that either already exists, or will be automatically created.</param>
    /// <param name="gmlCode">GML source code to compile as replacement code.</param>
    public void QueueReplace(string codeEntryName, string gmlCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(codeEntryName);
        ArgumentNullException.ThrowIfNull(gmlCode);

        QueueReplace(FindOrCreateCodeEntry(codeEntryName), gmlCode);
    }

    /// <summary>
    /// Queues a code append operation on this code import context.
    /// </summary>
    /// <remarks>
    /// Upon importing, the original code entry will first be decompiled, 
    /// and then the code string will be appended (with a newline in between).
    /// </remarks>
    /// <param name="codeEntryName">Code entry that either already exists, or will be automatically created.</param>
    /// <param name="gmlCode">GML source code to append to a decompiled string of the original code.</param>
    public void QueueAppend(string codeEntryName, string gmlCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(codeEntryName);
        ArgumentNullException.ThrowIfNull(gmlCode);

        QueueAppend(FindOrCreateCodeEntry(codeEntryName), gmlCode);
    }

    /// <summary>
    /// Queues a code prepend operation on this code import context.
    /// </summary>
    /// <remarks>
    /// Upon importing, the original code entry will first be decompiled, 
    /// and then the code string will be prepended (with a newline in between).
    /// </remarks>
    /// <param name="codeEntryName">Code entry that either already exists, or will be automatically created.</param>
    /// <param name="gmlCode">GML source code to prepend to a decompiled string of the original code.</param>
    public void QueuePrepend(string codeEntryName, string gmlCode)
    {
        ArgumentException.ThrowIfNullOrEmpty(codeEntryName);
        ArgumentNullException.ThrowIfNull(gmlCode);

        QueuePrepend(FindOrCreateCodeEntry(codeEntryName), gmlCode);
    }

    /// <summary>
    /// Queues a find and replace operation on this code import context.
    /// </summary>
    /// <param name="codeEntryName">Code entry that either already exists, or will be automatically created.</param>
    /// <param name="search">Code to search for in decompilation.</param>
    /// <param name="replacement">String to replace all occurrences of <paramref name="search"/> with.</param>
    /// <param name="caseSensitive">Whether the search should be case sensitive or not.</param>
    public void QueueFindReplace(string codeEntryName, string search, string replacement, bool caseSensitive = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(codeEntryName);
        ArgumentException.ThrowIfNullOrEmpty(search);
        ArgumentNullException.ThrowIfNull(replacement);

        QueueFindReplace(FindOrCreateCodeEntry(codeEntryName), search.ReplaceLineEndings("\n"), replacement, caseSensitive);
    }

    /// <summary>
    /// Queues a find and replace operation on this code import context, where string contents are compared on a line-by-line basis, 
    /// trimming whitespace at the beginning and end of each.
    /// </summary>
    /// <param name="codeEntryName">Code entry that either already exists, or will be automatically created.</param>
    /// <param name="search">Code to search for in decompilation.</param>
    /// <param name="replacement">String to replace all occurrences of <paramref name="search"/> with.</param>
    /// <param name="caseSensitive">Whether the search should be case sensitive or not.</param>
    public void QueueTrimmedLinesFindReplace(string codeEntryName, string search, string replacement, bool caseSensitive = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(codeEntryName);
        ArgumentException.ThrowIfNullOrWhiteSpace(search);
        ArgumentNullException.ThrowIfNull(replacement);

        QueueTrimmedLinesFindReplace(FindOrCreateCodeEntry(codeEntryName), search, replacement, caseSensitive);
    }

    /// <summary>
    /// Queues a regex find and replace operation on this code import context.
    /// </summary>
    /// <param name="codeEntryName">Code entry that either already exists, or will be automatically created.</param>
    /// <param name="search">Pattern to match against in decompilation.</param>
    /// <param name="replacement">String to replace all matches with.</param>
    /// <param name="caseSensitive">Whether the search should be case sensitive or not.</param>
    public void QueueRegexFindReplace(string codeEntryName, string search, string replacement, bool caseSensitive = true)
    {
        ArgumentException.ThrowIfNullOrEmpty(codeEntryName);
        ArgumentException.ThrowIfNullOrEmpty(search);
        ArgumentNullException.ThrowIfNull(replacement);

        QueueRegexFindReplace(FindOrCreateCodeEntry(codeEntryName), search, replacement, caseSensitive);
    }

    /// <summary>
    /// Performs all queued import operations, in order.
    /// </summary>
    /// <param name="throwOnFailedCompile">Whether to throw an exception on a failed compile.</param>
    /// <returns>Result from compilation.</returns>
    public CompileResult Import(bool throwOnFailedCompile = true)
    {
        // Use a lower-level compile group with persisted linking lookups (as this may require multiple passes)
        CompileResult result = CompileResult.SuccessfulResult;
        try
        {
            CompileGroup = new(Data, GlobalContext)
            {
                PersistLinkingLookups = true,
                MainThreadAction = MainThreadAction
            };
            foreach (ICodeImportOperation operation in _queuedOperations)
            {
                // Force a compile pass if code entry is repeated
                if (CompileQueuedCodeEntries.Contains(operation.CodeEntry))
                {
                    CompileQueuedCodeEntries.Clear();
                    result = result.CombineWith(CompileGroup.Compile());
                }

                // Perform actual import
                CompileQueuedCodeEntries.Add(operation.CodeEntry);
                operation.Import(this);
            }
        }
        catch
        {
            // Clear queued code entries that were processed, when an exception is thrown
            CompileQueuedCodeEntries.Clear();
            throw;
        }
        finally
        {
            // Clear queue
            _queuedOperations.Clear();
        }

        // Perform final compile if required
        if (CompileQueuedCodeEntries.Count > 0)
        {
            CompileQueuedCodeEntries.Clear();
            result = result.CombineWith(CompileGroup.Compile());
        }

        // Get rid of compile group, as it is no longer needed
        CompileGroup = null;

        // Handle errors
        if (!result.Successful)
        {
            if (throwOnFailedCompile)
            {
                throw new Exception("Compile errors occurred during code import:\n" + result.PrintAllErrors(true));
            }
            return result;
        }

        // Return compile result
        return result;
    }
}
