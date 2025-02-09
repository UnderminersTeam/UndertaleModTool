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
	/// An extension method, that returns the element in a <see cref="List{T}"/> of <see cref="UndertaleNamedResource"/>s
	/// that has a specified <paramref name="name"/>.
	/// </summary>
	/// <param name="list">The <see cref="List{T}"/> of <see cref="UndertaleNamedResource"/>s to search in.</param>
	/// <param name="name">The name of the <see cref="UndertaleNamedResource"/> to find.</param>
	/// <param name="ignoreCase">Whether casing should be ignored for searching.</param>
	/// <typeparam name="T">A type of <see cref="UndertaleNamedResource"/>.</typeparam>
	/// <returns>The element that has the specified name.</returns>
	public static T ByName<T>(this IList<T> list, string name, bool ignoreCase = false) where T : UndertaleNamedResource
	{
		StringComparison comparisonType = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

		foreach (T item in list)
		{
			if (item.Name.Content.Equals(name, comparisonType))
				return item;
		}

		return default(T);
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
		if (content == null)
			throw new ArgumentNullException(nameof(content));

		if (!createNew)
		{
			// TODO: without reference counting the strings, this may leave unused strings in the array
			foreach (UndertaleString str in list)
			{
				if (str.Content == content)
					return str;
			}
		}

		UndertaleString newString = new UndertaleString(content);
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

	public static UndertaleFunction EnsureDefined(this IList<UndertaleFunction> list, string name, IList<UndertaleString> strg, bool fast = false)
	{
		UndertaleFunction func = fast ? null : list.ByName(name);
        if (func == null)
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

	public static UndertaleVariable EnsureDefined(this IList<UndertaleVariable> list, string name, UndertaleInstruction.InstanceType inst, bool isBuiltin, IList<UndertaleString> strg, UndertaleData data, bool fast = false)
	{
		if (inst == UndertaleInstruction.InstanceType.Local)
			throw new InvalidOperationException("Use DefineLocal instead");
		bool bytecode14 = (data?.GeneralInfo?.BytecodeVersion <= 14);
		if (bytecode14)
			inst = UndertaleInstruction.InstanceType.Undefined;
		UndertaleVariable vari = fast ? null : list.Where((x) => x.Name?.Content == name && x.InstanceType == inst).FirstOrDefault();
		if (vari == null)
		{
			var str = strg.MakeString(name, out int id);

			var oldId = data.VarCount1;
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
					oldId = (uint)id;
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

			vari = new UndertaleVariable()
			{
				Name = str,
				InstanceType = inst,
				VarID = bytecode14 ? 0 : (isBuiltin ? (int)UndertaleInstruction.InstanceType.Builtin : (int)oldId),
				NameStringID = id
			};
			list.Add(vari);
		}
		return vari;
	}

	public static UndertaleVariable DefineLocal(this IList<UndertaleVariable> list, IList<UndertaleVariable> originalReferencedLocalVars, int localId, string name, IList<UndertaleString> strg, UndertaleData data)
	{
		bool bytecode14 = data?.GeneralInfo?.BytecodeVersion <= 14;
		if (bytecode14 || data?.CodeLocals is null)
		{
			UndertaleVariable search = list.Where((x) =>
				x.Name.Content == name && (bytecode14 || x.InstanceType == UndertaleInstruction.InstanceType.Local)
				).FirstOrDefault();
			if (search != null)
				return search;
		}

		// Use existing registered variables.
		if (originalReferencedLocalVars != null)
		{
			UndertaleVariable refvar;
			if (data?.IsVersionAtLeast(2, 3) == true)
				refvar = originalReferencedLocalVars.Where((x) => x.Name.Content == name).FirstOrDefault();
			else
				refvar = originalReferencedLocalVars.Where((x) => x.Name.Content == name && x.VarID == localId).FirstOrDefault();
			if (refvar != null)
				return refvar;
		}

		var str = strg.MakeString(name, out int id);
		if (data?.IsVersionAtLeast(2, 3) == true)
			localId = id;
		UndertaleVariable vari = new UndertaleVariable()
		{
			Name = str,
			InstanceType = bytecode14 ? UndertaleInstruction.InstanceType.Undefined : UndertaleInstruction.InstanceType.Local,
			VarID = bytecode14 ? 0 : localId,
			NameStringID = id
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