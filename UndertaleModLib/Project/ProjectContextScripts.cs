using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;

namespace UndertaleModLib.Project;

partial class ProjectContext
{
    /// <summary>
    /// Script options for the project context, when initialized.
    /// </summary>
    private ScriptOptions _scriptOptions = null;

    /// <summary>
    /// Full path to currently-executing script, or <see langword="null"/> if no script is executing.
    /// </summary>
    private string _scriptPath = null;

    /// <summary>
    /// Initializes scripting for this context, if not already done.
    /// </summary>
    private void InitializeScripting()
    {
        if (!AllowScripts)
        {
            throw new ProjectException("Attempted to run scripts, which has been disallowed");
        }
        if (_scriptOptions is not null)
        {
            return;
        }
        _scriptOptions = ScriptingUtil.CreateDefaultScriptOptions();
    }

    /// <summary>
    /// Deinitializes scripting for this context, if necessary.
    /// </summary>
    private void DeinitializeScripting()
    {
        _scriptOptions = null;
    }

    /// <summary>
    /// Runs a list of scripts based on their relative paths to the project context's root.
    /// </summary>
    private void RunScriptList(List<string> relativePathList)
    {
        if (relativePathList.Count == 0)
        {
            return;
        }
        InitializeScripting();
        foreach (string relativePath in relativePathList)
        {
            // Get path to script file
            string path = Path.GetFullPath(Path.Join(MainDirectory, relativePath));
            Paths.VerifyWithinDirectory(MainDirectory, path);

            // Load script contents
            string scriptText;
            try
            {
                scriptText = $"#line 1 \"{path}\"\n" + File.ReadAllText(path, Encoding.UTF8);
            }
            catch (Exception e)
            {
                throw new ProjectException($"Failed to read script \"{relativePath}\": {e}", e);
            }

            // Evaluate script
            MainThreadAction(() =>
            {
                _scriptPath = path;
                try
                {
                    Task<object> task = CSharpScript.EvaluateAsync(scriptText, _scriptOptions.WithFilePath(path).WithFileEncoding(Encoding.UTF8), this, typeof(IScriptInterface));
                    task.Wait();
                }
                catch (CompilationErrorException e)
                {
                    throw new ProjectException($"Script \"{relativePath}\" failed to compile: {e}", e);
                }
                catch (AggregateException e)
                {
                    if (e.InnerExceptions is [Exception inner])
                    {
                        throw new ProjectException($"Script \"{relativePath}\" failed to run: {ScriptingUtil.PrettifyException(in inner)}", e);
                    }
                    Exception generalEx = e;
                    throw new ProjectException($"Script \"{relativePath}\" failed to run: {ScriptingUtil.PrettifyException(in generalEx)}", e);
                }
                catch (Exception e)
                {
                    throw new ProjectException($"Script \"{relativePath}\" failed to run: {ScriptingUtil.PrettifyException(in e)}", e);
                }
                finally
                {
                    _scriptPath = null;
                }
            });
        }
    }

    /// <summary>
    /// Executes all pre-import scripts, as defined in the project options.
    /// </summary>
    private void RunPreImportScripts()
    {
        RunScriptList(_mainOptions.PreImportScripts);
    }

    /// <summary>
    /// Executes all pre-asset import scripts, as defined in the project options.
    /// </summary>
    private void RunPreAssetImportScripts()
    {
        RunScriptList(_mainOptions.PreAssetImportScripts);
    }

    /// <summary>
    /// Executes all pre-import scripts, as defined in the project options.
    /// </summary>
    private void RunPostImportScripts()
    {
        RunScriptList(_mainOptions.PostImportScripts);
    }
}
