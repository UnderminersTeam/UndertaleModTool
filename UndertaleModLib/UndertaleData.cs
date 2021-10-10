﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    public class UndertaleData
    {
        public UndertaleChunkFORM FORM;

        public UndertaleGeneralInfo GeneralInfo => FORM.GEN8?.Object;
        public UndertaleOptions Options => FORM.OPTN?.Object;
        public UndertaleLanguage Language => FORM.LANG?.Object;
        public IList<UndertaleExtension> Extensions => FORM.EXTN?.List;
        public IList<UndertaleSound> Sounds => FORM.SOND?.List;
        public IList<UndertaleAudioGroup> AudioGroups => FORM.AGRP?.List;
        public IList<UndertaleSprite> Sprites => FORM.SPRT?.List;
        public IList<UndertaleBackground> Backgrounds => FORM.BGND?.List;
        public IList<UndertalePath> Paths => FORM.PATH?.List;
        public IList<UndertaleScript> Scripts => FORM.SCPT?.List;
        public IList<UndertaleGlobalInit> GlobalInitScripts => FORM.GLOB?.List;
        public IList<UndertaleGlobalInit> GameEndScripts => FORM.GMEN?.List;
        public IList<UndertaleShader> Shaders => FORM.SHDR?.List;
        public IList<UndertaleFont> Fonts => FORM.FONT?.List;
        public IList<UndertaleTimeline> Timelines => FORM.TMLN?.List;
        public IList<UndertaleGameObject> GameObjects => FORM.OBJT?.List;
        public IList<UndertaleRoom> Rooms => FORM.ROOM?.List;
        //[Obsolete("Unused")]
        // DataFile
        public IList<UndertaleTexturePageItem> TexturePageItems => FORM.TPAG?.List;
        public IList<UndertaleCode> Code => FORM.CODE?.List;
        public IList<UndertaleVariable> Variables => FORM.VARI?.List;
        public uint VarCount1 { get => FORM.VARI.VarCount1; set => FORM.VARI.VarCount1 = value; }
        public uint VarCount2 { get => FORM.VARI.VarCount2; set => FORM.VARI.VarCount2 = value; }
        public bool DifferentVarCounts { get => FORM.VARI.DifferentVarCounts; set => FORM.VARI.DifferentVarCounts = value; }
        [Obsolete]
        public uint InstanceVarCount { get => VarCount1; set => VarCount1 = value; }
        [Obsolete]
        public uint InstanceVarCountAgain { get => VarCount2; set => VarCount2 = value; }
        public uint MaxLocalVarCount { get => FORM.VARI.MaxLocalVarCount; set => FORM.VARI.MaxLocalVarCount = value; }
        public IList<UndertaleFunction> Functions => FORM.FUNC?.Functions;
        public IList<UndertaleCodeLocals> CodeLocals => FORM.FUNC?.CodeLocals;
        public IList<UndertaleString> Strings => FORM.STRG?.List;
        public IList<UndertaleEmbeddedImage> EmbeddedImages => FORM.EMBI?.List;
        public IList<UndertaleEmbeddedTexture> EmbeddedTextures => FORM.TXTR?.List;
        public IList<UndertaleTextureGroupInfo> TextureGroupInfo => FORM.TGIN?.List;
        public IList<UndertaleEmbeddedAudio> EmbeddedAudio => FORM.AUDO?.List;

        public UndertaleTags Tags => FORM.TAGS?.Object;
        public IList<UndertaleAnimationCurve> AnimationCurves => FORM.ACRV?.List;
        public IList<UndertaleSequence> Sequences => FORM.SEQN?.List;

        public bool UnsupportedBytecodeVersion = false;
        public bool IsTPAG4ByteAligned = false;
        public bool ShortCircuit = true;
        public bool GMS2_2_2_302 = false;
        public bool GMS2_3 = false;
        public bool GMS2_3_1 = false;
        public bool GMS2_3_2 = false;
        public ToolInfo ToolInfo = new ToolInfo();
        public int PaddingAlignException = -1;

        public BuiltinList BuiltinList;
        public Dictionary<string, UndertaleFunction> KnownSubFunctions; // Cache for known 2.3-style function names for compiler speedups. Can be re-built by setting this to null.

        public UndertaleNamedResource ByName(string name, bool ignoreCase = false)
        {
            // TODO: Check if those are all possible types
            return Sounds.ByName(name, ignoreCase) ??
                Sprites.ByName(name, ignoreCase) ??
                Backgrounds.ByName(name, ignoreCase) ??
                Paths.ByName(name, ignoreCase) ??
                Scripts.ByName(name, ignoreCase) ??
                Fonts.ByName(name, ignoreCase) ??
                GameObjects.ByName(name, ignoreCase) ??
                Rooms.ByName(name, ignoreCase) ??
                Extensions.ByName(name, ignoreCase) ??
                Shaders.ByName(name, ignoreCase) ??
                Timelines.ByName(name, ignoreCase) ??
                AnimationCurves?.ByName(name, ignoreCase) ??
                Sequences?.ByName(name, ignoreCase) ??
                AudioGroups?.ByName(name, ignoreCase) ??
                (UndertaleNamedResource)null;
        }

        public int IndexOf(UndertaleNamedResource obj, bool panicIfInvalid = true)
        {
            if (obj is UndertaleSound)
                return Sounds.IndexOf(obj as UndertaleSound);
            if (obj is UndertaleSprite)
                return Sprites.IndexOf(obj as UndertaleSprite);
            if (obj is UndertaleBackground)
                return Backgrounds.IndexOf(obj as UndertaleBackground);
            if (obj is UndertalePath)
                return Paths.IndexOf(obj as UndertalePath);
            if (obj is UndertaleScript)
                return Scripts.IndexOf(obj as UndertaleScript);
            if (obj is UndertaleFont)
                return Fonts.IndexOf(obj as UndertaleFont);
            if (obj is UndertaleGameObject)
                return GameObjects.IndexOf(obj as UndertaleGameObject);
            if (obj is UndertaleRoom)
                return Rooms.IndexOf(obj as UndertaleRoom);
            if (obj is UndertaleExtension)
                return Extensions.IndexOf(obj as UndertaleExtension);
            if (obj is UndertaleShader)
                return Shaders.IndexOf(obj as UndertaleShader);
            if (obj is UndertaleTimeline)
                return Timelines.IndexOf(obj as UndertaleTimeline);
            if (obj is UndertaleAnimationCurve)
                return AnimationCurves.IndexOf(obj as UndertaleAnimationCurve);
            if (obj is UndertaleSequence)
                return Sequences.IndexOf(obj as UndertaleSequence);
            if (obj is UndertaleEmbeddedAudio)
                return EmbeddedAudio.IndexOf(obj as UndertaleEmbeddedAudio);
            if (obj is UndertaleEmbeddedTexture)
                return EmbeddedTextures.IndexOf(obj as UndertaleEmbeddedTexture);
            if (obj is UndertaleTexturePageItem)
                return TexturePageItems.IndexOf(obj as UndertaleTexturePageItem);
            if (obj is UndertaleAudioGroup)
                return AudioGroups.IndexOf(obj as UndertaleAudioGroup);

            if (panicIfInvalid)
                throw new InvalidOperationException();
            return -2;
        }

        internal int IndexOfByName(string line)
        {
            throw new NotImplementedException();
        }

        // Test if this data.win was built by GameMaker Studio 2.
        public bool IsGameMaker2()
        {
            return IsVersionAtLeast(2, 0, 0, 0);
        }


        // Old Versions: https://store.yoyogames.com/downloads/gm-studio/release-notes-studio-old.html
        // https://web.archive.org/web/20150304025626/https://store.yoyogames.com/downloads/gm-studio/release-notes-studio.html
        // Early Access: https://web.archive.org/web/20181002232646/http://store.yoyogames.com:80/downloads/gm-studio-ea/release-notes-studio.html
        public bool TestGMS1Version(uint stableBuild, uint betaBuild, bool allowGMS2 = false)
        {
            return (allowGMS2 || !IsGameMaker2()) && (IsVersionAtLeast(1, 0, 0, stableBuild) || (IsVersionAtLeast(1, 0, 0, betaBuild) && !IsVersionAtLeast(1, 0, 0, 1000)));
        }

        public bool IsVersionAtLeast(uint major, uint minor, uint release, uint build)
        {
            if (GeneralInfo.Major != major)
                return (GeneralInfo.Major > major);

            if (GeneralInfo.Minor != minor)
                return (GeneralInfo.Minor > minor);

            if (GeneralInfo.Release != release)
                return (GeneralInfo.Release > release);

            if (GeneralInfo.Build != build)
                return (GeneralInfo.Build > build);

            return true; // The version is exactly what supplied.
        }

        public int GetBuiltinSoundGroupID()
        {
            // It is known it works this way in 1.0.1266. The exact version which changed this is unknown.
            // If we find a game which does not fit the version identified here, we should fix this check.
            return TestGMS1Version(1354, 161, true) ? 0 : 1;
        }

        public bool IsYYC()
        {
            return GeneralInfo != null && Code == null;
        }

        public uint ExtensionFindLastId()
        {
            // The reason:
            // Extension function id is literally the index of it in the Runner internal lists
            // It must never overlap
            // So, a good helper is needed.

            uint id = 1; // first Id is always one, I checked.
            foreach (var extn in this.Extensions)
            {
                foreach (var file in extn.Files)
                {
                    foreach (var func in file.Functions)
                    {
                        if (func.ID > id)
                        {
                            id = func.ID;
                        }
                    }
                }
            }

            id++; // last id that *we* can use, so increment by one.
            return id;
        }

        public static UndertaleData CreateNew()
        {
            UndertaleData data = new UndertaleData();
            data.FORM = new UndertaleChunkFORM();
            data.FORM.Chunks["GEN8"] = new UndertaleChunkGEN8();
            data.FORM.Chunks["OPTN"] = new UndertaleChunkOPTN();
            data.FORM.Chunks["LANG"] = new UndertaleChunkLANG();
            data.FORM.Chunks["EXTN"] = new UndertaleChunkEXTN();
            data.FORM.Chunks["SOND"] = new UndertaleChunkSOND();
            data.FORM.Chunks["AGRP"] = new UndertaleChunkAGRP();
            data.FORM.Chunks["SPRT"] = new UndertaleChunkSPRT();
            data.FORM.Chunks["BGND"] = new UndertaleChunkBGND();
            data.FORM.Chunks["PATH"] = new UndertaleChunkPATH();
            data.FORM.Chunks["SCPT"] = new UndertaleChunkSCPT();
            data.FORM.Chunks["GLOB"] = new UndertaleChunkGLOB();
            data.FORM.Chunks["SHDR"] = new UndertaleChunkSHDR();
            data.FORM.Chunks["FONT"] = new UndertaleChunkFONT();
            data.FORM.Chunks["TMLN"] = new UndertaleChunkTMLN();
            data.FORM.Chunks["OBJT"] = new UndertaleChunkOBJT();
            data.FORM.Chunks["ROOM"] = new UndertaleChunkROOM();
            data.FORM.Chunks["DAFL"] = new UndertaleChunkDAFL();
            data.FORM.Chunks["TPAG"] = new UndertaleChunkTPAG();
            data.FORM.Chunks["CODE"] = new UndertaleChunkCODE();
            data.FORM.Chunks["VARI"] = new UndertaleChunkVARI();
            data.FORM.Chunks["FUNC"] = new UndertaleChunkFUNC();
            data.FORM.Chunks["STRG"] = new UndertaleChunkSTRG();
            data.FORM.Chunks["TXTR"] = new UndertaleChunkTXTR();
            data.FORM.Chunks["AUDO"] = new UndertaleChunkAUDO();
            data.FORM.GEN8.Object = new UndertaleGeneralInfo();
            data.FORM.OPTN.Object = new UndertaleOptions();
            data.FORM.LANG.Object = new UndertaleLanguage();
            data.GeneralInfo.Filename = data.Strings.MakeString("NewGame");
            data.GeneralInfo.Config = data.Strings.MakeString("Default");
            data.GeneralInfo.Name = data.Strings.MakeString("NewGame");
            data.GeneralInfo.DisplayName = data.Strings.MakeString("New UndertaleModTool Game");
            data.GeneralInfo.GameID = (uint)new Random().Next();
            data.GeneralInfo.Timestamp = (uint)new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds();
            data.Options.Constants.Add(new UndertaleOptions.Constant() { Name = data.Strings.MakeString("@@SleepMargin"), Value = data.Strings.MakeString(1.ToString()) });
            data.Options.Constants.Add(new UndertaleOptions.Constant() { Name = data.Strings.MakeString("@@DrawColour"), Value = data.Strings.MakeString(0xFFFFFFFF.ToString()) });
            data.Rooms.Add(new UndertaleRoom() { Name = data.Strings.MakeString("room0"), Caption = data.Strings.MakeString("") });
            data.BuiltinList = new BuiltinList(data);
            Decompiler.AssetTypeResolver.InitializeTypes(data);
            return data;
        }
    }

    public static class UndertaleDataExtensionMethods
    {
        public static T ByName<T>(this IList<T> list, string name, bool ignoreCase = false) where T : UndertaleNamedResource
        {
            if (ignoreCase)
            {
                foreach (var item in list)
                    if (item.Name.Content.Equals(name, StringComparison.OrdinalIgnoreCase))
                        return item;
            }
            else
            {
                foreach (var item in list)
                    if (item.Name.Content == name)
                        return item;
            }
            return default(T);
        }

        public static UndertaleCodeLocals For(this IList<UndertaleCodeLocals> list, UndertaleCode code)
        {
            // TODO: I'm not sure if the runner looks these up by name or by index
            return list.Where((x) => code.Name == x.Name).FirstOrDefault();
        }

        public static UndertaleString MakeString(this IList<UndertaleString> list, string content)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            // TODO: without reference counting the strings, this may leave unused strings in the array
            foreach (UndertaleString str in list)
            {
                if (str.Content == content)
                    return str;
            }

            UndertaleString newString = new UndertaleString(content);
            list.Add(newString);
            return newString;
        }

        public static UndertaleString MakeString(this IList<UndertaleString> list, string content, out int index)
        {
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            // TODO: without reference counting the strings, this may leave unused strings in the array
            for (int i = 0; i < list.Count; i++)
            {
                UndertaleString str = list[i];
                if (str.Content == content)
                {
                    index = i;
                    return str;
                }
            }

            UndertaleString newString = new UndertaleString(content);
            index = list.Count;
            list.Add(newString);
            return newString;
        }

        public static UndertaleFunction EnsureDefined(this IList<UndertaleFunction> list, string name, IList<UndertaleString> strg, bool fast = false)
        {
            UndertaleFunction func = fast ? null : list.ByName(name);
            if (func == null)
            {
                var str = strg.MakeString(name, out int id);
                func = new UndertaleFunction()
                {
                    Name = str,
                    NameStringID = id
                };
                list.Add(func);
            }
            return func;
        }

        public static UndertaleVariable EnsureDefined(this IList<UndertaleVariable> list, string name, UndertaleInstruction.InstanceType inst, bool isBuiltin, IList<UndertaleString> strg, UndertaleData data, bool fast = false)
        {
            if (inst == UndertaleInstruction.InstanceType.Local)
                throw new InvalidOperationException("Use DefineLocal instead");
            bool bytecode14 = (data?.GeneralInfo?.BytecodeVersion <= 14);
            if (bytecode14)
                inst = UndertaleInstruction.InstanceType.Undefined;
            UndertaleVariable vari = fast ? null : list.Where((x) => x.Name?.Content == name && x.InstanceType == inst).FirstOrDefault();
            if (vari == null)
            {
                var oldId = data.VarCount1;
                if (!bytecode14)
                {
                    if (!data.DifferentVarCounts)
                    { 
                        // Bytecode 16+
                        data.VarCount1++;
                        data.VarCount2++;
                    }
                    else
                    { 
                        // Bytecode 15
                        if (inst == UndertaleInstruction.InstanceType.Self && !isBuiltin)
                        {
			                oldId = data.VarCount2;
                            data.VarCount2++;
                        }
                        else if (inst == UndertaleInstruction.InstanceType.Global)
                        {
                            data.VarCount1++;
                        }
                    }
                }

                var str = strg.MakeString(name, out int id);
                vari = new UndertaleVariable()
                {
                    Name = str,
                    InstanceType = inst,
                    VarID = bytecode14 ? 0 : (isBuiltin ? (int)UndertaleInstruction.InstanceType.Builtin : (int)oldId),
                    NameStringID = id
                };
                list.Add(vari);
            }
            return vari;
        }

        public static UndertaleVariable DefineLocal(this IList<UndertaleVariable> list, UndertaleCode originalCode, int localId, string name, IList<UndertaleString> strg, UndertaleData data)
        {
            bool bytecode14 = (data?.GeneralInfo?.BytecodeVersion <= 14);
            if (bytecode14)
            {
                UndertaleVariable search = list.Where((x) => x.Name.Content == name).FirstOrDefault();
                if (search != null)
                    return search;
            }

            // Use existing registered variables.
            if (originalCode != null)
            {
                var referenced = originalCode.FindReferencedLocalVars();
                var refvar = referenced.Where((x) => x.Name.Content == name && x.VarID == localId).FirstOrDefault();
                if (refvar != null)
                    return refvar;
            }

            var str = strg.MakeString(name, out int id);
            UndertaleVariable vari = new UndertaleVariable()
            {
                Name = str,
                InstanceType = bytecode14 ? UndertaleInstruction.InstanceType.Undefined : UndertaleInstruction.InstanceType.Local,
                VarID = bytecode14 ? 0 : localId,
                NameStringID = id
            };
            if (!bytecode14 && list?.Count >= data.MaxLocalVarCount)
                data.MaxLocalVarCount = (uint) list?.Count + 1;
            list.Add(vari);
            return vari;
        }

        public static UndertaleExtensionFunction DefineExtensionFunction(this IList<UndertaleExtensionFunction> extfuncs, IList<UndertaleFunction> funcs, IList<UndertaleString> strg, uint id, uint kind, string name, UndertaleExtensionVarType rettype, string extname, params UndertaleExtensionVarType[] args)
        {
            var func = new UndertaleExtensionFunction()
            {
                ID = id,
                Name = strg.MakeString(name),
                ExtName = strg.MakeString(extname),
                Kind = kind,
                RetType = rettype
            };
	        foreach(var a in args)
                func.Arguments.Add(new UndertaleExtensionFunctionArg() { Type = a });
            extfuncs.Add(func);
            funcs.EnsureDefined(name, strg);
            return func;
        }
    }

    public class ToolInfo
    {
        // Info handle for the actual editor to store data on
        public bool ProfileMode = false;
        public string AppDataProfiles = null;
        public string CurrentMD5 = "Unknown";
    }
}
