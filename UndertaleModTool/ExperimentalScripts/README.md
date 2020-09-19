This directory contains experimental scripts, created by the community.

They are experimental. 
Although the scripts are very powerful, they may have bugs. 
Please report any bugs or issues with the experimental scripts to the Underminers server's #help channel on Discord at https://discord.gg/RxXpdwJ

## Descriptions of the experimental scripts

- `BetterVaporiserForUT.csx`: Replaces the current vapor system in Undertale with a new one that generates vapor on the fly for the appropriate sprite and can vaporize colored sprites.
- `DisableDogcheck.csx`: This disables dogcheck in Undertale and Deltarune such that you can load any room in the game from your save file.
- `ExportAllEmbeddedTextures.csx`: This exports all embedded textures to an `EmbeddedTextures` folder in your game files folder.
- `ExportAllSounds.csx`: This highly configurable sound exporter can export sounds both internally (like in Undertale) and in external audiogroups (like in Deltarune).
- `ExportAllStrings.csx`: Exports all strings to an external `strings.txt` file in your game's file directory.
- `ExportAllTexturesWithPadding.csx`: Modified version of sample script ExportAllTextures, where it additionally accounts for padding (giving consistent image sizes per sprite).
- `ExternalizeAllOGGs.csx`: Exports all sound files and also configures the game to load sounds from external files, allowing easy editing of in game sounds.
- `FontDataExport.csx`: Exports the glyph and texture data for one or more fonts.
- `FontDataImport.csx`: Imports the glyph data of one font. Drag the `.csv` file to the game file folder before use. It can easily make new fonts if used in conjunction with the import graphics scripts.
- `ImportAllEmbeddedTextures.csx`: This imports all embedded textures from an `EmbeddedTextures` folder in your game files folder.
- `ImportAllStrings.csx`: Imports all strings from an external `strings.txt` file in your game's file directory, overwriting the current game's strings with new ones.
- `ImportASound.csx`: Allows you to import a sound easily with many options for special configuration. Does not operate in bulk (only can import one sound at a time).
- `ImportSoundsBulk.csx`: ImportASound but in bulk (multiple files at a time supported).
- `ImportGML.csx`: Allows you to import decompiled code in bulk from a folder. It can replace existing code very easily. However, it can link new code to existing objects or scripts. If they do not exist then it can make entirely new objects or scripts for your code to link to. To automatically link your new code to an object or script, it must begin with `gml_Script` or `gml_Object`.
- There is an import graphics script that allows you to import a sprite of arbitrary size easily via a texture packer. The syntax of the files that can be used with this script is as follows:        
    * `SPRITENAME_FRAMENUMBER.png`
        - Replace `SPRITENAME` with your sprite's name (no spaces or punctuation). 
        - Replace `FRAMENUMBER` with the frame number of the sprite, starting at 0 for the first frame, 1 for the second frame, and so on.
    * There are two versions.
        - `ImportGraphics_v1.csx`: This only packs new embedded texture sheets and leaves the originals alone.
        - `ImportGraphics_v2.csx`: Import a sprite of arbitrary size easily using this script via a texture packer. This repacks all textures (no duplicate images/bloat) but takes longer to complete.
- There are additionally two scripts to assist in repairing data files where assets were accidentally moved from their original places.
    * `ExportAssetOrder.csx`: This exports all the names of a game's assets (currently excluding GMS2.3 constructs) to a text file.
    * `ImportAssetOrder.csx`: This takes an existing text file exported from `ExportAssetOrder.csx`, and uses it to reorganize the assets in the current data file.
        - If assets cannot be found, it will fail to fully complete. Assets not in the text file will simply be moved to the end of their respective lists.
- There are additionally two scripts to import and export GML assembly data.
    * `ExportASM.csx`: Exports assembly data to an `Export_Assembly` folder in your game files folder.
    * `ImportASM.csx`: Imports assembly data from a folder into your game.

## Credits

- `BetterVaporiserForUT.csx`: By Agentalex9 and Grossley
- `DisableDogcheck.csx`: By Grossley
- `ExportAllEmbeddedTextures.csx`: Improved from the original script by Grossley
- `ExportAllSounds.csx`: Improved from Kneesnap's script by Grossley
- `ExportAllStrings.csx`: Improved from the original script by Grossley
- `ExportAllTexturesWithPadding.csx`: Modified with the help of Agentalex9
- `ExternalizeAllOGGs.csx`: Improved by Grossley based on the work of nik (the cat)
- `FontDataExport.csx`: By mono21400
- `FontDataImport.csx`: By mono21400
- `ImportAllEmbeddedTextures.csx`: Improved from the original script by Grossley
- `ImportAllStrings.csx`: Improved from the original script by Grossley
- `ImportASound.csx`: By nik (the cat), Jockeholm, and Grossley
- `ImportSoundsBulk.csx`: By nik (the cat), Jockeholm, and Grossley
- `ImportGML.csx`: By samuelroy21 of the DSG team.
- The Import Graphics scripts were made by samuelroy21 with the help of colinator27, Grossley, and others of the DSG team.
- `ExportAssetOrder.csx`: By Grossley and colinator27
- `ImportAssetOrder.csx`: By colinator27
- `ExportASM.csx`: By mono21400
- `ImportASM.csx`: By samuelroy21 of the DSG team, mono21400