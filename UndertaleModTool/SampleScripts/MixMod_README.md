Looking for an excuse to play Undertale again but don't have a Switch? I might have something for you!

The (probably) first mod that is not just another string/texture swap. Made with my [UndertaleModTool](https://github.com/krzys-h/UndertaleModTool).

It's **Undertale, But Every Time A Song Plays Its A Random Remix From YouTube Instead!** (or MixMod for short)

And it does exactly that. Every time a song plays, it searches YouTube for "[song name] undertale remix" and plays a random result from the top N (where N depends on my estimation of songs popularity). Don't like the remix it selected? Just hit the spacebar to choose another random one!

**Installation instructions**:
1. Make sure you are on Undertale version >= 1.08 (older ones are not supported by UndertaleModTool because of bytecode changes) on Windows (because I didn't manage to figure out how to access GMS surfaces from extensions so I have to pass the whole DirectX context and draw manually - seriously, there is like no documentation on extensions at all)
2. Download [UndertaleModTool](https://github.com/krzys-h/UndertaleModTool/releases) (the mod script is bundled with the download because I'm too lazy to create a github repo for one file) and [GMWebExtension](https://github.com/krzys-h/GMWebExtension/releases) (the libs only zip file)
3. Make sure you backed up your Undertale installation
4. Open UndertaleModTool, load the data.win file from the Undertale directory
5. Select Scripts > Run builtin script > MixMod
6. Select Scripts > Run builtin script > BorderEnabler - this is optional, but gives some additional space on the screen for me to put the video player somewhere
7. Save the data.win file
8. Extract all files from the GMWebExtension zip into the Undertale directory
9. Run the game and hope you did everything right and it doesn't crash

I did like 4 quick playthroughs with this and it works most of the time. **Known issues**:
* Sometimes the wrong song plays. There is not much that can be done about it, it's just a YouTube search over all. If this happens, just make it choose another one by pressing spacebar.
* Some videos don't want to play. This is because official Chromium Embedded Framework binaries are distributed without h264 support for licensing reasons. It is possible to make it work if I build it from sources, but the sources themselves are like 12 GB and I need to leave the build over night because it takes so long and uses 100% of my CPU. I did it like twice already and still didn't manage to get a working version :> If this happens, just hit the spacebar again for now
* Almost every time Undertale tries to close on its own, it ends up crashing instead. Not a huge issue as you are restarting the game anyway ;) No idea why this happens, probably something is wrong with deinitialization order
* The end credits song is normally split into several files, making it hard to play a whole song instead. You will notice the video reloading during room transitions, but it should resume seamlessly.
* When Sans starts/stops the music really quick during his battle, the video doesn't even have enough time to load properly