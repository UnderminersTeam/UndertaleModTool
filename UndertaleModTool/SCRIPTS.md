This file contains information regarding the scripts that are bundled with the tool.

Many of them are experimental, so be weary of running more obscure scripts: make backups.
Please report any bugs or issues with the scripts to the Underminers server's #help channel on Discord at https://discord.gg/RxXpdwJ.

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
- `ReloadDeltaruneJSON.csx`: Script to add a hotkey to Deltarune, which reloads the language JSON.
- `TTFFonts (Deltarune).csx`: Marks all fonts in Deltarune to be externally loaded. Does not handle Japanese text.
- `UndertaleWithJSONs.csx`: This script JSONifies all Undertale versions with Japanese support, 1.05+. Switch languages using F11. Reload text for curent language from JSON on command using F12. Reloading from JSON may take about 10 seconds.

## Unpacker scripts

These scripts are self-explanatory from their names, but for clarification:
- `ExportAllSoundsOld.csx`: This is an old version of the script to export sounds, which doesn't handle all types of sounds properly.
- `ExportASM.csx`: Exports GML assembly.
- `ExportAllCode.csx`: Exports GML code. It has a GMS2.3 counterpart, as code entry names can be too long.
- `ExportMasks.csx`: For exporting sprite collision mask information.

## Repacker scripts

These scripts are self-explanatory from their names, but for clarification:
- `ImportASM.csx`: Imports GML assembly. It has a counterpart for GMS2.3 games, with longer code entry names.
- `ImportGML.csx`: Imports GML code. It also has a 2.3 counterpart for the same reason.
- `ImportMasks.csx`: For importing sprite collision mask information.

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

## Credits

For individual script credits, look at the source for any given script. Often, comments are at the top.

Here is the list of contributors as of writing:
- Agentalex9
- BenjaminUrquhart
- colinator27
- Grossley
- Kneesnap
- krzys_h
- Lassebq
- mono21400
- nik (the cat)
- samuelroy21 (among others from the DSG team)
- Yokim (Jockeholm)

Some contributors may be missing from this list.