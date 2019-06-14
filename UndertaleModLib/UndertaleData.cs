using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        public uint InstanceVarCount { get => FORM.VARI.InstanceVarCount; set => FORM.VARI.InstanceVarCount = value; }
        public uint InstanceVarCountAgain { get => FORM.VARI.InstanceVarCountAgain; set => FORM.VARI.InstanceVarCountAgain = value; }
        public uint MaxLocalVarCount { get => FORM.VARI.MaxLocalVarCount; set => FORM.VARI.MaxLocalVarCount = value; }
        public IList<UndertaleFunction> Functions => FORM.FUNC?.Functions;
        public IList<UndertaleCodeLocals> CodeLocals => FORM.FUNC?.CodeLocals;
        public IList<UndertaleString> Strings => FORM.STRG?.List;
        public IList<UndertaleEmbeddedImage> EmbeddedImages => FORM.EMBI?.List;
        public IList<UndertaleEmbeddedTexture> EmbeddedTextures => FORM.TXTR?.List;
        public IList<UndertaleTextureGroupInfo> TextureGroupInfo => FORM.TGIN?.List;
        public IList<UndertaleEmbeddedAudio> EmbeddedAudio => FORM.AUDO?.List;

        public bool UnsupportedBytecodeVersion = false;
        public int PaddingAlignException = -1;

        public UndertaleNamedResource ByName(string name)
        {
            // TODO: Check if those are all possible types
            return Sounds.ByName(name) ??
                Sprites.ByName(name) ??
                Backgrounds.ByName(name) ??
                Paths.ByName(name) ??
                Scripts.ByName(name) ??
                Fonts.ByName(name) ??
                GameObjects.ByName(name) ??
                Rooms.ByName(name) ??
                (UndertaleNamedResource)null;
        }

        public int IndexOf(UndertaleNamedResource obj)
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
            throw new InvalidOperationException();
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
            // If we find a game which does not fit the version identified here, we should this check.
            return IsVersionAtLeast(1, 0, 0, 1354) ? 0 : 1;
        }

        public bool IsYYC()
        {
            return GeneralInfo != null && Code == null;
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
            return data;
        }
    }

    public static class UndertaleDataExtensionMethods
    {
        public static T ByName<T>(this IList<T> list, string name) where T : UndertaleNamedResource
        {
            foreach(var item in list)
            {
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
                throw new ArgumentNullException("content");
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

        public static UndertaleFunction EnsureDefined(this IList<UndertaleFunction> list, string name, IList<UndertaleString> strg)
        {
            UndertaleFunction func = list.ByName(name);
            if (func == null)
            {
                func = new UndertaleFunction()
                {
                    Name = strg.MakeString(name),
                    UnknownChainEndingValue = 0 // TODO: seems to work...
                };
                list.Add(func);
            }
            return func;
        }

        public static UndertaleVariable EnsureDefined(this IList<UndertaleVariable> list, string name, UndertaleInstruction.InstanceType inst, bool isBuiltin, IList<UndertaleString> strg, UndertaleData data)
        {
            if (inst == UndertaleInstruction.InstanceType.Local)
                throw new InvalidOperationException("Use DefineLocal instead");
            bool bytecode14 = (data?.GeneralInfo?.BytecodeVersion <= 14);
            if (bytecode14)
                inst = UndertaleInstruction.InstanceType.Undefined;
            UndertaleVariable vari = list.Where((x) => x.Name.Content == name && x.InstanceType == inst).FirstOrDefault();
            if (vari == null)
            {
                var oldId = data.InstanceVarCount;
                if (!bytecode14)
                {
                    if (data.InstanceVarCount == data.InstanceVarCountAgain)
                    { // Example games that use this mode: Undertale v1.08, Undertale v1.11.
                        data.InstanceVarCount++;
                        data.InstanceVarCountAgain++;
                    }
                    else
                    { // Example Games which use this mode: Undertale v1.001.
                        if (inst == UndertaleInstruction.InstanceType.Self)
                        {
                            data.InstanceVarCountAgain++;
                        }
                        else if (inst == UndertaleInstruction.InstanceType.Global)
                        {
                            data.InstanceVarCount++;
                        }
                    }
                }

                vari = new UndertaleVariable()
                {
                    Name = strg.MakeString(name),
                    InstanceType = inst,
                    VarID = bytecode14 ? 0 : (isBuiltin ? (int)UndertaleInstruction.InstanceType.Builtin : (int)oldId),
                    UnknownChainEndingValue = 0 // TODO: seems to work...
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

            UndertaleVariable vari = new UndertaleVariable()
            {
                Name = strg.MakeString(name),
                InstanceType = bytecode14 ? UndertaleInstruction.InstanceType.Undefined : UndertaleInstruction.InstanceType.Local,
                VarID = bytecode14 ? 0 : localId,
                UnknownChainEndingValue = 0 // TODO: seems to work...
            };
            if (!bytecode14 && list?.Count >= data.MaxLocalVarCount)
                data.MaxLocalVarCount = (uint) list?.Count + 1;
            list.Add(vari);
            return vari;
        }

        public static UndertaleExtension.ExtensionFunction DefineExtensionFunction(this IList<UndertaleExtension.ExtensionFunction> extfuncs, IList<UndertaleFunction> funcs, IList<UndertaleString> strg, uint id, uint kind, string name, UndertaleExtension.ExtensionVarType rettype, string extname, params UndertaleExtension.ExtensionVarType[] args)
        {
            var func = new UndertaleExtension.ExtensionFunction()
            {
                ID = id,
                Name = strg.MakeString(name),
                ExtName = strg.MakeString(extname),
                Kind = kind,
                RetType = rettype
            };
	        foreach(var a in args)
                func.Arguments.Add(new UndertaleExtension.ExtensionFunctionArg() { Type = a });
            extfuncs.Add(func);
            funcs.EnsureDefined(name, strg);
            return func;
        }
    }
}
