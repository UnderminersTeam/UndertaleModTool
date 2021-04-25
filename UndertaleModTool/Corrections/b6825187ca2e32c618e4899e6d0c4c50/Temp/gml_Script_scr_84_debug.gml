var strings;
var process = argument0
if (!variable_global_exists("chemg_menu_depth"))
{
    global.chemg_menu_indices = array_create(0)
    global.chemg_menu_indices[0] = 0
    global.chemg_menu_depth = 0
    global.chemg_god_mode = 0
    global.chemg_show_room = 1
    global.chemg_font_test = 0
    var parent = ds_list_create()
    show_debug_message("init debug")
    var group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "Options")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[loadj]", "", "reload japanese")
    scr_84_add_menu_item(parent, "[lang]", "ja", "use japanese")
    scr_84_add_menu_item(parent, "[lang]", "en", "use english")
    scr_84_add_menu_item(parent, "[showroom]", "", "toggle room name")
    scr_84_add_menu_item(parent, "[restart]", "", "restart room")
    scr_84_add_menu_item(parent, "[god]", "", "god mode")
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "give item")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[item]", 1, "Dark Candy")
    scr_84_add_menu_item(parent, "[item]", 2, "ReviveMint")
    scr_84_add_menu_item(parent, "[item]", 3, "Glowshard")
    scr_84_add_menu_item(parent, "[item]", 4, "Manual")
    scr_84_add_menu_item(parent, "[item]", 5, "BrokenCake")
    scr_84_add_menu_item(parent, "[item]", 6, "Top Cake")
    scr_84_add_menu_item(parent, "[item]", 7, "SpinCake")
    scr_84_add_menu_item(parent, "[item]", 8, "Darkburger")
    scr_84_add_menu_item(parent, "[item]", 9, "LancerCookie")
    scr_84_add_menu_item(parent, "[item]", 10, "GigaSalad")
    scr_84_add_menu_item(parent, "[item]", 11, "Clubswich")
    scr_84_add_menu_item(parent, "[item]", 12, "HeartsDonut")
    scr_84_add_menu_item(parent, "[item]", 13, "ChocDiamond")
    scr_84_add_menu_item(parent, "[item]", 14, "FavSandwich")
    scr_84_add_menu_item(parent, "[item]", 15, "RouxlsRoux")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "give light item")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[lightitem]", 1, "Dark Candy")
    scr_84_add_menu_item(parent, "[lightitem]", 2, "ReviveMint")
    scr_84_add_menu_item(parent, "[lightitem]", 3, "Glowshard")
    scr_84_add_menu_item(parent, "[lightitem]", 4, "Manual")
    scr_84_add_menu_item(parent, "[lightitem]", 5, "BrokenCake")
    scr_84_add_menu_item(parent, "[lightitem]", 6, "Top Cake")
    scr_84_add_menu_item(parent, "[lightitem]", 7, "SpinCake")
    scr_84_add_menu_item(parent, "[lightitem]", 8, "Darkburger")
    scr_84_add_menu_item(parent, "[lightitem]", 9, "LancerCookie")
    scr_84_add_menu_item(parent, "[lightitem]", 10, "GigaSalad")
    scr_84_add_menu_item(parent, "[lightitem]", 11, "Clubswich")
    scr_84_add_menu_item(parent, "[lightitem]", 12, "HeartsDonut")
    scr_84_add_menu_item(parent, "[lightitem]", 13, "ChocDiamond")
    scr_84_add_menu_item(parent, "[lightitem]", 14, "FavSandwich")
    scr_84_add_menu_item(parent, "[lightitem]", 15, "RouxlsRoux")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "give key item")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[keyitem]", 1, "Cell Phone")
    scr_84_add_menu_item(parent, "[keyitem]", 2, "Egg")
    scr_84_add_menu_item(parent, "[keyitem]", 3, "BrokenCake")
    scr_84_add_menu_item(parent, "[keyitem]", 4, "Broken Key A")
    scr_84_add_menu_item(parent, "[keyitem]", 5, "Door Key")
    scr_84_add_menu_item(parent, "[keyitem]", 6, "Broken Key B")
    scr_84_add_menu_item(parent, "[keyitem]", 7, "Broken Key C")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "give weapon")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[weaponitem]", 1, "Wood Blade")
    scr_84_add_menu_item(parent, "[weaponitem]", 2, "Mane Ax")
    scr_84_add_menu_item(parent, "[weaponitem]", 3, "Red Scarf")
    scr_84_add_menu_item(parent, "[weaponitem]", 4, "EverybodyWeapon")
    scr_84_add_menu_item(parent, "[weaponitem]", 5, "Spookysword")
    scr_84_add_menu_item(parent, "[weaponitem]", 6, "Brave Ax")
    scr_84_add_menu_item(parent, "[weaponitem]", 7, "Devilsknife")
    scr_84_add_menu_item(parent, "[weaponitem]", 8, "Trefoil")
    scr_84_add_menu_item(parent, "[weaponitem]", 9, "Ragger")
    scr_84_add_menu_item(parent, "[weaponitem]", 10, "DaintyScarf")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "give armor")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[armoritem]", 1, "Amber Card")
    scr_84_add_menu_item(parent, "[armoritem]", 2, "Dice Brace")
    scr_84_add_menu_item(parent, "[armoritem]", 3, "Pink Ribbon")
    scr_84_add_menu_item(parent, "[armoritem]", 4, "White Ribbon")
    scr_84_add_menu_item(parent, "[armoritem]", 5, "IronShackle")
    scr_84_add_menu_item(parent, "[armoritem]", 6, "MouseToken")
    scr_84_add_menu_item(parent, "[armoritem]", 7, "Jevilstail")
    parent = scr_84_pop()
    scr_84_add_menu_item(parent, "[phone]", "", "give phone number")
    scr_84_add_menu_item(parent, "[gold]", 25, "+25 gold")
    scr_84_add_menu_item(parent, "[gold]", -25, "-25 gold")
    scr_84_add_menu_item(parent, "[lightgold]", 25, "+25 gold")
    scr_84_add_menu_item(parent, "[lightgold]", -25, "-25 gold")
    scr_84_add_menu_item(parent, "[fonttest]", "", "font test")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "Rooms")
    scr_84_push(parent)
    parent = group
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "special")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 131, "PLACE_DOG")
    scr_84_add_menu_item(parent, "[room]", 132, "room_legend")
    scr_84_add_menu_item(parent, "[room]", 133, "room_shop1")
    scr_84_add_menu_item(parent, "[room]", 134, "room_shop2")
    scr_84_add_menu_item(parent, "[room]", 135, "room_gameover")
    scr_84_add_menu_item(parent, "[room]", 136, "room_myroom_dark")
    scr_84_add_menu_item(parent, "[room]", 137, "PLACE_LOGO")
    scr_84_add_menu_item(parent, "[room]", 138, "PLACE_FAILURE")
    scr_84_add_menu_item(parent, "[room]", 139, "PLACE_MENU")
    scr_84_add_menu_item(parent, "[room]", 140, "room_ed")
    scr_84_add_menu_item(parent, "[room]", 145, "room_splashscreen")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "lightworld_exterior")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 7, "room_town_krisyard")
    scr_84_add_menu_item(parent, "[room]", 8, "room_town_northwest")
    scr_84_add_menu_item(parent, "[room]", 9, "room_town_north")
    scr_84_add_menu_item(parent, "[room]", 10, "room_beach")
    scr_84_add_menu_item(parent, "[room]", 11, "room_town_mid")
    scr_84_add_menu_item(parent, "[room]", 12, "room_town_apartments")
    scr_84_add_menu_item(parent, "[room]", 13, "room_town_south")
    scr_84_add_menu_item(parent, "[room]", 14, "room_town_school")
    scr_84_add_menu_item(parent, "[room]", 15, "room_town_church")
    scr_84_add_menu_item(parent, "[room]", 16, "room_graveyard")
    scr_84_add_menu_item(parent, "[room]", 17, "room_town_shelter")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "cardcastle")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 105, "room_cc_prison_cells")
    scr_84_add_menu_item(parent, "[roomdark]", 106, "room_cc_prisonlancer")
    scr_84_add_menu_item(parent, "[roomdark]", 107, "room_cc_prison_to_elevator")
    scr_84_add_menu_item(parent, "[roomdark]", 108, "room_cc_prison2")
    scr_84_add_menu_item(parent, "[roomdark]", 109, "room_cc_prisonelevator")
    scr_84_add_menu_item(parent, "[roomdark]", 110, "room_cc_elevator")
    scr_84_add_menu_item(parent, "[roomdark]", 111, "room_cc_prison_prejoker")
    scr_84_add_menu_item(parent, "[roomdark]", 112, "room_cc_joker")
    scr_84_add_menu_item(parent, "[roomdark]", 113, "room_cc_entrance")
    scr_84_add_menu_item(parent, "[roomdark]", 114, "room_cc_1f")
    scr_84_add_menu_item(parent, "[roomdark]", 115, "room_cc_rudinn")
    scr_84_add_menu_item(parent, "[roomdark]", 116, "room_cc_2f")
    scr_84_add_menu_item(parent, "[roomdark]", 117, "room_cc_rurus1")
    scr_84_add_menu_item(parent, "[roomdark]", 118, "room_cc_3f")
    scr_84_add_menu_item(parent, "[roomdark]", 119, "room_cc_hathy")
    scr_84_add_menu_item(parent, "[roomdark]", 120, "room_cc_4f")
    scr_84_add_menu_item(parent, "[roomdark]", 121, "room_cc_rurus2")
    scr_84_add_menu_item(parent, "[roomdark]", 122, "room_cc_clover")
    scr_84_add_menu_item(parent, "[roomdark]", 123, "room_cc_5f")
    scr_84_add_menu_item(parent, "[roomdark]", 124, "room_cc_lancer")
    scr_84_add_menu_item(parent, "[roomdark]", 125, "room_cc_6f")
    scr_84_add_menu_item(parent, "[roomdark]", 126, "room_cc_throneroom")
    scr_84_add_menu_item(parent, "[roomdark]", 127, "room_cc_preroof")
    scr_84_add_menu_item(parent, "[roomdark]", 128, "room_cc_kingbattle")
    scr_84_add_menu_item(parent, "[roomdark]", 129, "room_cc_prefountain")
    scr_84_add_menu_item(parent, "[roomdark]", 130, "room_cc_fountain")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "ralsei_castle")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 44, "room_castle_outskirts")
    scr_84_add_menu_item(parent, "[roomdark]", 45, "room_castle_town")
    scr_84_add_menu_item(parent, "[roomdark]", 46, "room_castle_front")
    scr_84_add_menu_item(parent, "[roomdark]", 47, "room_castle_tutorial")
    scr_84_add_menu_item(parent, "[roomdark]", 48, "room_castle_darkdoor")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "dark forest")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 73, "room_forest_savepoint1")
    scr_84_add_menu_item(parent, "[roomdark]", 74, "room_forest_area0")
    scr_84_add_menu_item(parent, "[roomdark]", 75, "room_forest_area1")
    scr_84_add_menu_item(parent, "[roomdark]", 76, "room_forest_area2")
    scr_84_add_menu_item(parent, "[roomdark]", 77, "room_forest_area2A")
    scr_84_add_menu_item(parent, "[roomdark]", 78, "room_forest_puzzle1")
    scr_84_add_menu_item(parent, "[roomdark]", 79, "room_forest_beforeclover")
    scr_84_add_menu_item(parent, "[roomdark]", 80, "room_forest_area3A")
    scr_84_add_menu_item(parent, "[roomdark]", 81, "room_forest_area3")
    scr_84_add_menu_item(parent, "[roomdark]", 82, "room_forest_savepoint2")
    scr_84_add_menu_item(parent, "[roomdark]", 83, "room_forest_smith")
    scr_84_add_menu_item(parent, "[roomdark]", 84, "room_forest_area4")
    scr_84_add_menu_item(parent, "[roomdark]", 85, "room_forest_dancers1")
    scr_84_add_menu_item(parent, "[roomdark]", 86, "room_forest_secret1")
    scr_84_add_menu_item(parent, "[roomdark]", 87, "room_forest_thrashmaker")
    scr_84_add_menu_item(parent, "[roomdark]", 88, "room_forest_starwalker")
    scr_84_add_menu_item(parent, "[roomdark]", 89, "room_forest_area5")
    scr_84_add_menu_item(parent, "[roomdark]", 90, "room_forest_savepoint_relax")
    scr_84_add_menu_item(parent, "[roomdark]", 91, "room_forest_maze1")
    scr_84_add_menu_item(parent, "[roomdark]", 92, "room_forest_maze_deadend")
    scr_84_add_menu_item(parent, "[roomdark]", 93, "room_forest_maze_susie")
    scr_84_add_menu_item(parent, "[roomdark]", 94, "room_forest_maze2")
    scr_84_add_menu_item(parent, "[roomdark]", 95, "room_forest_maze_deadend2")
    scr_84_add_menu_item(parent, "[roomdark]", 96, "room_forest_savepoint3")
    scr_84_add_menu_item(parent, "[roomdark]", 97, "room_forest_fightsusie")
    scr_84_add_menu_item(parent, "[roomdark]", 98, "room_forest_afterthrash2")
    scr_84_add_menu_item(parent, "[roomdark]", 99, "room_forest_afterthrash3")
    scr_84_add_menu_item(parent, "[roomdark]", 100, "room_forest_afterthrash4")
    scr_84_add_menu_item(parent, "[roomdark]", 101, "room_forest_castleview")
    scr_84_add_menu_item(parent, "[roomdark]", 102, "room_forest_chase1")
    scr_84_add_menu_item(parent, "[roomdark]", 103, "room_forest_chase2")
    scr_84_add_menu_item(parent, "[roomdark]", 104, "room_forest_castlefront")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "school")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 28, "room_torielclass")
    scr_84_add_menu_item(parent, "[room]", 29, "room_schoollobby")
    scr_84_add_menu_item(parent, "[room]", 30, "room_alphysclass")
    scr_84_add_menu_item(parent, "[room]", 31, "room_schooldoor")
    scr_84_add_menu_item(parent, "[room]", 32, "room_insidecloset")
    scr_84_add_menu_item(parent, "[room]", 33, "room_school_unusedroom")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "myhouse")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 2, "room_krisroom")
    scr_84_add_menu_item(parent, "[room]", 3, "room_krishallway")
    scr_84_add_menu_item(parent, "[room]", 4, "room_torroom")
    scr_84_add_menu_item(parent, "[room]", 5, "room_torhouse")
    scr_84_add_menu_item(parent, "[room]", 6, "room_torbathroom")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "dark_start")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 34, "room_dark1")
    scr_84_add_menu_item(parent, "[roomdark]", 35, "room_dark1a")
    scr_84_add_menu_item(parent, "[roomdark]", 36, "room_dark2")
    scr_84_add_menu_item(parent, "[roomdark]", 37, "room_dark3")
    scr_84_add_menu_item(parent, "[roomdark]", 38, "room_dark3a")
    scr_84_add_menu_item(parent, "[roomdark]", 39, "room_dark_wobbles")
    scr_84_add_menu_item(parent, "[roomdark]", 40, "room_dark_eyepuzzle")
    scr_84_add_menu_item(parent, "[roomdark]", 41, "room_dark7")
    scr_84_add_menu_item(parent, "[roomdark]", 42, "room_dark_chase1")
    scr_84_add_menu_item(parent, "[roomdark]", 43, "room_dark_chase2")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "rooms")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 0, "ROOM_INITIALIZE")
    scr_84_add_menu_item(parent, "[room]", 1, "PLACE_CONTACT")
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "myhouse")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 2, "room_krisroom")
    scr_84_add_menu_item(parent, "[room]", 3, "room_krishallway")
    scr_84_add_menu_item(parent, "[room]", 4, "room_torroom")
    scr_84_add_menu_item(parent, "[room]", 5, "room_torhouse")
    scr_84_add_menu_item(parent, "[room]", 6, "room_torbathroom")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "lightworld_exterior")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 7, "room_town_krisyard")
    scr_84_add_menu_item(parent, "[room]", 8, "room_town_northwest")
    scr_84_add_menu_item(parent, "[room]", 9, "room_town_north")
    scr_84_add_menu_item(parent, "[room]", 10, "room_beach")
    scr_84_add_menu_item(parent, "[room]", 11, "room_town_mid")
    scr_84_add_menu_item(parent, "[room]", 12, "room_town_apartments")
    scr_84_add_menu_item(parent, "[room]", 13, "room_town_south")
    scr_84_add_menu_item(parent, "[room]", 14, "room_town_school")
    scr_84_add_menu_item(parent, "[room]", 15, "room_town_church")
    scr_84_add_menu_item(parent, "[room]", 16, "room_graveyard")
    scr_84_add_menu_item(parent, "[room]", 17, "room_town_shelter")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "lightworld_interior")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 18, "room_hospital_lobby")
    scr_84_add_menu_item(parent, "[room]", 19, "room_hospital_hallway")
    scr_84_add_menu_item(parent, "[room]", 20, "room_hospital_rudy")
    scr_84_add_menu_item(parent, "[room]", 21, "room_hospital_room2")
    scr_84_add_menu_item(parent, "[room]", 22, "room_diner")
    scr_84_add_menu_item(parent, "[room]", 23, "room_townhall")
    scr_84_add_menu_item(parent, "[room]", 24, "room_flowershop_1f")
    scr_84_add_menu_item(parent, "[room]", 25, "room_flowershop_2f")
    scr_84_add_menu_item(parent, "[room]", 26, "room_library")
    scr_84_add_menu_item(parent, "[room]", 27, "room_alphysalley")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "school")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 28, "room_torielclass")
    scr_84_add_menu_item(parent, "[room]", 29, "room_schoollobby")
    scr_84_add_menu_item(parent, "[room]", 30, "room_alphysclass")
    scr_84_add_menu_item(parent, "[room]", 31, "room_schooldoor")
    scr_84_add_menu_item(parent, "[room]", 32, "room_insidecloset")
    scr_84_add_menu_item(parent, "[room]", 33, "room_school_unusedroom")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "dark_start")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 34, "room_dark1")
    scr_84_add_menu_item(parent, "[roomdark]", 35, "room_dark1a")
    scr_84_add_menu_item(parent, "[roomdark]", 36, "room_dark2")
    scr_84_add_menu_item(parent, "[roomdark]", 37, "room_dark3")
    scr_84_add_menu_item(parent, "[roomdark]", 38, "room_dark3a")
    scr_84_add_menu_item(parent, "[roomdark]", 39, "room_dark_wobbles")
    scr_84_add_menu_item(parent, "[roomdark]", 40, "room_dark_eyepuzzle")
    scr_84_add_menu_item(parent, "[roomdark]", 41, "room_dark7")
    scr_84_add_menu_item(parent, "[roomdark]", 42, "room_dark_chase1")
    scr_84_add_menu_item(parent, "[roomdark]", 43, "room_dark_chase2")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "ralsei_castle")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 44, "room_castle_outskirts")
    scr_84_add_menu_item(parent, "[roomdark]", 45, "room_castle_town")
    scr_84_add_menu_item(parent, "[roomdark]", 46, "room_castle_front")
    scr_84_add_menu_item(parent, "[roomdark]", 47, "room_castle_tutorial")
    scr_84_add_menu_item(parent, "[roomdark]", 48, "room_castle_darkdoor")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "dark field")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 49, "room_field_start")
    scr_84_add_menu_item(parent, "[roomdark]", 50, "room_field_forest")
    scr_84_add_menu_item(parent, "[roomdark]", 51, "room_field1")
    scr_84_add_menu_item(parent, "[roomdark]", 52, "room_field2")
    scr_84_add_menu_item(parent, "[roomdark]", 53, "room_field2A")
    scr_84_add_menu_item(parent, "[roomdark]", 54, "room_field_topchef")
    scr_84_add_menu_item(parent, "[roomdark]", 55, "room_field_puzzle1")
    scr_84_add_menu_item(parent, "[roomdark]", 56, "room_field_maze")
    scr_84_add_menu_item(parent, "[roomdark]", 57, "room_field_puzzle2")
    scr_84_add_menu_item(parent, "[roomdark]", 58, "room_field_getsusie")
    scr_84_add_menu_item(parent, "[roomdark]", 59, "room_field_shop1")
    scr_84_add_menu_item(parent, "[roomdark]", 60, "room_field_puzzletutorial")
    scr_84_add_menu_item(parent, "[roomdark]", 61, "room_field3")
    scr_84_add_menu_item(parent, "[roomdark]", 62, "room_field_boxpuzzle")
    scr_84_add_menu_item(parent, "[roomdark]", 63, "room_field4")
    scr_84_add_menu_item(parent, "[roomdark]", 64, "room_field_secret1")
    scr_84_add_menu_item(parent, "[roomdark]", 65, "room_field_checkers4")
    scr_84_add_menu_item(parent, "[roomdark]", 66, "room_field_checkers2")
    scr_84_add_menu_item(parent, "[roomdark]", 67, "room_field_checkers6")
    scr_84_add_menu_item(parent, "[roomdark]", 68, "room_field_checkers3")
    scr_84_add_menu_item(parent, "[roomdark]", 69, "room_field_checkers1")
    scr_84_add_menu_item(parent, "[roomdark]", 70, "room_field_checkers5")
    scr_84_add_menu_item(parent, "[roomdark]", 71, "room_field_checkers7")
    scr_84_add_menu_item(parent, "[roomdark]", 72, "room_field_checkersboss")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "dark forest")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 73, "room_forest_savepoint1")
    scr_84_add_menu_item(parent, "[roomdark]", 74, "room_forest_area0")
    scr_84_add_menu_item(parent, "[roomdark]", 75, "room_forest_area1")
    scr_84_add_menu_item(parent, "[roomdark]", 76, "room_forest_area2")
    scr_84_add_menu_item(parent, "[roomdark]", 77, "room_forest_area2A")
    scr_84_add_menu_item(parent, "[roomdark]", 78, "room_forest_puzzle1")
    scr_84_add_menu_item(parent, "[roomdark]", 79, "room_forest_beforeclover")
    scr_84_add_menu_item(parent, "[roomdark]", 80, "room_forest_area3A")
    scr_84_add_menu_item(parent, "[roomdark]", 81, "room_forest_area3")
    scr_84_add_menu_item(parent, "[roomdark]", 82, "room_forest_savepoint2")
    scr_84_add_menu_item(parent, "[roomdark]", 83, "room_forest_smith")
    scr_84_add_menu_item(parent, "[roomdark]", 84, "room_forest_area4")
    scr_84_add_menu_item(parent, "[roomdark]", 85, "room_forest_dancers1")
    scr_84_add_menu_item(parent, "[roomdark]", 86, "room_forest_secret1")
    scr_84_add_menu_item(parent, "[roomdark]", 87, "room_forest_thrashmaker")
    scr_84_add_menu_item(parent, "[roomdark]", 88, "room_forest_starwalker")
    scr_84_add_menu_item(parent, "[roomdark]", 89, "room_forest_area5")
    scr_84_add_menu_item(parent, "[roomdark]", 90, "room_forest_savepoint_relax")
    scr_84_add_menu_item(parent, "[roomdark]", 91, "room_forest_maze1")
    scr_84_add_menu_item(parent, "[roomdark]", 92, "room_forest_maze_deadend")
    scr_84_add_menu_item(parent, "[roomdark]", 93, "room_forest_maze_susie")
    scr_84_add_menu_item(parent, "[roomdark]", 94, "room_forest_maze2")
    scr_84_add_menu_item(parent, "[roomdark]", 95, "room_forest_maze_deadend2")
    scr_84_add_menu_item(parent, "[roomdark]", 96, "room_forest_savepoint3")
    scr_84_add_menu_item(parent, "[roomdark]", 97, "room_forest_fightsusie")
    scr_84_add_menu_item(parent, "[roomdark]", 98, "room_forest_afterthrash2")
    scr_84_add_menu_item(parent, "[roomdark]", 99, "room_forest_afterthrash3")
    scr_84_add_menu_item(parent, "[roomdark]", 100, "room_forest_afterthrash4")
    scr_84_add_menu_item(parent, "[roomdark]", 101, "room_forest_castleview")
    scr_84_add_menu_item(parent, "[roomdark]", 102, "room_forest_chase1")
    scr_84_add_menu_item(parent, "[roomdark]", 103, "room_forest_chase2")
    scr_84_add_menu_item(parent, "[roomdark]", 104, "room_forest_castlefront")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "cardcastle")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 105, "room_cc_prison_cells")
    scr_84_add_menu_item(parent, "[roomdark]", 106, "room_cc_prisonlancer")
    scr_84_add_menu_item(parent, "[roomdark]", 107, "room_cc_prison_to_elevator")
    scr_84_add_menu_item(parent, "[roomdark]", 108, "room_cc_prison2")
    scr_84_add_menu_item(parent, "[roomdark]", 109, "room_cc_prisonelevator")
    scr_84_add_menu_item(parent, "[roomdark]", 110, "room_cc_elevator")
    scr_84_add_menu_item(parent, "[roomdark]", 111, "room_cc_prison_prejoker")
    scr_84_add_menu_item(parent, "[roomdark]", 112, "room_cc_joker")
    scr_84_add_menu_item(parent, "[roomdark]", 113, "room_cc_entrance")
    scr_84_add_menu_item(parent, "[roomdark]", 114, "room_cc_1f")
    scr_84_add_menu_item(parent, "[roomdark]", 115, "room_cc_rudinn")
    scr_84_add_menu_item(parent, "[roomdark]", 116, "room_cc_2f")
    scr_84_add_menu_item(parent, "[roomdark]", 117, "room_cc_rurus1")
    scr_84_add_menu_item(parent, "[roomdark]", 118, "room_cc_3f")
    scr_84_add_menu_item(parent, "[roomdark]", 119, "room_cc_hathy")
    scr_84_add_menu_item(parent, "[roomdark]", 120, "room_cc_4f")
    scr_84_add_menu_item(parent, "[roomdark]", 121, "room_cc_rurus2")
    scr_84_add_menu_item(parent, "[roomdark]", 122, "room_cc_clover")
    scr_84_add_menu_item(parent, "[roomdark]", 123, "room_cc_5f")
    scr_84_add_menu_item(parent, "[roomdark]", 124, "room_cc_lancer")
    scr_84_add_menu_item(parent, "[roomdark]", 125, "room_cc_6f")
    scr_84_add_menu_item(parent, "[roomdark]", 126, "room_cc_throneroom")
    scr_84_add_menu_item(parent, "[roomdark]", 127, "room_cc_preroof")
    scr_84_add_menu_item(parent, "[roomdark]", 128, "room_cc_kingbattle")
    scr_84_add_menu_item(parent, "[roomdark]", 129, "room_cc_prefountain")
    scr_84_add_menu_item(parent, "[roomdark]", 130, "room_cc_fountain")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "special")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 131, "PLACE_DOG")
    scr_84_add_menu_item(parent, "[room]", 132, "room_legend")
    scr_84_add_menu_item(parent, "[room]", 133, "room_shop1")
    scr_84_add_menu_item(parent, "[room]", 134, "room_shop2")
    scr_84_add_menu_item(parent, "[room]", 135, "room_gameover")
    scr_84_add_menu_item(parent, "[room]", 136, "room_myroom_dark")
    scr_84_add_menu_item(parent, "[room]", 137, "PLACE_LOGO")
    scr_84_add_menu_item(parent, "[room]", 138, "PLACE_FAILURE")
    scr_84_add_menu_item(parent, "[room]", 139, "PLACE_MENU")
    scr_84_add_menu_item(parent, "[room]", 140, "room_ed")
    parent = scr_84_pop()
    scr_84_add_menu_item(parent, "[room]", 141, "room_empty")
    scr_84_add_menu_item(parent, "[room]", 142, "room_man")
    scr_84_add_menu_item(parent, "[roomdark]", 143, "room_DARKempty")
    scr_84_add_menu_item(parent, "[room]", 144, "room_battletest")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "dark field")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[roomdark]", 49, "room_field_start")
    scr_84_add_menu_item(parent, "[roomdark]", 50, "room_field_forest")
    scr_84_add_menu_item(parent, "[roomdark]", 51, "room_field1")
    scr_84_add_menu_item(parent, "[roomdark]", 52, "room_field2")
    scr_84_add_menu_item(parent, "[roomdark]", 53, "room_field2A")
    scr_84_add_menu_item(parent, "[roomdark]", 54, "room_field_topchef")
    scr_84_add_menu_item(parent, "[roomdark]", 55, "room_field_puzzle1")
    scr_84_add_menu_item(parent, "[roomdark]", 56, "room_field_maze")
    scr_84_add_menu_item(parent, "[roomdark]", 57, "room_field_puzzle2")
    scr_84_add_menu_item(parent, "[roomdark]", 58, "room_field_getsusie")
    scr_84_add_menu_item(parent, "[roomdark]", 59, "room_field_shop1")
    scr_84_add_menu_item(parent, "[roomdark]", 60, "room_field_puzzletutorial")
    scr_84_add_menu_item(parent, "[roomdark]", 61, "room_field3")
    scr_84_add_menu_item(parent, "[roomdark]", 62, "room_field_boxpuzzle")
    scr_84_add_menu_item(parent, "[roomdark]", 63, "room_field4")
    scr_84_add_menu_item(parent, "[roomdark]", 64, "room_field_secret1")
    scr_84_add_menu_item(parent, "[roomdark]", 65, "room_field_checkers4")
    scr_84_add_menu_item(parent, "[roomdark]", 66, "room_field_checkers2")
    scr_84_add_menu_item(parent, "[roomdark]", 67, "room_field_checkers6")
    scr_84_add_menu_item(parent, "[roomdark]", 68, "room_field_checkers3")
    scr_84_add_menu_item(parent, "[roomdark]", 69, "room_field_checkers1")
    scr_84_add_menu_item(parent, "[roomdark]", 70, "room_field_checkers5")
    scr_84_add_menu_item(parent, "[roomdark]", 71, "room_field_checkers7")
    scr_84_add_menu_item(parent, "[roomdark]", 72, "room_field_checkersboss")
    parent = scr_84_pop()
    group = ds_list_create()
    scr_84_add_menu_item(parent, "[group]", group, "lightworld_interior")
    scr_84_push(parent)
    parent = group
    scr_84_add_menu_item(parent, "[room]", 18, "room_hospital_lobby")
    scr_84_add_menu_item(parent, "[room]", 19, "room_hospital_hallway")
    scr_84_add_menu_item(parent, "[room]", 20, "room_hospital_rudy")
    scr_84_add_menu_item(parent, "[room]", 21, "room_hospital_room2")
    scr_84_add_menu_item(parent, "[room]", 22, "room_diner")
    scr_84_add_menu_item(parent, "[room]", 23, "room_townhall")
    scr_84_add_menu_item(parent, "[room]", 24, "room_flowershop_1f")
    scr_84_add_menu_item(parent, "[room]", 25, "room_flowershop_2f")
    scr_84_add_menu_item(parent, "[room]", 26, "room_library")
    scr_84_add_menu_item(parent, "[room]", 27, "room_alphysalley")
    parent = scr_84_pop()
    parent = scr_84_pop()
    global.chemg_menus = parent
}
if process
    return global.chemg_menu_depth > 0;
if gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gp_stickr)
{
    global.chemg_menu_depth = 1
    global.chemg_interact = global.interact
    global.chemg_yoffset = 0
    global.interact = 0
}
if (global.chemg_menu_depth > 0)
{
    parent = global.chemg_menus
    var change = 0
    var depth_ndx = (global.chemg_menu_depth - 1)
    var i = 0
    do
    {
        var choice_ndx = global.chemg_menu_indices[i]
        var choice = ds_list_find_value(parent, choice_ndx * 3)
        var choice_data = ds_list_find_value(parent, choice_ndx * 3 + 1)
        var choice_name = ds_list_find_value(parent, choice_ndx * 3 + 2)
        i += 1
        if i == global.chemg_menu_depth
        {
            break;
        }
        parent = choice_data
    }
    until false;
    var num_choices = (ds_list_size(parent) / 3)
    if (keyboard_check_pressed(vk_up) || gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gp_padu))
        change = -1
    else if (keyboard_check_pressed(vk_down) || gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gp_padd))
        change = 1
    else if (keyboard_check_pressed(vk_return) || gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gp_face2))
    {
        if (choice == "[group]")
        {
            global.chemg_menu_indices[global.chemg_menu_depth] = 0
            global.chemg_menu_depth += 1
        }
        else if (choice == "[loadj]")
        {
            var type = scr_84_lang_load()
            show_debug_message((("loaded " + type) + " lang file"))
            show_message((("loaded " + type) + " lang file"))
            global.chemg_menu_depth = 0
        }
        else if (choice == "[room]")
        {
            room_goto(choice_data)
            global.chemg_menu_depth = 0
        }
        else if (choice == "[roomdark]")
        {
            show_debug_message(("room_goto: " + choice_name))
            room_goto(choice_data)
            global.chemg_menu_depth = 0
        }
        else if (choice == "[lang]")
        {
            show_debug_message(("switch lang: " + choice_data))
            global.lang = choice_data
            global.chemg_menu_depth = 0
            scr_84_init_localization()
            room_restart()
        }
        else if (choice == "[restart]")
        {
            show_debug_message("restart room")
            room_restart()
            global.chemg_menu_depth = 0
        }
        else if (choice == "[god]")
            global.chemg_god_mode = (1 - global.chemg_god_mode)
        else if (choice == "[lightitem]")
        {
            if (scr_litemcheck(choice_data) == 0)
                scr_litemget(choice_data)
        }
        else if (choice == "[item]")
        {
            if (scr_itemcheck(choice_data) == 0)
                scr_itemget(choice_data)
        }
        else if (choice == "[keyitem]")
        {
            if (scr_keyitemcheck(choice_data) == 0)
                scr_keyitemget(choice_data)
        }
        else if (choice == "[weaponitem]")
        {
            if (scr_weaponcheck_inventory(choice_data) == 0)
                scr_weaponget(choice_data)
        }
        else if (choice == "[armoritem]")
        {
            if (scr_armorcheck_inventory(choice_data) == 0)
                scr_armorget(choice_data)
        }
        else if (choice == "[phone]")
            scr_phoneadd(202)
        else if (choice == "[gold]")
            global.gold = max(0, (global.gold + choice_data))
        else if (choice == "[lightgold]")
            global.lgold = max(0, (global.lgold + choice_data))
        else if (choice == "[showroom]")
            global.chemg_show_room = (!global.chemg_show_room)
        else if (choice == "[fonttest]")
        {
            global.chemg_font_test = (!global.chemg_font_test)
            global.chemg_menu_depth = 0
        }
        else
            show_debug_message(("unknown menu cmd:" + choice))
    }
    else if (keyboard_check_pressed(vk_escape) || gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gp_face1))
    {
        if (global.chemg_menu_depth > 0)
            global.chemg_menu_depth -= 1
        if (global.chemg_menu_depth == 0)
            global.interact = global.chemg_interact
    }
    for (i = 0; i < 10; i += 1)
    {
        global.input_pressed[i] = 0
        global.input_held[i] = 0
        global.input_released[i] = 0
    }
    keyboard_clear(vk_up)
    keyboard_clear(vk_down)
    keyboard_clear(vk_left)
    keyboard_clear(vk_right)
    keyboard_clear(vk_escape)
    keyboard_clear(vk_return)
    if (change != 0)
        global.chemg_menu_indices[depth_ndx] = (((global.chemg_menu_indices[depth_ndx] + num_choices) + change) % num_choices)
    draw_set_font(fnt_main)
    draw_set_colour(c_white)
    draw_set_halign(fa_left)
    draw_set_valign(fa_top)
    var yy = global.chemg_yoffset
    var vspacing = 15
    scr_84_draw_text_outline(10, yy, (((("====[ menu ]====[ gold: " + string(global.gold)) + " light gold: ") + string(global.lgold)) + " ]"))
    yy += vspacing
    global.chemg_max_depth = -1
    global.chemg_cursor_y = 0
    scr_84_draw_menu(global.chemg_menus, 10, yy, vspacing, global.chemg_menu_indices, 0, (global.chemg_menu_depth - 1))
    global.chemg_yoffset = min(10, (400 - (global.chemg_cursor_y - global.chemg_yoffset)))
}
var chemg_room_name = room_get_name(room)
if (global.chemg_god_mode > 0)
    chemg_room_name = (chemg_room_name + "[god]")
if global.chemg_show_room
{
    draw_set_font(fnt_main)
    draw_set_colour(c_white)
    var chemg_x = (635 - string_width(chemg_room_name))
    var chemg_y = 5
    scr_84_draw_text_outline(chemg_x, chemg_y, chemg_room_name)
}
if global.chemg_font_test
{
    if (!variable_global_exists("chemg_font_init"))
    {
        global.chemg_font_init = 1
        var ft = fnt_tinynoelle
        ft[array_length_1d(ft)] = fnt_dotumche
        ft[array_length_1d(ft)] = "NORMAL FONT"
        ft[array_length_1d(ft)] = fnt_mainbig
        ft[array_length_1d(ft)] = "SLOWER, SILENT"
        ft[array_length_1d(ft)] = fnt_main
        ft[array_length_1d(ft)] = "normal enemy font."
        ft[array_length_1d(ft)] = fnt_small
        ft[array_length_1d(ft)] = "battle dialogue"
        ft[array_length_1d(ft)] = fnt_ja_comicsans
        ft[array_length_1d(ft)] = "NORMAL MAIN FONT"
        ft[array_length_1d(ft)] = fnt_ja_small
        ft[array_length_1d(ft)] = "NORMAL FONT BIG"
        ft[array_length_1d(ft)] = fnt_ja_main
        ft[array_length_1d(ft)] = "toriel font"
        ft[array_length_1d(ft)] = fnt_ja_dotumche
        ft[array_length_1d(ft)] = "toriel font slow"
        ft[array_length_1d(ft)] = fnt_ja_mainbig
        ft[array_length_1d(ft)] = "susie font"
        ft[array_length_1d(ft)] = fnt_comicsans
        ft[array_length_1d(ft)] = "ralsei font"
        ft[array_length_1d(ft)] = 12
        ft[array_length_1d(ft)] = "noelle font"
        ft[array_length_1d(ft)] = 13
        ft[array_length_1d(ft)] = "berdly font"
        ft[array_length_1d(ft)] = 14
        ft[array_length_1d(ft)] = "sans font"
        ft[array_length_1d(ft)] = 15
        ft[array_length_1d(ft)] = "pap font"
        ft[array_length_1d(ft)] = 16
        ft[array_length_1d(ft)] = ")??? font"
        ft[array_length_1d(ft)] = 17
        ft[array_length_1d(ft)] = "undyne font"
        ft[array_length_1d(ft)] = 18
        ft[array_length_1d(ft)] = "asgore font"
        ft[array_length_1d(ft)] = 19
        ft[array_length_1d(ft)] = "lancer font"
        ft[array_length_1d(ft)] = 20
        ft[array_length_1d(ft)] = "alphys font"
        ft[array_length_1d(ft)] = 21
        ft[array_length_1d(ft)] = "temmie font"
        ft[array_length_1d(ft)] = 22
        ft[array_length_1d(ft)] = "alphys font small"
        ft[array_length_1d(ft)] = 23
        ft[array_length_1d(ft)] = "noelle font small"
        ft[array_length_1d(ft)] = 30
        ft[array_length_1d(ft)] = "susie dark world"
        ft[array_length_1d(ft)] = 31
        ft[array_length_1d(ft)] = "ralsei dark world"
        ft[array_length_1d(ft)] = 32
        ft[array_length_1d(ft)] = "lancer dark world"
        ft[array_length_1d(ft)] = 33
        ft[array_length_1d(ft)] = "king dark world"
        ft[array_length_1d(ft)] = 35
        ft[array_length_1d(ft)] = "joker dark world"
        ft[array_length_1d(ft)] = 36
        ft[array_length_1d(ft)] = "NORMAL FONT SILENT"
        ft[array_length_1d(ft)] = 37
        ft[array_length_1d(ft)] = "susie dark world slow, spaced."
        ft[array_length_1d(ft)] = 40
        ft[array_length_1d(ft)] = "inteo"
        ft[array_length_1d(ft)] = 41
        ft[array_length_1d(ft)] = "intro slower"
        ft[array_length_1d(ft)] = 42
        ft[array_length_1d(ft)] = "big silent slower"
        ft[array_length_1d(ft)] = 45
        ft[array_length_1d(ft)] = "battle dialogue ral"
        ft[array_length_1d(ft)] = 46
        ft[array_length_1d(ft)] = "battle dialogue lan"
        ft[array_length_1d(ft)] = 47
        ft[array_length_1d(ft)] = "battle dialogue sus"
        ft[array_length_1d(ft)] = 48
        ft[array_length_1d(ft)] = "king dark world battle"
        ft[array_length_1d(ft)] = 50
        ft[array_length_1d(ft)] = "enemy"
        ft[array_length_1d(ft)] = 51
        ft[array_length_1d(ft)] = "hellish yak text"
        ft[array_length_1d(ft)] = 52
        ft[array_length_1d(ft)] = "hellish yak text 2"
        ft[array_length_1d(ft)] = 53
        ft[array_length_1d(ft)] = "enemy: Susie"
        ft[array_length_1d(ft)] = 54
        ft[array_length_1d(ft)] = "enemy: Susie 2"
        ft[array_length_1d(ft)] = 55
        ft[array_length_1d(ft)] = "rudy font"
        ft[array_length_1d(ft)] = 60
        ft[array_length_1d(ft)] = "SLOWER, SILENT 2"
        ft[array_length_1d(ft)] = 666
        ft[array_length_1d(ft)] = "GLOW TEXT"
        ft[array_length_1d(ft)] = 667
        ft[array_length_1d(ft)] = "GLOW TEXT 2"
        ft[array_length_1d(ft)] = -1
        ft[array_length_1d(ft)] = fnt_main
        ft[array_length_1d(ft)] = -2
        ft[array_length_1d(ft)] = fnt_ja_main
        ft[array_length_1d(ft)] = -3
        ft[array_length_1d(ft)] = fnt_mainbig
        ft[array_length_1d(ft)] = -4
        ft[array_length_1d(ft)] = fnt_ja_mainbig
        ft[array_length_1d(ft)] = -5
        ft[array_length_1d(ft)] = fnt_tinynoelle
        ft[array_length_1d(ft)] = -6
        ft[array_length_1d(ft)] = fnt_ja_tinynoelle
        ft[array_length_1d(ft)] = -7
        ft[array_length_1d(ft)] = fnt_dotumche
        ft[array_length_1d(ft)] = -8
        ft[array_length_1d(ft)] = fnt_ja_dotumche
        ft[array_length_1d(ft)] = -9
        ft[array_length_1d(ft)] = fnt_comicsans
        ft[array_length_1d(ft)] = -10
        ft[array_length_1d(ft)] = fnt_ja_comicsans
        ft[array_length_1d(ft)] = -11
        ft[array_length_1d(ft)] = fnt_small
        ft[array_length_1d(ft)] = -12
        ft[array_length_1d(ft)] = fnt_ja_small
        global.chemg_font_types = ft
        global.chemg_font_type_ndx = 0
    }
    ft = global.chemg_font_types
    var num_types = (array_length_1d(ft) / 2)
    change = 0
    if (keyboard_check_pressed(vk_right) || gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gp_padr))
        change = 1
    else if (keyboard_check_pressed(vk_left) || gamepad_button_check_pressed(obj_gamecontroller.gamepad_id, gp_padl))
        change = -1
    global.chemg_font_type_ndx = (((global.chemg_font_type_ndx + change) + num_types) % num_types)
    var fndx = (global.chemg_font_type_ndx * 2)
    var xx = 10
    yy = 250
    strings[0] = "Pack my box with five"
    strings[1] = "dozen liquor jugs."
    strings[2] = "これは日本語です。"
    strings[3] = "魔物に食われない！"
    var typer = ft[fndx]
    if (typer >= 0)
    {
        global.typer = typer
        scr_texttype()
        var font = myfont
        var msg = ((((("(" + string(global.typer)) + ") ") + ft[(fndx + 1)]) + ", font: ") + font_get_name(myfont))
        vspacing = vspace
    }
    else
    {
        font = ft[(fndx + 1)]
        msg = ("font: " + font_get_name(font))
        vspacing = (font_get_size(font) + 2)
    }
    draw_set_font(font)
    draw_set_colour(c_white)
    for (i = 0; i < array_length_1d(strings); i += 1)
    {
        var str = strings[i]
        if (typer >= 0)
        {
            var len = string_length(str)
            var wx = xx
            for (var j = 1; j <= len; j += 1)
            {
                var mychar = string_copy(str, j, 1)
                draw_text(wx, ((yy + 20) + (i * vspacing)), mychar)
                wx += hspace
                if (global.lang == "ja")
                {
                    if (ord(mychar) < 256 || (ord(mychar) >= 65377 && ord(mychar) <= 65439))
                        wx -= (hspace / 2)
                }
            }
        }
        else
            draw_text(xx, ((yy + 20) + (i * vspacing)), str)
    }
    draw_set_colour(c_white)
    draw_set_font(fnt_main)
    draw_text(xx, yy, ("<-/-> to change typer: " + msg))
}
