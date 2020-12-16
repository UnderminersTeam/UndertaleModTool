// Exports the names of assets in a data file in order.
// Made by Grossley and colinator27.

using System.Text;
using System;
using System.IO;

// Get the path, and check for overwriting
string outputPath = Path.Combine(Path.GetDirectoryName(FilePath) + Path.DirectorySeparatorChar, "asset_names.txt");
if (File.Exists(outputPath))
{
	bool overwriteCheck = ScriptQuestion(@"An 'asset_names.txt' file already exists. 
Would you like to overwrite it?");
	if (overwriteCheck)
		File.Delete(outputPath);
	else
	{
		ScriptError("An 'asset_names.txt' file already exists. Please remove it and try again.", "Error: Export already exists.");
		return;
	}
}

using (StreamWriter writer = new StreamWriter(outputPath))
{
	// Write Sounds.
	writer.WriteLine("@@sounds@@");
	if (Data.Sounds.Count > 0) 
	{
		foreach (UndertaleSound sound in Data.Sounds)
			writer.WriteLine(sound.Name.Content);
	}
	// Write Sprites.
	writer.WriteLine("@@sprites@@");
	if (Data.Sprites.Count > 0) 
	{
		foreach (var sprite in Data.Sprites)
			writer.WriteLine(sprite.Name.Content);
	}
	
	// Write Backgrounds.
	writer.WriteLine("@@backgrounds@@");
	if (Data.Backgrounds.Count > 0)
	{
		foreach (var background in Data.Backgrounds)
			writer.WriteLine(background.Name.Content);
	}
	
	// Write Paths.
	writer.WriteLine("@@paths@@");
	if (Data.Paths.Count > 0) 
	{
		foreach (UndertalePath path in Data.Paths)
			writer.WriteLine(path.Name.Content);
	}
	
	// Write Scripts.
	writer.WriteLine("@@scripts@@");
	if (Data.Scripts.Count > 0) 
	{
		foreach (UndertaleScript script in Data.Scripts)
			writer.WriteLine(script.Name.Content);
	}
	
	// Write Fonts.
	writer.WriteLine("@@fonts@@");
	if (Data.Fonts.Count > 0) 
	{
		foreach (UndertaleFont font in Data.Fonts)
			writer.WriteLine(font.Name.Content);
	}

	// Write Objects.
	writer.WriteLine("@@objects@@");
	if (Data.GameObjects.Count > 0) 
	{
		foreach (UndertaleGameObject gameObject in Data.GameObjects)
			writer.WriteLine(gameObject.Name.Content);
	}
	
	// Write Timelines.
	writer.WriteLine("@@timelines@@");
	if (Data.Timelines.Count > 0)
	{
		foreach (UndertaleTimeline timeline in Data.Timelines)
			writer.WriteLine(timeline.Name.Content);
	}

	// Write Rooms.
	writer.WriteLine("@@rooms@@");
	if (Data.Rooms.Count > 0)
	{
		foreach (UndertaleRoom room in Data.Rooms)
			writer.WriteLine(room.Name.Content);
	}

	// Write Shaders.
	writer.WriteLine("@@shaders@@");
	if (Data.Shaders.Count > 0)
	{
		foreach (UndertaleShader shader in Data.Shaders)
			writer.WriteLine(shader.Name.Content);
	}

	// Write Extensions.
	writer.WriteLine("@@extensions@@");
	if (Data.Extensions.Count > 0) 
	{
		foreach (UndertaleExtension extension in Data.Extensions)
			writer.WriteLine(extension.Name.Content);
	}

	// TODO: Perhaps detect GMS2.3, export those asset names as well.
}
