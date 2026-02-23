/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Decompiler.AST;

namespace Underanalyzer.Decompiler.GameSpecific;

/// <summary>
/// Simple lookup for names to macro types.
/// </summary>
public class NameMacroTypeResolver : IMacroTypeResolver
{
    private Dictionary<string, IMacroType?> Variables { get; }
    private Dictionary<string, IMacroType?> FunctionArguments { get; }
    private Dictionary<string, IMacroType?> FunctionReturn { get; }

    /// <summary>
    /// Initializes an empty name resolver.
    /// </summary>
    public NameMacroTypeResolver()
    {
        Variables = [];
        FunctionArguments = [];
        FunctionReturn = [];
    }

    /// <summary>
    /// Initializes a name resolver with pre-populated data.
    /// </summary>
    public NameMacroTypeResolver(Dictionary<string, IMacroType?> variables, 
                                 Dictionary<string, IMacroType?> functionArguments, 
                                 Dictionary<string, IMacroType?> functionReturn)
    {
        Variables = new(variables);
        FunctionArguments = new(functionArguments);
        FunctionReturn = new(functionReturn);
    }

    /// <summary>
    /// Defines a variable's macro type for this resolver.
    /// </summary>
    public void DefineVariableType(string name, IMacroType? type)
    {
        Variables[name] = type;
    }

    /// <summary>
    /// Defines a function's arguments macro type for this resolver.
    /// </summary>
    public void DefineFunctionArgumentsType(string name, IMacroType? type)
    {
        FunctionArguments[name] = type;
    }

    /// <summary>
    /// Defines a function's return macro type for this resolver.
    /// </summary>
    public void DefineFunctionReturnType(string name, IMacroType? type)
    {
        FunctionReturn[name] = type;
    }

    public IMacroType? ResolveVariableType(ASTCleaner cleaner, string? variableName)
    {
        if (variableName is not null && Variables.TryGetValue(variableName, out IMacroType? macroType))
        {
            return macroType;
        }
        return null;
    }

    public IMacroType? ResolveFunctionArgumentTypes(ASTCleaner cleaner, string? functionName)
    {
        if (functionName is not null && FunctionArguments.TryGetValue(functionName, out IMacroType? macroType))
        {
            return macroType;
        }
        return null;
    }

    public IMacroType? ResolveReturnValueType(ASTCleaner cleaner, string? functionName)
    {
        if (functionName is not null && FunctionReturn.TryGetValue(functionName, out IMacroType? macroType))
        {
            return macroType;
        }
        return null;
    }
}