using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using UndertaleModLib;
using UndertaleModLib.Scripting;
using UndertaleModLib.Models;

EnsureDataLoaded();

ScriptMessage("Select the file to copy the sprite origins from");

UndertaleData donorData;
string donorDataPath = PromptLoadFile(null, null);
if (donorDataPath == null)
    throw new ScriptException("The donor data path was not set.");

using (var stream = new FileStream(donorDataPath, FileMode.Open, FileAccess.Read))
    donorData = UndertaleIO.Read(stream, (warning, _) => ScriptMessage("A warning occured while trying to load " + donorDataPath + ":\n" + warning));

foreach (var sprite in Data.Sprites)
{
    if (sprite is null)
        continue;
    var donorSpr = donorData.Sprites.ByName(sprite.Name.Content);
    if (donorSpr is not null)
    {
        sprite.OriginXWrapper = donorSpr.OriginXWrapper;
        sprite.OriginYWrapper = donorSpr.OriginYWrapper;
    }
}
