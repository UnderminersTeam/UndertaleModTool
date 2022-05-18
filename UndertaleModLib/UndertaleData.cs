using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    /// <summary>
    /// An object representing a Game Maker: Studio data file.
    /// </summary>
    /// <remarks>This is basically the heart of the data file, which is usually named <c>data.win</c>, <c>data.unx</c>,
    /// <c>data.ios</c> or <c>data.droid</c>, depending for which OS the game was compiled for.. <br/>
    /// It includes all the data within it accessible by either the <see cref="FORM"/>-Chunk attribute,
    /// but also via already organized attributes such as <see cref="Backgrounds"/> or <see cref="GameObjects"/>.
    /// TODO: add more documentation about how a data file works at one point.</remarks>
    public class UndertaleData
    {
        /// <summary>
        /// Indexer to access the resource list by its name.
        /// </summary>
        /// <param name="resourceTypeName">The resource name to get.</param>
        /// <exception cref="MissingMemberException"> if the data file does not contain a property with that name.</exception>
        public object this[string resourceTypeName]
        {
            get
            {
                var property = GetType().GetProperty(resourceTypeName);
                if (property is null)
                    throw new MissingMemberException($"\"UndertaleData\" doesn't contain a property named \"{resourceTypeName}\".");

                return property.GetValue(this, null);
            }
            set
            {
                var property = GetType().GetProperty(resourceTypeName);
                if (property is null)
                    throw new MissingMemberException($"\"UndertaleData\" doesn't contain a property named \"{resourceTypeName}\".");

                property.SetValue(this, value, null);
            }
        }

        /// <summary>
        /// Indexer to access the resource list by its items type.
        /// </summary>
        /// <param name="resourceType">The resource type to get.</param>
        /// <exception cref="NotSupportedException"> if the type is not an <see cref="UndertaleNamedResource"/>.</exception>
        /// <exception cref="MissingMemberException"> if the data file does not contain a property of that type.</exception>
        public object this[Type resourceType]
        {
            get
            {
                if (!typeof(UndertaleNamedResource).IsAssignableFrom(resourceType))
                    throw new NotSupportedException($"\"{resourceType.FullName}\" is not a UndertaleNamedResource.");

                var property = GetType().GetProperties().Where(x => x.PropertyType.Name == "IList`1")
                                                        .FirstOrDefault(x => x.PropertyType.GetGenericArguments()[0] == resourceType);
                if (property is null)
                    throw new MissingMemberException($"\"UndertaleData\" doesn't contain a resource list of type \"{resourceType.FullName}\".");

                return property.GetValue(this, null);
            }
            set
            {
                if (!typeof(UndertaleNamedResource).IsAssignableFrom(resourceType))
                    throw new NotSupportedException($"\"{resourceType.FullName}\" is not a UndertaleNamedResource.");

                var property = GetType().GetProperties().Where(x => x.PropertyType.Name == "IList`1")
                                                        .FirstOrDefault(x => x.PropertyType.GetGenericArguments()[0] == resourceType);
                if (property is null)
                    throw new MissingMemberException($"\"UndertaleData\" doesn't contain a resource list of type \"{resourceType.FullName}\".");

                property.SetValue(this, value, null);
            }
        }

        /// <summary>
        /// The FORM chunk of the data file.
        /// </summary>
        public UndertaleChunkFORM FORM;

        /// <summary>
        /// General info of the data file.
        /// </summary>
        public UndertaleGeneralInfo GeneralInfo => FORM.GEN8?.Object;

        /// <summary>
        /// General Options of the data file.
        /// </summary>
        public UndertaleOptions Options => FORM.OPTN?.Object;

        /// <summary>
        /// Languages of the data file.
        /// </summary>
        public UndertaleLanguage Language => FORM.LANG?.Object;

        /// <summary>
        /// The used extensions of the data file.
        /// </summary>
        public IList<UndertaleExtension> Extensions => FORM.EXTN?.List;

        /// <summary>
        /// The used sounds of the data file.
        /// </summary>
        public IList<UndertaleSound> Sounds => FORM.SOND?.List;

        /// <summary>
        /// The audio groups of the data file.
        /// </summary>
        public IList<UndertaleAudioGroup> AudioGroups => FORM.AGRP?.List;

        /// <summary>
        /// The sprites of the data file.
        /// </summary>
        public IList<UndertaleSprite> Sprites => FORM.SPRT?.List;

        /// <summary>
        /// The backgrounds (or Tilesets) of the data file.
        /// </summary>
        public IList<UndertaleBackground> Backgrounds => FORM.BGND?.List;

        /// <summary>
        /// The paths of the data file.
        /// </summary>
        public IList<UndertalePath> Paths => FORM.PATH?.List;

        /// <summary>
        /// The scripts of the data file.
        /// </summary>
        public IList<UndertaleScript> Scripts => FORM.SCPT?.List;

        /// <summary>
        /// The global initialization scripts of the data file.
        /// </summary>
        public IList<UndertaleGlobalInit> GlobalInitScripts => FORM.GLOB?.List;

        /// <summary>
        /// The global end scripts of the data file.
        /// </summary>
        public IList<UndertaleGlobalInit> GameEndScripts => FORM.GMEN?.List;

        /// <summary>
        /// The used shaders of the data file.
        /// </summary>
        public IList<UndertaleShader> Shaders => FORM.SHDR?.List;

        /// <summary>
        /// The fonts of the data file.
        /// </summary>
        public IList<UndertaleFont> Fonts => FORM.FONT?.List;

        /// <summary>
        /// The Timelines of the data file.
        /// </summary>
        public IList<UndertaleTimeline> Timelines => FORM.TMLN?.List;

        /// <summary>
        /// The game objects of the data file.
        /// </summary>
        public IList<UndertaleGameObject> GameObjects => FORM.OBJT?.List;

        /// <summary>
        /// The rooms of the data file.
        /// </summary>
        public IList<UndertaleRoom> Rooms => FORM.ROOM?.List;
        //[Obsolete("Unused")]
        // DataFile

        /// <summary>
        /// The texture page items from the data file.
        /// </summary>
        public IList<UndertaleTexturePageItem> TexturePageItems => FORM.TPAG?.List;

        /// <summary>
        /// The code entries of the data file.
        /// </summary>
        public IList<UndertaleCode> Code => FORM.CODE?.List;

        /// <summary>
        /// The used variables of the data file.
        /// </summary>
        public IList<UndertaleVariable> Variables => FORM.VARI?.List;

        /// <summary>
        /// TODO: Unknown value, need more research.
        /// </summary>
        public uint VarCount1 { get => FORM.VARI.VarCount1; set => FORM.VARI.VarCount1 = value; }

        /// <summary>
        /// TODO: Unknown value, need more research.
        /// </summary>
        public uint VarCount2 { get => FORM.VARI.VarCount2; set => FORM.VARI.VarCount2 = value; }

        /// <summary>
        /// TODO: Unknown value, need more research. Also obsolete.
        /// </summary>
        public bool DifferentVarCounts { get => FORM.VARI.DifferentVarCounts; set => FORM.VARI.DifferentVarCounts = value; }

        /// <summary>
        /// TODO: Unknown value, need more research. Also obsolete.
        /// </summary>
        [Obsolete]
        public uint InstanceVarCount { get => VarCount1; set => VarCount1 = value; }

        /// <summary>
        /// TODO: Unknown value, need more research. Also obsolete.
        /// </summary>
        [Obsolete]
        public uint InstanceVarCountAgain { get => VarCount2; set => VarCount2 = value; }

        /// <summary>
        /// TODO: Unknown value, need more research.
        /// </summary>
        public uint MaxLocalVarCount { get => FORM.VARI.MaxLocalVarCount; set => FORM.VARI.MaxLocalVarCount = value; }

        /// <summary>
        /// The functions of the data file.
        /// </summary>
        public IList<UndertaleFunction> Functions => FORM.FUNC?.Functions;

        /// <summary>
        /// The code locals of the data file.
        /// </summary>
        public IList<UndertaleCodeLocals> CodeLocals => FORM.FUNC?.CodeLocals;

        /// <summary>
        /// The used strings of the data file.
        /// </summary>
        public IList<UndertaleString> Strings => FORM.STRG?.List;

        /// <summary>
        /// The embedded images of the data file. This is used to store built-in particle sprites,
        /// every time you use <c>part_sprite</c> functions.
        /// </summary>
        public IList<UndertaleEmbeddedImage> EmbeddedImages => FORM.EMBI?.List;

        /// <summary>
        /// The embedded textures of the data file.
        /// </summary>
        public IList<UndertaleEmbeddedTexture> EmbeddedTextures => FORM.TXTR?.List;

        /// <summary>
        /// The texture group infos of the data file.
        /// </summary>
        public IList<UndertaleTextureGroupInfo> TextureGroupInfo => FORM.TGIN?.List;

        /// <summary>
        /// The embedded audio of the data file.
        /// </summary>
        public IList<UndertaleEmbeddedAudio> EmbeddedAudio => FORM.AUDO?.List;

        /// <summary>
        /// The used tags of the data file.
        /// </summary>
        public UndertaleTags Tags => FORM.TAGS?.Object;

        /// <summary>
        /// The animation curves of the data file.
        /// </summary>
        public IList<UndertaleAnimationCurve> AnimationCurves => FORM.ACRV?.List;

        /// <summary>
        /// The sequences of the data file.
        /// </summary>
        public IList<UndertaleSequence> Sequences => FORM.SEQN?.List;

        /// <summary>
        /// Whether this is an unsupported bytecode version.
        /// </summary>
        public bool UnsupportedBytecodeVersion = false;

        /// <summary>
        /// Whether the Texture Page Items (TPGA) chunk is 4 byte aligned.
        /// </summary>
        public bool IsTPAG4ByteAligned = false;

        /// <summary>
        /// Whether the data file has short circuiting enabled.
        /// </summary>
        public bool ShortCircuit = true;

        /// <summary>
        /// Whether the data file is from version GMS2.2.2.302
        /// </summary>
        public bool GMS2_2_2_302 = false;

        /// <summary>
        /// Whether the data file is from version GMS2.3
        /// </summary>
        public bool GMS2_3 = false;

        /// <summary>
        /// Whether the data file is from version GMS2.3.1
        /// </summary>
        public bool GMS2_3_1 = false;

        /// <summary>
        /// Whether the data file is from version GMS2.3.2
        /// </summary>
        public bool GMS2_3_2 = false;

        /// <summary>
        /// Whether the data file uses the QOI format for images.
        /// </summary>
        public bool UseQoiFormat = false;

        /// <summary>
        /// Whether the data file uses BZip compression.
        /// </summary>
        public bool UseBZipFormat = false;

        /// <summary>
        /// Whether the data file is from version GMS2022.1.
        /// </summary>
        public bool GMS2022_1 = false;

        /// <summary>
        /// Whether the data file is from version GMS2022.2.
        /// </summary>
        public bool GMS2022_2 = false;

        /// <summary>
        /// Whether the data file is from version GMS2022.3.
        /// </summary>
        public bool GM2022_3 = false;

        /// <summary>
        /// Some info for the editor to store data on.
        /// </summary>
        public readonly ToolInfo ToolInfo = new ToolInfo();

        /// <summary>
        /// Shows the current padding value. <c>-1</c> indicates a pre 1.4.9999 padding, where the default is 16.
        /// </summary>
        public int PaddingAlignException = -1;

        /// <summary>
        /// A list of known Game Maker: Studio constants and variables.
        /// </summary>
        public BuiltinList BuiltinList;

        /// <summary>
        /// Cache for known 2.3-style function names for compiler speedups. Can be re-built by setting this to null.
        /// </summary>
        public Dictionary<string, UndertaleFunction> KnownSubFunctions;

        //Profile mode related properties

        //TODO: Why are the functions that deal with the cache in a completely different place than the cache parameters? These have *no* place of being here.
        /// <summary>
        /// A <see cref="Dictionary{TKey,TValue}"/><typeparam name="TKey"></typeparam> of cached decompiled code,
        /// with the code name as the Key and the decompiled code text as the value.
        /// </summary>
        public ConcurrentDictionary<string, string> GMLCache { get; set; }

        /// <summary>
        /// A list of names of code entries which failed to compile or decompile.
        /// </summary>
        public List<string> GMLCacheFailed { get; set; }

        /// <summary>
        /// A list of names of modified code entries.
        /// </summary>
        public ConcurrentBag<string> GMLCacheChanged { get; set; } = new();

        /// <summary>
        /// A list of names of code entries that were edited before the "Use decompiled code cache" setting was enabled.
        /// </summary>
        public List<string> GMLEditedBefore { get; set; }

        /// <summary>
        /// Whether the decompiled code cache has been saved to disk with no new cache changes happening since then.
        /// </summary>
        public bool GMLCacheWasSaved { get; set; }

        /// <summary>
        /// Whether the decompiled code cache is generated. This will be <see langword="false"/> if it's currently generating and
        /// <see langword="true"/> otherwise.
        /// </summary>
        public bool GMLCacheIsReady { get; set; } = true;

        /// <summary>
        /// Get a resource from the data file by name.
        /// </summary>
        /// <param name="name">The name of the desired resource.</param>
        /// <param name="ignoreCase">Whether to ignore casing while searching.</param>
        /// <returns>The <see cref="UndertaleResource"/>.</returns>
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

        /// <summary>
        /// Reports the zero-based index of the first occurence of the specified <see cref="UndertaleNamedResource"/>.
        /// </summary>
        /// <param name="obj">The object to get the index of.</param>
        /// <param name="panicIfInvalid">Whether to throw if <paramref name="obj"/> is not a valid object.</param>
        /// <returns>The zero-based index position of the <paramref name="obj"/> parameter if it is found or -2 if it is not.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="panicIfInvalid"/> is <see langword="true"/>
        /// and <paramref name="obj"/> could not be found.</exception>
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

        /// <summary>
        /// Reports whether the data file was build by Game Maker Studio 2.
        /// </summary>
        /// <returns><see langword="true"/> if yes, <see langword="false"/> if not.</returns>
        public bool IsGameMaker2()
        {
            return IsVersionAtLeast(2, 0, 0, 0);
        }


        // Old Versions: https://store.yoyogames.com/downloads/gm-studio/release-notes-studio-old.html
        // https://web.archive.org/web/20150304025626/https://store.yoyogames.com/downloads/gm-studio/release-notes-studio.html
        // Early Access: https://web.archive.org/web/20181002232646/http://store.yoyogames.com:80/downloads/gm-studio-ea/release-notes-studio.html
        private bool TestGMS1Version(uint stableBuild, uint betaBuild, bool allowGMS2 = false)
        {
            return (allowGMS2 || !IsGameMaker2()) && (IsVersionAtLeast(1, 0, 0, stableBuild) || (IsVersionAtLeast(1, 0, 0, betaBuild) && !IsVersionAtLeast(1, 0, 0, 1000)));
        }

        /// <summary>
        /// Reports whether the version of the data file is the same or higher than a specified version.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="release">The release version.</param>
        /// <param name="build">The build version.</param>
        /// <returns>Whether the version of the data file is the same or higher than a specified version.</returns>
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

        /// <summary>
        /// TODO: needs to be documented on what this does.
        /// </summary>
        /// <returns>TODO</returns>
        public int GetBuiltinSoundGroupID()
        {
            // It is known it works this way in 1.0.1266. The exact version which changed this is unknown.
            // If we find a game which does not fit the version identified here, we should fix this check.
            return TestGMS1Version(1354, 161, true) ? 0 : 1;
        }

        /// <summary>
        /// Reports whether the data file was compiled with YYC.
        /// </summary>
        /// <returns><see langword="true"/> if yes, <see langword="false"/> if not.</returns>
        public bool IsYYC()
        {
            return GeneralInfo != null && Code == null;
        }

        /// <summary>
        /// TODO: Undocumented helper method.
        /// </summary>
        /// <returns>TODO</returns>
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

        /// <summary>
        /// Creates a new empty data file.
        /// </summary>
        /// <returns>The newly created data file.</returns>
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
            data.GeneralInfo.FileName = data.Strings.MakeString("NewGame");
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

    /// <summary>
    /// An info handle for an editor to store data on.
    /// </summary>
    public class ToolInfo
    {
        /// <summary>
        /// Whether profile mode is enabled.
        /// </summary>
        public bool ProfileMode = false;

        /// <summary>
        /// The location of the profiles folder.
        /// </summary>
        public string AppDataProfiles = "";

        /// <summary>
        /// The MD5 hash of the current file.
        /// </summary>
        public string CurrentMD5 = "Unknown";
    }
}
