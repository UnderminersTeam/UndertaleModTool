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
        object Highlighted { get; }
        object Selected { get; }
        bool CanSave { get; }

        void EnsureDataLoaded();

        void ScriptMessage(string message);
        bool ScriptQuestion(string message);
        void ScriptOpenURL(string url);

        void ChangeSelection(object newsel);
    }
}
