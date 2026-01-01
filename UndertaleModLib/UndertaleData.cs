using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Underanalyzer.Decompiler;
using Underanalyzer.Decompiler.GameSpecific;
using UndertaleModLib.Compiler;
using UndertaleModLib.Models;

namespace UndertaleModLib
{
    /// <summary>
    /// An object representing a GameMaker Studio data file.
    /// </summary>
    /// <remarks>This is basically the heart of the data file, which is usually named <c>data.win</c>, <c>data.unx</c>,
    /// <c>data.ios</c> or <c>data.droid</c>, depending for which OS the game was compiled for.. <br/>
    /// It includes all the data within it accessible by either the <see cref="FORM"/>-Chunk attribute,
    /// but also via already organized attributes such as <see cref="Backgrounds"/> or <see cref="GameObjects"/>.
    /// TODO: add more documentation about how a data file works at one point.</remarks>
    public class UndertaleData : IDisposable
    {
        /// <summary>
        /// Indexer to access the resource list by its name.
        /// </summary>
        /// <param name="resourceTypeName">The resource name to get.</param>
        /// <exception cref="MissingMemberException"> if the data file does not contain a property with that name.</exception>
        public IList this[string resourceTypeName]
        {
            get
            {
                // Prevent recursion
                if (resourceTypeName == "Item")
                    return null;

                var property = GetType().GetProperty(resourceTypeName);
                if (property is null)
                    throw new MissingMemberException($"\"UndertaleData\" doesn't contain a property named \"{resourceTypeName}\".");

                return property.GetValue(this, null) as IList;
            }
            set
            {
                // Prevent recursion
                if (resourceTypeName == "Item")
                    return;
                
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
        public IList this[Type resourceType]
        {
            get
            {
                // Prevent recursion
                if (resourceType == typeof(UndertaleResource))
                    return null;

                if (!typeof(UndertaleResource).IsAssignableFrom(resourceType))
                    throw new NotSupportedException($"\"{resourceType.FullName}\" is not an UndertaleResource.");

                var property = GetType().GetProperties().Where(x => x.PropertyType.Name == "IList`1")
                                                        .FirstOrDefault(x => x.PropertyType.GetGenericArguments()[0] == resourceType);
                if (property is null)
                    throw new MissingMemberException($"\"UndertaleData\" doesn't contain a resource list of type \"{resourceType.FullName}\".");

                return property.GetValue(this, null) as IList;
            }
            set
            {
                if (!typeof(UndertaleResource).IsAssignableFrom(resourceType))
                    throw new NotSupportedException($"\"{resourceType.FullName}\" is not an UndertaleResource.");

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
        /// The feature flags stored in the data file.
        /// </summary>
        public UndertaleFeatureFlags FeatureFlags => FORM.FEAT?.Object;

        /// <summary>
        /// The filter effects stored in the data file.
        /// </summary>
        public IList<UndertaleFilterEffect> FilterEffects => FORM.FEDS?.List;

        /// <summary>
        /// The particle systems stored in the data file.
        /// </summary>
        public IList<UndertaleParticleSystem> ParticleSystems => FORM.PSYS?.List;

        /// <summary>
        /// The particle system emitters stored in the data file.
        /// </summary>
        public IList<UndertaleParticleSystemEmitter> ParticleSystemEmitters => FORM.PSEM?.List;


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
        /// Whether the data file has array copy-on-write enabled.
        /// </summary>
        public bool ArrayCopyOnWrite = false;

        /// <summary>
        /// The last room particle system instance ID of the data file (incrementing).
        /// </summary>
        /// <remarks>
        /// The first actual usable ID is 8388608.
        /// </remarks>
        public int LastParticleSystemInstanceID { get; set; } = 8388607;

        /// <summary>
        /// Some info for the editor to store data on.
        /// </summary>
        public readonly ToolInfo ToolInfo = new ToolInfo();

        /// <summary>
        /// Shows the current padding value. <c>-1</c> indicates a pre 1.4.9999 padding, where the default is 16.
        /// </summary>
        public int PaddingAlignException = -1;

        /// <summary>
        /// A list of known GameMaker Studio constants and variables.
        /// </summary>
        public BuiltinList BuiltinList;

        /// <summary>
        /// Cache for 2.3-style functions defined in global scripts. Can be re-built by setting this to null.
        /// </summary>
        public GlobalFunctions GlobalFunctions;

        /// <summary>
        /// Registry for macro types, their resolvers, and other data specific to this game.
        /// </summary>
        public GameSpecificRegistry GameSpecificRegistry;

        /// <summary>
        /// An array of a <see cref="UndertaleData"/> properties with <see cref="IList{T}"/> as their type.
        /// </summary>
        public PropertyInfo[] AllListProperties { get; private set; }

        /// <summary>
        /// Initializes new <see cref="UndertaleData"/> instance.
        /// </summary>
        public UndertaleData()
        {
            AllListProperties = GetType().GetProperties()
                                .Where(x => x.PropertyType.IsGenericType
                                            && x.PropertyType.GetGenericTypeDefinition() == typeof(IList<>))
                                .ToArray();
        }

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
        /// Reports the zero-based index of the first occurrence of the specified <see cref="UndertaleResource"/>.
        /// </summary>
        /// <param name="obj">The object to get the index of.</param>
        /// <param name="panicIfInvalid">Whether to throw if <paramref name="obj"/> is not a valid object.</param>
        /// <returns>The zero-based index position of the <paramref name="obj"/> parameter if it is found or -2 if it is not.</returns>
        /// <exception cref="InvalidOperationException"><paramref name="panicIfInvalid"/> is <see langword="true"/>
        /// and <paramref name="obj"/> could not be found.</exception>
        public int IndexOf(UndertaleResource obj, bool panicIfInvalid = true)
        {
            Type objType = obj.GetType();
            PropertyInfo objListPropInfo = AllListProperties.FirstOrDefault(x => x.PropertyType.GetGenericArguments()[0] == objType);
            if (objListPropInfo is not null)
            {
                if (objListPropInfo.GetValue(this) is IList list)
                    return list.IndexOf(obj);
            }

            if (panicIfInvalid)
                throw new InvalidOperationException();
            return -2;
        }

        internal int IndexOfByName(string line)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Reports whether the data file was built by GameMaker Studio 2.
        /// </summary>
        /// <returns><see langword="true"/> if yes, <see langword="false"/> if not.</returns>
        public bool IsGameMaker2()
        {
            return IsVersionAtLeast(2);
        }


        // Old Versions: https://store.yoyogames.com/downloads/gm-studio/release-notes-studio-old.html
        // https://web.archive.org/web/20150304025626/https://store.yoyogames.com/downloads/gm-studio/release-notes-studio.html
        // Early Access: https://web.archive.org/web/20181002232646/http://store.yoyogames.com:80/downloads/gm-studio-ea/release-notes-studio.html
        private bool TestGMS1Version(uint stableBuild, uint betaBuild, bool allowGMS2 = false)
        {
            return (allowGMS2 || !IsGameMaker2()) && (IsVersionAtLeast(1, 0, 0, stableBuild) || (IsVersionAtLeast(1, 0, 0, betaBuild) && !IsVersionAtLeast(1, 0, 0, 1000)));
        }

        /// <summary>
        /// Sets the GMS2+ version flag in GeneralInfo.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="release">The release version.</param>
        /// <param name="build">The build version.</param>
        /// <param name="isLTS">If included, alter the data branch between LTS and non-LTS.</param>
        public void SetGMS2Version(uint major, uint minor = 0, uint release = 0, uint build = 0, bool? isLTS = null)
        {
            if (major != 2 && major != 2022 && major != 2023 && major != 2024)
                throw new NotSupportedException("Attempted to set a version of GameMaker " + major + " using SetGMS2Version");

            GeneralInfo.Major = major;
            GeneralInfo.Minor = minor;
            GeneralInfo.Release = release;
            GeneralInfo.Build = build;

            if (isLTS is not null)
            {
                SetLTS((bool)isLTS);
            }
        }

        /// <summary>
        /// Sets the branch type in GeneralInfo to the appropriate LTS or non-LTS version based on 
        /// </summary>
        /// <param name="isLTS">If included, alter the data branch between LTS and non-LTS.</param>
        public void SetLTS(bool isLTS)
        {
            // Insert additional logic as needed for new branches using IsVersionAtLeast
            GeneralInfo.Branch = isLTS ? UndertaleGeneralInfo.BranchType.LTS2022_0 : UndertaleGeneralInfo.BranchType.Post2022_0;
        }

        /// <summary>
        /// Reports whether the version of the data file is the same or higher than a specified version.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="release">The release version.</param>
        /// <param name="build">The build version.</param>
        /// <returns>Whether the version of the data file is the same or higher than a specified version.</returns>
        public bool IsVersionAtLeast(uint major, uint minor = 0, uint release = 0, uint build = 0)
        {
            if (GeneralInfo is null)
            {
                Debug.WriteLine("\"UndertaleData.IsVersionAtLeast()\" error - \"GeneralInfo\" is null.");
                return false;
            }

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
        /// Reports whether the version of the data file is the same or higher than a specified version, and off the LTS branch that lacks some features.
        /// </summary>
        /// <param name="major">The major version.</param>
        /// <param name="minor">The minor version.</param>
        /// <param name="release">The release version.</param>
        /// <param name="build">The build version.</param>
        /// <returns>Whether the version of the data file is the same or higher than a specified version. Always false for LTS.</returns>
        public bool IsNonLTSVersionAtLeast(uint major, uint minor = 0, uint release = 0, uint build = 0)
        {
            if (GeneralInfo is null)
            {
                Debug.WriteLine("\"UndertaleData.IsNonLTSVersionAtLeast()\" error - \"GeneralInfo\" is null.");
                return false;
            }

            if (GeneralInfo.Branch < UndertaleGeneralInfo.BranchType.Post2022_0)
                return false;

            return IsVersionAtLeast(major, minor, release, build);
        }

        /// <summary>
        /// Returns the ID of the builtin (i.e., embedded in the main data file) audio group. Varies depending on version.
        /// </summary>
        /// <returns>ID of the builtin audio group; 0 or 1.</returns>
        public int GetBuiltinSoundGroupID()
        {
            // It is known it works this way in 1.0.1266. The exact version which changed this is unknown.
            // If we find a game which does not fit the version identified here, we should fix this check.
            return TestGMS1Version(1250, 161, true) ? 0 : 1;
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
            foreach (UndertaleChunk chunk in data.FORM.Chunks.Values)
                data.FORM.ChunksTypeDict[chunk.GetType()] = chunk;
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
            Decompiler.GameSpecificResolver.Initialize(data);
            return data;
        }

        /// <summary>
        /// Creates a new resource based on type of the list.
        /// </summary>
        public static UndertaleResource CreateResource(IList list)
        {
            Type resourceType = list.GetType().GetGenericArguments()[0];
            return (Activator.CreateInstance(resourceType) as UndertaleResource)!;
        }

        /// <summary>
        /// Get the default name of a resource based on a list (e.g., sprite0)
        /// </summary>
        public static string GetDefaultResourceName(IList list)
        {
            Type resourceType = list.GetType().GetGenericArguments()[0];
            if (resourceType == typeof(UndertaleTexturePageItem) ||
                resourceType == typeof(UndertaleEmbeddedAudio) ||
                resourceType == typeof(UndertaleEmbeddedTexture))
            {
                return null;
            }

            string typeName = resourceType.Name.Replace("Undertale", "").Replace("GameObject", "Object").ToLower();
            string resourceName = typeName + list.Count;

            return resourceName;
        }

        /// <summary>
        /// Initialize newly created resource.
        /// </summary>
        /// <param name="resource">The resource to initialize.</param>
        /// <param name="list">The list where the resource might reside.</param>
        /// <param name="resourceName">Name to set to resource if supported.</param>
        public void InitializeResource(UndertaleResource resource, IList list, string resourceName)
        {
            // Set up name
            if (resource is UndertaleNamedResource namedResource)
            {
                UndertaleString name = resource switch
                {
                    // UTMT only names.
                    UndertaleTexturePageItem => new UndertaleString("PageItem " + list.Count),
                    UndertaleEmbeddedAudio => new UndertaleString("EmbeddedSound " + list.Count),
                    UndertaleEmbeddedTexture => new UndertaleString("Texture " + list.Count),
                    _ => Strings.MakeString(resourceName, createNew: true),
                };

                namedResource.Name = name;
            }

            if (resource is UndertaleString _string)
            {
                _string.Content = resourceName;
            }
            else if (resource is UndertaleRoom room)
            {
                if (IsVersionAtLeast(2))
                {
                    room.Caption = null;
                    room.Backgrounds.Clear();
                    if (IsVersionAtLeast(2024, 13))
                    {
                        room.Flags |= IsVersionAtLeast(2024, 13) ? UndertaleRoom.RoomEntryFlags.IsGM2024_13 : UndertaleRoom.RoomEntryFlags.IsGMS2;
                    }
                    else
                    {
                        room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2;
                        if (IsVersionAtLeast(2, 3))
                        {
                            room.Flags |= UndertaleRoom.RoomEntryFlags.IsGMS2_3;
                        }
                    }
                }
                else
                {
                    room.Caption = Strings.MakeString("", createNew: true);
                }
            }
            else if (resource is UndertaleScript script)
            {
                if (IsVersionAtLeast(2, 3))
                {
                    script.Code = UndertaleCode.CreateEmptyEntry(this, Strings.MakeString($"gml_GlobalScript_{script.Name.Content}", createNew: true));
                    if (GlobalInitScripts is IList<UndertaleGlobalInit> globalInitScripts)
                    {
                        globalInitScripts.Add(new UndertaleGlobalInit()
                        {
                            Code = script.Code,
                        });
                    }
                }
                else
                {
                    script.Code = UndertaleCode.CreateEmptyEntry(this, Strings.MakeString($"gml_Script_{script.Name.Content}", createNew: true));
                }
            }
            else if (resource is UndertaleCode code)
            {
                if (CodeLocals is not null)
                {
                    code.LocalsCount = 1;
                    UndertaleCodeLocals.CreateEmptyEntry(this, code.Name);
                }
                else
                {
                    code.WeirdLocalFlag = true;
                }
            }
            else if (resource is UndertaleExtension)
            {
                if (GeneralInfo?.Major >= 2 ||
                    (GeneralInfo?.Major == 1 && GeneralInfo?.Build >= 1773) ||
                    (GeneralInfo?.Major == 1 && GeneralInfo?.Build == 1539))
                {
                    var newProductID = new byte[] { 0xBA, 0x5E, 0xBA, 0x11, 0xBA, 0xDD, 0x06, 0x60, 0xBE, 0xEF, 0xED, 0xBA, 0x0B, 0xAB, 0xBA, 0xBE };
                    FORM.EXTN.productIdData.Add(newProductID);
                }
            }
            else if (resource is UndertaleShader shader)
            {
                shader.GLSL_ES_Vertex = Strings.MakeString("", createNew: true);
                shader.GLSL_ES_Fragment = Strings.MakeString("", createNew: true);
                shader.GLSL_Vertex = Strings.MakeString("", createNew: true);
                shader.GLSL_Fragment = Strings.MakeString("", createNew: true);
                shader.HLSL9_Vertex = Strings.MakeString("", createNew: true);
                shader.HLSL9_Fragment = Strings.MakeString("", createNew: true);
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Type disposableType = typeof(IDisposable);
            PropertyInfo[] dataProperties = GetType().GetProperties();
            var dataDisposableProps = dataProperties.Except(AllListProperties)
                                                    .Where(x => disposableType.IsAssignableFrom(x.PropertyType));

            // Dispose disposable properties
            foreach (PropertyInfo disposableProp in dataDisposableProps)
            {
                // If property is null
                if (disposableProp.GetValue(this) is not IDisposable disposable)
                    continue;

                disposable.Dispose();
            }

            // Clear all object lists (sprites, code, etc.)
            foreach (PropertyInfo dataListProperty in AllListProperties)
            {
                // If it's an indexer property
                if (dataListProperty.Name == "Item")
                    continue;

                // If list is null
                if (dataListProperty.GetValue(this) is not IList list)
                    continue;

                // If list elements are disposable
                if (disposableType.IsAssignableFrom(list.GetType().GetGenericArguments()[0]))
                {
                    foreach (IDisposable disposable in list)
                        disposable?.Dispose();
                }

                list.Clear();
            }

            // Clear other references
            FORM = null;
            GlobalFunctions = null;
            GameSpecificRegistry = null;
        }
    }

    /// <summary>
    /// An info handle for an editor to store data on.
    /// </summary>
    public class ToolInfo
    {
        /// <summary>
        /// Default settings to be used by the Underanalyzer decompiler,
        /// for a tool and in any scripts that desire matching the same settings.
        /// </summary>
        public IDecompileSettings DecompilerSettings = new DecompileSettings();

        /// <summary>
        /// Function that returns the prefix to be used when 
        /// resolving instance ID references in the compiler and decompiler.
        /// </summary>
        public Func<string> InstanceIdPrefix = () => "inst_";
    }
}
