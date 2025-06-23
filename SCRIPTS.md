This file contains descriptions of scripts that are bundled with the tool.

Many scripts are experimental, so be careful using them. **Always make frequent backups.**
Please report any bugs or issues you encounter.

## Resource Exporters

These are scripts designed to extract/dump assets from a game, whether for analysis or for reimporting later.

Most of these scripts will prompt you to select a folder/directory to export files, *directly*, to. Take care not to overwrite data on accident.

- `ExportAllAssembly.csx`: Exports GML assembly.
- `ExportAllCode.csx`: Exports decompiled GML code.
- `ExportAllEmbeddedTextures.csx`: Exports all texture pages as PNG files.
- `ExportAllFonts.csx`: Exports PNGs and glyph information of all fonts.
- `ExportAllMasks.csx`: Exports sprite collision masks as black-and-white PNGs.
- `ExportAllRoomsToPNG.csx`: Exports previews of all rooms as PNG files.
	* (Note: This is currently only supported on the Windows GUI.)
- `ExportAllShaders.csx`: Exports shader metadata, source code, and compiled (binary) code.
- `ExportAllSounds.csx`: Exports sounds as WAV/OGG files.
- `ExportAllSprites.csx`: Exports sprites as PNG files. Optionally includes padding, which is when a sprite frame doesn't use its full extents.
- `ExportAllTextures.csx`: Exports PNGs of all texture page items referenced by sprites, fonts, and backgrounds (tilesets).
- `ExportAllTexturesGrouped.csx`: Same as the above script, except this one creates sub-directories for each asset.
- `ExportAllTilesets.csx`: Exports tilesets (or backgrounds) as PNG files.
- `ExportAllStrings.csx`: Exports all strings to a text file. Optionally includes strings with newlines (which cannot be reimported).
- `ExportAllStringsJSON.csx`: Exports all strings to a JSON text file. Always includes strings with newlines.
- `ExportSpecificCode.csx`: Exports specific code entries, given the names of objects, scripts, or code entries directly.
- `ExportSpecificSprites.csx`: Exports specific sprites, given their names. Optionally includes padding.
- `ExportSpritesAsGIF.csx`: Exports sprites to animated GIF files. Will always preserve padding.
- `ExportTextureGroups.csx`: Exports PNGs of all texture page items referenced by sprites, fonts, and tilesets. Grouped into directories by texture groups, and exports full texture pages as well. Optionally includes padding.

## Resource Importers

These are scripts designed to import assets into a game, usually requiring certain types of files/data as input.

- `ApplyBasicGraphicsMod.csx`: Imports graphics without using a texture packer, by swapping the sprite in place on its texture sheet, as long as it's the same size.
- `ImportAllStrings.csx`: Imports strings from a text file, as long as those strings are from the same unmodified game, and that strings containing newlines are not present.
- `ImportAllStringsJSON.csx`: Imports strings from a JSON text file, as long as those strings are from the same unmodified game.
- `ImportAllTilesets.csx`: Imports tileset (or background) images from a folder, as long as corresponding assets already exist in the game data.
- `ImportAssembly.csx`: Imports GML assembly.
- `ImportGML.csx`: Imports and compiles GML code. Will automatically create certain types of assets when possible. Code can also reference scripts/functions defined in the same import.
- `ImportGMS2FontData.csx`: Imports font data directly from a GMS2 .yy font project file. May break depending on GameMaker version.
- `ImportGraphics.csx`: Imports sprites or backgrounds as PNG files from a folder. Sprites are in the format `filename_N.png`, where N is the frame number starting at 0. Put files in a folder named "Backgrounds" to import them as backgrounds.
- `ImportGraphicsAdvanced.csx`: Imports sprites or backgrounds as PNG or GIF files from a folder. Supports a variety of formats (frame numbers starting at 1 or 0, GameMaker `_stripN`, animated GIFs, single-frame sprites). Has an additional dialog after importing to set sprite offsets and, for GMS2 games, animation speed.
	* (Note: This is currently only supported on the Windows GUI.)
- `ImportMasks.csx`: Imports sprite collision masks from black-and-white PNG files.
- `ImportShaders.csx`: Imports shader data, expected to be formatted as exported by `ExportShaders.csx`.
- `ImportSingleSound.csx`: Imports a single sound asset, WAV or OGG format.
- `ImportSounds.csx`: Imports sound assets, WAV or OGG format.
- `NewTextureRepacker.csx`: Regenerates all texture pages using a texture packer, and configurable settings by editing the script. Read the script's comments for more details.
	* (Be very careful when using this script, as data can very easily get corrupted or broken.)
- `ReduceEmbeddedTexturePages.csx`: Similar to above texture page regenerator, but a more naive/legacy implementation.
	* (Similar to the previous script, be very careful when using this script.)

## Sample Scripts

These are an old set of scripts that can be referenced for how scripting works, or just for fun.

- `DeltaHATE.csx`: Mod showcasing asset shuffling techniques on the SURVEY_PROGRAM version of Deltarune, with hilarious results. Similar to another modding tool called "HATE", for Undertale.
- `DeltaMILK.csx`: Replaces every non-background sprite in Deltarune chapter 1 with milk.
- `HeCanBeEverywhere.csx`: Experimental mod adding multiple Jevils to the SURVEY_PROGRAM version of Deltarune.
- `MixMod.csx`: As of writing, this script is non-functioning. It's intended to use a GameMaker extension to play music from YouTube, in Undertale.
- `RoomOfDetermination.csx`: Mod showcasing room editing and code modding capabilities, in Undertale.
- `TheWholeWorldRevolving.csx`: Mod that permanently rotates the screen in the SURVEY_PROGRAM version of Deltarune.

## UTDR Scripts

These are scripts developed primarily for use in Undertale/Deltarune.

- `Debug.csx`: Enables or disables debug mode in all supported Undertale/Deltarune versions.
- `DeltaruneClearShaderData.csx`: Clears shader data from Deltarune, allowing older versions of the game to run on ancient hardware.
- `DeltaruneReloadJSON.csx`: Adds a hotkey to Deltarune, which reloads the language JSON.
- `DeltaruneTTFFonts.csx`: Marks all fonts in Deltarune to be externally loaded. Does not handle Japanese text.
- `DisableDogcheck.csx`: Disables dogcheck in Undertale and Deltarune such that you can load any room in the game from your save file.
- `ShowRoomName.csx`: Adds a room ID and name display to the screen when debug is enabled, on Undertale or Deltarune.
- `TouchControlsEnabler.csx`: Adds mobile touch controls to Undertale or Deltarune.
- `UndertaleBattlegroupSelector.csx`: Adds a battlegroup selector debug hotkey to Undertale.
- `UndertaleBetterVaporiser.csx`: Replaces the current vapor system in Undertale with a new one that generates vapor on the fly for the appropriate sprite and can vaporize colored sprites.
- `UndertaleBorderEnabler.csx`: Enables borders on PC Undertale for versions prior to Undertale Switch.
- `UndertaleBorderEnablerv1_11.csx`: Enables borders on PC Undertale for the 1.11 version of Undertale (Switch).
- `UndertaleChangeHomeBattlegroup.csx`: Changes the target battlegroup when pressing the Home key in Undertale.
- `UndertaleDebugMsg.csx`: Displays the contents of `global.msg` on-screen while debug mode is enabled in Undertale.
- `UndertaleDebugToggler.csx`: Makes it possible to switch debug mode on and off using F1 in Undertale.
- `UndertaleDialogSimulator.csx`: Adds a dialogue editor to Undertale.
- `UndertaleFixAlphysLabCrashAndroid.csx`: Fixes a crash at Alphys's lab on Android devices in Undertale.
- `UndertaleGoToRoom.csx`: Adds a debug hotkey to go to a room ID, in Undertale.
- `UndertaleRunButton.csx`: Removes the debug check from the Backspace hotkey speed boost in Undertale.
- `UndertaleSafeBlaster.csx`: Adds a new version of obj_gasterblaster that isn't tied to the Sans boss fight specifically.
- `UndertaleSimplifyBattlegroupScript.csx`: Removes duplicate code from `scr_battlegroup` in Undertale.
- `UndertaleSwitchAndXboxOnPC.csx`: Applies light modifications to the Switch/Xbox versions of Undertale so that they can run on PC correctly.
- `UndertaleTTFFonts.csx`: Marks all fonts in Undertale to be externally loaded. Does not handle Japanese text.
- `UndertaleWithJSONs.csx`: This script JSONifies all Undertale versions with Japanese support, 1.05+. Switch languages using F11. Reload text for current language from JSON on command using F12.
- `UndertaleWASD.csx`: Adds WASD controls to Undertale.

### Utility Scripts

- `ExternalizeAllOGGs.csx`: Externalizes all OGG sound effects from a game.
- `ExtractEmbeddedDataFile.csx`: Extracts a data file from an executable or memory dump file. Not guaranteed to produce usable results.
- `FancyRoomSelect.csx`: Adds a mod that makes selecting and teleporting to different rooms in a game very user-friendly.
- `FindAndReplace.csx`: Allows for performing find and replace operations in GML code across an entire game.
- `FindObjectsInRooms.csx`: Finds instances of the desired object types in all room data of a game.
- `FindObjectsWithSprite.csx`: Finds object types assigned the desired sprite pattern in a game.
- `FontEditor.csx`: Allows for editing font data in detail using a custom GUI.
	* (Note: This is currently only supported on the Windows GUI.)
- `GoToRoom_AutoLocatePersistentObj.csx`: Similar to `GoToRoom.csx` for Undertale/Deltarune, but instead attempts to locate a persistent object automatically, making it portable. Results will vary by game.
- `MergeImages.csx`: Loads images from two directories, and exports side-by-side images of all corresponding images.
- `ScaleAllTextures.csx`: Scales all texture pages in a game. Very likely to cause visual problems.
- `SearchLimited.csx`: Performs a GML code search, in a limited subset of the game code.

## Technical Scripts

These scripts are highly technical and niche. **You should REALLY know what you're doing before using any of these, more so than other scripts. They are likely to break.**

- `13_To_14.csx`, `14_To_16.csx`, `15_To_16.csx`, `15_To_17_To_16.csx`, `16_To_17.csx`, `ConvertFrom17to16.csx`, `ConvertFrom17to16_for_2.3.csx`: Attempts to convert the bytecode version of games.
- `AutoBackup.csx`: An automatic backup script of the data.win file which runs in the background while you work, with a GUI. May be unstable.
- `CheckDecompiler.csx`: Checks the decompiler/compiler accuracy of a game and returns the inaccurately decompiled scripts in the game directory.
- `CopySound.csx`, `CopySoundInternal.csx`: Attempts to copy sound data, from one game to another, and within the same game, respectively.
- `CopySpriteBgFont.csx`, `CopySpriteBgFontInternal.csx`: Attempts to copy sprite/background/font data, from one game to another, and within the same game, respectively.
- `ExecutionOrder.csx`: When active, displays the order that object events execute in supported games, as well as interactions with `global.interact`.
- `ExportAllCodeSync.csx`: Exports GML code, but not in parallel. This is much slower, but prevents any threading issues, should they arise.
- There are two scripts to assist in repairing data files where assets were accidentally moved from their original places.
    * `ExportAssetOrder.csx`: This exports all the names of a game's assets (currently excluding GameMaker 2.3+ assets) to a text file.
    * `ImportAssetOrder.csx`: This takes an existing text file exported from `ExportAssetOrder.csx`, and uses it to reorganize the assets in the current data file.
        - If assets cannot be found, it will fail to fully complete. Assets not in the text file will simply be moved to the end of their respective lists.
- `FindNullsInOffsetMap.csx`: Used to find null objects in the offset map, which is an internal data file structure.
- `FindUnknownFunctions.csx`: Used to identify improperly-defined functions in a game, which lead to a game failing to boot.
- `FindUnusedStrings.csx`: Used to find unused strings in a game. Obsolete to the new GUI that can do the same.
- `GameObjectCopy.csx`, `GameObjectCopyInternal.csx`: Attempts to copy object data, from one game to another, and within the same game, respectively.
- `GetAllChunkNames.csx`: Returns C# code containing the list of chunk names in the opened game data file.
- `GetTempDirectory.csx`: Returns the current base directory, used for debugging .NET bundled versions of the tool.
- `ImportGraphics_Full_Repack.csx`: Similar to `ImportGraphics.csx`, but exports and reimports all textures in the game. Can be extremely slow and memory/storage-intensive.
- `LintAllScripts.csx`: Debugging utility for making sure scripts compile properly.
- `MatchRoomOrderToInternalOrder.csx`: Matches the room asset order to the actual room order.
- `Profiler.csx`: Based on the execution order script, but reworked to be a profiler and stack tracer, to identify freeze locations. Performs a lot of disk operations.
- `RealignRoomInternalOrder.csx`: Matches the room order to the room asset order. Obsolete to the GUI that can do the same.
- `RemoveUnusedSounds.csx`: Attempts to remove unused sounds from a game.
- `RestoreMissingCodeLocals.csx`: Attempts to restore code locals to code entries that are missing them, only applicable to certain GameMaker versions.
- `TestExportAllCode.csx`: Records instances of tool crashes when decompiling all code.
- `ToggleAlignment.csx`: Toggles 4-byte alignment in the TPAG chunk, part of a game data file.
- `VariableFixer.csx`: Attempts to fix variable configurations in the data file, for certain GameMaker versions.
