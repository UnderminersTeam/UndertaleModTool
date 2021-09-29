using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using System.Security.Cryptography;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Most likely incompatible with the new Deltarune Chapter 1 & 2 demo, run at your own risk");
}

ScriptMessage("Enter the object(s) to copy");

int copiedGameObjectsCount = 0;
List<String> splitStringsList = new List<String>();
string abc123 = "";
abc123 = SimpleTextInput("Menu", "Enter name(s) of game objects", abc123, true);
string[] subs = abc123.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
foreach (var sub in subs)
{
    splitStringsList.Add(sub.Trim());
}
for (var j = 0; j < splitStringsList.Count; j++)
{
    for (var k = 0; k < Data.GameObjects.Count; k++)
    {
        UndertaleGameObject obj = Data.GameObjects[k];
        if (splitStringsList[j].ToLower() == obj.Name.Content.ToLower())
        {
            UndertaleGameObject donorOBJ = Data.GameObjects.ByName(obj.Name.Content);
            UndertaleGameObject nativeOBJ = new UndertaleGameObject();
            nativeOBJ.Name = Data.Strings.MakeString(obj.Name.Content + "_Copy");
            Data.GameObjects.Add(nativeOBJ);
            nativeOBJ.Sprite = Data.Sprites.ByName(donorOBJ.Sprite.Name.Content);
            nativeOBJ.Visible = donorOBJ.Visible;
            nativeOBJ.Solid = donorOBJ.Solid;
            nativeOBJ.Depth = donorOBJ.Depth;
            nativeOBJ.Persistent = donorOBJ.Persistent;
            nativeOBJ.ParentId = Data.GameObjects.ByName(donorOBJ.ParentId.Name.Content);
            if (donorOBJ.TextureMaskId != null)
                nativeOBJ.TextureMaskId = Data.Sprites.ByName(donorOBJ.TextureMaskId.Name.Content);
            nativeOBJ.UsesPhysics = donorOBJ.UsesPhysics;
            nativeOBJ.IsSensor = donorOBJ.IsSensor;
            nativeOBJ.CollisionShape = donorOBJ.CollisionShape;
            nativeOBJ.Density = donorOBJ.Density;
            nativeOBJ.Restitution = donorOBJ.Restitution;
            nativeOBJ.Group = donorOBJ.Group;
            nativeOBJ.LinearDamping = donorOBJ.LinearDamping;
            nativeOBJ.AngularDamping = donorOBJ.AngularDamping;
            nativeOBJ.Friction = donorOBJ.Friction;
            nativeOBJ.Awake = donorOBJ.Awake;
            nativeOBJ.Kinematic = donorOBJ.Kinematic;
            nativeOBJ.PhysicsVertices.Clear();
            foreach (UndertaleGameObject.UndertalePhysicsVertex vert in donorOBJ.PhysicsVertices)
            {
                UndertaleGameObject.UndertalePhysicsVertex vert_new = new UndertaleGameObject.UndertalePhysicsVertex();
                vert_new.X = vert.X;
                vert_new.Y = vert.Y;
                nativeOBJ.PhysicsVertices.Add(vert_new);
            }
            try
            {
                nativeOBJ.Events.Clear();
                for (var i = 0; i < donorOBJ.Events.Count; i++)
                {
					UndertalePointerList<UndertaleGameObject.Event> newEvent = new UndertalePointerList<UndertaleGameObject.Event>();
                    foreach (UndertaleGameObject.Event evnt in donorOBJ.Events[i])
                    {
                        UndertaleGameObject.Event newevnt = new UndertaleGameObject.Event();
                        foreach (UndertaleGameObject.EventAction donorACT in evnt.Actions)
                        {
                            UndertaleGameObject.EventAction nativeACT = new UndertaleGameObject.EventAction();
                            newevnt.Actions.Add(nativeACT);
                            nativeACT.LibID = donorACT.LibID;
                            nativeACT.ID = donorACT.ID;
                            nativeACT.Kind = donorACT.Kind;
                            nativeACT.UseRelative = donorACT.UseRelative;
                            nativeACT.IsQuestion = donorACT.IsQuestion;
                            nativeACT.UseApplyTo = donorACT.UseApplyTo;
                            nativeACT.ExeType = donorACT.ExeType;
                            if (donorACT.ActionName != null)
                                nativeACT.ActionName = Data.Strings.MakeString(donorACT.ActionName.Content);
                            if (donorACT.CodeId?.Name?.Content != null)
                            {
                                ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));
                                string codeToCopy = "";
                                try
                                {
                                    codeToCopy = (donorACT.CodeId != null ? Decompiler.Decompile(donorACT.CodeId, DECOMPILE_CONTEXT.Value) : "");
                                }
                                catch (Exception e)
                                {
                                    codeToCopy = ("/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                                }
                                string NewGMLName = ((donorACT.CodeId?.Name?.Content).Replace(obj.Name.Content,(obj.Name.Content + "_Copy")));
                                try
                                {
                                    ImportGMLString(NewGMLName, codeToCopy, false, false);
                                }
                                catch (Exception ec)
                                {
                                    ScriptError("Uh oh, " + NewGMLName + " has an error: " + ec.Message);
                                }
                                nativeACT.CodeId = Data.Code.ByName(NewGMLName);
                                nativeACT.CodeId.LocalsCount = donorACT.CodeId.LocalsCount;
                                nativeACT.CodeId.ArgumentsCount = donorACT.CodeId.ArgumentsCount;
                                nativeACT.CodeId.WeirdLocalsFlag = donorACT.CodeId.WeirdLocalsFlag;
                                nativeACT.CodeId.Offset = donorACT.CodeId.Offset;
                                nativeACT.CodeId.WeirdLocalFlag = donorACT.CodeId.WeirdLocalFlag;
                                if (Data?.GeneralInfo.BytecodeVersion > 14)
                                {
                                    UndertaleCodeLocals donorlocals = Data.CodeLocals.ByName(donorACT.CodeId?.Name?.Content);
                                    UndertaleCodeLocals nativelocals = new UndertaleCodeLocals();
                                    nativelocals.Name = Data.Strings.MakeString(NewGMLName);
                                    nativelocals.Locals.Clear();
                                    foreach (UndertaleCodeLocals.LocalVar argsLocalDonor in donorlocals.Locals)
                                    {
                                        UndertaleCodeLocals.LocalVar argsLocal = new UndertaleCodeLocals.LocalVar();
                                        argsLocal.Name = Data.Strings.MakeString(argsLocalDonor.Name.Content);
                                        argsLocal.Index = argsLocalDonor.Index;
                                        nativelocals.Locals.Add(argsLocal);
                                    }
                                    nativeACT.CodeId.LocalsCount = (uint)nativelocals.Locals.Count;
                                    nativeACT.CodeId.GenerateLocalVarDefinitions(nativeACT.CodeId.FindReferencedLocalVars(), nativelocals); // Dunno if we actually need this line, but it seems to work?
                                }
                            }
                            nativeACT.ArgumentCount = donorACT.ArgumentCount;
                            nativeACT.Who = donorACT.Who;
                            nativeACT.Relative = donorACT.Relative;
                            nativeACT.IsNot = donorACT.IsNot;
                            nativeACT.UnknownAlwaysZero = donorACT.UnknownAlwaysZero;
                        }
                        newevnt.EventSubtype = evnt.EventSubtype;
                        newEvent.Add(newevnt);
                    }
					nativeOBJ.Events.Add(newEvent);
                }
            }
            catch
            {
                // Something went wrong, but probably because it's trying to check something non-existent
                // Just keep going
            }
            copiedGameObjectsCount += 1;
        }
    }
}
