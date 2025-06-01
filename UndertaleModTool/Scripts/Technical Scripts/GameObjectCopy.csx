using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using System.Security.Cryptography;
using UndertaleModLib.Models;
using UndertaleModLib.Decompiler;

EnsureDataLoaded();

ScriptMessage("Select the file to copy from");

UndertaleData DonorData;
string DonorDataPath = PromptLoadFile(null, null);
if (DonorDataPath == null)
    throw new ScriptException("The donor data path was not set.");

using (var stream = new FileStream(DonorDataPath, FileMode.Open, FileAccess.Read))
    DonorData = UndertaleIO.Read(stream, (warning, _) => ScriptMessage("A warning occured while trying to load " + DonorDataPath + ":\n" + warning));
var DonorDataEmbeddedTexturesCount = DonorData.EmbeddedTextures.Count;
DonorData.BuiltinList = new BuiltinList(DonorData);
GameSpecificResolver.Initialize(DonorData);

bool donorIs2_3 = DonorData.IsVersionAtLeast(2, 3);

int copiedGameObjectsCount = 0;
List<string> splitStringsList = GetSplitStringsList("game object");
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
            if (donorOBJ.Sprite != null)
                nativeOBJ.Sprite = Data.Sprites.ByName(donorOBJ.Sprite.Name.Content);
            if (donorOBJ.Visible != null)
                nativeOBJ.Visible = donorOBJ.Visible;
            if (donorOBJ.Solid != null)
                nativeOBJ.Solid = donorOBJ.Solid;
            if (donorOBJ.Depth != null)
                nativeOBJ.Depth = donorOBJ.Depth;
            if (donorOBJ.Persistent != null)
                nativeOBJ.Persistent = donorOBJ.Persistent;
            if (donorOBJ.ParentId != null)
                nativeOBJ.ParentId = Data.GameObjects.ByName(donorOBJ.ParentId.Name.Content);
            if (donorOBJ.TextureMaskId != null)
                nativeOBJ.TextureMaskId = Data.Sprites.ByName(donorOBJ.TextureMaskId.Name.Content);
            if (donorOBJ.UsesPhysics != null)
                nativeOBJ.UsesPhysics = donorOBJ.UsesPhysics;
            if (donorOBJ.IsSensor != null)
                nativeOBJ.IsSensor = donorOBJ.IsSensor;
            if (donorOBJ.CollisionShape != null)
                nativeOBJ.CollisionShape = donorOBJ.CollisionShape;
            if (donorOBJ.Density != null)
                nativeOBJ.Density = donorOBJ.Density;
            if (donorOBJ.Restitution != null)
                nativeOBJ.Restitution = donorOBJ.Restitution;
            if (donorOBJ.Group != null)
                nativeOBJ.Group = donorOBJ.Group;
            if (donorOBJ.LinearDamping != null)
                nativeOBJ.LinearDamping = donorOBJ.LinearDamping;
            if (donorOBJ.AngularDamping != null)
                nativeOBJ.AngularDamping = donorOBJ.AngularDamping;
            if (donorOBJ.Friction != null)
                nativeOBJ.Friction = donorOBJ.Friction;
            if (donorOBJ.Awake != null)
                nativeOBJ.Awake = donorOBJ.Awake;
            if (donorOBJ.Kinematic != null)
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
                    nativeOBJ.Events.Add(newEvent);
                    foreach (UndertaleGameObject.Event evnt in donorOBJ.Events[i])
                    {
                        UndertaleGameObject.Event newevnt = new UndertaleGameObject.Event();
                        newevnt.EventSubtype = evnt.EventSubtype;
                        newEvent.Add(newevnt);
                        foreach (UndertaleGameObject.EventAction donorACT in evnt.Actions)
                        {
                            UndertaleGameObject.EventAction nativeACT = new UndertaleGameObject.EventAction();
                            newevnt.Actions.Add(nativeACT);
                            if (donorACT.LibID != null)
                                nativeACT.LibID = donorACT.LibID;
                            if (donorACT.ID != null)
                                nativeACT.ID = donorACT.ID;
                            if (donorACT.Kind != null)
                                nativeACT.Kind = donorACT.Kind;
                            if (donorACT.UseRelative != null)
                                nativeACT.UseRelative = donorACT.UseRelative;
                            if (donorACT.IsQuestion != null)
                                nativeACT.IsQuestion = donorACT.IsQuestion;
                            if (donorACT.UseApplyTo != null)
                                nativeACT.UseApplyTo = donorACT.UseApplyTo;
                            if (donorACT.ExeType != null)
                                nativeACT.ExeType = donorACT.ExeType;
                            if (donorACT.ActionName != null)
                                nativeACT.ActionName = Data.Strings.MakeString(donorACT.ActionName.Content);
                            if (donorACT.CodeId?.Name?.Content != null)
                            {
                                GlobalDecompileContext globalDecompileContext = new(DonorData);
                                string codeToCopy = "";
                                try
                                {
                                    codeToCopy = (donorACT.CodeId != null 
                                        ? new Underanalyzer.Decompiler.DecompileContext(globalDecompileContext, donorACT.CodeId, Data.ToolInfo.DecompilerSettings)
                                                .DecompileToString() 
                                        : "");
                                }
                                catch (Exception e)
                                {
                                    codeToCopy = ("/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                                }
                                try
                                {
                                    UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);
                                    importGroup.QueueReplace(donorACT.CodeId?.Name?.Content, codeToCopy);
                                    importGroup.Import();
                                }
                                catch (Exception ec)
                                {
                                    ScriptError("Uh oh, " + donorACT.CodeId?.Name?.Content + " has an error: " + ec.Message);
                                }
                                nativeACT.CodeId = Data.Code.ByName(donorACT.CodeId?.Name?.Content);
                                nativeACT.CodeId.LocalsCount = donorACT.CodeId.LocalsCount;
                                nativeACT.CodeId.ArgumentsCount = donorACT.CodeId.ArgumentsCount;
                                nativeACT.CodeId.Offset = donorACT.CodeId.Offset;
                                nativeACT.CodeId.WeirdLocalFlag = donorACT.CodeId.WeirdLocalFlag;
                                if (Data.CodeLocals is not null)
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
                                }
                            }
                            nativeACT.ArgumentCount = donorACT.ArgumentCount;
                            nativeACT.Who = donorACT.Who;
                            nativeACT.Relative = donorACT.Relative;
                            nativeACT.IsNot = donorACT.IsNot;
                            nativeACT.UnknownAlwaysZero = donorACT.UnknownAlwaysZero;
                        }
                    }
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

List<string> GetSplitStringsList(string assetType)
{
    ScriptMessage("Enter the " + assetType + "(s) to copy");
    List<string> splitStringsList = new List<string>();
    string InputtedText = "";
    InputtedText = SimpleTextInput("Menu", "Enter the name(s) of the " + assetType + "(s)", InputtedText, true);
    string[] IndividualLineArray = InputtedText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
    foreach (var OneLine in IndividualLineArray)
    {
        splitStringsList.Add(OneLine.Trim());
    }
    return splitStringsList;
}