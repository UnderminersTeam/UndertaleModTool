using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Models;

namespace UndertaleModLib.Compiler
{
    public static partial class Compiler
    {
        private static UndertaleData data;
        private static Dictionary<string, int> assetIds = new Dictionary<string, int>();
        private static List<string> scripts = new List<string>();
        private static Dictionary<string, VariableInfo> userDefinedVariables = new Dictionary<string, VariableInfo>();
        private static bool ensureFunctionsDefined = true;
        private static bool ensureVariablesDefined = true;
        public static int LastCompiledArgumentCount = 0;
        public static bool SuccessfulCompile = false;
        public static Dictionary<string, string> LocalVars = new Dictionary<string, string>();
        public static Dictionary<string, string> GlobalVars = new Dictionary<string, string>();
        public static Dictionary<string, Dictionary<string, int>> Enums = new Dictionary<string, Dictionary<string, int>>();
        public static UndertaleCode OriginalCode;

        private static void AddAssetsFromList<T>(IList<T> list) where T : UndertaleNamedResource
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

        // A simple matching convenience
        public static bool In<T>(this T obj, params T[] args)
        {
            return args.Contains(obj);
        }

        private static void MakeAssetDictionary()
        {
            assetIds.Clear();
            AddAssetsFromList(data?.GameObjects);
            AddAssetsFromList(data?.Sprites);
            AddAssetsFromList(data?.Sounds);
            AddAssetsFromList(data?.Backgrounds);
            AddAssetsFromList(data?.Paths);
            AddAssetsFromList(data?.Fonts);
            AddAssetsFromList(data?.Timelines);
            AddAssetsFromList(data?.Scripts);
            AddAssetsFromList(data?.Shaders);
            AddAssetsFromList(data?.Rooms);
            AddAssetsFromList(data?.AudioGroups);

            scripts.Clear();
            if (data?.Scripts != null)
            {
                foreach (UndertaleScript s in data.Scripts)
                {
                    scripts.Add(s.Name.Content);
                }
            }
            if (data?.Extensions != null)
            {
                foreach (UndertaleExtension e in data.Extensions)
                {
                    foreach (UndertaleExtension.ExtensionFile file in e.Files)
                    {
                        foreach (UndertaleExtension.ExtensionFunction func in file.Functions)
                        {
                            scripts.Add(func.Name.Content);
                        }
                    }
                }
            }
        }

        private static int GetAssetIndexByName(string name)
        {
            if (assetIds.TryGetValue(name, out int val))
            {
                return val;
            }
            return -1;
        }

        public static void SetUndertaleData(UndertaleData data)
        {
            Compiler.data = data;
            MakeAssetDictionary();
        }

        public static void SetEnsureFunctionsDefined(bool val)
        {
            ensureFunctionsDefined = val;
        }

        public static void SetEnsureVariablesDefined(bool val)
        {
            ensureVariablesDefined = val;
        }

        public static string CompileGMLText(string input, UndertaleData data = null, UndertaleCode oldCode = null)
        {
            // Set up
            if (data != null)
                SetUndertaleData(data);
            Compiler.OriginalCode = oldCode;
            LastCompiledArgumentCount = 0;
            userDefinedVariables.Clear();

            // Peform lexical analysis
            List<Lexer.Token> tokens = Lexer.LexString(input);

            // Parse tokens, make syntax tree
            Parser.Statement block = Parser.ParseTokens(tokens);

            // Optimize and process syntax tree
            Parser.Statement optimizedBlock = null;
            if (Parser.ErrorMessages.Count == 0)
                optimizedBlock = Parser.Optimize(block);

            // Handle errors from either function
            if (Parser.ErrorMessages.Count > 0)
            {
                SuccessfulCompile = false;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("; Error{0} parsing code:", Parser.ErrorMessages.Count == 1 ? "" : "s");
                sb.AppendLine();
                sb.AppendLine("; ");
                foreach (string msg in Parser.ErrorMessages)
                {
                    sb.AppendFormat("; {0}", msg);
                    sb.AppendLine();
                }
                return sb.ToString();
            }

            // Write assembly code
            AssemblyWriter.Reset();
            string result = AssemblyWriter.GetAssemblyCodeFromStatement(optimizedBlock);

            if (AssemblyWriter.ErrorMessages.Count > 0)
            {
                SuccessfulCompile = false;
                StringBuilder sb = new StringBuilder();
                sb.AppendFormat("; Error{0} writing assembly code:", AssemblyWriter.ErrorMessages.Count == 1 ? "" : "s");
                sb.AppendLine();
                sb.AppendLine("; ");
                foreach (string msg in AssemblyWriter.ErrorMessages)
                {
                    sb.AppendFormat("; {0}", msg);
                    sb.AppendLine();
                }
                sb.AppendLine();
                sb.Append(result);
                return sb.ToString();
            }

            if (ensureVariablesDefined)
            {
                foreach (KeyValuePair<string, string> v in GlobalVars)
                {
                    data?.Variables?.EnsureDefined(v.Key, UndertaleInstruction.InstanceType.Global, false, data.Strings, data);
                }
            }

            SuccessfulCompile = true;
            return result;
        }
    }
}
