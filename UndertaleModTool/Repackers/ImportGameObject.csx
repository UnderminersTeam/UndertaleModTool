// Written by SolventMercury

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using UndertaleModLib.Util;
using UndertaleModLib.Models;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;

ScriptMessage("Select the GameObject to import");
string GameObjectInputPath = PromptLoadFile("Import which file", "Json Files|*.json");;
if (GameObjectInputPath == null) {
	throw new System.Exception("The game object's path was not set.");
}

UndertaleGameObject newGameObject = new UndertaleGameObject();

ReadGameObject(GameObjectInputPath);

void ReadGameObject(string filePath) {
	FileStream stream = File.OpenRead(filePath);
	byte[] jsonUtf8Bytes= new byte[stream.Length];

	stream.Read(jsonUtf8Bytes, 0, jsonUtf8Bytes.Length);
	stream.Close();

	JsonReaderOptions options = new JsonReaderOptions {
		AllowTrailingCommas = true,
		CommentHandling = JsonCommentHandling.Skip
	};

	Utf8JsonReader reader = new Utf8JsonReader(jsonUtf8Bytes, options);
	

	ReadAnticipateStartObj(ref reader);
	ReadName(ref reader);
	ReadMainVals(ref reader);
	ReadPhysicsVerts(ref reader);
	ReadAllEvents(ref reader);
	ReadAnticipateEndObj(ref reader);
	if (Data.GameObjects.ByName(newGameObject.Name.Content) == null) {
		Data.GameObjects.Add(newGameObject);
	}
}

void ReadMainVals(ref Utf8JsonReader reader) {
	
	string spriteName            = ReadString(ref reader);
	
	newGameObject.Visible        = ReadBool(ref reader);
	newGameObject.Solid          = ReadBool(ref reader);
	newGameObject.Depth          = (int)ReadNum(ref reader);
	newGameObject.Persistent     = ReadBool(ref reader);
	
	string parentName            = ReadString(ref reader);
	string texMaskName           = ReadString(ref reader);
	
	newGameObject.UsesPhysics    = ReadBool(ref reader);
	newGameObject.IsSensor       = ReadBool(ref reader);
	newGameObject.CollisionShape = (CollisionShapeFlags)ReadNum(ref reader);
	newGameObject.Density        = ReadFloat(ref reader);
	newGameObject.Restitution    = ReadFloat(ref reader);
	newGameObject.Group          = (uint)ReadNum(ref reader);
	newGameObject.LinearDamping  = ReadFloat(ref reader);
	newGameObject.AngularDamping = ReadFloat(ref reader);
	newGameObject.Friction       = ReadFloat(ref reader);
	newGameObject.Awake          = ReadBool(ref reader);
	newGameObject.Kinematic      = ReadBool(ref reader);
	
	if (spriteName == null) {
		newGameObject.Sprite = null;
	} else {
		newGameObject.Sprite = Data.Sprites.ByName(spriteName);
	}
	
	if (parentName == null) {
		newGameObject.ParentId = null;
	} else {
		newGameObject.ParentId = Data.GameObjects.ByName(parentName);
	}
	
	if (texMaskName == null) {
		newGameObject.TextureMaskId = null;
	} else {
		newGameObject.TextureMaskId = Data.Sprites.ByName(texMaskName);
	}
}

void ReadPhysicsVerts(ref Utf8JsonReader reader) {	
	newGameObject.PhysicsVertices.Clear();
	ReadAnticipateStartArray(ref reader);
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.StartObject) {
			UndertaleGameObject.UndertalePhysicsVertex physVert = new UndertaleGameObject.UndertalePhysicsVertex();
			physVert.X = ReadNum(ref reader);
			physVert.Y = ReadNum(ref reader);
			newGameObject.PhysicsVertices.Add(physVert);
			continue;
		}
		if (reader.TokenType == JsonTokenType.EndObject) {
			continue;
		} 
		if (reader.TokenType == JsonTokenType.EndArray) {
			break;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected Integer - found {0}", reader.TokenType));
		}
	}
}

void ReadAllEvents(ref Utf8JsonReader reader) {
	
	ReadAnticipateStartArray(ref reader);
	int eventListIndex = -1;
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.StartArray) {
			eventListIndex++;
			newGameObject.Events[eventListIndex].Clear();
			foreach (UndertaleGameObject.Event eventToAdd in ReadEvents(ref reader)) {
				newGameObject.Events[eventListIndex].Add(eventToAdd);
			}
			continue;
		}
		if (reader.TokenType == JsonTokenType.EndObject) {
			continue;
		} 
		if (reader.TokenType == JsonTokenType.EndArray) {
			break;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected Integer - found {0}", reader.TokenType));
		}
	}
}

List<UndertaleGameObject.Event> ReadEvents(ref Utf8JsonReader reader) {
	List<UndertaleGameObject.Event> eventsToReturn = new List<UndertaleGameObject.Event>();
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.EndArray) {
			return eventsToReturn;
		}
		if (reader.TokenType == JsonTokenType.StartObject) {
			UndertaleGameObject.Event newEvent = new UndertaleGameObject.Event();
			newEvent.EventSubtype = (uint)ReadNum(ref reader);
			newEvent.Actions.Clear();
			foreach (UndertaleGameObject.EventAction action in ReadActions(ref reader)) {
				newEvent.Actions.Add(action);
			}
			eventsToReturn.Add(newEvent);
			ReadAnticipateEndObj(ref reader);
		}
	}
	throw new Exception("ERROR: Could not find end of array token - Events.");
}

List<UndertaleGameObject.EventAction> ReadActions(ref Utf8JsonReader reader) {
	List<UndertaleGameObject.EventAction> actionsToReturn = new List<UndertaleGameObject.EventAction>();
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.EndArray) {
			return actionsToReturn;
		}
		if (reader.TokenType == JsonTokenType.StartObject) {
			UndertaleGameObject.EventAction newAction = ReadAction(ref reader);
			actionsToReturn.Add(newAction);
		}
	}
	throw new Exception("ERROR: Could not find end of array token - Actions.");
}

UndertaleGameObject.EventAction ReadAction(ref Utf8JsonReader reader) {
	UndertaleGameObject.EventAction newAction = new UndertaleGameObject.EventAction();
	newAction.LibID         = (uint)ReadNum(ref reader);
	newAction.ID            = (uint)ReadNum(ref reader);
	newAction.Kind          = (uint)ReadNum(ref reader);
	newAction.UseRelative   = ReadBool(ref reader);
	newAction.IsQuestion    = ReadBool(ref reader);
	newAction.UseApplyTo    = ReadBool(ref reader);
	newAction.ExeType       = (uint)ReadNum(ref reader);
	string actionName       = ReadString(ref reader);
	string codeId           = ReadString(ref reader);
	newAction.ArgumentCount = (uint)ReadNum(ref reader);
	newAction.Who           = (int)ReadNum(ref reader);
	newAction.Relative      = ReadBool(ref reader);
	newAction.IsNot         = ReadBool(ref reader);
	
	if (actionName == null) {
		newAction.ActionName  = null;
	} else {
		newAction.ActionName  = new UndertaleString(actionName);
	}
	
	if (codeId == null) {
		newAction.CodeId  = null;
	} else {
		newAction.CodeId  = Data.Code.ByName(codeId);
	}
	
	ReadAnticipateEndObj(ref reader);
	return newAction;
}

void ReadName(ref Utf8JsonReader reader) {
	
	string name = ReadString(ref reader);
	if (name == null) {
		throw new Exception("ERROR: Object name was null - object name must be defined!");
	}
	if (Data.GameObjects.ByName(name) != null) {
		newGameObject = Data.GameObjects.ByName(name);
	} else {
		newGameObject = new UndertaleGameObject();
		newGameObject.Name = new UndertaleString(name);
		
	}
}

// Read tokens of specified type

bool ReadBool(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.True) {
			return true;
		} else if (reader.TokenType == JsonTokenType.False) {
			return false;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected Boolean - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected Boolean.");
}

long ReadNum(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.Number) {
			return reader.GetInt64();
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected Integer - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected Integer.");
}

float ReadFloat(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.Number) {
			return reader.GetSingle();
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected Decimal - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected Decimal.");
}

string ReadString(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.String) {
			return reader.GetString();
		} else if (reader.TokenType == JsonTokenType.Null) {
			return null;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected String - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected String.");
}

// Watch for certain meta-tokens

void ReadAnticipateStartObj(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.StartObject) {
			return;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected StartObject - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected String.");
}

void ReadAnticipateEndObj(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.EndObject) {
			return;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected EndObject - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected String.");
}

void ReadAnticipateStartArray(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.StartArray) {
			return;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected StartArray - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected String.");
}

void ReadAnticipateEndArray(ref Utf8JsonReader reader) {
	while (reader.Read()) {
		if (reader.TokenType == JsonTokenType.PropertyName) {
			continue;
		}
		if (reader.TokenType == JsonTokenType.EndArray) {
			return;
		} else {
			throw new Exception(String.Format("ERROR: Unexpected token type. Expected EndArray - found {0}", reader.TokenType));
		}
	}
	throw new Exception("ERROR: Did not find value of expected type. Expected String.");
}