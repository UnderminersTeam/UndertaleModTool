EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Error 0: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}
else if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1&2")
{
    ScriptError("Error 1: Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}

ScriptMessage("Select the MILK that you prefer\nReplace every non-background sprite with milk (for Deltarune)\nby krzys_h");

var milk = Data.Sprites.ByName("spr_checkers_milk").Textures[0].Texture;
foreach (var sprite in Data.Sprites)
{
    if (sprite.Name.Content.StartsWith("bg_"))
        continue;
    foreach (var tex in sprite.Textures)
        tex.Texture = milk;
}