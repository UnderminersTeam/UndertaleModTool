﻿using System;
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
        string ScriptPath { get; }
        object Highlighted { get; }
        object Selected { get; }
        bool CanSave { get; }
        ScriptConfiguration Configuration { get; }

        void EnsureDataLoaded();

        void ScriptMessage(string message);
        void SetUMTConsoleText(string message);
        bool ScriptQuestion(string message);
        void ScriptError(string error, string title = "Error", bool SetConsoleText = true);
        void ScriptOpenURL(string url);
        string ScriptInputDialog(string titleText, string labelText, string defaultInputBoxText, string cancelButtonText, string submitButtonText, bool isMultiline, bool preventClose);
        string SimpleTextInput(string title, string label, string defaultValue, bool allowMultiline);
        void SetFinishedMessage(bool isFinishedMessageEnabled);
        void UpdateProgressBar(string message, string status, double progressValue, double maxValue);
        void HideProgressBar();

        void ChangeSelection(object newsel);

        string PromptChooseDirectory(string prompt);

        string PromptLoadFile(string defaultExt, string filter);
    }
}
