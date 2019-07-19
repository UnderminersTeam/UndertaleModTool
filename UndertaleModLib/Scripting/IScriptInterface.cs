using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Scripting
{
    public interface IScriptInterface
    {
        UndertaleData Data { get; }
        string FilePath { get; }
        object Highlighted { get; }
        object Selected { get; }
        bool CanSave { get; }

        void EnsureDataLoaded();

        void ScriptMessage(string message);
        bool ScriptQuestion(string message);
        void ScriptError(string error, string title);
        void ScriptOpenURL(string url);
        void UpdateProgressBar(string message, string status, double progressValue, double maxValue);
        void HideProgressBar();

        void ChangeSelection(object newsel);

        string PromptChooseDirectory(string prompt);

        string PromptLoadFile(string defaultExt, string filter);
    }
}
