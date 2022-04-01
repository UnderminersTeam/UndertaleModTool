// Written by SolventMercury

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;
using UndertaleModLib.Models;

ScriptMessage("Select the GameObject to import");
string gameObjectInputPath = PromptLoadFile("Import which file", "Json Files|*.json");
if (gameObjectInputPath == null) throw new ScriptException("The game object's path was not set.");

UndertaleGameObject newGameObject = new UndertaleGameObject();

ReadGameObject(gameObjectInputPath);

void ReadGameObject(string filePath)
{
    FileStream stream = File.OpenRead(filePath);
    byte[] jsonUtf8Bytes = new byte[stream.Length];

    stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
    stream.Close();

    JsonReaderOptions options = new JsonReaderOptions
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    Utf8JsonReader reader = new Utf8JsonReader(jsonUtf8Bytes, options);

    ReadAnticipateJSONObject(ref reader, JsonTokenType.StartObject);
    ReadName(ref reader);
    ReadMainValues(ref reader);
    ReadPhysicsVerts(ref reader);
    ReadAllEvents(ref reader);
    ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
    if (Data.GameObjects.ByName(newGameObject.Name.Content) == null) Data.GameObjects.Add(newGameObject);
}

void ReadMainValues(ref Utf8JsonReader reader)
{
    string spriteName = ReadString(ref reader);

    newGameObject.Visible = ReadBool(ref reader);
    newGameObject.Solid = ReadBool(ref reader);
    newGameObject.Depth = (int) ReadNum(ref reader);
    newGameObject.Persistent = ReadBool(ref reader);

    string parentName = ReadString(ref reader);
    string texMaskName = ReadString(ref reader);

    newGameObject.UsesPhysics = ReadBool(ref reader);
    newGameObject.IsSensor = ReadBool(ref reader);
    newGameObject.CollisionShape = (CollisionShapeFlags) ReadNum(ref reader);
    newGameObject.Density = ReadFloat(ref reader);
    newGameObject.Restitution = ReadFloat(ref reader);
    newGameObject.Group = (uint) ReadNum(ref reader);
    newGameObject.LinearDamping = ReadFloat(ref reader);
    newGameObject.AngularDamping = ReadFloat(ref reader);
    newGameObject.Friction = ReadFloat(ref reader);
    newGameObject.Awake = ReadBool(ref reader);
    newGameObject.Kinematic = ReadBool(ref reader);

    newGameObject.Sprite = (spriteName == null) ? null : Data.Sprites.ByName(spriteName);

    newGameObject.ParentId = (parentName == null) ? null : Data.GameObjects.ByName(parentName);

    newGameObject.TextureMaskId = (texMaskName == null) ? null : Data.Sprites.ByName(texMaskName);
}

void ReadPhysicsVerts(ref Utf8JsonReader reader)
{
    newGameObject.PhysicsVertices.Clear();
    ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
    while (reader.Read())
    {
        if (reader.TokenType == JsonTokenType.StartObject)
        {
            UndertaleGameObject.UndertalePhysicsVertex physVert = new UndertaleGameObject.UndertalePhysicsVertex();
            physVert.X = ReadNum(ref reader);
            physVert.Y = ReadNum(ref reader);
            newGameObject.PhysicsVertices.Add(physVert);
            continue;
        }

        if (reader.TokenType == JsonTokenType.EndObject) continue;
        if (reader.TokenType == JsonTokenType.EndArray) break;

        throw new Exception($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
    }
}

void ReadAllEvents(ref Utf8JsonReader reader)
{
    ReadAnticipateJSONObject(ref reader, JsonTokenType.StartArray);
    int eventListIndex = -1;
    while (reader.Read())
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            eventListIndex++;
            newGameObject.Events[eventListIndex].Clear();
            foreach (UndertaleGameObject.Event eventToAdd in ReadEvents(ref reader)) newGameObject.Events[eventListIndex].Add(eventToAdd);
            continue;
        }

        if (reader.TokenType == JsonTokenType.EndObject) continue;
        if (reader.TokenType == JsonTokenType.EndArray) break;

        throw new Exception($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
    }
}

List<UndertaleGameObject.Event> ReadEvents(ref Utf8JsonReader reader)
{
    List<UndertaleGameObject.Event> eventsToReturn = new List<UndertaleGameObject.Event>();
    while (reader.Read())
    {
        if (reader.TokenType == JsonTokenType.PropertyName) continue;
        if (reader.TokenType == JsonTokenType.EndArray) return eventsToReturn;
        if (reader.TokenType != JsonTokenType.StartObject) continue;

        UndertaleGameObject.Event newEvent = new UndertaleGameObject.Event();
        newEvent.EventSubtype = (uint) ReadNum(ref reader);
        newEvent.Actions.Clear();
        foreach (UndertaleGameObject.EventAction action in ReadActions(ref reader)) newEvent.Actions.Add(action);
        eventsToReturn.Add(newEvent);
        ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
    }

    throw new Exception("ERROR: Could not find end of array token - Events.");
}

List<UndertaleGameObject.EventAction> ReadActions(ref Utf8JsonReader reader)
{
    List<UndertaleGameObject.EventAction> actionsToReturn = new List<UndertaleGameObject.EventAction>();
    while (reader.Read())
    {
        if (reader.TokenType == JsonTokenType.PropertyName) continue;
        if (reader.TokenType == JsonTokenType.EndArray) return actionsToReturn;
        if (reader.TokenType != JsonTokenType.StartObject) continue;

        UndertaleGameObject.EventAction newAction = ReadAction(ref reader);
        actionsToReturn.Add(newAction);
    }

    throw new Exception("ERROR: Could not find end of array token - Actions.");
}

UndertaleGameObject.EventAction ReadAction(ref Utf8JsonReader reader)
{
    UndertaleGameObject.EventAction newAction = new UndertaleGameObject.EventAction();
    newAction.LibID = (uint) ReadNum(ref reader);
    newAction.ID = (uint) ReadNum(ref reader);
    newAction.Kind = (uint) ReadNum(ref reader);
    newAction.UseRelative = ReadBool(ref reader);
    newAction.IsQuestion = ReadBool(ref reader);
    newAction.UseApplyTo = ReadBool(ref reader);
    newAction.ExeType = (uint) ReadNum(ref reader);
    string actionName = ReadString(ref reader);
    string codeId = ReadString(ref reader);
    newAction.ArgumentCount = (uint) ReadNum(ref reader);
    newAction.Who = (int) ReadNum(ref reader);
    newAction.Relative = ReadBool(ref reader);
    newAction.IsNot = ReadBool(ref reader);

    if (actionName == null)
    {
        newAction.ActionName = null;
    }
    else
    {
        newAction.ActionName = new UndertaleString(actionName);

        if (!Data.Strings.Any(s => s == newAction.ActionName))
            Data.Strings.Add(newAction.ActionName);
    }

    if (codeId == null)
        newAction.CodeId = null;
    else
        newAction.CodeId = Data.Code.ByName(codeId);

    ReadAnticipateJSONObject(ref reader, JsonTokenType.EndObject);
    return newAction;
}

void ReadName(ref Utf8JsonReader reader)
{
    string name = ReadString(ref reader);
    if (name == null) throw new Exception("ERROR: Object name was null - object name must be defined!");
    if (Data.GameObjects.ByName(name) != null)
    {
        newGameObject = Data.GameObjects.ByName(name);
    }
    else
    {
        newGameObject = new UndertaleGameObject();
        newGameObject.Name = new UndertaleString(name);
        Data.Strings.Add(newGameObject.Name);
    }
}

// Read tokens of specified type

bool ReadBool(ref Utf8JsonReader reader)
{
    while (reader.Read())
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.PropertyName: continue;
            case JsonTokenType.True: return true;
            case JsonTokenType.False: return false;
            default: throw new Exception($"ERROR: Unexpected token type. Expected Boolean - found {reader.TokenType}");
        }
    }

    throw new Exception("ERROR: Did not find value of expected type. Expected Boolean.");
}

long ReadNum(ref Utf8JsonReader reader)
{
    while (reader.Read())
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.PropertyName: continue;
            case JsonTokenType.Number: return reader.GetInt64();
            default: throw new Exception($"ERROR: Unexpected token type. Expected Integer - found {reader.TokenType}");
        }
    }

    throw new Exception("ERROR: Did not find value of expected type. Expected Integer.");
}

float ReadFloat(ref Utf8JsonReader reader)
{
    while (reader.Read())
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.PropertyName: continue;
            case JsonTokenType.Number: return reader.GetSingle();
            default: throw new Exception($"ERROR: Unexpected token type. Expected Decimal - found {reader.TokenType}");
        }
    }

    throw new Exception("ERROR: Did not find value of expected type. Expected Decimal.");
}

string ReadString(ref Utf8JsonReader reader)
{
    while (reader.Read())
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.PropertyName: continue;
            case JsonTokenType.String: return reader.GetString();
            case JsonTokenType.Null: return null;
            default: throw new Exception($"ERROR: Unexpected token type. Expected String - found {reader.TokenType}");
        }
    }

    throw new Exception("ERROR: Did not find value of expected type. Expected String.");
}

// Watch for certain meta-tokens

void ReadAnticipateJSONObject(ref Utf8JsonReader reader, JsonTokenType allowedTokenType)
{
    while (reader.Read())
    {
        if (reader.TokenType == JsonTokenType.PropertyName)
            continue;
        if (reader.TokenType == allowedTokenType)
            return;
        throw new Exception($"ERROR: Unexpected token type. Expected {allowedTokenType} - found {reader.TokenType}");
    }

    throw new Exception("ERROR: Did not find value of expected type. Expected String.");
}