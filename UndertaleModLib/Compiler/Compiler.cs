using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UndertaleModLib.Models;
using static UndertaleModLib.Compiler.Compiler.AssemblyWriter;
using static UndertaleModLib.Util.AssetReferenceTypes;

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
        public static bool GMS2_3;
        public bool BooleanTypeEnabled => Data.IsVersionAtLeast(2, 3, 7);
        public bool TypedAssetRefs => Data.IsVersionAtLeast(2023, 8);
        public int LastCompiledArgumentCount = 0;
        public Dictionary<string, string> LocalVars = new Dictionary<string, string>();
        public Dictionary<string, string> GlobalVars = new Dictionary<string, string>(); 
        public Stack<Compiler.Parser.FunctionParseInfo> FunctionParseStack = new();
        public Dictionary<Compiler.Parser.Statement, Compiler.Parser.FunctionParseInfo> FunctionParseInfo = new();
        public List<(string, Compiler.Parser.Statement)> EnumStatements = new List<(string, Compiler.Parser.Statement)>();
        public Dictionary<string, Dictionary<string, long?>> Enums = new Dictionary<string, Dictionary<string, long?>>();
        public bool FirstPassResolvingEnums = false;
        public UndertaleCode OriginalCode;
        public IList<UndertaleVariable> OriginalReferencedLocalVars;
        public BuiltinList BuiltInList => Data.BuiltinList;

        public bool SuccessfulCompile = false;
        public bool HasError = false;
        public string ResultError = null;
        public List<UndertaleInstruction> ResultAssembly = null;

        public List<string> FunctionsToObliterate = new();

        public Compiler.MainThreadDelegate MainThreadDelegate = (f) => { f(); };

        public CompileContext(UndertaleData data, UndertaleCode oldCode)
        {
            Data = data;
            OriginalCode = oldCode;
            OriginalReferencedLocalVars = OriginalCode?.FindReferencedLocalVars();
        }

        /// <summary>
        /// Returns the asset index (including encoded reference type, if applicable) of a given identifier,
        /// or -1 if no asset/reference is found.
        /// </summary>
        public int GetAssetIndexByName(string name)
        {
            // Look up asset names
            if (assetIds.TryGetValue(name, out int val))
            {
                return val;
            }

            // Handle named instance IDs
            string instanceIdPrefix = Data.ToolInfo.InstanceIdPrefix();
            if (name.StartsWith(instanceIdPrefix, StringComparison.InvariantCulture))
            {
                if (int.TryParse(name[instanceIdPrefix.Length..], out int id) && id >= 100000)
                {
                    if (TypedAssetRefs)
                    {
                        // Add type to ID
                        id = (id & 0xffffff) | ((ConvertFromRefType(Data, RefType.RoomInstance) & 0x7f) << 24);
                    }
                    return id;
                }
            }

            // Nothing found
            return -1;
        }

        public void OnSuccessfulFinish()
        {
            if (ensureVariablesDefined)
            {
                MainThreadDelegate.Invoke(() =>
                {
                    foreach (KeyValuePair<string, string> v in GlobalVars)
                        Data?.Variables?.EnsureDefined(v.Key, UndertaleInstruction.InstanceType.Global, false, Data.Strings, Data);
                    if (Data is not null)
                    {
                        foreach (string name in FunctionsToObliterate)
                        {
                            string scriptName = "gml_Script_" + name;
                            UndertaleScript scriptObj = Data.Scripts.ByName(scriptName);
                            if (scriptObj is not null)
                                Data.Scripts.Remove(scriptObj);
                            UndertaleCode codeObj = Data.Code.ByName(scriptName);
                            if (codeObj is not null)
                            {
                                Data.Code.Remove(codeObj);
                                OriginalCode.ChildEntries.Remove(codeObj);
                            }
                            UndertaleFunction functionObj = Data.Functions.ByName(scriptName);
                            if (functionObj is not null)
                                Data.Functions.Remove(functionObj);
                            Data.GlobalFunctions.NameToFunction.Remove(name);
                            Data.GlobalFunctions.FunctionToName.Remove(functionObj);
                        }
                        FunctionsToObliterate.Clear();
                    }
                });
            }

            SuccessfulCompile = true;
        }

        public void SetError(string error)
        {
            HasError = true;
            ResultError = error;
        }

        public void Setup(bool redoAssets = false)
        {
            SuccessfulCompile = false;
            HasError = false;
            ResultError = null;
            ResultAssembly = null;

            LastCompiledArgumentCount = 0;
            userDefinedVariables.Clear();
            FunctionsToObliterate.Clear();
            if (redoAssets || assetIds.Count == 0)
                MakeAssetDictionary();
        }

        private void MakeAssetDictionary()
        {
            // Clear the dictionary first and set the worst case max size so that we don't resize it over and over
            assetIds.Clear();
            scripts.Clear();
            if (Data is null) return;
            
            int maxSize = 0;
            maxSize += Data.GameObjects?.Count ?? 0;
            maxSize += Data.Sprites?.Count ?? 0;
            maxSize += Data.Sounds?.Count ?? 0;
            maxSize += Data.Backgrounds?.Count ?? 0;
            maxSize += Data.Paths?.Count ?? 0;
            maxSize += Data.Fonts?.Count ?? 0;
            maxSize += Data.Timelines?.Count ?? 0;
            maxSize += Data.Scripts?.Count ?? 0;
            maxSize += Data.Shaders?.Count ?? 0;
            maxSize += Data.Rooms?.Count ?? 0;
            maxSize += Data.AudioGroups?.Count ?? 0;
            maxSize += Data.AnimationCurves?.Count ?? 0;
            maxSize += Data.Sequences?.Count ?? 0;
            maxSize += Data.ParticleSystems?.Count ?? 0;
            
            assetIds.EnsureCapacity(maxSize);
            scripts.EnsureCapacity(Data.Scripts?.Count ?? 0);

            AddAssetsFromList(Data.GameObjects, RefType.Object);
            AddAssetsFromList(Data.Sprites, RefType.Sprite);
            AddAssetsFromList(Data.Sounds, RefType.Sound);
            AddAssetsFromList(Data.Backgrounds, RefType.Background);
            AddAssetsFromList(Data.Paths, RefType.Path);
            AddAssetsFromList(Data.Fonts, RefType.Font);
            AddAssetsFromList(Data.Timelines, RefType.Timeline);
            if (!GMS2_3)
                AddAssetsFromList(Data.Scripts, RefType.Script /* not actually used */);
            AddAssetsFromList(Data.Shaders, RefType.Shader);
            AddAssetsFromList(Data.Rooms, RefType.Room);
            AddAssetsFromList(Data.AudioGroups, RefType.Sound /* apparently? */);
            AddAssetsFromList(Data.AnimationCurves, RefType.AnimCurve);
            AddAssetsFromList(Data.Sequences, RefType.Sequence);
            AddAssetsFromList(Data.ParticleSystems, RefType.ParticleSystem);

            if (Data.Scripts is not null)
            {
                foreach (UndertaleScript s in Data.Scripts)
                {
                    scripts.Add(s.Name.Content);
                }
            }
            if (Data.Extensions is not null)
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

        private void AddAssetsFromList<T>(IList<T> list, RefType type) where T : UndertaleNamedResource
        {
            if (list == null)
                return;
            if (TypedAssetRefs)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    string name = list[i].Name?.Content;
                    if (name != null)
                    {
                        // Typed asset refs pack their type into the ID
                        assetIds[name] = (i & 0xffffff) | ((ConvertFromRefType(Data, type) & 0x7f) << 24);
                    }
                }
            }
            else
            {
                for (int i = 0; i < list.Count; i++)
                {
                    string name = list[i].Name?.Content;
                    if (name != null)
                        assetIds[name] = i;
                }
            }
        }
    }

    public static partial class Compiler
    {
        public delegate void MainThreadFunc();
        public delegate void MainThreadDelegate(MainThreadFunc f);

        // A simple matching convenience
        public static bool In<T>(this T obj, params T[] args)
        {
            return args.Contains(obj);
        }

        public static CompileContext CompileGMLText(string input, UndertaleData data, UndertaleCode code, MainThreadDelegate mainThreadDelegate)
        {
            var ctx = new CompileContext(data, code);
            ctx.MainThreadDelegate = mainThreadDelegate;
            return CompileGMLText(input, ctx);
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
                sb.AppendFormat("Error{0} parsing code when trying to compile modified \"{1}\":", Parser.ErrorMessages.Count == 1 ? "" : "s", context.OriginalCode?.Name?.Content);
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
                sb.AppendFormat("Error{0} writing assembly code when trying to compile modified \"{1}\":", codeWriter.ErrorMessages.Count == 1 ? "" : "s", context.OriginalCode?.Name?.Content);
                sb.AppendLine();
                sb.AppendLine();
                foreach (string msg in codeWriter.ErrorMessages)
                    sb.AppendLine(msg);
                context.SetError(sb.ToString());
                return context;
            }

            context.OnSuccessfulFinish();
            return context;
        }
    }
}
