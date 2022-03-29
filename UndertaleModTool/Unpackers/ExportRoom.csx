// Written by SolventMercury

using System;
using System.IO;
using System.Text;
using System.Text.Json;
using UndertaleModLib.Util;
using UndertaleModLib.Models;

ScriptMessage("Select the Room output directory");
string RoomOutputPath = PromptChooseDirectory("Export to where");;
if (RoomOutputPath == null) {
	throw new System.Exception("The room exporter's output path was not set.");
}

StreamWriter sw;
foreach(UndertaleRoom room in Data.Rooms) {
	if (room != null) {
		try {
			string jsonString = RoomToJson(room);
			sw = new StreamWriter(Path.Join(RoomOutputPath, room.Name.Content) + ".json");
			sw.Write(jsonString);
		} catch (Exception e) {
			throw e;
		} finally {
			sw.Close();
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


// TODO: Use custom enum encoders

string RoomToJson(UndertaleRoom room) {
	JsonWriterOptions writerOptions = new() {Indented = true};
	using MemoryStream stream = new();
	using Utf8JsonWriter writer = new(stream, writerOptions);
	writer.WriteStartObject();
	// Params
	//{
	WriteString(writer, "name", room.Name);
	WriteString(writer, "caption", room.Caption);
	writer.WriteNumber("width", room.Width);
	writer.WriteNumber("height", room.Height);
	writer.WriteNumber("speed", room.Speed);
	writer.WriteBoolean("persistent", room.Persistent);
	writer.WriteNumber("background_color", room.BackgroundColor);
	writer.WriteBoolean("draw_background_color", room.DrawBackgroundColor);
	//}
	
	// GMS 2 Params
	//{
	if (room.CreationCodeId != null) {
		WriteString(writer, "creation_code_id", room.CreationCodeId.Name);
	} else {
		writer.WriteNull("creation_code_id");
	}

	writer.WriteNumber("flags", Convert.ToInt32(room.Flags));
	writer.WriteBoolean("world", room.World);
	writer.WriteNumber("top", room.Top);
	writer.WriteNumber("left", room.Left);
	writer.WriteNumber("right", room.Right);
	writer.WriteNumber("bottom", room.Bottom);
	writer.WriteNumber("gravity_x", room.GravityX);
	writer.WriteNumber("gravity_y", room.GravityY);
	writer.WriteNumber("meters_per_pixel", room.MetersPerPixel);
	
	//}
	
	// Now the part that sucks
	
	// Backgrounds
	writer.WriteStartArray("backgrounds");
	if (room.Backgrounds != null) {
		foreach (UndertaleRoom.Background bg in room.Backgrounds) {
			writer.WriteStartObject();
			if (bg != null) {
				writer.WriteNumber("calc_scale_x", bg.CalcScaleX);
				writer.WriteNumber("calc_scale_y", bg.CalcScaleY);
				writer.WriteBoolean("enabled", bg.Enabled);
				writer.WriteBoolean("foreground", bg.Foreground);

				if (bg.BackgroundDefinition != null) {
					WriteString(writer, "background_definition", bg.BackgroundDefinition.Name);
				} else {
					writer.WriteNull("background_definition");
				}
				
				writer.WriteNumber("x", bg.X);
				writer.WriteNumber("y", bg.Y);
				writer.WriteNumber("tile_x", bg.TileX);
				writer.WriteNumber("tile_y", bg.TileY);
				writer.WriteNumber("speed_x", bg.SpeedX);
				writer.WriteNumber("speed_y", bg.SpeedY);
				writer.WriteBoolean("stretch", bg.Stretch);

			}
			
			writer.WriteEndObject();
		}
	}
	writer.WriteEndArray();

	// Views
	writer.WriteStartArray("views");
	if (room.Views != null) {
		foreach (UndertaleRoom.View view in room.Views) {
			writer.WriteStartObject();
			
			if (view != null) {
				writer.WriteBoolean("enabled", view.Enabled);
				writer.WriteNumber("view_x", view.ViewX);
				writer.WriteNumber("view_y", view.ViewY);
				writer.WriteNumber("view_width", view.ViewWidth);
				writer.WriteNumber("view_height", view.ViewHeight);
				writer.WriteNumber("port_x", view.PortX);
				writer.WriteNumber("port_y", view.PortY);
				writer.WriteNumber("port_width", view.PortWidth);
				writer.WriteNumber("port_height", view.PortHeight);
				writer.WriteNumber("border_x", view.BorderX);
				writer.WriteNumber("border_y", view.BorderY);
				writer.WriteNumber("speed_x", view.SpeedX);
				writer.WriteNumber("speed_y", view.SpeedY);
				
				if (view.ObjectId != null) {
					WriteString(writer, "object_id", view.ObjectId.Name);
				} else {
					writer.WriteNull("object_id");
				}
			}
			
			writer.WriteEndObject();
		}
	}
	writer.WriteEndArray();
	
	// GameObjects
	writer.WriteStartArray("game_objects");
	if (room.GameObjects != null) {
		foreach (UndertaleRoom.GameObject go in room.GameObjects) {
			writer.WriteStartObject();
			if (go != null) {
				writer.WriteNumber("x", go.X);
				writer.WriteNumber("y", go.Y);

				if (go.ObjectDefinition != null) {
					WriteString(writer, "object_definition", go.ObjectDefinition.Name);
				} else {
					writer.WriteNull("object_definition");
				}

				writer.WriteNumber("instance_id", go.InstanceID);

				if (go.CreationCode != null) {
					WriteString(writer, "creation_code", go.CreationCode.Name);
				} else {
					writer.WriteNull("creation_code");
				}

				writer.WriteNumber("scale_x", go.ScaleX);
				writer.WriteNumber("scale_y", go.ScaleY);
				writer.WriteNumber("color", go.Color);
				writer.WriteNumber("rotation", go.Rotation);
				
				if (go.PreCreateCode != null) {
					WriteString(writer, "pre_create_code", go.PreCreateCode.Name);
				} else {
					writer.WriteNull("pre_create_code");
				}

				writer.WriteNumber("image_speed", go.ImageSpeed);
				writer.WriteNumber("image_index", go.ImageIndex);

			}
			
			writer.WriteEndObject();
		}
	}
	writer.WriteEndArray();
	
	// Tiles
	writer.WriteStartArray("tiles");
	if (room.Tiles != null) {
		foreach (UndertaleRoom.Tile tile in room.Tiles) {
			writer.WriteStartObject();
			if (tile != null) {
				writer.WriteBoolean("sprite_mode", tile._SpriteMode);
				writer.WriteNumber("x", tile.X);
				writer.WriteNumber("y", tile.Y);

				if (tile.BackgroundDefinition != null) {
					WriteString(writer, "background_definition", tile.BackgroundDefinition.Name);
				} else {
					writer.WriteNull("background_definition");
				}

				if (tile.SpriteDefinition != null) {
					WriteString(writer, "sprite_definition", tile.SpriteDefinition.Name);
				} else {
					writer.WriteNull("sprite_definition");
				}

				writer.WriteNumber("source_x", tile.SourceX);
				writer.WriteNumber("source_y", tile.SourceY);
				writer.WriteNumber("width", tile.Width);
				writer.WriteNumber("height", tile.Height);
				writer.WriteNumber("tile_depth", tile.TileDepth);
				writer.WriteNumber("instance_id", tile.InstanceID);
				writer.WriteNumber("scale_x", tile.ScaleX);
				writer.WriteNumber("scale_y", tile.ScaleY);
				writer.WriteNumber("color", tile.Color);

			}
			
			writer.WriteEndObject();
		}
	}
	writer.WriteEndArray();
	
	// Layers
	// This is the part that super sucks
	
	writer.WriteStartArray("layers");
	if (room.Layers != null) {
		foreach (UndertaleRoom.Layer layer in room.Layers) {
			writer.WriteStartObject();
			if (layer != null) {
				//{
				WriteString(writer, "layer_name", layer.LayerName);
				writer.WriteNumber("layer_id", layer.LayerId);
				writer.WriteNumber("layer_type", Convert.ToInt32(layer.LayerType));
				writer.WriteNumber("layer_depth", layer.LayerDepth);
				writer.WriteNumber("x_offset", layer.XOffset);
				writer.WriteNumber("y_offset", layer.YOffset);
				writer.WriteNumber("h_speed", layer.HSpeed);
				writer.WriteNumber("v_speed", layer.VSpeed);
				writer.WriteBoolean("is_visible", layer.IsVisible);
				//}
				writer.WriteStartObject("layer_data");
				if (layer.Data != null) {
					if (layer.LayerType == UndertaleRoom.LayerType.Background) {
						UndertaleRoom.Layer.LayerBackgroundData layerData = (UndertaleRoom.Layer.LayerBackgroundData)layer.Data;

						writer.WriteNumber("calc_scale_x", layerData.CalcScaleX);
						writer.WriteNumber("calc_scale_y", layerData.CalcScaleY);
						writer.WriteBoolean("visible", layerData.Visible);
						writer.WriteBoolean("foreground", layerData.Foreground);
						
						if (layerData.Sprite != null) {
							WriteString(writer, "sprite", layerData.Sprite.Name);
						} else {
							writer.WriteNull("sprite");
						}

						writer.WriteBoolean("tiled_horizontally", layerData.TiledHorizontally);
						writer.WriteBoolean("tiled_vertically", layerData.TiledVertically);
						writer.WriteBoolean("stretch", layerData.Stretch);
						writer.WriteNumber("color", layerData.Color);
						writer.WriteNumber("first_frame", layerData.FirstFrame);
						writer.WriteNumber("animation_speed", layerData.AnimationSpeed);
						writer.WriteNumber("animation_speed_type", Convert.ToInt32(layerData.AnimationSpeedType));
					}
					
					if (layer.LayerType == UndertaleRoom.LayerType.Instances) {
						UndertaleRoom.Layer.LayerInstancesData layerData = (UndertaleRoom.Layer.LayerInstancesData)layer.Data;
						
						writer.WriteStartArray("instances");
						if (layerData.Instances != null) {
							foreach (UndertaleRoom.GameObject instance in layerData.Instances) {
								writer.WriteStartObject();
								if (instance != null) {
									writer.WriteNumber("x", instance.X);
									writer.WriteNumber("y", instance.Y);

									if (instance.ObjectDefinition != null) {
										WriteString(writer, "object_definition", instance.ObjectDefinition.Name);
									} else {
										writer.WriteNull("object_definition");
									}

									writer.WriteNumber("instance_id", instance.InstanceID);

									if (instance.CreationCode != null) {
										WriteString(writer, "creation_code", instance.CreationCode.Name);
									} else {
										writer.WriteNull("creation_code");
									}

									writer.WriteNumber("scale_x", instance.ScaleX);
									writer.WriteNumber("scale_y", instance.ScaleY);
									writer.WriteNumber("color", instance.Color);
									writer.WriteNumber("rotation", instance.Rotation);
									
									if (instance.PreCreateCode != null) {
										WriteString(writer, "pre_create_code", instance.PreCreateCode.Name);
									} else {
										writer.WriteNull("pre_create_code");
									}

									writer.WriteNumber("image_speed", instance.ImageSpeed);
									writer.WriteNumber("image_index", instance.ImageIndex);

								}
								
								writer.WriteEndObject();
							}
						}
						writer.WriteEndArray();
					}
					
					// Awful^3
					if (layer.LayerType == UndertaleRoom.LayerType.Assets) {
						UndertaleRoom.Layer.LayerAssetsData layerData = (UndertaleRoom.Layer.LayerAssetsData)layer.Data;
						// Tiles
						writer.WriteStartArray("legacy_tiles");
						if (layerData.LegacyTiles != null) {
							foreach (UndertaleRoom.Tile tile in layerData.LegacyTiles) {
								writer.WriteStartObject();
								if (tile != null) {
									writer.WriteBoolean("sprite_mode", tile._SpriteMode);
									writer.WriteNumber("x", tile.X);
									writer.WriteNumber("y", tile.Y);
									
									if (tile.SpriteDefinition != null) {
										WriteString(writer, "background_definition", tile.SpriteDefinition.Name);
									} else {
										writer.WriteNull("background_definition");
									}

									if (tile.BackgroundDefinition != null) {
										WriteString(writer, "sprite_definition", tile.BackgroundDefinition.Name);
									} else {
										writer.WriteNull("sprite_definition");
									}

									writer.WriteNumber("source_x", tile.SourceX);
									writer.WriteNumber("source_y", tile.SourceY);
									writer.WriteNumber("width", tile.Width);
									writer.WriteNumber("height", tile.Height);
									writer.WriteNumber("tile_depth", tile.TileDepth);
									writer.WriteNumber("instance_id", tile.InstanceID);
									writer.WriteNumber("scale_x", tile.ScaleX);
									writer.WriteNumber("scale_y", tile.ScaleY);
									writer.WriteNumber("color", tile.Color);
								}
								writer.WriteEndObject();
							}
						}
						writer.WriteEndArray();
						
						// Sprites
						writer.WriteStartArray("sprites");
						if (layerData.Sprites != null) {
							foreach (UndertaleRoom.SpriteInstance sprite in layerData.Sprites) {
								writer.WriteStartObject();
								if (sprite != null) {
									WriteString(writer, "name", sprite.Name);
									
									if (sprite.Sprite != null) {
										WriteString(writer, "sprite", sprite.Sprite.Name);
									} else {
										writer.WriteNull("sprite");
									}

									writer.WriteNumber("x", sprite.X);
									writer.WriteNumber("y", sprite.Y);
									writer.WriteNumber("scale_x", sprite.ScaleX);
									writer.WriteNumber("scale_y", sprite.ScaleY);
									writer.WriteNumber("color", sprite.Color);
									writer.WriteNumber("animation_speed", sprite.AnimationSpeed);
									writer.WriteNumber("animation_speed_type", Convert.ToInt32(sprite.AnimationSpeedType));
									writer.WriteNumber("frame_index", sprite.FrameIndex);
									writer.WriteNumber("rotation", sprite.Rotation);

								}
								writer.WriteEndObject();
							}
						}
						writer.WriteEndArray();
						
						// Sequences
						writer.WriteStartArray("sequences");
						if (layerData.Sequences != null) {
							foreach (UndertaleRoom.SequenceInstance sequence in layerData.Sequences) {
								writer.WriteStartObject();
								if (sequence != null) {
									WriteString(writer, "name", sequence.Name);
									
									if (sequence.Sequence != null) {
										WriteString(writer, "sequence", sequence.Sequence.Name);
									} else {
										writer.WriteNull("sequence");
									}

									writer.WriteNumber("x", sequence.X);
									writer.WriteNumber("y", sequence.Y);
									writer.WriteNumber("scale_x", sequence.ScaleX);
									writer.WriteNumber("scale_y", sequence.ScaleY);
									writer.WriteNumber("color", sequence.Color);
									writer.WriteNumber("animation_speed", sequence.AnimationSpeed);
									writer.WriteNumber("animation_speed_type", Convert.ToInt32(sequence.AnimationSpeedType));
									writer.WriteNumber("frame_index", sequence.FrameIndex);
									writer.WriteNumber("rotation", sequence.Rotation);
								}
								writer.WriteEndObject();
							}
						}
						writer.WriteEndArray();
						
						// NineSlices
						writer.WriteStartArray("nine_slices");
						if (layerData.NineSlices != null) {
							foreach (UndertaleRoom.SpriteInstance nineSlice in layerData.NineSlices) {
								writer.WriteStartObject();
								if (nineSlice != null) {
									WriteString(writer, "name", nineSlice.Name);
									
									if (nineSlice.Sprite != null) {
										WriteString(writer, "sprite", nineSlice.Sprite.Name);
									} else {
										writer.WriteNull("sprite");
									}

									writer.WriteNumber("x", nineSlice.X);
									writer.WriteNumber("y", nineSlice.Y);
									writer.WriteNumber("scale_x", nineSlice.ScaleX);
									writer.WriteNumber("scale_y", nineSlice.ScaleY);
									writer.WriteNumber("color", nineSlice.Color);
									writer.WriteNumber("animation_speed", nineSlice.AnimationSpeed);
									writer.WriteNumber("animation_speed_type", Convert.ToInt32(nineSlice.AnimationSpeedType));
									writer.WriteNumber("frame_index", nineSlice.FrameIndex);
									writer.WriteNumber("rotation", nineSlice.Rotation);

								}
								writer.WriteEndObject();
							}
						}
						writer.WriteEndArray();
					}
					
					if (layer.LayerType == UndertaleRoom.LayerType.Tiles) {
						UndertaleRoom.Layer.LayerTilesData layerData = (UndertaleRoom.Layer.LayerTilesData)layer.Data;

						writer.WriteNumber("tiles_x", layerData.TilesX);
						writer.WriteNumber("tiles_y", layerData.TilesY);

						writer.WriteStartArray("tile_data");
						if (layerData.TileData != null) {
							for (int x = 0; x < layerData.TileData.Length; x++) {
								writer.WriteStartArray();
								for (int y = 0; y < layerData.TileData[x].Length; y++) {
									writer.WriteStartObject();
									writer.WriteNumber("id", (layerData.TileData[x])[y]);
									writer.WriteEndObject();
								}
								writer.WriteEndArray();
							}
						}
						writer.WriteEndArray();
					}
				}
				writer.WriteEndObject();
			}
			
			writer.WriteEndObject();
		}
	}
	writer.WriteEndArray();
	
	

	
	
	writer.WriteEndObject();
	writer.Flush();
	
	string json = Encoding.UTF8.GetString(stream.ToArray());
	return json;
}