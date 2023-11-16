This file contains information regarding the scripts that are bundled with the tool.

Many of them are experimental, so be weary of running more obscure scripts: make backups.
Please report any bugs or issues with the scripts to the Underminers server's #help channel on Discord at https://discord.gg/hnyMDypMbN.

## Sample scripts

Originally the only category for bundled scripts, mostly contains the original scripts made for the tool.
They are relatively self-explanatory, but there are also some helpful general-purpose scripts included:
- `BorderEnabler.csx`: Enables borders on PC versions of Undertale.
- `DebugToggler.csx`: Enables a hotkey to toggle debug mode in Undertale and Deltarune.
- `EnableDebug.csx`: Enables debug mode in Undertale/Deltarune.
- `FindAndReplace.csx`: Tool to find and replace GML code across an entire game.
- `GoToRoom.csx`: Enables a hotkey to warp to a supplied room ID in a game.
- `RunSwitchOnPC.csx`: Converts the Switch version of Undertale to run on PC (certain versions).
- `Search.csx`: Tool to search the GML code across an entire game.
- `ShowRoomName.csx`: Enables an overlay to display the current room name and ID.
- `TTFFonts.csx`: Marks all fonts in Undertale to be externally loaded. Does not handle Japanese text.

## Community scripts

- `AutoBackup.csx`: An automatic backup script of the data.win file which runs in the background while you work, with a GUI.
- `BetterVaporiserForUT.csx`: Replaces the current vapor system in Undertale with a new one that generates vapor on the fly for the appropriate sprite and can vaporize colored sprites.
- `BorderEnablerv1_11.csx`: Enables borders on PC for the 1.11 version of Undertale (Switch).
- `ChangeHomeBattlegroup.csx`: Changes the target battlegroup when pressing the Home key in Undertale.
- `DisableDogcheck.csx`: This disables dogcheck in Undertale and Deltarune such that you can load any room in the game from your save file.
- `EditGMS2TileData.csx`: A temporary script to help edit tile data in GMS2 games.
- `ExternalizeAllOGGs.csx`: Script to externalize all OGG sound effects from a game.
- `FancyRoomSelect.csx`: Script to make selecting and teleporting to different rooms in a game very user-friendly.
- `FixAlphysLabCrashAndroid`: Script that fixes a crash at Alphys's lab on Android devices.
- `ReloadDeltaruneJSON.csx`: Script to add a hotkey to Deltarune, which reloads the language JSON.
- `TouchControlsEnabler.csx`: Adds mobile touch controls to an Undertale or Deltarune data.win.
- `TTFFonts (Deltarune).csx`: Marks all fonts in Deltarune to be externally loaded. Does not handle Japanese text.
- `UndertaleWithJSONs.csx`: This script JSONifies all Undertale versions with Japanese support, 1.05+. Switch languages using F11. Reload text for curent language from JSON on command using F12. Reloading from JSON may take about 10 seconds.

## Unpacker scripts

These scripts are self-explanatory from their names, but for clarification:
- `ExportAllSoundsOld.csx`: This is an old version of the script to export sounds, which doesn't handle all types of sounds properly.
- `ExportASM.csx`: Exports GML assembly.
- `ExportAllCode.csx`: Exports GML code. It has a GMS2.3 counterpart, as code entry names can be too long.
- `ExportAllSprites.csx`: Exports sprites with all padding removed. Creates and saves in `Export_Sprites` directory where `data.win` is opened from. Use `ExportAllSpritesWithPadding.csx` if you need to preserve the padding.
- `ExportMasks.csx`: For exporting sprite collision mask information.
- `ExportAllSpritesWithPadding.csx`: Exports sprites preserving padding. Creates and saves in `Export_Textures` directory where `data.win` is opened from.
- `MergeImages.csx`: A script that can bulk merge images together for comparisons, from different folders.
- `DumpSpecificCode.csx`: Used to export specific code entries should you have object names.

## Repacker scripts

These scripts are self-explanatory from their names, but for clarification:
- `ImportASM.csx`: Imports GML assembly. It has a counterpart for GMS2.3 games, with longer code entry names.
- `ImportGML.csx`: Imports GML code. It also has a 2.3 counterpart for the same reason.
- `ImportMasks.csx`: For importing sprite collision mask information.
- `ApplyBasicGraphicsMod.csx`: Imports graphics without a repacker, by swapping the sprite in place on its texture sheet, as long as it's the same size.

## Technical scripts

Your mileage with these scripts may vary drastically. Most of them are for very specific usage that are not fully documented here.

- There are two scripts to assist in repairing data files where assets were accidentally moved from their original places.
    * `ExportAssetOrder.csx`: This exports all the names of a game's assets (currently excluding GMS2.3 constructs) to a text file.
    * `ImportAssetOrder.csx`: This takes an existing text file exported from `ExportAssetOrder.csx`, and uses it to reorganize the assets in the current data file.
        - If assets cannot be found, it will fail to fully complete. Assets not in the text file will simply be moved to the end of their respective lists.
- `CheckDecompiler.csx`: This script checks the decompiler/compiler accuracy of a game and returns the inaccurately decompiled scripts in the game directory.
- `ExportAllCodeSync.csx`: Exports GML code, but not asynchronously, possibly preventing errors.
- `ExtractEmbeddedDataFile.csx`: Extracts an embedded data file from a YYC-compiled game or from a dump file from memory.
- `FindUnknownFunctions.csx`: Used to identify improperly-defined functions in a game, which lead to a game failing to boot.
- `ExecutionOrder.csx`: When active, displays the order that object events execute in supported games, as well as interactions with "global.interact"
- `Profiler.csx`: Based on the execution order script, but reworked to be a profiler and stack tracer, to identify freeze locations. Performs a lot of disk operations.
- `ExportAndConvert_2_3_ASM.csx`: Hackily processes GMS2.3 assembly, attempting to get rid of some new function constructs when possible (making it easier to edit).
- `ImportGraphics_Full_Repack.csx`: Also known as ImportGraphics_v2, this completely repacks all sprite data in texture sheets, thoroughly.
- `TestExportAllCode.csx`: Records instances of entire tool crashes when decompiling code.
- `TextInputTestWinForms.csx`: Self-explanatory test script, demonstrating text input in WPF.
- `TextInputTestWPF.csx`: Similar to above.

## Credits

For individual script credits, look at the source for any given script. Often, comments are at the top.

Here is the list of contributors as of writing:
- Agentalex9
- [BenjaminUrquhart](https://github.com/BenjaminUrquhart)
- [colinator27](https://github.com/colinator27)
- [GitMuslim](https://github.com/GitMuslim)
- [Grossley](https://github.com/Grossley)
- [Kneesnap](https://github.com/Kneesnap)
- [krzys_h](https://github.com/krzys-h)
- Lassebq
- mono21400
- [nik (the cat)](https://github.com/nkrapivin)
- samuelroy21 (among others from the DSG team)
- Yokim (Jockeholm)

Some contributors may be missing from this list.
