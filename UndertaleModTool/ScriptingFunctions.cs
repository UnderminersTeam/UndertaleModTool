using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using System.Security.Cryptography;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using System.Reflection;
using Newtonsoft.Json;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace UndertaleModTool
{
    // Adding misc. scripting functions here
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public string LintAllScripts()
        {
            string output = "";
            output += LintADir(Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CommunityScripts")));
            output += LintADir(Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Repackers")));
            output += LintADir(Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SampleScripts")));
            output += LintADir(Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TechnicalScripts")));
            output += LintADir(Directory.GetFiles(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Unpackers")));
            return output;
        }
        public string LintADir(string[] dirFiles)
        {
            string output = "";
            foreach (string file in dirFiles)
            {
                output += LintScript(file);
            }
            return output;
        }
        public string LintScript(string file1)
        {
            string output = "";
            using (StreamWriter sw = File.AppendText("C:/Users/USER/Desktop/Errors.txt"))
            {
                if (!(file1.EndsWith(".csx")))
                {
                    output = "Success: " + file1 + "\n";
                    sw.WriteLine(output);
                    return output;
                }
                try
                {
                    string messageResult = RunScriptLint(file1);
                    if (messageResult.Contains("CompilationErrorException"))
                        output = "Failed: " + file1 + "\n" + messageResult + "\n";
/*
                    else
                        output = "Success: " + file1 + "\n";
*/
                }
                catch (Exception e)
                {
                    output = "Failed: " + file1 + ": " + e.ToString() + "\n";
                }
                sw.WriteLine(output);
                return output;
            }
        }
        public string RunScriptLint(string path)
        {
            try
            {
                using (var loader = new InteractiveAssemblyLoader())
                {
                    loader.RegisterDependency(typeof(UndertaleObject).GetTypeInfo().Assembly);
                    loader.RegisterDependency(GetType().GetTypeInfo().Assembly);
                    loader.RegisterDependency(typeof(JsonConvert).GetTypeInfo().Assembly);

                    var script = CSharpScript.Create<object>(File.ReadAllText(path), ScriptOptions.Default
                        .AddImports("UndertaleModLib", "UndertaleModLib.Models", "UndertaleModLib.Decompiler", "UndertaleModLib.Scripting", "UndertaleModLib.Compiler")
                        .AddImports("UndertaleModTool", "System", "System.IO", "System.Collections.Generic", "System.Text.RegularExpressions")
                        .AddReferences(typeof(UndertaleObject).GetTypeInfo().Assembly)
                        .AddReferences(GetType().GetTypeInfo().Assembly)
                        .AddReferences(typeof(JsonConvert).GetTypeInfo().Assembly)
                        .AddReferences(typeof(System.Text.RegularExpressions.Regex).GetTypeInfo().Assembly),
                        typeof(IScriptInterface), loader);

                    ScriptPath = path;

                    object result = (script.RunAsync(this));
                    return (result != null ? result.ToString() : Path.GetFileName(path) + " finished!");
                }
            }
            catch (CompilationErrorException exc)
            {
                return exc.ToString();
            }
            catch (Exception exc)
            {
                return exc.ToString();
            }
        }
    }
}
