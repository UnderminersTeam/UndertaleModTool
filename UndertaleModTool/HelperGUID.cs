using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.Hosting;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack.Dialogs;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Decompiler;
using UndertaleModLib.Models;
using UndertaleModLib.ModelsDebug;
using UndertaleModLib.Scripting;
using UndertaleModTool.Windows;
using System.Security.Cryptography;

namespace UndertaleModTool
{
    //Make new GUID helper functions
    public partial class MainWindow : Window, INotifyPropertyChanged, IScriptInterface
    {
        public void ReassignGUIDs(string GUID, uint ObjectIndex)
        {
            int eventIdx = (int)Enum.Parse(typeof(EventTypes), "Collision");
            for (var i = 0; i < Data.GameObjects.Count; i++)
            {
                UndertaleGameObject obj = Data.GameObjects[i];
                try
                {
                    foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                    {
                        foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                        {
                            if (action.CodeId.Name.Content.Contains(GUID))
                            {
                                evnt.EventSubtype = ObjectIndex;
                            }
                        }
                    }
                }
                catch
                {
                }
            }
        }
        public uint ReduceCollisionValue(List<uint> possible_values)
        {
            if (possible_values.Count == 1 && (possible_values[0] != 999999))
                return possible_values[0];
            else if (possible_values.Count > 0)
            {
                bool NoObjectFound = true;
                string GameObjectName = "";
                string GameObjectListNames = "";
                foreach (uint objID in possible_values)
                {
                    GameObjectListNames += Data.GameObjects[(int)objID].Name.Content;
                    GameObjectListNames += "\n";
                }
                uint GameObjectIDValue = 0;
                while (NoObjectFound)
                {
                    string object_index = SimpleTextInput("Multiple object were found. Select only one object below from the set, or, if none below match, some other object name:", "Object enter box.", GameObjectListNames, true);
                    for (var i = 0; i < Data.GameObjects.Count; i++)
                    {
                        if (Data.GameObjects[i].Name.Content.ToLower() == object_index.ToLower())
                        {
                            NoObjectFound = false;
                            GameObjectName = Data.GameObjects[i].Name.Content;
                            GameObjectIDValue = (uint)i;
                        }
                    }
                }
                return GameObjectIDValue;
            }
            else if (possible_values[0] == 999999)
            {
                bool NoObjectFound = true;
                string GameObjectName = "";
                uint GameObjectIDValue = 0;
                while (NoObjectFound)
                {
                    string object_index = SimpleTextInput("Object could not be found. Please enter it below:", "Object enter box.", "", false);
                    for (var i = 0; i < Data.GameObjects.Count; i++)
                    {
                        if (Data.GameObjects[i].Name.Content.ToLower() == object_index.ToLower())
                        {
                            NoObjectFound = false;
                            GameObjectName = Data.GameObjects[i].Name.Content;
                            GameObjectIDValue = (uint)i;
                        }
                    }
                }
                return GameObjectIDValue;
            }
            else
            {
                return 0;
            }
        }

        public String GetGUIDFromCodeName(string codeName)
        {
            string afterPrefix = codeName.Substring(11);
            if (afterPrefix.LastIndexOf("_Collision_") != -1)
            {
                string s2 = "_Collision_";
                return afterPrefix.Substring(afterPrefix.LastIndexOf("_Collision_") + s2.Length, afterPrefix.Length - (afterPrefix.LastIndexOf("_Collision_") + s2.Length));
            }
            else
                return "Invalid";
        }

        public List<uint> GetCollisionValueFromCodeNameGUID(string codeName)
        {
            int eventIdx = (int)Enum.Parse(typeof(EventTypes), "Collision");
            List<uint> possible_values = new List<uint>();
            for (var i = 0; i < Data.GameObjects.Count; i++)
            {
                UndertaleGameObject obj = Data.GameObjects[i];
                try
                {
                    foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                    {
                        foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                        {
                            if (action.CodeId.Name.Content == codeName)
                            {
                                if (Data.GameObjects[(int)evnt.EventSubtype] != null)
                                {
                                    possible_values.Add(evnt.EventSubtype);
                                    return possible_values;
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            possible_values = GetCollisionValueFromGUID(GetGUIDFromCodeName(codeName));
            return possible_values;
        }

        public List<uint> GetCollisionValueFromGUID(string GUID)
        {
            int eventIdx = (int)Enum.Parse(typeof(EventTypes), "Collision");
            List<uint> possible_values = new List<uint>();
            for (var i = 0; i < Data.GameObjects.Count; i++)
            {
                UndertaleGameObject obj = Data.GameObjects[i];
                try
                {
                    foreach (UndertaleGameObject.Event evnt in obj.Events[eventIdx])
                    {
                        foreach (UndertaleGameObject.EventAction action in evnt.Actions)
                        {
                            if (action.CodeId.Name.Content.Contains(GUID))
                            {
                                if (!(possible_values.Contains(evnt.EventSubtype)))
                                {
                                    possible_values.Add(evnt.EventSubtype);
                                }
                            }
                        }
                    }
                }
                catch
                {
                }
            }
            if (possible_values.Count == 0)
            {
                possible_values.Add(999999);
                return possible_values;
            }
            else
            {
                return possible_values;
            }
        }
    }
}
