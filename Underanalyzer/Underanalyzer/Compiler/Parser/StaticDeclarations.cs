/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Nodes;
using static Underanalyzer.IGMInstruction;

namespace Underanalyzer.Compiler.Parser;

/// <summary>
/// Helper to parse static variable declarations.
/// </summary>
internal static class StaticDeclarations
{
    /// <summary>
    /// Parses a static variable declaration from the given context's current position.
    /// </summary>
    public static void Parse(ParseContext context)
    {
        // Parse "static" keyword
        if (context.EnsureToken(KeywordKind.Static) is not TokenKeyword tokenKeyword)
        {
            return;
        }

        // Create static initializer block for the current scope if one doesn't exist yet
        if (!context.CurrentScope.IsFunction)
        {
            context.CompileContext.PushError("Cannot declare static variables outside of a function", tokenKeyword);
        }
        else
        {
            context.CurrentScope.StaticInitializerBlock ??= BlockNode.CreateEmpty(tokenKeyword, 8);
        }

        // Parse list of variables being declared
        while (!context.EndOfCode && context.Tokens[context.Position] is TokenVariable tokenVariable)
        {
            context.Position++;

            // Check if overriding a builtin variable
            // TODO: modern gamemaker versions probably allow this, and we can disable this check
            if (tokenVariable.BuiltinVariable is not null)
            {
                context.CompileContext.PushError($"Declaring local variable over builtin '{tokenVariable.Text}'", tokenVariable);
            }

            // Add to this scope's static list
            // TODO: check for duplicates and conflicts with named arguments/locals?
            context.CurrentScope.DeclareStatic(tokenVariable.Text);

            // Check for assignment
            if (context.IsCurrentToken(OperatorKind.Assign) || context.IsCurrentToken(OperatorKind.Assign2))
            {
                context.Position++;
                if (Expressions.ParseExpression(context) is IASTNode value)
                {
                    // Add assignment statement for this variable
                    if (context.CurrentScope.StaticInitializerBlock is BlockNode initBlock)
                    {
                        SimpleVariableNode newVariable = new(tokenVariable.Text, null);
                        newVariable.SetExplicitInstanceType(InstanceType.Static);
                        initBlock.Children.Add(new AssignNode(AssignNode.AssignKind.Normal, newVariable, value));
                    }
                }
                else
                {
                    // Failed to parse expression; stop parsing static var declaration
                    break;
                }
            }
            else
            {
                // No value getting assigned, which is not allowed
                context.CompileContext.PushError("Static variable declaration must assign an initial value", tokenVariable);
            }

            // If next token isn't a comma, we're done parsing the list
            if (!context.IsCurrentToken(SeparatorKind.Comma))
            {
                break;
            }

            // Otherwise, move past comma and keep parsing
            context.Position++;
        }
    }
}
