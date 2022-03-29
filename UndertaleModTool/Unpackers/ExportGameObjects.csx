// Written by SolventMercury

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using UndertaleModLib.Util;
using UndertaleModLib.Models;

ScriptMessage("Select the GameObject output directory");
string GameObjectOutputPath = PromptChooseDirectory("Export to where");;
if (GameObjectOutputPath == null) {
	throw new System.Exception("The patch's output path was not set.");
}

StreamWriter sw;
foreach(UndertaleGameObject gameObject in Data.GameObjects) {
	if (gameObject != null) {
		try {
			string jsonString = GameObjectToJson(gameObject);
			sw = new StreamWriter(Path.Join(GameObjectOutputPath, gameObject.Name.Content) + ".json");
			sw.Write(jsonString);
		} catch (Exception e) {
			throw e;
		} finally {
			if (sw != null) {
				sw.Close();
			}
		}
	}
}

void WriteString(Utf8JsonWriter writer, string propertyName, UndertaleString stringToWrite) {
	if (stringToWrite == null) {
		writer.WriteNull(propertyName);
	} else {
		if (stringToWrite.Content == null) {
			writer.WriteNull(propertyName);
		} else {
			writer.WriteString(propertyName, stringToWrite.Content);
		}
	}
}

string GameObjectToJson(UndertaleGameObject gameObject) {
	JsonWriterOptions writerOptions = new() {Indented = true};
	using MemoryStream stream = new();
	using Utf8JsonWriter writer = new(stream, writerOptions);
	writer.WriteStartObject();
	string json;
	if (gameObject == null) {
		writer.WriteEndObject();
		writer.Flush();
		json = Encoding.UTF8.GetString(stream.ToArray());
		return json;
	}
	
	WriteString(writer, "name", gameObject.Name);
	
	if (gameObject.Sprite != null) {
		WriteString(writer, "sprite", gameObject.Sprite.Name);
	} else {
		writer.WriteNull("sprite");
	}
	
	writer.WriteBoolean("visible", gameObject.Visible);
	writer.WriteBoolean("solid", gameObject.Solid);
	writer.WriteNumber("depth", gameObject.Depth);
	writer.WriteBoolean("persistent", gameObject.Persistent);
	
	if (gameObject.ParentId != null) {
		WriteString(writer, "parent_id", gameObject.ParentId.Name);
	} else {
		writer.WriteNull("parent_id");
	}
	
	if (gameObject.TextureMaskId != null) {
		WriteString(writer, "texture_mask_id", gameObject.TextureMaskId.Name);
	} else {
		writer.WriteNull("texture_mask_id");
	}
	
	writer.WriteBoolean("uses_physics", gameObject.UsesPhysics);
	writer.WriteBoolean("is_sensor", gameObject.IsSensor);
	writer.WriteNumber("collision_shape", Convert.ToInt32(gameObject.CollisionShape));
	writer.WriteNumber("density", gameObject.Density);
	writer.WriteNumber("restitution", gameObject.Restitution);
	writer.WriteNumber("group", gameObject.Group);
	writer.WriteNumber("linear_damping", gameObject.LinearDamping);
	writer.WriteNumber("angular_damping", gameObject.AngularDamping);
	writer.WriteNumber("friction", gameObject.Friction);
	writer.WriteBoolean("awake", gameObject.Awake);
	writer.WriteBoolean("kinematic", gameObject.Kinematic);
	
	writer.WriteStartArray("physics_vertices");
	if (gameObject.PhysicsVertices != null) {
		foreach(UndertaleGameObject.UndertalePhysicsVertex vertex in gameObject.PhysicsVertices) {
			writer.WriteStartObject();
			writer.WriteNumber("x", vertex.X);
			writer.WriteNumber("y", vertex.Y);
			writer.WriteEndObject();
		}
	}
	writer.WriteEndArray();
	
	writer.WriteStartArray("events");
	if (gameObject.Events != null) {
		foreach(IList<UndertaleGameObject.Event> eventList in gameObject.Events) {
			writer.WriteStartArray();
			if (eventList != null) {
				foreach(UndertaleGameObject.Event objectEvent in eventList) {
					writer.WriteStartObject();
					if (objectEvent != null) {
						writer.WriteNumber("event_subtype", objectEvent.EventSubtype);
						writer.WriteStartArray("actions");
						if (objectEvent.Actions != null) {
							foreach(UndertaleGameObject.EventAction action in objectEvent.Actions) {
								writer.WriteStartObject();
								if (action != null) {
									writer.WriteNumber("lib_id", action.LibID);
									writer.WriteNumber("id", action.ID);
									writer.WriteNumber("kind", action.Kind);
									writer.WriteBoolean("use_relative", action.UseRelative);
									writer.WriteBoolean("is_question", action.IsQuestion);
									writer.WriteBoolean("use_apply_to", action.UseApplyTo);
									writer.WriteNumber("exe_type", action.ExeType);
									
									WriteString(writer, "action_name", action.ActionName);
									
									if (action.CodeId != null) {
										WriteString(writer, "code_id", action.CodeId.Name);
									} else {
										writer.WriteNull("code_id");
									}
									
									writer.WriteNumber("argument_count", action.ArgumentCount);
									writer.WriteNumber("who", action.Who);
									writer.WriteBoolean("relative", action.Relative);
									writer.WriteBoolean("is_not", action.IsNot);
								}
								writer.WriteEndObject();
							}
						}
						writer.WriteEndArray();
					}
					writer.WriteEndObject();
				}
			}
			writer.WriteEndArray();
		}
	}
	writer.WriteEndArray();
	
	writer.WriteEndObject();
	
	writer.Flush();
	
	json = Encoding.UTF8.GetString(stream.ToArray());
	return json;
	return "";
}