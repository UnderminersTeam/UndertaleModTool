EnsureDataLoaded();

if (Data?.GeneralInfo?.DisplayName?.Content.ToLower() == "deltarune chapter 1 & 2")
{
    ScriptError("Incompatible with the new Deltarune Chapter 1 & 2 demo");
    return;
}


/* TODO: Respect loop/no loop setting properly */

ScriptError("This script has not been updated to reflect the new extension format in UndertaleModTool and so has been temporarily disabled.");
return;

// I don't know how much work it will be to fix this, so I'm going to comment it all out for now, and try to fix it later.

/*
var browserext = new UndertaleExtension()
{
    Name = Data.Strings.MakeString("GMWebBrowser"),
    ClassName = Data.Strings.MakeString(""),
    EmptyString = Data.Strings.MakeString("")
};
browserext.Files.Add(new UndertaleExtensionFile() 
{
    Filename = Data.Strings.MakeString("GMWebExtension.dll"),
    Kind = UndertaleExtensionKind.DLL,
    InitScript = Data.Strings.MakeString("__webextension_native_init"),
    CleanupScript = Data.Strings.MakeString("__webextension_native_exit"),
});
// 0xc = cdecl?
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 1, 0xc, "RegisterCallbacks", UndertaleExtensionVarType.Double, "RegisterCallbacks", UndertaleExtensionVarType.String, UndertaleExtensionVarType.String, UndertaleExtensionVarType.String, UndertaleExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 2, 0xc, "__webextension_native_init", UndertaleExtensionVarType.Double, "__webextension_native_init");
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 3, 0xc, "__webextension_native_exit", UndertaleExtensionVarType.Double, "__webextension_native_exit");
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 4, 0xc, "__webextension_set_device", UndertaleExtensionVarType.Double, "__webextension_set_device", UndertaleExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 5, 0xc, "browser_create", UndertaleExtensionVarType.Double, "browser_create", UndertaleExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 6, 0xc, "browser_destroy", UndertaleExtensionVarType.Double, "browser_destroy", UndertaleExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 7, 0xc, "browser_load", UndertaleExtensionVarType.Double, "browser_load", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 8, 0xc, "browser_load_html", UndertaleExtensionVarType.Double, "browser_load_html", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 9, 0xc, "browser_resize", UndertaleExtensionVarType.Double, "browser_resize", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.Double, UndertaleExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 10, 0xc, "browser_draw", UndertaleExtensionVarType.Double, "browser_draw", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.Double, UndertaleExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 11, 0xc, "browser_is_initialized", UndertaleExtensionVarType.Double, "browser_is_initialized", UndertaleExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 12, 0xc, "browser_js", UndertaleExtensionVarType.Double, "browser_js", UndertaleExtensionVarType.Double, UndertaleExtensionVarType.String);
Data.Extensions.Add(browserext);

Data.Functions.EnsureDefined("window_device", Data.Strings);
Data.Functions.EnsureDefined("http_get", Data.Strings);
Data.Functions.EnsureDefined("ds_list_size", Data.Strings);
Data.Functions.EnsureDefined("ds_list_find_value", Data.Strings);
Data.Functions.EnsureDefined("show_message", Data.Strings);
Data.Functions.EnsureDefined("show_debug_message", Data.Strings);
Data.Variables.EnsureDefined("my_browser", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_current_song", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_request", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_song_title", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_last_song", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_cache", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).AppendGML(@"
__webextension_set_device(window_device())
my_browser = browser_create("""");
youtube_current_song = """";
youtube_request = noone;
youtube_song_title = """";
youtube_last_song = """";
youtube_cache = ds_map_create();", Data);

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.PostDraw, Data.Strings, Data.Code, Data.CodeLocals).AppendGML(@"
var w = (window_get_width() / 6.5);
var h = ((w / 16) * 9);
browser_resize(my_browser, w, h);
browser_draw(my_browser, 5, 5);
window_set_caption(youtube_current_song);", Data);

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals).AppendGML(@"
draw_set_font(fnt_maintext);
draw_set_color(0xFFFF);
draw_text(((((surface_get_width(application_surface) * global.window_scale) - string_width(youtube_song_title)) / 2) + global.window_yofs), 0, youtube_song_title);", Data);

var MOD_get_mus_query = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_MOD_get_mus_query") };
MOD_get_mus_query.AppendGML(@"
if (argument0 == 213)
    return ""Ghost + Fight"";
if (argument0 == 214)
    return ""Once+Upon+a+Time"";
if (argument0 == 216)
    return ""Fallen+Down"";
if (argument0 == 217)
    return ""Your+Best+Friend"";
if (argument0 == 218)
    return ""Anticipation"";
if (argument0 == 219)
    return ""Unnecessary+Tension"";
if (argument0 == 220)
    return ""Start+Menu"";
if (argument0 == 221)
    return ""Start+Menu"";
if (argument0 == 222)
    return ""Start+Menu"";
if (argument0 == 223)
    return ""Start+Menu"";
if (argument0 == 224)
    return ""Start+Menu"";
if (argument0 == 225)
    return ""Start+Menu"";
if (argument0 == 226)
    return ""Menu+%28Full%29"";
if (argument0 == 227)
    return ""Home"";
if (argument0 == 232)
    return ""Heartache"";
if (argument0 == 233)
    return ""Home+%28Music+Box%29"";
if (argument0 == 234)
    return ""Ruins"";
if (argument0 == 235)
    return ""Enemy+Approaching"";
if (argument0 == 236)
    return ""game+over+theme"";
if (argument0 == 241)
    return ""Dogsong"";
if (argument0 == 242)
    return ""Bonetrousle"";
if (argument0 == 243)
    return ""Shop"";
if (argument0 == 244)
    return ""Snowdin+Town"";
if (argument0 == 251)
    return ""%22Dating+Start%21%22"";
if (argument0 == 252)
    return ""%22Dating+Tense%21%22"";
if (argument0 == 253)
    return ""%22Dating+Fight%21%22"";
if (argument0 == 254)
    return ""Premonition"";
if (argument0 == 255)
    return ""Snowy"";
if (argument0 == 257)
    return ""Nyeh+Heh+Heh%21"";
if (argument0 == 262)
    return ""Bird+That+Carries+You+Over+A+Disproportionately+Small+Gap"";
if (argument0 == 264)
    return ""Dummy%21"";
if (argument0 == 267)
    return ""Thundersnail"";
if (argument0 == 268)
    return ""NGAHHH%21%21"";
if (argument0 == 269)
    return ""She%27s+Playing+Piano"";
if (argument0 == 270)
    return ""Waterfall"";
if (argument0 == 271)
    return ""Quiet+Water"";
if (argument0 == 273)
    return ""Run%21"";
if (argument0 == 276)
    return ""Memory"";
if (argument0 == 279)
    return ""Pathetic+House"";
if (argument0 == 282)
    return ""Chill"";
if (argument0 == 283)
    return ""Spooktune"";
if (argument0 == 284)
    return ""Spookwave"";
if (argument0 == 285)
    return ""Ghouliday"";
if (argument0 == 286)
    return ""Spear+of+Justice"";
if (argument0 == 291)
    return ""Metal+Crusher"";
if (argument0 == 292)
    return ""Hotel"";
if (argument0 == 294)
    return ""Spider+Dance"";
if (argument0 == 295)
    return ""It%27s+Raining+Somewhere+Else"";
if (argument0 == 297)
    return ""Live+Report"";
if (argument0 == 298)
    return ""Death+Report"";
if (argument0 == 299)
    return ""Can+You+Really+Call+This+A+Hotel%2C+I+Didn%27t+Receive+A+Mint+On+My+Pillow+Or+Anything"";
if (argument0 == 300)
    return ""CORE"";
if (argument0 == 301)
    return ""Death+by+Glamour"";
if (argument0 == 304)
    return ""Another+Medium"";
if (argument0 == 312)
    return ""Confession"";
if (argument0 == 313)
    return ""Oh%21+One+True+Love"";
if (argument0 == 314)
    return ""Oh%21+One+True+Love"";
if (argument0 == 315)
    return ""Oh%21+One+True+Love"";
if (argument0 == 316)
    return ""Oh%21+One+True+Love"";
if (argument0 == 338)
    return ""ASGORE"";
if (argument0 == 383)
    return ""Amalgam"";
if (argument0 == 387)
    return ""Temmie+Village"";
if (argument0 == 388)
    return ""Tem+Shop"";
if (argument0 == 390)
    return ""Here+We+Are"";
if (argument0 == 391)
    return ""%22An+Ending%22"";
if (argument0 == 392)
    return ""Battle+Against+a+True+Hero"";
if (argument0 == 393)
    return ""But+the+Earth+Refused+to+Die"";
if (argument0 == 394)
    return ""Power+of+NEO"";
if (argument0 == 395)
    return ""MEGALOVANIA"";
if (argument0 == 404)
    return ""Fallen+Down+%28Reprise%29"";
if (argument0 == 405)
    return ""Don%27t+Give+Up"";
if (argument0 == 408)
    return ""Hopes+and+Dreams"";
if (argument0 == 409)
    return ""SAVE+the+World"";
if (argument0 == 411)
    return ""Reunited"";
if (argument0 == 412)
    return ""Respite"";
if (argument0 == 413)
    return ""Burn+in+Despair%21"";
if (argument0 == 415)
    return ""Wrong+Enemy+%21%3F"";
if (argument0 == 421)
    return ""Uwa%21%21+So+Holiday"";
if (argument0 == 422)
    return ""Uwa%21%21+So+Temperate"";
if (argument0 == 424)
    return ""Uwa%21%21+So+HEATS%21%21%E2%99%AB"";
if (argument0 == 425)
    return ""Stronger+Monsters"";
if (argument0 == 432)
    return ""Bring+It+In%2C+Guys%21"";
if (argument0 == 433)
    return ""Bring+It+In%2C+Guys%21"";
if (argument0 == 434)
    return ""Bring+It+In%2C+Guys%21"";
if (argument0 == 435)
    return ""Bring+It+In%2C+Guys%21"";
if (argument0 == 436)
    return ""Bring+It+In%2C+Guys%21"";
if (argument0 == 437)
    return ""Bring+It+In%2C+Guys%21"";
if (argument0 == 438)
    return ""Bring+It+In%2C+Guys%21"";
if (argument0 == 442)
    return ""Last+Goodbye"";
return """";", Data);

Data.Code.Add(MOD_get_mus_query);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = MOD_get_mus_query.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("MOD_get_mus_query"), Code = MOD_get_mus_query });
Data.Functions.EnsureDefined("MOD_get_mus_query", Data.Strings);

var MOD_get_mus_count = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_MOD_get_mus_count") };
MOD_get_mus_count.AppendGML(@"
if (argument0 == ""game + over + theme"")
    return 5;
else if (argument0 == ""%22Dating+Tense%21%22"")
    return 5;
else if (argument0 == ""Confession"")
    return 5;
else if (argument0 == ""Premonition"")
    return 3;
else if (argument0 == ""Dogsong"")
    return 5;
else if (argument0 == ""Run%21"")
    return 3;
else if (argument0 == ""Respite"")
    return 3;
else if (argument0 == ""%22An+Ending%22"")
    return 5;
else if (argument0 == ""Battle+Against+a+True+Hero"")
    return 50;
else if (argument0 == ""Waterfall"")
    return 50;
else if (argument0 == ""Quiet+Water"")
    return 30;
else if (argument0 == ""Snowy"")
    return 50;
else if (argument0 == ""CORE"")
    return 50;
else if (argument0 == ""Spider+Dance"")
    return 50;
else if (argument0 == ""Memory"")
    return 50;
else if (argument0 == ""Once+Upon+a+Time"")
    return 50;
else if (argument0 == ""Enemy+Approaching"")
    return 50;
else if (argument0 == ""Heatache"")
    return 50;
else if (argument0 == ""ASGORE"")
    return 50;
else if (argument0 == ""MEGALOVANIA"")
    return 50;
else if (argument0 == ""Hopes+and+Dreams"")
    return 50;
else
    return 15;", Data);
Data.Code.Add(MOD_get_mus_count);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = MOD_get_mus_count.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("MOD_get_mus_count"), Code = MOD_get_mus_count });
Data.Functions.EnsureDefined("MOD_get_mus_count", Data.Strings);

var youtube_load_song = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_load_song") };
youtube_load_song.AppendGML(@"
with (obj_time) {
    var items = ds_map_find_value(youtube_cache, youtube_current_song);
    var item = ds_list_find_value(items, irandom((ds_list_size(items) - 1)));
    browser_js(my_browser, ((""change_song('"" + ds_map_find_value(ds_map_find_value(item, ""id""), ""videoId"")) + ""')""));
    youtube_song_title = ((ds_map_find_value(ds_map_find_value(item, ""snippet""), ""channelTitle"") + "" - "") + ds_map_find_value(ds_map_find_value(item, ""snippet""), ""title""));
}", Data);
Data.Code.Add(youtube_load_song);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_load_song.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_load_song"), Code = youtube_load_song });
Data.Functions.EnsureDefined("youtube_load_song", Data.Strings);

var youtube_play = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_play") };
youtube_play.AppendGML(@"
with (obj_time) {
    var song = MOD_get_mus_query(argument0);
    if (song != """") {
        if (song != youtube_current_song) {
            youtube_current_song = song;
            if browser_is_initialized(my_browser) {
                if (youtube_current_song != youtube_last_song) {
                    youtube_last_song = song;
                    browser_js(my_browser, ""change_song(null)"");
                    if is_undefined(ds_map_find_value(youtube_cache, youtube_current_song))
                        youtube_request = http_get(""https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults="" + string(MOD_get_mus_count(youtube_current_song)) + ""&type=video&videoEmbeddable=true&fields=items(id%2FvideoId%2Csnippet(channelId%2CchannelTitle%2Ctitle))&q="" + youtube_current_song + ""+%22undertale+remix%22&key=AIzaSyANCxd-4e8cXdOx99SFiF24j2GF0Nid0Lc"");
                    else
                        youtube_load_song();
                } else
                    browser_js(my_browser, ""resume_song()"");
            }
        }
        return 1337;
    } else {
        this_song_i = audio_play_sound(argument0, argument3, argument4);
        audio_sound_pitch(argument0, argument2);
        audio_sound_gain(argument0, argument1, 0);
        return this_song_i;
    }
}", Data);
Data.Code.Add(youtube_play);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_play.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_play"), Code = youtube_play });
Data.Functions.EnsureDefined("youtube_play", Data.Strings);

var youtube_stop = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_stop") };
youtube_stop.AppendGML(@"
with (obj_time) {
    browser_js(my_browser, ""change_song(null)"");
    youtube_current_song = """";
    youtube_song_title = """";
    youtube_request = noone;
}", Data);
Data.Code.Add(youtube_stop);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_stop.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_stop"), Code = youtube_stop });
Data.Functions.EnsureDefined("youtube_stop", Data.Strings);

var youtube_is_playing = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_is_playing") };
youtube_is_playing.AppendGML(@"
with (obj_time)
    return ((youtube_current_song != """") && (argument0 == youtube_current_song));", Data);

Data.Code.Add(youtube_is_playing);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_is_playing.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_is_playing"), Code = youtube_is_playing });
Data.Functions.EnsureDefined("youtube_is_playing", Data.Strings);

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Other, (uint)62u, Data.Strings, Data.Code, Data.CodeLocals).AppendGML(@"
var data;
if (ds_map_find_value(async_load, ""id"") == youtube_request && ds_map_find_value(async_load, ""status"") == 0) {
    data = json_decode(ds_map_find_value(async_load, ""result""));
    ds_map_add(youtube_cache, youtube_current_song, ds_map_find_value(data, ""items""));
    youtube_request = noone;
    youtube_load_song();
}", Data);

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Other, (uint)70u, Data.Strings, Data.Code, Data.CodeLocals).AppendGML(@"
if (ds_map_find_value(async_load, ""id"") == 103)
    trophy_handle_load_state();
if (ds_map_find_value(async_load, ""id"") == 1337) {
    var type = ds_map_find_value(async_load, ""type"");
    if (type == ""browser_initialized"") {
        show_debug_message(""Browser initialized"");
        browser_load_html(my_browser, ""<style>head, body, div, iframe { width: 100%; height: 100%; margin: 0; background-color: black; }</style><script src='https://www.youtube.com/iframe_api' async='async'></script><script>var current_song; var loaded = false; var player, prev_player; var last_change; var last_song; function change_song(songid) { if (player && current_song == songid) return; if (!current_song && songid != last_song) { last_change = new Date().getTime(); } current_song = songid; if (current_song != null) { last_song = current_song; } if (player) { if (current_song != null) { if (prev_player != null) { document.body.removeChild(player.getIframe()); } else { prev_player = player; prev_player.getIframe().style.display = 'none'; } player = null; } else { document.body.removeChild(player.getIframe()); player = null; if (prev_player != null) { document.body.removeChild(prev_player.getIframe()); prev_player = null; } } } if (loaded && current_song != null) { var container = document.createElement('div'); document.body.appendChild(container); player = new YT.Player(container, { height: '100%', width: '100%', videoId: current_song, events: { 'onReady': onPlayerReady, 'onStateChange': onPlayerStateChange } }); } } function resume_song() { change_song(last_song); } function onYouTubeIframeAPIReady() { loaded = true; if (current_song) change_song(current_song); } function onPlayerReady(event) { var desiredPos = (new Date().getTime() - last_change)/1000 % player.getDuration(); event.target.seekTo(desiredPos, true); event.target.playVideo(); } function onPlayerStateChange(event) { if (event.target == player) { if (player.getDuration() != 0) { var desiredPos = (new Date().getTime() - last_change)/1000 % player.getDuration(); if (Math.abs(desiredPos - player.getCurrentTime()) > 1) player.seekTo(desiredPos, true); } if (event.data == YT.PlayerState.PLAYING) { if (prev_player) { document.body.removeChild(prev_player.getIframe()); prev_player = null; } } if (event.data == YT.PlayerState.PAUSED || event.data == YT.PlayerState.ENDED) { player.playVideo(); } } }</script>"");
    }
}", Data);

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, 32, Data.Strings, Data.Code, Data.CodeLocals).AppendGML(@"
if (youtube_current_song != """")
    youtube_load_song();", Data);

Data.Scripts.ByName("caster_play").Code.ReplaceGML("return youtube_play(argument0, argument1, argument2, 100, false);", Data);
Data.Scripts.ByName("caster_play_l").Code.ReplaceGML("return youtube_play(argument0, argument1, argument2, 100, false);", Data);
Data.Scripts.ByName("caster_loop").Code.ReplaceGML("return youtube_play(argument0, argument1, argument2, 120, true);", Data);

Data.Scripts.ByName("caster_resume").Code.ReplaceGML(@"
if (MOD_get_mus_query(argument0) != """")
    youtube_play(argument0, 0, 0, 0, false);
else
    audio_resume_sound(argument0);", Data);

Data.Scripts.ByName("caster_pause").Code.ReplaceGML(@"
if youtube_is_playing(MOD_get_mus_query(argument0))
    youtube_stop();
else
    audio_pause_sound(argument0);", Data);

Data.Scripts.ByName("caster_free").Code.ReplaceGML(@"
if (argument0 != all) {
    if youtube_is_playing(MOD_get_mus_query(argument0))
        youtube_stop();
    else
        audio_stop_sound(argument0);
} else {
    audio_stop_all();
    youtube_stop();
}", Data);

Data.GameObjects.ByName("obj_titleimage").EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data.Strings, Data.Code, Data.CodeLocals).AppendGML(@"
draw_set_color(0xFFFF);
scr_setfont(fnt_small);
scr_drawtext_centered_scaled(160, 150, ""But Every Time A Song Plays\nIts A Random Remix From YouTube Instead"", 1, 1);
scr_drawtext_centered_scaled(240, 160, ""mod by krzys_h and Kneesnap"", 1, 1);
_temp_local_var_2 = draw_set_color(0x808080)", Data);

ScriptMessage("Finished! Enjoy!");*/