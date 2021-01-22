//
// Temporary script for tile layer editing before a proper editor will be introduced. Script by Lassebq with the help of JocKe.
//
private void Cancel()
{
	//ScriptMessage("Canceling script...");
	finaloutput = "Canceling script...";
}
private void GetTileData(string roomindex)
{
	uint[][] arr;
	if (roomindex == null)
	{
		arr = Data.Rooms.ByName(roomname).Layers[trueindex].TilesData.TileData;
	}
	else
	{
		arr = Data.Rooms[int.Parse(roomindex)].Layers[trueindex].TilesData.TileData;
	}
	int length1 = arr.Length;
	string output = "uint[][] arr2 = new uint[" + length1.ToString() + "][];\n";
	for (int row = 0; row < arr.Length; row++)
	{
	output += "arr2[" + row.ToString() + "] = new uint[" + arr[row].Length.ToString() + "]{ ";
		for (int col = 0; col < arr[row].Length; col++)
	output += String.Format("{0}{1} ", arr[row][col], col == arr[row].Length - 1 ? "" : ",");
		output += "};\n";
	}
	if (roomindex == null)
	{
		output += "\nData.Rooms.ByName(" + "\"" + roomname + "\"" + ").Layers[" + trueindex + "].TilesData.TileData = arr2;";
	}
	else
	{
		output += "\nData.Rooms[" + roomindex + "].Layers[" + trueindex + "].TilesData.TileData = arr2;";
	}
	finaloutput = output;
}
string finaloutput;
string input = ScriptInputDialog("Specify value", "Room name: (Leave empty if you want to specify room index)", "", "Cancel", "Ok", true, false);
string roomname = input;
string roomindex;
string layerindex;
bool roomindexfail = false;
int trueindex;
if (roomname != null)
{
	if (Data.Rooms.ByName(roomname) != null || roomname == "")
	{
	if (roomname == "")
	{
		input = ScriptInputDialog("Specify value", "Room index:", "", "Cancel", "Ok", true, false);
		roomindex = input;
		if (!int.TryParse(roomindex,out trueindex) && roomindex != null)
		{
			roomindexfail = true;
		}
	}
	if (!roomindexfail && roomindex != null || roomname != "")
	{
		if (roomname != "")
		{
			input = ScriptInputDialog("Specify value", "Layer index:", "", "Cancel", "Ok", true, false);
			layerindex = input;
			if (layerindex != null)
			{
				if (int.TryParse(layerindex,out trueindex))
				{
					trueindex = int.Parse(layerindex);
					if (roomname == "")
					{
					if (trueindex <= Data.Rooms[int.Parse(roomindex)].Layers.Count - 1 && trueindex >= 0)
					{
						if (Data.Rooms[int.Parse(roomindex)].Layers[trueindex].Data.GetType() == typeof(UndertaleModLib.Models.UndertaleRoom.Layer.LayerTilesData))
						{
							GetTileData(roomindex);
						}
						else {ScriptError("Selected layer is not a tile layer", "Error"); finaloutput = "Incorrect layer type: " + Data.Rooms[int.Parse(roomindex)].Layers[trueindex].Data.GetType();}
					}
					else {ScriptError("Layer does not exist", "Error"); finaloutput = "Specified layer index was out of bounds.";}
					}
					else
					{
					if (trueindex <= Data.Rooms.ByName(roomname).Layers.Count - 1 && trueindex >= 0)
					{
						if (Data.Rooms.ByName(roomname).Layers[trueindex].Data.GetType() == typeof(UndertaleModLib.Models.UndertaleRoom.Layer.LayerTilesData))
						{
							GetTileData(roomindex);
						}
						else {ScriptError("Selected layer is not a tile layer", "Error"); finaloutput = "Incorrect layer type: " + Data.Rooms.ByName(roomname).Layers[trueindex].Data.GetType();}
					}
					else {ScriptError("Layer does not exist", "Error"); finaloutput = "Specified layer index was out of bounds.";}
					}
				}
				else {ScriptError("Index must be a number", "Error"); finaloutput = "Layer index value was given in the incorrent format.";}
			}
			else Cancel();
		}
		else if (int.Parse(roomindex) <= Data.Rooms.Count - 1 && int.Parse(roomindex) >= 0)
		{
			input = ScriptInputDialog("Specify value", "Layer index:", "", "Cancel", "Ok", true, false);
			layerindex = input;
			if (layerindex != null)
			{
				if (int.TryParse(layerindex,out trueindex))
				{
					trueindex = int.Parse(layerindex);
					if (roomname == "")
					{
					if (trueindex <= Data.Rooms[int.Parse(roomindex)].Layers.Count - 1 && trueindex >= 0)
					{
						if (Data.Rooms[int.Parse(roomindex)].Layers[trueindex].Data.GetType() == typeof(UndertaleModLib.Models.UndertaleRoom.Layer.LayerTilesData))
						{
							GetTileData(roomindex);
						}
						else {ScriptError("Selected layer is not a tile layer", "Error"); finaloutput = "Incorrect layer type: " + Data.Rooms[int.Parse(roomindex)].Layers[trueindex].Data.GetType();}
					}
					else {ScriptError("Layer does not exist", "Error"); finaloutput = "Specified layer index was out of bounds.";}
					}
					else
					{
					if (trueindex <= Data.Rooms.ByName(roomname).Layers.Count - 1 && trueindex >= 0)
					{
						if (Data.Rooms.ByName(roomname).Layers[trueindex].Data.GetType() == typeof(UndertaleModLib.Models.UndertaleRoom.Layer.LayerTilesData))
						{
							GetTileData(roomindex);
						}
						else {ScriptError("Selected layer is not a tile layer", "Error"); finaloutput = "Incorrect layer type: " + Data.Rooms.ByName(roomname).Layers[trueindex].Data.GetType();}
					}
					else {ScriptError("Layer does not exist", "Error"); finaloutput = "Specified layer index was out of bounds.";}
					}
				}
				else {ScriptError("Index must be a number", "Error"); finaloutput = "Layer index value was given in the incorrent format.";}
			}
			else Cancel();
		}
		else {ScriptError("Room does not exist", "Error"); finaloutput = "Specified room index was out of bounds.";}
	}
	else if (!roomindexfail && roomindex == null) Cancel();
	else {ScriptError("Index must be a number", "Error"); finaloutput = "Room index value was given in the incorrent format.";}
	}
	else {ScriptError("Invalid room name", "Error"); finaloutput = "Could not find a room with specified name.";}
}
else Cancel();
finaloutput
