using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;
using static UndertaleModLib.Compiler.Compiler.AssemblyWriter;

namespace UndertaleModLib.Compiler
{
    public class CompileContext
    {
        public UndertaleData Data;
        public Dictionary<string, int> assetIds = new Dictionary<string, int>();
        public List<string> scripts = new List<string>();
        public Dictionary<string, VariableInfo> userDefinedVariables = new Dictionary<string, VariableInfo>();
        public bool ensureFunctionsDefined = true;
        public bool ensureVariablesDefined = true;
        public int LastCompiledArgumentCount = 0;
        public Dictionary<string, string> LocalVars = new Dictionary<string, string>();
        public Dictionary<string, string> GlobalVars = new Dictionary<string, string>();
        public Dictionary<string, Dictionary<string, int>> Enums = new Dictionary<string, Dictionary<string, int>>();
        public UndertaleCode OriginalCode;
        public BuiltinList BuiltInList;

        public bool SuccessfulCompile = false;
        public bool HasError = false;
        public string ResultError = null;
        public string ResultAssembly = null;

        public CompileContext(UndertaleData data, UndertaleCode oldCode)
        {
            Data = data;
            OriginalCode = oldCode;
        }

        public int GetAssetIndexByName(string name)
        {
            return assetIds.TryGetValue(name, out int val) ? val : -1;
        }

        public void OnSuccessfulFinish()
        {
            if (ensureVariablesDefined)
                foreach (KeyValuePair<string, string> v in GlobalVars)
                    Data?.Variables?.EnsureDefined(v.Key, UndertaleInstruction.InstanceType.Global, false, Data.Strings, Data);

            SuccessfulCompile = true;
        }

        public void SetError(string error)
        {
            HasError = true;
            ResultError = error;

            string[] split = error.Split('\n');
            StringBuilder sb = new StringBuilder();
            foreach (string line in split)
                sb.Append("; " + line + "\n");
            ResultAssembly = sb.ToString();
        }

        public void Setup(bool redoAssets = false)
        {
            SuccessfulCompile = false;
            HasError = false;
            ResultError = null;
            ResultAssembly = null;

            LastCompiledArgumentCount = 0;
            userDefinedVariables.Clear();
            if (redoAssets || assetIds.Count == 0)
                MakeAssetDictionary();
        }

        private void MakeAssetDictionary()
        {
            assetIds.Clear();
            AddAssetsFromList(Data?.GameObjects);
            AddAssetsFromList(Data?.Sprites);
            AddAssetsFromList(Data?.Sounds);
            AddAssetsFromList(Data?.Backgrounds);
            AddAssetsFromList(Data?.Paths);
            AddAssetsFromList(Data?.Fonts);
            AddAssetsFromList(Data?.Timelines);
            AddAssetsFromList(Data?.Scripts);
            AddAssetsFromList(Data?.Shaders);
            AddAssetsFromList(Data?.Rooms);
            AddAssetsFromList(Data?.AudioGroups);

            scripts.Clear();
            if (Data?.Scripts != null)
            {
                foreach (UndertaleScript s in Data.Scripts)
                {
                    scripts.Add(s.Name.Content);
                }
            }
            if (Data?.Extensions != null)
            {
                foreach (UndertaleExtension e in Data.Extensions)
                {
                    foreach (UndertaleExtensionFile file in e.Files)
                    {
                        foreach (UndertaleExtensionFunction func in file.Functions)
                        {
                            scripts.Add(func.Name.Content);
                        }
                    }
                }
            }
        }

        private void AddAssetsFromList<T>(IList<T> list) where T : UndertaleNamedResource
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
            {
                string name = list[i].Name?.Content;
                if (name != null)
                    assetIds[name] = i;
            }
        }
    }

    public static partial class Compiler
    {

        // A simple matching convenience
        public static bool In<T>(this T obj, params T[] args)
        {
            return args.Contains(obj);
        }

        public static CompileContext CompileGMLText(string input, UndertaleData data, UndertaleCode code)
        {
            return CompileGMLText(input, new CompileContext(data, code));
        }

        public static CompileContext CompileGMLText(string input, CompileContext context, bool redoAssets = false)
        {
            context.Setup(redoAssets); // Set up
            List<Lexer.Token> tokens = Lexer.LexString(context, input); // Peform lexical analysis
            Parser.Statement block = Parser.ParseTokens(context, tokens); // Parse tokens, make syntax tree

            // Optimize and process syntax tree
            Parser.Statement optimizedBlock = null;
            if (Parser.ErrorMessages.Count == 0)
                optimizedBlock = Parser.Optimize(context, block);

            // Handle errors from either function
            if (Parser.ErrorMessages.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Error{0} parsing code:", Parser.ErrorMessages.Count == 1 ? "" : "s");
                sb.AppendLine();
                sb.AppendLine();
                foreach (string msg in Parser.ErrorMessages)
                    sb.AppendLine(msg);
                context.SetError(sb.ToString());
                return context;
            }

            CodeWriter codeWriter = AssemblyWriter.AssembleStatement(context, optimizedBlock); // Write assembly code
            context.ResultAssembly = codeWriter.Finish();

            if (codeWriter.ErrorMessages.Count > 0)
            {
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("Error{0} writing assembly code:", codeWriter.ErrorMessages.Count == 1 ? "" : "s");
                sb.AppendLine();
                sb.AppendLine();
                foreach (string msg in codeWriter.ErrorMessages)
                    sb.AppendLine(msg);
                sb.AppendLine();
                sb.Append(context.ResultAssembly);
                context.SetError(sb.ToString());
                return context;
            }

            context.OnSuccessfulFinish();
            return context;
        }
    }
}
