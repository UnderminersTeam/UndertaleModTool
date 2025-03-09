using System;
using System.Collections.Generic;
using System.Text;
using Underanalyzer.Compiler;
using Underanalyzer.Compiler.Errors;
using UndertaleModLib.Models;

namespace UndertaleModLib.Compiler;

/// <summary>
/// An error that occurred during compilation.
/// </summary>
public readonly struct CompileError
{
    // Original error produced by compiler (if present, otherwise null)
    private readonly ICompileError _sourceError = null;

    // Original exception produced by compiler (if present, otherwise null)
    private readonly Exception _exception = null;

    /// <summary>
    /// Code entry the error occurred in.
    /// </summary>
    public UndertaleCode Code { get; }

    /// <summary>
    /// Base, but possibly uninformative, error message.
    /// </summary>
    public string BaseMessage => _sourceError?.BaseMessage ?? _exception.Message;

    /// <summary>
    /// Generates a more detailed error message than <see cref="BaseMessage"/> (e.g., with more context).
    /// </summary>
    public string GenerateDetailedMessage()
    {
        if (_sourceError is not null)
        {
            return _sourceError.GenerateMessage();
        }
        if (_exception is CompilerException compilerException)
        {
            return compilerException.Message;
        }
        return $"Unexpected exception thrown: {_exception.Message}";
    }

    /// <summary>
    /// Creates a compile error struct with the given code entry and source error from the inner compiler.
    /// </summary>
    internal CompileError(UndertaleCode code, ICompileError sourceError)
    {
        Code = code;
        _sourceError = sourceError;
    }

    /// <summary>
    /// Creates a compile error struct with the given code entry and exception.
    /// </summary>
    internal CompileError(UndertaleCode code, Exception exception)
    {
        Code = code;
        _exception = exception;
    }
}

/// <summary>
/// Result of a compilation.
/// </summary>
public readonly struct CompileResult
{
    /// <summary>
    /// Whether all compilation operations were successful.
    /// </summary>
    public bool Successful { get; } = false;

    /// <summary>
    /// List of errors generated during compilation.
    /// </summary>
    /// <remarks>
    /// If <see cref="Successful"/> is <see langword="true"/>, this is <see langword="null"/>.
    /// </remarks>
    public IEnumerable<CompileError> Errors => _errors;

    /// <summary>
    /// A standard successful result instance.
    /// </summary>
    public static readonly CompileResult SuccessfulResult = new(true, null);

    /// <summary>
    /// A standard unsuccessful result instance, with no errors.
    /// </summary>
    public static readonly CompileResult UnsuccessfulResult = new(false, null);

    // List of errors generated during compilation, or null.
    private readonly List<CompileError> _errors = null;

    /// <summary>
    /// Creates a compile result struct with the given information.
    /// </summary>
    /// <param name="successful">Whether compilation was successful.</param>
    /// <param name="errors">If compilation was unsuccessful, the list of errors produced; otherwise, <see langword="null"/>.</param>
    internal CompileResult(bool successful, List<CompileError> errors)
    {
        Successful = successful;
        _errors = errors;
    }

    /// <summary>
    /// Combines two compile results together, including any potential errors.
    /// </summary>
    /// <param name="otherResult">Other compile result to combine with.</param>
    /// <returns>A new combined compile result.</returns>
    public CompileResult CombineWith(CompileResult otherResult)
    {
        if (!Successful || !otherResult.Successful)
        {
            List<CompileError> combinedErrors = new((_errors?.Count ?? 0) + (otherResult._errors?.Count ?? 0));
            if (_errors is not null)
            {
                combinedErrors.AddRange(_errors);
            }
            if (otherResult._errors is not null)
            {
                combinedErrors.AddRange(otherResult._errors);
            }
            return new CompileResult(false, combinedErrors);
        }
        return SuccessfulResult;
    }

    /// <summary>
    /// Helper method to print all errors into a single string, delimited by newlines.
    /// </summary>
    /// <param name="codeEntryNames">Whether to print code entry names along with each error.</param>
    /// <returns>String with all error messages.</returns>
    public string PrintAllErrors(bool codeEntryNames)
    {
        if (Errors is null)
        {
            return "(unknown errors occurred)";
        }

        StringBuilder sb = new(128);
        foreach (CompileError error in Errors)
        {
            if (codeEntryNames)
            {
                sb.Append(error.Code.Name?.Content);
                sb.Append(": ");
            }
            sb.Append(error.GenerateDetailedMessage());
            sb.AppendLine();
        }
        return sb.ToString();
    }
}
