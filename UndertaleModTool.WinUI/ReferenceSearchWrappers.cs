using System.Collections.Generic;
using UndertaleModLib.Models;

namespace UndertaleModTool.Windows;

public class GeneralInfoEditor
{
    public UndertaleGeneralInfo GeneralInfo { get; }

    public UndertaleOptions Options { get; }

    public UndertaleLanguage Language { get; }

    public GeneralInfoEditor(UndertaleGeneralInfo generalInfo, UndertaleOptions options, UndertaleLanguage language)
    {
        GeneralInfo = generalInfo;
        Options = options;
        Language = language;
    }
}

public class GlobalInitEditor
{
    public IList<UndertaleGlobalInit> GlobalInits { get; }

    public GlobalInitEditor(IList<UndertaleGlobalInit> globalInits)
    {
        GlobalInits = globalInits;
    }
}

public class GameEndEditor
{
    public IList<UndertaleGlobalInit> GameEnds { get; }

    public GameEndEditor(IList<UndertaleGlobalInit> gameEnds)
    {
        GameEnds = gameEnds;
    }
}
