# UndertaleModTool Super Usable

A fork of [AwfulNasty's fork](https://github.com/AwfulNasty/UndertaleModTool/tree/RoomEditorChanges) of [UndertaleModTool](https://github.com/krzys-h/UndertaleModTool) that adds even more stuff, still mainly for Pizza Tower modding.

# Features
- Stuff from the original UTMT Usable (AwfulNasty's fork): can open scr_player_mach3; button to add creation code to rooms and objects in rooms
- Types for some Pizza Tower stuff (like `spr_*` variables for characterspr)
- Additional functions for structs and stuff
- Automatic selection/creation of instance layers and grid snapping when dragging objects into rooms
- Maybe more to come in the future!

# Download

None yet, you'll have to compile it yourself.

# Compiling

[Same as vanilla UTMT.](https://github.com/krzys-h/UndertaleModTool#compilation-instructions)

<!--
  commandline building:

  dotnet publish UndertaleModTool -c Release -r win-x64 --self-contained false -p:PublishSingleFile=True --output bin/non-sc
  dotnet publish UndertaleModTool -c Release -r win-x64 --self-contained true -p:PublishSingleFile=True --output bin/sc
  dotnet publish UndertaleModCli -c Release -r win-x64 --self-contained false -p:PublishSingleFile=True --output bin/cli

-->