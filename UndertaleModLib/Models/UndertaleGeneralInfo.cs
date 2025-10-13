using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UndertaleModLib.Compiler;
using UndertaleModLib.Decompiler;

namespace UndertaleModLib.Models;

/// <summary>
/// General info about a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public partial class UndertaleGeneralInfo : UndertaleObject, IDisposable
{
    /// <summary>
    /// Information flags a data file can use.
    /// </summary>
    [Flags]
    public enum InfoFlags : uint
    {
        /// <summary>
        /// Start the game as fullscreen.
        /// </summary>
        Fullscreen = 0x0001,
        /// <summary>
        /// Use synchronization to avoid tearing.
        /// </summary>
        SyncVertex1 = 0x0002,
        /// <summary>
        /// Use synchronization to avoid tearing. TODO: difference?
        /// </summary>
        SyncVertex2 = 0x0004,
        /// <summary>
        /// Interpolate colours between pixels.
        /// </summary>
        Interpolate = 0x0008,
        /// <summary>
        /// Keep aspect ratio during scaling.
        /// </summary>
        Scale = 0x0010,
        /// <summary>
        /// Display mouse cursor.
        /// </summary>
        ShowCursor = 0x0020,
        /// <summary>
        /// Allows window to be resized.
        /// </summary>
        Sizeable = 0x0040,
        /// <summary>
        /// Allows fullscreen switching TODO: ???
        /// </summary>
        ScreenKey = 0x0080,

        SyncVertex3 = 0x0100,
        StudioVersionB1 = 0x0200,
        StudioVersionB2 = 0x0400,
        StudioVersionB3 = 0x0800,

        /// <summary>
        /// studioVersion = (infoFlags &amp; InfoFlags.StudioVersionMask) >> 9
        /// </summary>
        StudioVersionMask = 0x0E00,
        /// <summary>
        /// Whether Steam (or the YoYoPlayer) is enabled.
        /// </summary>
        SteamEnabled = 0x1000,

        /// <summary>
        /// When enabled, the game will write save data to %appdata%\GameName, otherwise it will write to %localappdata%\GameName
        /// </summary>
        UseAppDataSaveLocation = 0x2000,

        /// <summary>
        /// Whether the game supports borderless window
        /// </summary>
        BorderlessWindow = 0x4000,
        /// <summary>
        /// Tells the runner to run Javascript code
        /// </summary>
        JavaScriptMode = 0x8000,

        LicenseExclusions = 0x10000,

        /// <summary>
        /// This flag is set when a game is launched from the Gamemaker Studio 2 IDE using the 'Run' or 'Debug' options
        /// </summary>
        GameRunFromGMS2IDE = 0x20000,
    }

    /// <summary>
    /// Function classifications a data file can have.
    /// </summary>
    [Flags]
    public enum FunctionClassification : ulong
    {
        None = 0x0UL,
        Internet = 0x1UL,
        Joystick = 0x2UL,
        Gamepad = 0x4UL,
        Immersion = 0x8UL,
        Screengrab = 0x10UL,
        Math = 0x20UL,
        Action = 0x40UL,
        MatrixD3D = 0x80UL,
        D3DModel = 0x100UL,
        DataStructures = 0x200UL,
        File = 0x400UL,
        INI = 0x800UL,
        Filename = 0x1000UL,
        Directory = 0x2000UL,
        Environment = 0x4000UL,
        UNUSED1 = 0x8000UL,
        HTTP = 0x10000UL,
        Encoding = 0x20000UL,
        UIDialog = 0x40000UL,
        MotionPlanning = 0x80000UL,
        ShapeCollision = 0x100000UL,
        Instance = 0x200000UL,
        Room = 0x400000UL,
        Game = 0x800000UL,
        Display = 0x1000000UL,
        Device = 0x2000000UL,
        Window = 0x4000000UL,
        DrawColor = 0x8000000UL,
        Texture = 0x10000000UL,
        Layer = 0x20000000UL,
        String = 0x40000000UL,
        Tiles = 0x80000000UL,
        Surface = 0x100000000UL,
        Skeleton = 0x200000000UL,
        IO = 0x400000000UL,
        Variables = 0x800000000UL,
        Array = 0x1000000000UL,
        ExternalCall = 0x2000000000UL,
        Notification = 0x4000000000UL,
        Date = 0x8000000000UL,
        Particle = 0x10000000000UL,
        Sprite = 0x20000000000UL,
        Clickable = 0x40000000000UL,
        LegacySound = 0x80000000000UL,
        Audio = 0x100000000000UL,
        Event = 0x200000000000UL,
        UNUSED2 = 0x400000000000UL,
        FreeType = 0x800000000000UL,
        Analytics = 0x1000000000000UL,
        UNUSED3 = 0x2000000000000UL,
        UNUSED4 = 0x4000000000000UL,
        Achievement = 0x8000000000000UL,
        CloudSaving = 0x10000000000000UL,
        Ads = 0x20000000000000UL,
        OS = 0x40000000000000UL,
        IAP = 0x80000000000000UL,
        Facebook = 0x100000000000000UL,
        Physics = 0x200000000000000UL,
        FlashAA = 0x400000000000000UL,
        Console = 0x800000000000000UL,
        Buffer = 0x1000000000000000UL,
        Steam = 0x2000000000000000UL,
        UNUSED5 = 2310346608841064448UL,
        Shaders = 0x4000000000000000UL,
        VertexBuffers = 9223372036854775808UL
    }

    /// <summary>
    /// Indicates whether debugging support is disabled.
    /// </summary>
    public bool IsDebuggerDisabled { get; set; } = true;

    /// <summary>
    /// The bytecode version of the data file.
    /// </summary>
    public byte BytecodeVersion { get; set; } = 0x10;

    /// <summary>
    /// Likely padding, IsDebuggerDisabled and BytecodeVersion are written together as a 32-bit integer
    /// </summary>
    public ushort Padding { get; set; } = 0;

    /// <summary>
    /// The file name of the runner.
    /// </summary>
    public UndertaleString FileName { get; set; }

    /// <summary>
    /// Which GameMaker configuration the data file was compiled with.
    /// </summary>
    public UndertaleString Config { get; set; }

    /// <summary>
    /// The last object id of the data file.
    /// </summary>
    public uint LastObj { get; set; } = 100000;

    /// <summary>
    /// The last tile id of the data file.
    /// </summary>
    public uint LastTile { get; set; } = 10000000;

    /// <summary>
    /// The game id of the data file.
    /// </summary>
    public uint GameID { get; set; } = 13371337;

    /// <summary>
    /// The DirectPlay GUID of the data file
    /// </summary>
    /// <remarks>This is always empty in Game Maker: Studio.</remarks>
    public Guid DirectPlayGuid { get; set; } = Guid.Empty;

    /// <summary>
    /// The name of the game.
    /// </summary>
    public UndertaleString Name { get; set; }

    /// <summary>
    /// Different GameMaker release branches. LTS has some but not all features of equivalent newer versions.
    /// </summary>
    public enum BranchType
    {
        Pre2022_0,
        LTS2022_0,
        Post2022_0
    }

    /// <summary>
    /// The GameMaker release branch of the data file. May be set to <see cref="BranchType.Post2022_0"/> when features exempted from LTS are detected.
    /// </summary>
    public BranchType Branch = BranchType.Pre2022_0;

    /// <summary>
    /// The major version of the data file.
    /// If greater than 1, serialization produces "2.0.0.0" due to the flag no longer updating in data.win
    /// </summary>
    public uint Major { get; set; } = 1;

    /// <summary>
    /// The minor version of the data file.
    /// </summary>
    public uint Minor { get; set; } = 0;

    /// <summary>
    /// The Release version of the data file.
    /// </summary>
    public uint Release { get; set; } = 0;

    /// <summary>
    /// The build version of the data file.
    /// </summary>
    public uint Build { get; set; } = 1337;

    /// <summary>
    /// The default window width of the game.
    /// </summary>
    public uint DefaultWindowWidth { get; set; } = 1024;

    /// <summary>
    /// The default window height of the game.
    /// </summary>
    public uint DefaultWindowHeight { get; set; } = 768;

    /// <summary>
    /// The info flags of the data file.
    /// </summary>
    public InfoFlags Info { get; set; } = InfoFlags.Interpolate | InfoFlags.Scale | InfoFlags.ShowCursor | InfoFlags.ScreenKey | InfoFlags.StudioVersionB3;

    /// <summary>
    /// The MD5 of the license used to compile the game.
    /// </summary>
    public byte[] LicenseMD5 { get; set; } = new byte[16];

    /// <summary>
    /// The CRC32 of the license used to compile the game.
    /// </summary>
    public uint LicenseCRC32 { get; set; }

    /// <summary>
    /// The UNIX timestamp the game was compiled.
    /// </summary>
    public ulong Timestamp { get; set; } = 0;

    /// <summary>
    /// The name that gets displayed in the window.
    /// </summary>
    public UndertaleString DisplayName { get; set; }


    public ulong ActiveTargets { get; set; } = 0;

    /// <summary>
    /// The function classifications of this data file.
    /// </summary>
    public FunctionClassification FunctionClassifications { get; set; } = FunctionClassification.None; // Initializing it with None is a very bad idea. TODO: then do something about it?

    /// <summary>
    /// The Steam app id of the game.
    /// </summary>
    public int SteamAppID { get; set; } = 0;

    /// <summary>
    /// The port the data file exposes for the debugger.
    /// </summary>
    public uint DebuggerPort { get; set; } = 6502;

    /// <summary>
    /// The room order of the data file.
    /// </summary>
    public UndertaleSimpleResourcesList<UndertaleRoom, UndertaleChunkROOM> RoomOrder { get; set; } = new();

    /// <summary>
    /// TODO: unknown, some sort of checksum.
    /// </summary>
    public List<long> GMS2RandomUID { get; set; } = new List<long>();

    /// <summary>
    /// The FPS of the data file. GameMaker Studio 2 only.
    /// </summary>
    public float GMS2FPS { get; set; } = 30.0f;

    /// <summary>
    /// Whether the data file allows statistics. GameMaker Studio 2 only.
    /// </summary>
    public bool GMS2AllowStatistics { get; set; } = true;

    /// <summary>
    /// TODO: more unknown checksum data.
    /// </summary>
    public byte[] GMS2GameGUID { get; set; } = new byte[16];

    /// <summary>
    /// Whether the random UID's timestamp was initially offset.
    /// </summary>
    public bool InfoTimestampOffset { get; set; } = true;

    public static (uint, uint, uint, uint, BranchType) TestForCommonGMSVersions(UndertaleReader reader,
                                                                    (uint, uint, uint, uint, BranchType) readVersion)
    {
        (uint Major, uint Minor, uint Release, uint Build, BranchType Branch) detectedVer = readVersion;

        // Some GMS2+ version detection. The rest is spread around, mostly in UndertaleChunks.cs
        if (reader.AllChunkNames.Contains("UILR"))      // 2024.13, not present on LTS
            detectedVer = (2024, 13, 0, 0, BranchType.Post2022_0);
        else if (reader.AllChunkNames.Contains("PSEM")) // 2023.2, not present on LTS
            detectedVer = (2023, 2, 0, 0, BranchType.Post2022_0);
        else if (reader.AllChunkNames.Contains("FEAT")) // 2022.8
            detectedVer = (2022, 8, 0, 0, BranchType.Pre2022_0);
        else if (reader.AllChunkNames.Contains("FEDS")) // 2.3.6
            detectedVer = (2, 3, 6, 0, BranchType.Pre2022_0);
        else if (reader.AllChunkNames.Contains("SEQN")) // 2.3
            detectedVer = (2, 3, 0, 0, BranchType.Pre2022_0);
        else if (reader.AllChunkNames.Contains("TGIN")) // 2.2.1
            detectedVer = (2, 2, 1, 0, BranchType.Pre2022_0);

        return detectedVer;
    }

    /// <inheritdoc/>
    /// <exception cref="IOException">If <see cref="LicenseMD5"/> or <see cref="GMS2GameGUID"/> has an invalid length.</exception>
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(IsDebuggerDisabled ? (byte)1 : (byte)0);
        writer.Write(BytecodeVersion);
        writer.Write(Padding);
        writer.WriteUndertaleString(FileName);
        writer.WriteUndertaleString(Config);
        writer.Write(LastObj);
        writer.Write(LastTile);
        writer.Write(GameID);
        writer.Write(DirectPlayGuid.ToByteArray());
        writer.WriteUndertaleString(Name);
        if (Major == 1)
        {
            writer.Write(Major);
            writer.Write(Minor);
            writer.Write(Release);
            writer.Write(Build);
        }
        else
        {
            // The version number here is no longer updated,
            // but it's still useful for the tool
            writer.Write((uint)2);
            writer.Write((uint)0);
            writer.Write((uint)0);
            writer.Write((uint)0);
        }
        writer.Write(DefaultWindowWidth);
        writer.Write(DefaultWindowHeight);
        writer.Write((uint)Info);
        writer.Write(LicenseCRC32);
        if (LicenseMD5.Length != 16)
            throw new IOException("LicenseMD5 has invalid length");
        writer.Write(LicenseMD5);
        writer.Write(Timestamp);
        writer.WriteUndertaleString(DisplayName);

        // Should always be zero.
        writer.Write(ActiveTargets);

        // Very weird value. Decides if Runner should initialize certain subsystems.
        writer.Write((ulong)FunctionClassifications);

        writer.Write(SteamAppID);
        if (BytecodeVersion >= 14)
            writer.Write(DebuggerPort);
        writer.WriteUndertaleObject(RoomOrder);
        if (Major >= 2)
        {
            // Write random UID
            Random random = new Random((int)(Timestamp & 4294967295L));
            long firstRandom = (long)random.Next() << 32 | (long)random.Next();
            long infoNumber = GetInfoNumber(firstRandom, InfoTimestampOffset);
            int infoLocation = Math.Abs((int)((long)Timestamp & 65535L) / 7 + (int)(GameID - DefaultWindowWidth) + RoomOrder.Count) % 4;
            GMS2RandomUID.Clear();
            writer.Write(firstRandom);
            GMS2RandomUID.Add(firstRandom);
            for (int i = 0; i < 4; i++)
            {
                if (i == infoLocation)
                {
                    writer.Write(infoNumber);
                    GMS2RandomUID.Add(infoNumber);
                }
                else
                {
                    int first = random.Next();
                    int second = random.Next();
                    writer.Write(first);
                    writer.Write(second);
                    GMS2RandomUID.Add(((long)first << 32) | (long)second);
                }
            }
            writer.Write(GMS2FPS);
            if (GMS2GameGUID.Length != 16)
                throw new IOException("GMS2GameGUID has invalid length");
            writer.Write(GMS2AllowStatistics);
            writer.Write(GMS2GameGUID);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Func<UndertaleString> readFileNameDelegate;
        if (reader.ReadOnlyGEN8)
            readFileNameDelegate = () =>
            {
                UndertaleString res = reader.ReadUndertaleString();
                if (res.Content is not null)
                    return res;

                reader.SwitchReaderType(false);
                long returnTo = reader.Position;
                reader.Position = reader.GetOffsetMapRev()[res];
                reader.ReadUndertaleObject<UndertaleString>();
                reader.Position = returnTo;
                reader.SwitchReaderType(true);

                return res;
            };
        else
            readFileNameDelegate = reader.ReadUndertaleString;

        IsDebuggerDisabled = reader.ReadByte() != 0;
        BytecodeVersion = reader.ReadByte();
        Padding = reader.ReadUInt16();
        FileName = readFileNameDelegate();
        Config = reader.ReadUndertaleString();
        LastObj = reader.ReadUInt32();
        LastTile = reader.ReadUInt32();
        GameID = reader.ReadUInt32();
        byte[] guidData = reader.ReadBytes(16);
        DirectPlayGuid = new Guid(guidData);
        Name = reader.ReadUndertaleString();
        Major = reader.ReadUInt32();
        Minor = reader.ReadUInt32();
        Release = reader.ReadUInt32();
        Build = reader.ReadUInt32();

        if (reader.ReadOnlyGEN8)
            return;

        // TestForCommonGMSVersions is run during the object counting phase, so the previous general info is always accurate.
        var prevGenInfo = reader.undertaleData.GeneralInfo;
        Major = prevGenInfo.Major;
        Minor = prevGenInfo.Minor;
        Release = prevGenInfo.Release;
        Build = prevGenInfo.Build;
        Branch = prevGenInfo.Branch;

        DefaultWindowWidth = reader.ReadUInt32();
        DefaultWindowHeight = reader.ReadUInt32();
        Info = (InfoFlags)reader.ReadUInt32();
        LicenseCRC32 = reader.ReadUInt32();
        LicenseMD5 = reader.ReadBytes(16);
        Timestamp = reader.ReadUInt64();
        DisplayName = reader.ReadUndertaleString();
        ActiveTargets = reader.ReadUInt64();
        FunctionClassifications = (FunctionClassification)reader.ReadUInt64();
        SteamAppID = reader.ReadInt32();
        if (BytecodeVersion >= 14)
            DebuggerPort = reader.ReadUInt32();
        RoomOrder = reader.ReadUndertaleObject<UndertaleSimpleResourcesList<UndertaleRoom, UndertaleChunkROOM>>();
        if (Major >= 2)
        {
            // Begin parsing random UID, and verify it based on original algorithm
            GMS2RandomUID = new List<long>();

            Random random = new Random((int)((long)Timestamp & 4294967295L));
            long firstRandom = (long)random.Next() << 32 | (long)random.Next();
            if (reader.ReadInt64() != firstRandom)
                throw new Exception("Unexpected random UID #1");
            int infoLocation = Math.Abs((int)((long)Timestamp & 65535L) / 7 + (int)(GameID - DefaultWindowWidth) + RoomOrder.Count) % 4;
            for (int i = 0; i < 4; i++)
            {
                if (i == infoLocation)
                {
                    long curr = reader.ReadInt64();
                    GMS2RandomUID.Add(curr);
                    if (curr != GetInfoNumber(firstRandom, true))
                    {
                        if (curr != GetInfoNumber(firstRandom, false))
                            throw new Exception("Unexpected random UID info");
                        else
                            InfoTimestampOffset = false;
                    }
                }
                else
                {
                    int first = reader.ReadInt32();
                    int second = reader.ReadInt32();
                    if (first != random.Next())
                        throw new Exception("Unexpected random UID #2");
                    if (second != random.Next())
                        throw new Exception("Unexpected random UID #3");
                    GMS2RandomUID.Add((long)(first << 32) | (long)second);
                }
            }
            GMS2FPS = reader.ReadSingle();
            GMS2AllowStatistics = reader.ReadBoolean();
            GMS2GameGUID = reader.ReadBytes(16);
        }
        reader.undertaleData.UnsupportedBytecodeVersion = BytecodeVersion < 13 || BytecodeVersion > 17;
        reader.Bytecode14OrLower = BytecodeVersion <= 14;
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        reader.Position++; // "IsDebuggerDisabled"
        byte bytecodeVer = reader.ReadByte();
        bool readDebugPort = bytecodeVer >= 14;

        reader.Position += (uint)(122 + (readDebugPort ? 4 : 0));

        // "RoomOrder"
        return 1 + UndertaleSimpleResourcesList<UndertaleRoom, UndertaleChunkROOM>.UnserializeChildObjectCount(reader);
    }

    /// <summary>
    /// Generates "info number" used for GMS2 UIDs.
    /// </summary>
    private long GetInfoNumber(long firstRandom, bool infoTimestampOffset)
    {
        long infoNumber = (long)Timestamp;
        if (infoTimestampOffset)
            infoNumber -= 1000;
        ulong temp = (ulong)infoNumber;
        temp = ((temp << 56 & 18374686479671623680UL) | (temp >> 8 & 71776119061217280UL) |
                (temp << 32 & 280375465082880UL) | (temp >> 16 & 1095216660480UL) | (temp << 8 & 4278190080UL) |
                (temp >> 24 & 16711680UL) | (temp >> 16 & 65280UL) | (temp >> 32 & 255UL));
        infoNumber = (long)temp;
        infoNumber ^= firstRandom;
        infoNumber = ~infoNumber;
        infoNumber ^= ((long)GameID << 32 | (long)GameID);
        infoNumber ^= ((long)(DefaultWindowWidth + (int)Info) << 48 |
                       (long)(DefaultWindowHeight + (int)Info) << 32 |
                       (long)(DefaultWindowHeight + (int)Info) << 16 |
                       (long)(DefaultWindowWidth + (int)Info));
        infoNumber ^= BytecodeVersion;
        return infoNumber;
    }

    /// <inheritdoc />
    public override string ToString()
    {
        if (Major == 1)
            return DisplayName + " (GMS " + Major + "." + Minor + "." + Release + "." + Build + ", bytecode " + BytecodeVersion + ")";
        else
        {
            StringBuilder sb = new(DisplayName?.ToString() ?? "");
            if (Major < 2022 || (Major == 2022 && Minor < 3))
                sb.Append(" (GMS ");
            else
                sb.Append(" (GM ");
            if (Branch == BranchType.LTS2022_0) // TODO: Is there some way to dynamically get this from the enum?
            {
                sb.Append("2022.0");
            }
            else
            {
                sb.Append(Major);
                sb.Append('.');
                sb.Append(Minor);
            }
            if (Release != 0)
            {
                sb.Append('.');
                sb.Append(Release);
                if (Build != 0)
                {
                    sb.Append('.');
                    sb.Append(Build);
                }
            }
            if (Major < 2022)
            {
                sb.Append(", bytecode ");
                sb.Append(BytecodeVersion);
            }
            sb.Append(')');
            return sb.ToString();
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        FileName = null;
        Config = null;
        Name = null;
        DisplayName = null;
        RoomOrder = new();
        GMS2RandomUID = new();
    }
}

/// <summary>
/// General options about a data file.
/// </summary>
[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleOptions : UndertaleObject, IDisposable
{
    /// <summary>
    /// Option flags a data file can use
    /// </summary>
    [Flags]
    public enum OptionsFlags : ulong
    {
        /// <summary>
        /// If the game should start in fullscreen.
        /// </summary>
        FullScreen = 0x1,
        /// <summary>
        /// If pixels should be interpolated.
        /// </summary>
        InterpolatePixels = 0x2,
        /// <summary>
        /// If the new audio format should be used.
        /// </summary>
        UseNewAudio = 0x4,
        /// <summary>
        /// If borderless window should be used.
        /// </summary>
        NoBorder = 0x8,
        /// <summary>
        /// If the mouse cursor should be shown.
        /// </summary>
        ShowCursor = 0x10,
        /// <summary>
        /// If the window should be resizable.
        /// </summary>
        Sizeable = 0x20,
        /// <summary>
        /// If the window should stay on top.
        /// </summary>
        StayOnTop = 0x40,
        /// <summary>
        /// If the resolution can be changed.
        /// </summary>
        ChangeResolution = 0x80,

        NoButtons = 0x100,
        ScreenKey = 0x200,
        HelpKey = 0x400,
        QuitKey = 0x800,
        SaveKey = 0x1000,
        ScreenShotKey = 0x2000,
        CloseSec = 0x4000,
        Freeze = 0x8000,
        ShowProgress = 0x10000,
        LoadTransparent = 0x20000,
        ScaleProgress = 0x40000,
        DisplayErrors = 0x80000,
        WriteErrors = 0x100000,
        AbortErrors = 0x200000,
        VariableErrors = 0x400000,
        CreationEventOrder = 0x800000,
        UseFrontTouch = 0x1000000,
        UseRearTouch = 0x2000000,
        UseFastCollision = 0x4000000,
        FastCollisionCompatibility = 0x8000000,
        DisableSandbox = 0x10000000,
        EnableCopyOnWrite = 0x20000000,
        LegacyJsonParsing = 0x40000000,
        LegacyNumberConversion = 0x80000000,
        LegacyOtherBehavior = 0x100000000,
        AudioErrorBehavior = 0x200000000,
        AllowInstanceChange = 0x400000000,
        LegacyPrimitiveDrawing = 0x800000000
    }

    /// <summary>
    /// Shader extension flag. Always set to int.MinValue
    /// </summary>
    public int ShaderExtensionFlag { get; set; } = int.MinValue;

    /// <summary>
    /// Shader extension version. Always set to 2
    /// </summary>
    public int ShaderExtensionVersion { get; set; } = 2;

    /// <summary>
    /// Option flags the data file uses.
    /// </summary>
    public OptionsFlags Info { get; set; } = OptionsFlags.InterpolatePixels | OptionsFlags.UseNewAudio | OptionsFlags.ShowCursor | OptionsFlags.ScreenKey | OptionsFlags.QuitKey | OptionsFlags.SaveKey | OptionsFlags.ScreenShotKey | OptionsFlags.CloseSec | OptionsFlags.ScaleProgress | OptionsFlags.DisplayErrors | OptionsFlags.VariableErrors | OptionsFlags.CreationEventOrder;

    /// <summary>
    /// The window scale. // TODO: is this a legacy gm thing, or still used today? 
    /// </summary>
    public int Scale { get; set; } = -1;

    /// <summary>
    /// The window color. TODO: unused? Legacy GM remnant? Is this the "Color outside the room region" thing?
    /// </summary>
    public uint WindowColor { get; set; } = 0;

    /// <summary>
    /// The Color depth the game uses. Used only in Game Maker 8 and earlier.
    /// </summary>
    public uint ColorDepth { get; set; } = 0;

    /// <summary>
    /// The game's resolution. Used only in Game Maker 8 and earlier.
    /// </summary>
    public uint Resolution { get; set; } = 0;

    /// <summary>
    /// The game's refresh rate. Used only in Game Maker 8 and earlier.
    /// </summary>
    public uint Frequency { get; set; } = 0;

    /// <summary>
    /// Whether the game uses V-Sync. Used only in Game Maker 8 and earlier.
    /// </summary>
    public uint VertexSync { get; set; } = 0;

    /// <summary>
    /// The priority of the game process. The higher the number, the more priority will be given to the game. Used only in Game Maker 8 and earlier.
    /// </summary>
    public uint Priority { get; set; } = 0;
    
    /// <summary>
    /// The background of the loading bar when loading GameMaker 8 games.
    /// </summary>
    public UndertaleSprite.TextureEntry BackImage { get; set; } = new UndertaleSprite.TextureEntry();
    
    /// <summary>
    /// The image of the loading bar when loading GameMaker 8 games.
    /// </summary>
    public UndertaleSprite.TextureEntry FrontImage { get; set; } = new UndertaleSprite.TextureEntry();
    
    /// <summary>
    /// The image that gets shown when loading GameMaker 8 games.
    /// </summary>
    public UndertaleSprite.TextureEntry LoadImage { get; set; } = new UndertaleSprite.TextureEntry();
    
    /// <summary>
    /// The transparency value of <see cref="LoadImage"/>. 255 indicates fully opaque, 0 means fully transparent. 
    /// </summary>
    public uint LoadAlpha { get; set; } = 255;

    /// <summary>
    /// A list of Constants that the game uses.
    /// </summary>
    public UndertaleSimpleList<Constant> Constants { get; set; } = new UndertaleSimpleList<Constant>();

    //TODO: not shown in GUI right now!!!
    public bool NewFormat { get; set; } = true;

    /// <summary>
    /// A class for game constants.
    /// </summary>
    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class Constant : UndertaleObject, IStaticChildObjectsSize, IDisposable
    {
        public static readonly uint ChildObjectsSize = 8;
        /// <summary>
        /// The name of the constant.
        /// </summary>
        public UndertaleString Name { get; set; }

        /// <summary>
        /// The value of the constant.
        /// </summary>
        public UndertaleString Value { get; set; }

        /// <inheritdoc />
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(Value);
        }

        /// <inheritdoc />
        public void Unserialize(UndertaleReader reader)
        {
            Name = reader.ReadUndertaleString();
            Value = reader.ReadUndertaleString();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Name = null;
            Value = null;
        }
    }

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        if (NewFormat)
        {
            writer.Write(ShaderExtensionFlag);
            writer.Write(ShaderExtensionVersion);
            writer.Write((ulong)Info);
            writer.Write(Scale);
            writer.Write(WindowColor);
            writer.Write(ColorDepth);
            writer.Write(Resolution);
            writer.Write(Frequency);
            writer.Write(VertexSync);
            writer.Write(Priority);
            writer.WriteUndertaleObject(BackImage);
            writer.WriteUndertaleObject(FrontImage);
            writer.WriteUndertaleObject(LoadImage);
            writer.Write(LoadAlpha);
            writer.WriteUndertaleObject(Constants);
        }
        else
        {
            writer.Write((Info & OptionsFlags.FullScreen) == OptionsFlags.FullScreen);
            writer.Write((Info & OptionsFlags.InterpolatePixels) == OptionsFlags.InterpolatePixels);
            writer.Write((Info & OptionsFlags.UseNewAudio) == OptionsFlags.UseNewAudio);
            writer.Write((Info & OptionsFlags.NoBorder) == OptionsFlags.NoBorder);
            writer.Write((Info & OptionsFlags.ShowCursor) == OptionsFlags.ShowCursor);
            writer.Write(Scale);
            writer.Write((Info & OptionsFlags.Sizeable) == OptionsFlags.Sizeable);
            writer.Write((Info & OptionsFlags.StayOnTop) == OptionsFlags.StayOnTop);
            writer.Write(WindowColor);
            writer.Write((Info & OptionsFlags.ChangeResolution) == OptionsFlags.ChangeResolution);
            writer.Write(ColorDepth);
            writer.Write(Resolution);
            writer.Write(Frequency);
            writer.Write((Info & OptionsFlags.NoButtons) == OptionsFlags.NoButtons);
            writer.Write(VertexSync);
            writer.Write((Info & OptionsFlags.ScreenKey) == OptionsFlags.ScreenKey);
            writer.Write((Info & OptionsFlags.HelpKey) == OptionsFlags.HelpKey);
            writer.Write((Info & OptionsFlags.QuitKey) == OptionsFlags.QuitKey);
            writer.Write((Info & OptionsFlags.SaveKey) == OptionsFlags.SaveKey);
            writer.Write((Info & OptionsFlags.ScreenShotKey) == OptionsFlags.ScreenShotKey);
            writer.Write((Info & OptionsFlags.CloseSec) == OptionsFlags.CloseSec);
            writer.Write(Priority);
            writer.Write((Info & OptionsFlags.Freeze) == OptionsFlags.Freeze);
            writer.Write((Info & OptionsFlags.ShowProgress) == OptionsFlags.ShowProgress);
            writer.WriteUndertaleObject(BackImage);
            writer.WriteUndertaleObject(FrontImage);
            writer.WriteUndertaleObject(LoadImage);
            writer.Write((Info & OptionsFlags.LoadTransparent) == OptionsFlags.LoadTransparent);
            writer.Write(LoadAlpha);
            writer.Write((Info & OptionsFlags.ScaleProgress) == OptionsFlags.ScaleProgress);
            writer.Write((Info & OptionsFlags.DisplayErrors) == OptionsFlags.DisplayErrors);
            writer.Write((Info & OptionsFlags.WriteErrors) == OptionsFlags.WriteErrors);
            writer.Write((Info & OptionsFlags.AbortErrors) == OptionsFlags.AbortErrors);
            writer.Write((Info & OptionsFlags.VariableErrors) == OptionsFlags.VariableErrors);
            writer.Write((Info & OptionsFlags.CreationEventOrder) == OptionsFlags.CreationEventOrder);
            writer.WriteUndertaleObject(Constants);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        NewFormat = reader.ReadInt32() == Int32.MinValue;
        reader.Position -= 4;
        if (NewFormat)
        {
            ShaderExtensionFlag = reader.ReadInt32();
            ShaderExtensionVersion = reader.ReadInt32();
            Info = (OptionsFlags)reader.ReadUInt64();
            Scale = reader.ReadInt32();
            WindowColor = reader.ReadUInt32();
            ColorDepth = reader.ReadUInt32();
            Resolution = reader.ReadUInt32();
            Frequency = reader.ReadUInt32();
            VertexSync = reader.ReadUInt32();
            Priority = reader.ReadUInt32();
            BackImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
            FrontImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
            LoadImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
            LoadAlpha = reader.ReadUInt32();
            Constants = reader.ReadUndertaleObject<UndertaleSimpleList<Constant>>();
        }
        else
        {
            Info = 0;
            if (reader.ReadBoolean()) Info |= OptionsFlags.FullScreen;
            if (reader.ReadBoolean()) Info |= OptionsFlags.InterpolatePixels;
            if (reader.ReadBoolean()) Info |= OptionsFlags.UseNewAudio;
            if (reader.ReadBoolean()) Info |= OptionsFlags.NoBorder;
            if (reader.ReadBoolean()) Info |= OptionsFlags.ShowCursor;
            Scale = reader.ReadInt32();
            if (reader.ReadBoolean()) Info |= OptionsFlags.Sizeable;
            if (reader.ReadBoolean()) Info |= OptionsFlags.StayOnTop;
            WindowColor = reader.ReadUInt32();
            if (reader.ReadBoolean()) Info |= OptionsFlags.ChangeResolution;
            ColorDepth = reader.ReadUInt32();
            Resolution = reader.ReadUInt32();
            Frequency = reader.ReadUInt32();
            if (reader.ReadBoolean()) Info |= OptionsFlags.NoButtons;
            VertexSync = reader.ReadUInt32();
            if (reader.ReadBoolean()) Info |= OptionsFlags.ScreenKey;
            if (reader.ReadBoolean()) Info |= OptionsFlags.HelpKey;
            if (reader.ReadBoolean()) Info |= OptionsFlags.QuitKey;
            if (reader.ReadBoolean()) Info |= OptionsFlags.SaveKey;
            if (reader.ReadBoolean()) Info |= OptionsFlags.ScreenShotKey;
            if (reader.ReadBoolean()) Info |= OptionsFlags.CloseSec;
            Priority = reader.ReadUInt32();
            if (reader.ReadBoolean()) Info |= OptionsFlags.Freeze;
            if (reader.ReadBoolean()) Info |= OptionsFlags.ShowProgress;
            BackImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
            FrontImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
            LoadImage = reader.ReadUndertaleObject<UndertaleSprite.TextureEntry>();
            if (reader.ReadBoolean()) Info |= OptionsFlags.LoadTransparent;
            LoadAlpha = reader.ReadUInt32();
            if (reader.ReadBoolean()) Info |= OptionsFlags.ScaleProgress;
            if (reader.ReadBoolean()) Info |= OptionsFlags.DisplayErrors;
            if (reader.ReadBoolean()) Info |= OptionsFlags.WriteErrors;
            if (reader.ReadBoolean()) Info |= OptionsFlags.AbortErrors;
            if (reader.ReadBoolean()) Info |= OptionsFlags.VariableErrors;
            if (reader.ReadBoolean()) Info |= OptionsFlags.CreationEventOrder;
            Constants = reader.ReadUndertaleObject<UndertaleSimpleList<Constant>>();
        }
    }

    /// <inheritdoc cref="UndertaleObject.UnserializeChildObjectCount(UndertaleReader)"/>
    public static uint UnserializeChildObjectCount(UndertaleReader reader)
    {
        uint count = 0;
        bool newFormat = reader.ReadInt32() == Int32.MinValue;
        reader.Position -= 4;

        reader.Position += newFormat ? 60u : 140u;
        count += 3; // images

        // "Constants"
        count += 1 + UndertaleSimpleList<Constant>.UnserializeChildObjectCount(reader);

        return count;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        if (Constants is not null)
        {
            foreach (Constant constant in Constants)
                constant?.Dispose();
        }
        BackImage = new();
        FrontImage = new();
        LoadImage = new();
        Constants = null;
    }
}

[PropertyChanged.AddINotifyPropertyChangedInterface]
public class UndertaleLanguage : UndertaleObject, IDisposable
{
    // Seems to be a list of entry IDs paired to strings for several languages
    public uint Unknown1 { get; set; } = 1;
    public uint LanguageCount { get; set; } = 0;
    public uint EntryCount { get; set; } = 0;

    public List<UndertaleString> EntryIDs { get; set; } = new List<UndertaleString>();
    public List<LanguageData> Languages { get; set; } = new List<LanguageData>();

    /// <inheritdoc />
    public void Serialize(UndertaleWriter writer)
    {
        writer.Write(Unknown1);
        LanguageCount = (uint)Languages.Count;
        writer.Write(LanguageCount);
        EntryCount = (uint)EntryIDs.Count;
        writer.Write(EntryCount);

        foreach (UndertaleString s in EntryIDs)
        {
            writer.WriteUndertaleString(s);
        }

        foreach (LanguageData ld in Languages)
        {
            ld.Serialize(writer);
        }
    }

    /// <inheritdoc />
    public void Unserialize(UndertaleReader reader)
    {
        Unknown1 = reader.ReadUInt32();
        LanguageCount = reader.ReadUInt32();
        EntryCount = reader.ReadUInt32();

        // Read the identifiers for each entry
        for (int i = 0; i < EntryCount; i++)
        {
            EntryIDs.Add(reader.ReadUndertaleString());
        }

        // Read the data for each language
        for (int i = 0; i < LanguageCount; i++)
        {
            LanguageData ld = new LanguageData();
            ld.Unserialize(reader, EntryCount);
            Languages.Add(ld);
        }
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        GC.SuppressFinalize(this);

        foreach (LanguageData data in Languages)
            data?.Dispose();
        EntryIDs = new();
        Languages = new();
    }

    [PropertyChanged.AddINotifyPropertyChangedInterface]
    public class LanguageData : IDisposable
    {
        public UndertaleString Name { get; set; }
        public UndertaleString Region { get; set; }
        public List<UndertaleString> Entries { get; set; } = new List<UndertaleString>();
        // values that correspond to IDs in the main chunk content

        /// <summary>
        /// Serializes <see cref="LanguageData"/> into a specified <see cref="UndertaleWriter"/>.
        /// </summary>
        /// <param name="writer">Where to serialize to.</param>
        public void Serialize(UndertaleWriter writer)
        {
            writer.WriteUndertaleString(Name);
            writer.WriteUndertaleString(Region);
            foreach (UndertaleString s in Entries)
            {
                writer.WriteUndertaleString(s);
            }
        }

        /// <summary>
        /// Deserializes <see cref="LanguageData"/> from a specified <see cref="UndertaleReader"/> and with a specified entries count.
        /// </summary>
        /// <param name="reader">Where to deserialize from.</param>
        /// <param name="entryCount">Count of entries to read.</param>
        public void Unserialize(UndertaleReader reader, uint entryCount)
        {
            Name = reader.ReadUndertaleString();
            Region = reader.ReadUndertaleString();
            for (uint i = 0; i < entryCount; i++)
            {
                Entries.Add(reader.ReadUndertaleString());
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            GC.SuppressFinalize(this);

            Entries = new();
            Name = null;
            Region = null;
        }
    }
}
