using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using UndertaleModLib.Models;
using UndertaleModLib.Scripting;

namespace UndertaleModTool
{
    // GUID helper functions for collision events
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
                    // Silently ignore, some values can be null along the way
                }
            }
        }

        public uint ReduceCollisionValue(List<uint> possible_values)
        {
            if (possible_values.Count == 1)
            {
                if (possible_values[0] != uint.MaxValue)
                    return possible_values[0];

                // Nothing found, pick new one
                bool obj_found = false;
                uint obj_index = 0;
                while (!obj_found)
                {
                    string object_index = SimpleTextInput("Object could not be found. Please enter it below:",
                                                            "Object enter box.", "", false).ToLower();
                    for (var i = 0; i < Data.GameObjects.Count; i++)
                    {
                        if (Data.GameObjects[i].Name.Content.ToLower() == object_index)
                        {
                            obj_found = true;
                            obj_index = (uint)i;
                        }
                    }
                }
                return obj_index;
            }
            
            if (possible_values.Count != 0)
            {
                // 2 or more possible values, make a list to choose from

                string gameObjectNames = "";
                foreach (uint objID in possible_values)
                    gameObjectNames += Data.GameObjects[(int)objID].Name.Content + "\n";

                bool obj_found = false;
                uint obj_index = 0;
                while (!obj_found)
                {
                    string object_index = SimpleTextInput("Multiple objects were found. Select only one object below from the set, or, if none below match, some other object name:",
                                                          "Object enter box.", gameObjectNames, true).ToLower();
                    for (var i = 0; i < Data.GameObjects.Count; i++)
                    {
                        if (Data.GameObjects[i].Name.Content.ToLower() == object_index)
                        {
                            obj_found = true;
                            obj_index = (uint)i;
                        }
                    }
                }
                return obj_index;
            }

            return 0;
        }

        public string GetGUIDFromCodeName(string codeName)
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
                    // Silently ignore, some values can be null along the way
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
                                if (!possible_values.Contains(evnt.EventSubtype))
                                {
                                    possible_values.Add(evnt.EventSubtype);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    // Silently ignore, some values can be null along the way
                }
            }

            if (possible_values.Count == 0)
            {
                possible_values.Add(uint.MaxValue);
                return possible_values;
            }
            else
            {
                return possible_values;
            }
        }
    }
}
