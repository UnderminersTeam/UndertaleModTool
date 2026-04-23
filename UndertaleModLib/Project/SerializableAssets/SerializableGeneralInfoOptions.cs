using System.Text.Json.Serialization;
using UndertaleModLib.Models;
using static UndertaleModLib.Models.UndertaleGeneralInfo;
using static UndertaleModLib.Models.UndertaleOptions;

namespace UndertaleModLib.Project.SerializableAssets;

internal sealed class SerializableGeneralInfoOptions
{
    /// <inheritdoc cref="UndertaleGeneralInfo.FileName"/>
    public string FileName { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.Config"/>
    public string Config { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.GameID"/>
    public uint? GameID { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.Name"/>
    public string Name { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.DefaultWindowWidth"/>
    public uint? DefaultWindowWidth { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.DefaultWindowHeight"/>
    public uint? DefaultWindowHeight { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.Info"/>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public InfoFlags? GeneralInfo { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.DisplayName"/>
    public string DisplayName { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.SteamAppID"/>
    public int? SteamAppID { get; set; }

    /// <inheritdoc cref="UndertaleGeneralInfo.GMS2FPS"/>
    public float? GMS2FPS { get; set; }

    /// <inheritdoc cref="UndertaleOptions.Info"/>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OptionsFlags? OptionsInfo { get; set; }

    /// <inheritdoc cref="UndertaleOptions.VertexSync"/>
    public uint? VertexSync { get; set; }

    /// <summary>
    /// Imports the data from this object into the given data file.
    /// </summary>
    public void ImportTo(UndertaleData data)
    {
        UndertaleGeneralInfo generalInfo = data.GeneralInfo;
        if (FileName is not null)
        {
            generalInfo.FileName = data.Strings.MakeString(FileName);
        }
        if (Config is not null)
        {
            generalInfo.Config = data.Strings.MakeString(Config);
        }
        if (GameID is not null)
        {
            generalInfo.GameID = GameID.Value;
        }
        if (Name is not null)
        {
            generalInfo.Name = data.Strings.MakeString(Name);
        }
        if (DefaultWindowWidth is not null)
        {
            generalInfo.DefaultWindowWidth = DefaultWindowWidth.Value;
        }
        if (DefaultWindowHeight is not null)
        {
            generalInfo.DefaultWindowHeight = DefaultWindowHeight.Value;
        }
        if (GeneralInfo is not null)
        {
            generalInfo.Info = GeneralInfo.Value;
        }
        if (DisplayName is not null)
        {
            generalInfo.DisplayName = data.Strings.MakeString(DisplayName);
        }
        if (SteamAppID is not null)
        {
            generalInfo.SteamAppID = SteamAppID.Value;
        }
        if (GMS2FPS is not null)
        {
            generalInfo.GMS2FPS = GMS2FPS.Value;
        }

        UndertaleOptions options = data.Options;
        if (OptionsInfo is not null)
        {
            options.Info = OptionsInfo.Value;
        }
        if (VertexSync is not null)
        {
            options.VertexSync = VertexSync.Value;
        }
    }
}
