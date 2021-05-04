// Sets up NXTALE to run well on PC.

EnsureDataLoaded();

if (Data.GeneralInfo.Name.Content != "NXTALE")
{
    ScriptError("This script can only be used with\nThe Nintendo Switch version of Undertale.", "Not NXTALE");
    return;
}

// Enables borders and disables interpolation. It does this by making platform-specifc code run on desktop.
ReplaceTextInGML("gml_Object_obj_time_Draw_77", "global.osflavor >= 3", "1");

// This enables Mad Mew Mew's entrance.
ReplaceTextInGML("gml_Object_obj_kitchenchecker_Create_0", "global.osflavor == 4 || global.osflavor == 5", "1");
ReplaceTextInGML("gml_Object_obj_kitchenchecker_Alarm_2", "(global.osflavor == 4 || global.osflavor == 5) && ", "");
ReplaceTextInGML("gml_Object_obj_npc_room_Create_0", "(global.osflavor != 4 && global.osflavor != 5) || ", "");

// Done.
ScriptMessage(@"NXTALE Enabler by Kneesnap

NOTE: You're not done yet!

Copy 'mus_mewmew.ogg', 'mus_sfx_dogseal.ogg', and 'DELTARUNE.exe'
into the folder you will save this game archive.
Use the DELTARUNE runner to run Undertale.

Also if you have the Steam version, you may need to put all the files there instead.");