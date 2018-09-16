# UndertaleModTool
*(seeing such an amazing tool...<br/>
... fills you with DETERMINATION)*

![flowey](flowey.png)

heya. I heard you like digging deep into Undertale data so I made a tool just for you! Downloads are here: https://github.com/krzys-h/UndertaleModTool/releases

### Main features
* Can read every single byte from the lastest PC version Undertale data.win file (bytecode version 16 = 0x10 only, at least for now) and then recreate a byte-for-byte exact copy from the decoded data.
* Properly handles all of the pointers in the file so that if you add/remove stuff, make things longer/shorter, move them around etc. the file format won't break.
* An editor which lets you change (almost) every single value, including unknown ones. A lot better than a hex editor, huh?
* Includes a (very) simple level editor
* Allows for code disassembly and EDITING. This means you can add any custom code to the game, as long as you are determined enough to write it in GML assembly.
* Experimental high-level decompiler. The output is accurate and I've not seen it totally break in a long time, but it could use some more cleaning up of the high-level structures.
* Support for running scripts that automatically modify your data file - this is the way to distribute mods, but creating them is manual job for now. It also serves as a replacement for sharing hex editor offsets - if you make it into a file-format-aware script instead, there is much smaller change of it breaking after an update.
* All core IO functionality extracted into a library for use in external tools
* Can generate an .yydebug file for the GM:S debugger so that you can edit variables live [EXPERIMENTAL]
* Some hacks to make it work with GM:S 2. Should be able to load (but not yet save or fully display) the latest Nintendo Switch release.

### Included scripts
I also included some of my test scripts. They are:
* EnableDebug: does just that, makes the global variable 'debug' be enabled at game start. If you don't know about Undertale's debug mode, check out https://tcrf.net/Undertale/Debug_Mode
* DebugToggler: similar to the above, but instead toggles the debug mode on and off with F12
* BorderEnabler: lets you import the PlayStation exclusive borders into the PC version and patches all version checks so that they display properly
* GoToRoom: Replaces the debug mode functionality of the F3 button with a dialog that lets you jump to any room by ID
* ShowRoomName: Displays the current room name and ID on screen in debug mode
* testing: nothing important, just displays random text on the main menu - the first script I ever made

### Bug reports, contributing
All contributions are welcome! If you find a bug, a data file that does not load etc. please report it on the [issues page](https://github.com/krzys-h/UndertaleModTool/issues). Pull requests and help with decoding the format is welcome too! Here is my current list of stuff that needs to be worked on:
* Discover all missing stuff in the GML2 file format so that Nintendo Switch edition can be fully loaded, finish updating editors, add saving code
* Add support for missing chunks so that games other than Undertale can be loaded: Timelines, [Extensions](https://www.reddit.com/r/Underminers/comments/9g51wd/so_today_im_releasing_the_tool_i_promised_a_few/e62hbu1/) and Shaders
* Finish writing the decompiler:
  * add support for resolving resource types (requires propagating information from builtin function calls backwards, possibly across different code fragments)
  * decompile other loop types than just while(true)
  * decompile compound if expressions (things like `a || b` are decomposed into multiple ifs internally because GML has short-circuit evaluation)
  * make sure the output syntax is fully GML-compatible
* Write a matching GML compiler, so that edits of high-level code are possible
* Add support for other bytecode versions
* Maybe try to write a GameMaker Project output module, should be technically possible after decompiler is done but I'm not sure I really want to do that for legal reasons...

### data.win file format
Interested in the file and instruction format research I've done while working on this? Check out these:
* https://github.com/krzys-h/UndertaleModTool/wiki/Corrections-to-Game-Maker:-Studio-1.4-data.win-format-and-VM-bytecode,-.yydebug-format-and-debugger-instructions
* https://github.com/krzys-h/UndertaleModTool/wiki/Changes-in-Game-Maker:-Studio-2

### Special thanks
Undertale has a special thanks section so I will have one to! No minigame here, though.

Special thanks to everybody who did previous research on unpacking and decompiling Undertale, it was a really huge help:
* https://pcy.ulyssis.be/undertale/
* https://github.com/donkeybonks/acolyte/wiki/Bytecode
* https://github.com/PoroCYon/Altar.NET
* https://github.com/WarlockD/GMdsam
* [@NarryG](https://github.com/NarryG) for [helping me figure out](https://github.com/krzys-h/UndertaleModTool/issues/3) the missing stuff for GMS2 and Nintendo Switch release

And of course, special thanks to Toby Fox and the whole Undertale team for making the game ;)

How about a random screenshot because I'm bad at writing READMEs? NYEH HEH HEH HEH!

![screenshot](screenshot.png)
