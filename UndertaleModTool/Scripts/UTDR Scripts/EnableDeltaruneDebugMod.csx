EnsureDataLoaded();

ScriptMessage("Enabling debug mode in Chapter 3");

UndertaleModLib.Compiler.CodeImportGroup importGroup = new(Data);

var obj_initializer2 = Data.GameObjects.ByName("obj_initializer2");
if (obj_initializer2 == null)
{
    ScriptError("Could not find obj_initializer2");
    return;
}

var createCode = obj_initializer2.EventHandlerFor(EventType.Create, (uint)0, Data);
if (createCode == null)
{
    ScriptError("Could not find Create event for obj_initializer2");
    return;
}

string newCode = @"
global.is_console = scr_is_switch_os() || os_type == os_ps4 || os_type == os_ps5;
if (!global.is_console)
    window_enable_borderless_fullscreen(true);
global.debug = 1;
var launch_data = scr_init_launch_parameters();
global.launcher = launch_data.is_launcher;
textures_loaded = false;
if (global.is_console)
    texture_set_interpolation(false);A
if (global.launcher)
{
    if (scr_is_switch_os() && !variable_global_exists(""switchlogin""))
    {
        global.switchlogin = launch_data.switch_id;
        
        if (global.switchlogin >= 0)
            switch_save_data_mount(global.switchlogin);
        
        while (global.switchlogin < 0)
            global.switchlogin = switch_accounts_select_account(true, false, false);
        
        if (!switch_accounts_is_user_open(global.switchlogin))
            switch_accounts_open_user(global.switchlogin);
    }
}
else if (scr_is_switch_os() && !variable_global_exists(""switchlogin""))
{
    var account_id = -1;
    
    while (account_id < 0)
        account_id = switch_accounts_select_account(true, false, false);
    
    global.switchlogin = account_id;
    switch_accounts_open_user(global.switchlogin);
}
if (!instance_exists(obj_event_manager))
{
    var event_manager = instance_create(0, 0, obj_event_manager);
    
    with (event_manager)
        init();
    
    if (os_type == os_ps4 || os_type == os_ps5)
    {
        with (event_manager)
            enable_trophies();
    }
}
global.screen_border_id = stringsetloc(""Dynamic"", ""obj_initializer2_slash_Create_0_gml_22_0"");
global.screen_border_active = true;
global.screen_border_alpha = 0;
global.screen_border_state = 0;
global.screen_border_dynamic_fade_id = 0;
global.screen_border_dynamic_fade_level = 0;
global.savedata_async_id = -1;
global.savedata_async_load = false;
global.savedata_error = false;
global.savedata_debuginfo = """";
global.versionno = ""v0.0.086"";
if (scr_is_switch_os())
{
}
if (os_type == os_ps4 || os_type == os_ps5)
{
}
global.game_won = false;
scr_input_manager_init();
if (global.is_console)
{
    if (os_type == os_ps4 || os_type == os_ps5)
        window_set_cursor(cr_none);
    
    ossafe_init();
    ossafe_savedata_load();
}
else
{
    global_flagname_init();
    scr_84_init_localization();
    pal_swap_init_system(18);
    global.damagefont = font_add_sprite_ext(spr_numbersfontbig, ""0123456789"", 20, 0);
    global.damagefontgold = font_add_sprite_ext(spr_numbersfontbig_gold, ""0123456789+-%"", 20, 0);
    global.hpfont = font_add_sprite_ext(spr_numbersfontsmall, ""0123456789-+"", 0, 2);
    
    if (sprite_exists(asset_get_index(""spr_tvlandfont"")))
        global.tvlandfont = font_add_sprite_ext(spr_tvlandfont, ""ABCDEFGHIJKLMNOPQRSTUVWXYZ.?!:…abcdefghijklmnopqrstuvwxyz1234567890"", 0, 1);
    
    scr_gamestart();
    
    for (i = 0; i < 100; i += 1)
        global.tempflag[i] = 0;
    
    global.heartx = 300;
    global.hearty = 220;
    global.swordboardeath = 0;
    scr_load_audio();
    
    if (!instance_exists(obj_time))
        instance_create(0, 0, obj_time);
}
loadtex = -4;
if (global.is_console)
    loadtex = instance_create(0, 0, obj_prefetchtex);
else
    scr_prefetch_textures();
textures_loaded = false;
";

importGroup.QueueReplace(createCode, newCode);

importGroup.Import();
ChangeSelection(createCode);
ScriptMessage("Debug mode is now permanently enabled!");
