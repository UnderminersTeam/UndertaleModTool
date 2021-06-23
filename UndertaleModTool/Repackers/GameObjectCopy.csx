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

ScriptMessage("Select the file to copy from");

UndertaleData DonorData;
string DonorDataPath = PromptLoadFile(null, null);
if (DonorDataPath == null)
    throw new System.Exception("The donor data path was not set.");

using (var stream = new FileStream(DonorDataPath, FileMode.Open, FileAccess.Read))
    DonorData = UndertaleIO.Read(stream, warning => ScriptMessage("A warning occured while trying to load " + DonorDataPath + ":\n" + warning));
var DonorDataEmbeddedTexturesCount = DonorData.EmbeddedTextures.Count;
DonorData.BuiltinList = new BuiltinList(DonorData);
AssetTypeResolver.InitializeTypes(DonorData);

ScriptMessage("Enter the object(s) to copy");

int copiedGameObjectsCount = 0;
List<String> splitStringsList = new List<String>();
string abc123 = "";
abc123 = SimpleTextInput("Menu", "Enter name(s) of game objects", abc123, true);
string[] subs = abc123.Split(new[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
foreach (var sub in subs)
{
    splitStringsList.Add(sub);
}
for (var j = 0; j < splitStringsList.Count; j++)
{
    foreach (UndertaleGameObject obj in DonorData.GameObjects)
    {
        if (splitStringsList[j].ToLower() == obj.Name.Content.ToLower())
        {
            UndertaleGameObject nativeOBJ = Data.GameObjects.ByName(obj.Name.Content);
            UndertaleGameObject donorOBJ = DonorData.GameObjects.ByName(obj.Name.Content);
            if (nativeOBJ == null)
            {
                nativeOBJ = new UndertaleGameObject();
                nativeOBJ.Name = Data.Strings.MakeString(obj.Name.Content);
                Data.GameObjects.Add(nativeOBJ);
            }
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
                                ThreadLocal<DecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<DecompileContext>(() => new DecompileContext(DonorData, false));
                                string codeToCopy = "";
                                try
                                {
                                    codeToCopy = (donorACT.CodeId != null ? Decompiler.Decompile(donorACT.CodeId, DECOMPILE_CONTEXT.Value) : "");
                                }
                                catch (Exception e)
                                {
                                    codeToCopy = ("/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                                }
                                try
                                {
                                    ImportGMLString(donorACT.CodeId?.Name?.Content, codeToCopy, false, false);
                                }
                                catch (Exception ec)
                                {
                                    ScriptError("Uh oh, " + donorACT.CodeId?.Name?.Content + " has an error: " + ec.Message);
                                }
                                nativeACT.CodeId = Data.Code.ByName(donorACT.CodeId?.Name?.Content);
                                nativeACT.CodeId.LocalsCount = donorACT.CodeId.LocalsCount;
                                nativeACT.CodeId.ArgumentsCount = donorACT.CodeId.ArgumentsCount;
                                nativeACT.CodeId.WeirdLocalsFlag = donorACT.CodeId.WeirdLocalsFlag;
                                nativeACT.CodeId.Offset = donorACT.CodeId.Offset;
                                nativeACT.CodeId.WeirdLocalFlag = donorACT.CodeId.WeirdLocalFlag;
                                if (Data?.GeneralInfo.BytecodeVersion > 14)
                                {
                                    UndertaleCodeLocals nativelocals = Data.CodeLocals.ByName(donorACT.CodeId?.Name?.Content);
                                    UndertaleCodeLocals donorlocals = DonorData.CodeLocals.ByName(donorACT.CodeId?.Name?.Content);
                                    if (Data.CodeLocals.ByName(donorACT.CodeId?.Name?.Content) == null)
                                    {
                                        nativelocals = new UndertaleCodeLocals();
                                        nativelocals.Name = Data.Strings.MakeString(donorACT.CodeId?.Name?.Content);
                                    }
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
