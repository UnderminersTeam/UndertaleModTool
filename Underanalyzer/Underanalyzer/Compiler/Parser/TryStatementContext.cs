/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

namespace Underanalyzer.Compiler.Parser;

/// <summary>
/// Class used to store data related to a specific try statement being post-processed.
/// </summary>
/// <remarks>
/// This emulates some buggy behavior of the official GMS2 GML compiler, intentionally.
/// </remarks>
internal sealed class TryStatementContext(string breakVariableName, string continueVariableName, bool hasFinally)
{
    /// <summary>
    /// Name of the variable used for break statements inside of the try statement.
    /// </summary>
    public string BreakVariableName { get; } = breakVariableName;

    /// <summary>
    /// Name of the variable used for break statements inside of the try statement.
    /// </summary>
    public string ContinueVariableName { get; } = continueVariableName;

    /// <summary>
    /// Whether the try statement has a finally block.
    /// </summary>
    public bool HasFinally { get; } = hasFinally;

    /// <summary>
    /// Whether a break/continue statement variable assignment was generated (dynamically changed).
    /// </summary>
    /// <remarks>
    /// This gets reset for both the try and catch blocks individually.
    /// </remarks>
    public bool HasBreakContinueVariable { get; set; } = false;

    /// <summary>
    /// Whether break/continue code should be generated. This is dynamically changed based on control flow.
    /// </summary>
    /// <remarks>
    /// This is bugged with while loops in the official compiler, which this replicates.
    /// Also, this only has any effect when <see cref="IGameContext.UsingBetterTryBreakContinue"/> is <see langword="true"/>.
    /// </remarks>
    public bool ShouldGenerateBreakContinueCode { get; set; } = true;

    /// <summary>
    /// Whether throw statements are allowed to generate extra code to handle finally.
    /// </summary>
    public bool ThrowFinallyGeneration { get; set; } = false;
}
