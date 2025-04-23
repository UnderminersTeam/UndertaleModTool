using System;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib.Models;

namespace UndertaleModLib;

/// <summary>
/// Extension methods for <see cref="UndertaleData"/>.
/// </summary>
public static class UndertaleDataExtensionMethods
{
    /// <summary>
    /// An extension method, that returns the element in a <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s
    /// that has a specified <paramref name="name"/>.
    /// </summary>
    /// <param name="list">The <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s to search in.</param>
    /// <param name="name">The name of the <see cref="UndertaleNamedResource"/> to find.</param>
    /// <param name="ignoreCase">Whether casing should be ignored for searching.</param>
    /// <typeparam name="T">A type of <see cref="UndertaleNamedResource"/>.</typeparam>
    /// <returns>The element that has the specified name.</returns>
    public static T ByName<T>(this IList<T> list, string name, bool ignoreCase = false) where T : UndertaleNamedResource
    {
        StringComparison comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (T item in list)
        {
            if (item is null)
            {
                continue;
            }
            if (item.Name.Content.Equals(name, comparisonType))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    /// An extension method, that returns the index of an element in a <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s
    /// that has a specified <paramref name="name"/>.
    /// </summary>
    /// <param name="list">The <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s to search in.</param>
    /// <param name="name">The name of the <see cref="UndertaleNamedResource"/> to find.</param>
    /// <param name="ignoreCase">Whether casing should be ignored for searching.</param>
    /// <typeparam name="T">A type of <see cref="UndertaleNamedResource"/>.</typeparam>
    /// <returns>The index of the element that has the specified name, or -1 if none is found.</returns>
    public static int IndexOfName<T>(this IList<T> list, string name, bool ignoreCase = false) where T : UndertaleNamedResource
    {
        StringComparison comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is not T item)
            {
                continue;
            }
            if (item.Name.Content.Equals(name, comparisonType))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// An extension method, that returns the element in a <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s
    /// that has a specified <paramref name="name"/>.
    /// </summary>
    /// <param name="list">The <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s to search in.</param>
    /// <param name="name">The name of the <see cref="UndertaleNamedResource"/> to find.</param>
    /// <param name="ignoreCase">Whether casing should be ignored for searching.</param>
    /// <typeparam name="T">A type of <see cref="UndertaleNamedResource"/>.</typeparam>
    /// <returns>The element that has the specified name.</returns>
    public static T ByName<T>(this IList<T> list, ReadOnlySpan<char> name, bool ignoreCase = false) where T : UndertaleNamedResource
    {
        StringComparison comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        foreach (T item in list)
        {
            if (item is null)
            {
                continue;
            }
            if (item.Name.Content.AsSpan().Equals(name, comparisonType))
            {
                return item;
            }
        }

        return default;
    }

    /// <summary>
    /// An extension method, that returns the index of an element in a <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s
    /// that has a specified <paramref name="name"/>.
    /// </summary>
    /// <param name="list">The <see cref="IList{T}"/> of <see cref="UndertaleNamedResource"/>s to search in.</param>
    /// <param name="name">The name of the <see cref="UndertaleNamedResource"/> to find.</param>
    /// <param name="ignoreCase">Whether casing should be ignored for searching.</param>
    /// <typeparam name="T">A type of <see cref="UndertaleNamedResource"/>.</typeparam>
    /// <returns>The index of the element that has the specified name, or -1 if none is found.</returns>
    public static int IndexOfName<T>(this IList<T> list, ReadOnlySpan<char> name, bool ignoreCase = false) where T : UndertaleNamedResource
    {
        StringComparison comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i] is not T item)
            {
                continue;
            }
            if (item.Name.Content.AsSpan().Equals(name, comparisonType))
            {
                return i;
            }
        }

        return -1;
    }

    public static UndertaleCodeLocals For(this IList<UndertaleCodeLocals> list, UndertaleCode code)
    {
        // TODO: I'm not sure if the runner looks these up by name or by index
        return list.FirstOrDefault(x => code.Name == x.Name);
    }

    /// <summary>
    /// Creates <paramref name="content"/> as a new <see cref="UndertaleString"/>,
    /// adds it to a <see cref="List{T}"/> of <see cref="UndertaleString"/> if it does not exist yet, and returns it.
    /// </summary>
    /// <param name="list">The <see cref="List{T}"/> of <see cref="UndertaleString"/>.</param>
    /// <param name="content">The string to create a <see cref="UndertaleString"/> of.</param>
    /// <param name="createNew">Whether to create a new <see cref="UndertaleString"/> if the one with the same content exists.</param>
    /// <returns><paramref name="content"/> as a <see cref="UndertaleString"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is null.</exception>
    public static UndertaleString MakeString(this IList<UndertaleString> list, string content, bool createNew = false)
    {
        ArgumentNullException.ThrowIfNull(content);

        // Search through all existing strings if desired
        if (!createNew)
        {
            // TODO: without reference counting the strings, this may leave unused strings in the array
            foreach (UndertaleString str in list)
            {
                if (str.Content == content)
                {
                    return str;
                }
            }
        }

        // Create a brand-new string
        UndertaleString newString = new(content);
        list.Add(newString);
        return newString;
    }

    /// <summary>
    /// Creates <paramref name="content"/> as a new <see cref="UndertaleString"/>,
    /// adds it to a <see cref="List{T}"/> of <see cref="UndertaleString"/> if it does not exist yet, and returns it.
    /// </summary>
    /// <param name="list">The <see cref="List{T}"/> of <see cref="UndertaleString"/>.</param>
    /// <param name="content">The string to create a <see cref="UndertaleString"/> of.</param>
    /// <param name="index">The index where the newly created <see cref="UndertaleString"/> is located in <paramref name="list"/>.</param>
    /// <returns><paramref name="content"/> as a <see cref="UndertaleString"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="content"/> is null.</exception>
    public static UndertaleString MakeString(this IList<UndertaleString> list, string content, out int index)
    {
        if (content == null)
            throw new ArgumentNullException(nameof(content));

        // TODO: without reference counting the strings, this may leave unused strings in the array
        for (int i = 0; i < list.Count; i++)
        {
            UndertaleString str = list[i];
            if (str.Content == content)
            {
                index = i;
                return str;
            }
        }

        UndertaleString newString = new UndertaleString(content);
        index = list.Count;
        list.Add(newString);
        return newString;
    }

    public static UndertaleFunction EnsureDefined(this IList<UndertaleFunction> list, string name, IList<UndertaleString> strg)
    {
        UndertaleFunction func = list.ByName(name);
        if (func is null)
        {
            var str = strg.MakeString(name, out int id);
            func = new UndertaleFunction()
            {
                Name = str,
                NameStringID = id
            };
            list.Add(func);
        }
        return func;
    }

    public static UndertaleVariable EnsureDefined(this IList<UndertaleVariable> list, UndertaleString nameString, int nameStringId, UndertaleInstruction.InstanceType inst, bool isBuiltin, UndertaleData data)
    {
        // Local variables are defined distinctly
        if (inst == UndertaleInstruction.InstanceType.Local)
        {
            throw new InvalidOperationException("Use DefineLocal instead");
        }

        // Handle builtin variables always using "self"
        if (isBuiltin)
        {
            inst = UndertaleInstruction.InstanceType.Self;
        }

        // Handle bytecode 14 differences
        bool bytecode14 = data.GeneralInfo.BytecodeVersion <= 14;
        if (bytecode14)
        {
            // In bytecode 14, instance types are always undefined for some reason
            inst = UndertaleInstruction.InstanceType.Undefined;
        }

        // Search for existing variable that can be used
        foreach (UndertaleVariable variable in list)
        {
            if (variable.Name == nameString && variable.InstanceType == inst)
            {
                return variable;
            }
        }

        // Otherwise, make a new variable. Update variables counts first.
        uint oldId = data.VarCount1;
        if (!bytecode14)
        {
            if (data.IsVersionAtLeast(2, 3))
            {
                // GMS 2.3+
                if (!isBuiltin)
                {
                    data.VarCount1++;
                    data.VarCount2 = data.VarCount1;
                }
                oldId = (uint)nameStringId;
            }
            else if (!data.DifferentVarCounts)
            {
                // Bytecode 16+
                data.VarCount1++;
                data.VarCount2++;
            }
            else
            {
                // Bytecode 15
                if (inst == UndertaleInstruction.InstanceType.Self && !isBuiltin)
                {
                    oldId = data.VarCount2;
                    data.VarCount2++;
                }
                else if (inst == UndertaleInstruction.InstanceType.Global)
                {
                    data.VarCount1++;
                }
            }
        }

        // Actually create new variable
        UndertaleVariable newVariable = new()
        {
            Name = nameString,
            InstanceType = inst,
            VarID = bytecode14 ? 0 : (isBuiltin ? (int)UndertaleInstruction.InstanceType.Builtin : (int)oldId),
            NameStringID = nameStringId
        };
        list.Add(newVariable);

        return newVariable;
    }

    public static UndertaleVariable DefineLocal(this IList<UndertaleVariable> list, UndertaleData data, int varId, UndertaleString nameString, int nameStringId)
    {
        // In bytecode 14, look up on entire variable list for existing locals...
        bool bytecode14 = data.GeneralInfo.BytecodeVersion <= 14;
        if (bytecode14)
        {
            foreach (UndertaleVariable variable in list)
            {
                if (variable.Name == nameString && (bytecode14 || variable.InstanceType == UndertaleInstruction.InstanceType.Local))
                {
                    return variable;
                }
            }
        }

        // Define new local
        UndertaleVariable vari = new()
        {
            Name = nameString,
            InstanceType = bytecode14 ? UndertaleInstruction.InstanceType.Undefined : UndertaleInstruction.InstanceType.Local,
            VarID = bytecode14 ? 0 : varId,
            NameStringID = nameStringId
        };
        list.Add(vari);
        return vari;
    }

    public static UndertaleExtensionFunction DefineExtensionFunction(this IList<UndertaleExtensionFunction> extfuncs, IList<UndertaleFunction> funcs, IList<UndertaleString> strg, uint id, uint kind, string name, UndertaleExtensionVarType rettype, string extname, params UndertaleExtensionVarType[] args)
    {
        var func = new UndertaleExtensionFunction()
        {
            ID = id,
            Name = strg.MakeString(name),
            ExtName = strg.MakeString(extname),
            Kind = kind,
            RetType = rettype
        };
        foreach(var a in args)
            func.Arguments.Add(new UndertaleExtensionFunctionArg() { Type = a });
        extfuncs.Add(func);
        funcs.EnsureDefined(name, strg);
        return func;
    }
}