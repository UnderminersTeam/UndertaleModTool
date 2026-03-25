/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System.Collections.Generic;
using Underanalyzer.Compiler.Lexer;
using Underanalyzer.Compiler.Nodes;

namespace Underanalyzer.Compiler.Parser;

/// <summary>
/// Helper for parsing parts of function calls.
/// </summary>
internal static class Functions
{
    /// <summary>
    /// Parses argument list of a function call, returning a list of AST nodes for each argument, in order.
    /// </summary>
    public static List<IASTNode> ParseCallArguments(ParseContext context, int maxArgumentCount)
    {
        List<IASTNode> arguments = new(16);

        IToken? open = context.EnsureToken(SeparatorKind.GroupOpen);
        while (!context.EndOfCode && !context.IsCurrentToken(SeparatorKind.GroupClose))
        {
            // If there's a comma here, that means an argument is skipped (and should be "undefined")
            if (context.IsCurrentToken(SeparatorKind.Comma))
            {
                arguments.Add(SimpleVariableNode.CreateUndefined(context));
                context.Position++;
                continue;
            }

            // Parse current argument
            if (Expressions.ParseExpression(context) is IASTNode argument)
            {
                arguments.Add(argument);
            }
            else
            {
                // Failed to parse argument; stop parsing arguments
                break;
            }

            // If at end of code, stop here
            if (context.EndOfCode)
            {
                break;
            }

            // We expect either a comma (separating the arguments), or a group close
            if (context.IsCurrentToken(SeparatorKind.Comma))
            {
                context.Position++;
                continue;
            }

            // Should be a group close at this point
            if (!context.IsCurrentToken(SeparatorKind.GroupClose))
            {
                // Failed to find group end, so give error and stop parsing
                IToken currentToken = context.Tokens[context.Position];
                context.CompileContext.PushError(
                    $"Expected '{TokenSeparator.KindToString(SeparatorKind.Comma)}' or " +
                    $"'{TokenSeparator.KindToString(SeparatorKind.GroupClose)}', " +
                    $"got {currentToken}", currentToken);
                break;
            }
        }
        context.EnsureToken(SeparatorKind.GroupClose);

        // Ensure there's not too many arguments being passed in
        if (arguments.Count > maxArgumentCount)
        {
            context.CompileContext.PushError("Calling function with too many arguments", open);
        }

        return arguments;
    }
}
