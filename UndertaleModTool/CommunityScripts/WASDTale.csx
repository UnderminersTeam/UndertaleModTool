// Enables WASD controls for all Undertale versions
// Made by Grossley with invaluable help from Lil Alien.

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


ScriptMessage(@"This script enables WASD controls for
all Undertale versions.");

if (Data.GeneralInfo.Name.Content == "NXTALE" || Data.GeneralInfo.Name.Content.StartsWith("UNDERTALE")) 
{
Data.Code.ByName("gml_Object_obj_time_Step_1").AppendGML(@"
if (global.debug == 0)
{
    if keyboard_check(ord(""W""))
        keyboard_key_press(vk_up)
    if keyboard_check_released(ord(""W""))
        keyboard_key_release(vk_up)
    if keyboard_check(ord(""A""))
        keyboard_key_press(vk_left)
    if keyboard_check_released(ord(""A""))
        keyboard_key_release(vk_left)
    if keyboard_check(ord(""S""))
        keyboard_key_press(vk_down)
    if keyboard_check_released(ord(""S""))
        keyboard_key_release(vk_down)
    if keyboard_check(ord(""D""))
        keyboard_key_press(vk_right)
    if keyboard_check_released(ord(""D""))
        keyboard_key_release(vk_right)
}
", Data);
}
else
{
    ScriptError("This script can only be used with Undertale!", "Not Undertale");
    return;
}

// Done.
ChangeSelection(Data.Code.ByName("gml_Object_obj_time_Step_1")); // Show.
ScriptMessage(@"WASD is now enabled.");