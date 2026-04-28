/*
  This Source Code Form is subject to the terms of the Mozilla Public
  License, v. 2.0. If a copy of the MPL was not distributed with this
  file, You can obtain one at https://mozilla.org/MPL/2.0/.
*/

using System;

namespace Underanalyzer.Compiler;

/// <summary>Represents errors that occur during compilation.</summary>
public class CompilerException : Exception
{
    
    /// <summary>Initializes a new instance of the <see cref="CompilerException" /> class with a specified error message.</summary>
    /// <param name="message">The message that describes the error.</param>
    public CompilerException(string message)
        : base(message)
    { }

    /// <summary>Initializes a new instance of the <see cref="CompilerException" /> class with a specified error message and
    /// a reference to the inner exception that is the cause of this exception.</summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="inner">The exception that is the cause of the current exception, or a null reference
    /// (<see langword="Nothing" /> in Visual Basic) if no inner exception is specified.</param>
    public CompilerException(string message, Exception inner)
        : base(message, inner)
    { }
}
