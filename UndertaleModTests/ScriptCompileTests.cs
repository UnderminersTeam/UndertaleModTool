using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using UndertaleModLib.Scripting;
using UndertaleModLib.Util;

namespace UndertaleModTests
{
    [TestClass]
    public class ScriptCompileTests
    {
        [TestMethod]
        public void ExportAllSoundsScriptCompiles()
        {
            string scriptPath = FindRepositoryFile(
                Path.Combine("UndertaleModTool", "Scripts", "Resource Exporters", "ExportAllSounds.csx"));
            string scriptText = $"#line 1 \"{scriptPath}\"{Environment.NewLine}" +
                                File.ReadAllText(scriptPath, Encoding.UTF8);

            var script = CSharpScript.Create<object>(
                scriptText,
                ScriptingUtil.CreateDefaultScriptOptions()
                             .WithFilePath(scriptPath)
                             .WithFileEncoding(Encoding.UTF8),
                typeof(IScriptInterface));

            Diagnostic[] errors = script.Compile()
                                      .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
                                      .ToArray();

            Assert.AreEqual(
                0,
                errors.Length,
                string.Join(Environment.NewLine, errors.Select(error => error.ToString())));
        }

        private static string FindRepositoryFile(string relativePath)
        {
            DirectoryInfo directory = new(AppContext.BaseDirectory);
            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, relativePath);
                if (File.Exists(candidate))
                    return candidate;

                directory = directory.Parent;
            }

            throw new FileNotFoundException($"Could not find repository file: {relativePath}");
        }
    }
}
