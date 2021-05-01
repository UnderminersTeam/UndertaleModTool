switch argument0
{
    case 0:
        break
    case 1:
        global.msg[0] = "\XLa, la.^3 &Time to wake&up and\R smell\X &the^4 pain./"
        global.msg[1] = "* Though^2.^4.^6.^8.&It's still a&little shaky./"
        global.msg[2] = "fhuehfuehfuehfuheufhe/%"
        global.msg[3] = "%%%"
        break
    case 2:
        global.msg[0] = "* TestMonster and its cohorts&draw near!"
        global.msg[1] = "%%%"
        break
    case 3:
        global.msg[0] = " "
        if (global.monster[0] == true)
        {
            with (global.monsterinstance[0])
                script_execute(scr_mercystandard)
            adder = "\W"
            if (global.monsterinstance[0].mercy < 0)
            {
                if (global.flag[22] == 0)
                    adder = "\Y"
                if (global.flag[22] == 2)
                    adder = "\p"
            }
            global.msg[0] = adder
            global.msg[0] += (scr_gettext("battle_name_header") + global.monstername[0])
            if (global.monstertype[0] == global.monstertype[1] || global.monstertype[0] == global.monstertype[2])
                global.msg[0] += scr_gettext("battle_name_a")
        }
        global.msg[0] += "\W &"
        if (global.monster[1] == true)
        {
            with (global.monsterinstance[1])
                script_execute(scr_mercystandard)
            if (global.monsterinstance[1].mercy < 0 && global.flag[22] == 0)
                global.msg[0] += "\Y"
            if (global.monsterinstance[1].mercy < 0 && global.flag[22] == 2)
                global.msg[0] += "\p"
            global.msg[0] += (scr_gettext("battle_name_header") + global.monstername[1])
            if (global.monstertype[1] == global.monstertype[0])
                global.msg[0] += scr_gettext("battle_name_b")
        }
        global.msg[0] += "\W &"
        if (global.monster[2] == true)
        {
            with (global.monsterinstance[2])
                script_execute(scr_mercystandard)
            if (global.monsterinstance[2].mercy < 0 && global.flag[22] == 0)
                global.msg[0] += "\Y"
            if (global.monsterinstance[2].mercy < 0 && global.flag[22] == 2)
                global.msg[0] += "\p"
            global.msg[0] += (scr_gettext("battle_name_header") + global.monstername[2])
            if (global.monstertype[2] == global.monstertype[1])
                global.msg[0] += scr_gettext("battle_name_c")
        }
        global.msg[1] = "%%%"
        break
    case 7:
        global.msg[0] = ""
        for (i = 0; i < 3; i += 1)
        {
            if (global.monster[i] == true)
            {
                with (global.monsterinstance[i])
                    script_execute(scr_mercystandard)
                if (global.monsterinstance[i].mercy < 0 && global.flag[22] == 0)
                    global.msg[0] = "\Y"
                if (global.monsterinstance[i].mercy < 0 && global.flag[22] == 2)
                    global.msg[0] = "\p"
            }
        }
        global.msg[0] += scr_gettext("battle_mercy_spare")
        if (global.mercy == 0)
            global.msg[0] += (" &\W" + scr_gettext("battle_mercy_flee"))
        break
    case 9:
        global.msg[0] = (scr_gettext("item_menub_header") + global.itemnameb[0])
        if (global.item[1] != 0)
            global.msg[0] += (scr_gettext("item_menub_header") + global.itemnameb[1])
        global.msg[0] += "&"
        if (global.item[2] != 0)
            global.msg[0] += (scr_gettext("item_menub_header") + global.itemnameb[2])
        if (global.item[3] != 0)
            global.msg[0] += (scr_gettext("item_menub_header") + global.itemnameb[3])
        global.msg[0] += ("&" + scr_gettext("item_menub_page1"))
        global.msg[1] = "%%%"
        break
    case 10:
        global.msg[0] = (scr_gettext("item_menub_header") + global.itemnameb[4])
        if (global.item[5] != 0)
            global.msg[0] += (scr_gettext("item_menub_header") + global.itemnameb[5])
        global.msg[0] += "&"
        if (global.item[6] != 0)
            global.msg[0] += (scr_gettext("item_menub_header") + global.itemnameb[6])
        if (global.item[7] != 0)
            global.msg[0] += (scr_gettext("item_menub_header") + global.itemnameb[7])
        global.msg[0] += ("&" + scr_gettext("item_menub_page2"))
        global.msg[1] = "%%%"
        break
    case 11:
        global.msg[0] += " &"
        if (global.item[8] < 99)
            global.msg[0] += scr_gettext("recover_hp", string(global.item[8]))
        else
            global.msg[0] += scr_gettext("recover_hp_max")
        break
    case 12:
        i = round(random(18))
        if (i == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_99")
        if (i == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_100")
        if (i == 2)
            global.msg[0] = scr_gettext("SCR_TEXT_102")
        if (i == 3)
            global.msg[0] = scr_gettext("SCR_TEXT_103")
        if (i > 3)
            global.msg[0] = scr_gettext("SCR_TEXT_104")
        global.msg[0] += "/%"
        break
    case 14:
        i = round(random(20))
        if (i == 0 || i == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_111")
        if (i == 2)
            global.msg[0] = scr_gettext("SCR_TEXT_112")
        if (i > 3)
            global.msg[0] = scr_gettext("SCR_TEXT_113")
        if (i == 3)
            global.msg[0] = scr_gettext("SCR_TEXT_114")
        if (global.xpreward[3] > 0 || global.goldreward[3] > 0)
            global.msg[0] = scr_gettext("SCR_TEXT_121", string(global.xpreward[3]), string(global.goldreward[3]))
        break
    case 15:
        if (room == room_ruins1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_127")
            global.msg[1] = scr_gettext("SCR_TEXT_128")
        }
        if (room == room_ruins7)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_129")
            global.msg[1] = scr_gettext("SCR_TEXT_130")
        }
        if (room == room_ruins12A)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_131")
            global.msg[1] = scr_gettext("SCR_TEXT_132")
        }
        if (room == room_ruins19)
            global.msg[0] = scr_gettext("SCR_TEXT_133")
        if (global.flag[202] >= 20)
            global.msg[0] = scr_gettext("SCR_TEXT_134")
        if (room == room_tundra1)
            global.msg[0] = scr_gettext("SCR_TEXT_135")
        if (room == room_tundra3)
            global.msg[0] = scr_gettext("SCR_TEXT_136")
        if (room == room_tundra_spaghetti)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_137")
            global.msg[1] = scr_gettext("SCR_TEXT_138")
        }
        if (room == room_tundra_lesserdog)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_140")
            global.msg[1] = scr_gettext("SCR_TEXT_141")
            if (global.flag[55] == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_142")
                global.msg[1] = scr_gettext("SCR_TEXT_143")
            }
        }
        if (room == room_tundra_town)
            global.msg[0] = scr_gettext("SCR_TEXT_144")
        if (room == room_water2)
            global.msg[0] = scr_gettext("SCR_TEXT_145")
        if (room == room_water4)
            global.msg[0] = scr_gettext("SCR_TEXT_146")
        if (room == room_water_savepoint1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_147")
            global.msg[1] = scr_gettext("SCR_TEXT_148")
        }
        if (room == room_water_preundyne)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_150")
            if (global.flag[86] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_151")
            global.msg[1] = scr_gettext("SCR_TEXT_152")
        }
        if (room == room_water_trashzone2)
            global.msg[0] = scr_gettext("SCR_TEXT_153")
        if (room == room_water_trashsavepoint)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_156")
            global.msg[1] = scr_gettext("SCR_TEXT_157")
            global.msg[2] = scr_gettext("SCR_TEXT_158")
            global.msg[3] = scr_gettext("SCR_TEXT_159")
            global.msg[4] = scr_gettext("SCR_TEXT_160")
            if (global.flag[91] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_161")
            global.flag[91] = 1
        }
        if (room == room_water_friendlyhub)
            global.msg[0] = scr_gettext("SCR_TEXT_164")
        if (room == room_water_temvillage)
            global.msg[0] = scr_gettext("SCR_TEXT_165")
        if (room == room_water_undynefinal)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_168")
            if (global.flag[99] > 0)
                global.msg[0] = scr_gettext("SCR_TEXT_169")
            if (global.flag[350] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_170")
        }
        if (room == room_fire_prelab)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_174")
            global.msg[1] = scr_gettext("SCR_TEXT_175")
        }
        if (room == room_fire6)
            global.msg[0] = scr_gettext("SCR_TEXT_180")
        if (room == room_fire_savepoint1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_184")
            global.msg[1] = scr_gettext("SCR_TEXT_185")
        }
        if (room == room_fire_mewmew2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_187")
            global.msg[1] = scr_gettext("SCR_TEXT_188")
        }
        if (room == room_fire_hotelfront_1)
            global.msg[0] = scr_gettext("SCR_TEXT_191")
        if (room == room_fire_hotellobby)
            global.msg[0] = scr_gettext("SCR_TEXT_195")
        if (room == room_fire_core_branch)
            global.msg[0] = scr_gettext("SCR_TEXT_199")
        if (room == room_fire_core_premett)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_203")
            global.msg[1] = scr_gettext("SCR_TEXT_204")
        }
        if (room == room_fire_savepoint2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_208")
            global.msg[1] = scr_gettext("SCR_TEXT_209")
        }
        break
    case 16:
        i = round(random(14))
        script_execute(scr_itemname)
        if (i <= 12)
            global.msg[0] = scr_gettext("item_box_store1", global.itemname[global.menucoord[6]])
        if (i > 12)
            global.msg[0] = scr_gettext("item_box_store2", global.itemname[global.menucoord[6]])
        if (i > 13)
            global.msg[0] = scr_gettext("item_box_store3", global.itemname[global.menucoord[6]])
        global.msg[0] += "/%"
        break
    case 17:
        i = round(random(14))
        script_execute(scr_storagename, 300)
        if (i <= 12)
            global.msg[0] = scr_gettext("item_box_take1", global.itemname[global.menucoord[6]])
        if (i > 12)
            global.msg[0] = scr_gettext("item_box_take2", global.itemname[global.menucoord[6]])
        if (i > 13)
            global.msg[0] = scr_gettext("item_box_take3", global.itemname[global.menucoord[6]])
        global.msg[0] += "/%"
        break
    case 18:
        global.msg[0] = scr_gettext("item_full_inventory")
        break
    case 19:
        global.msg[0] = scr_gettext("item_full_box")
        break
    case 23:
        global.msg[0] = scr_gettext("SCR_TEXT_242")
        break
    case 30:
        global.msg[0] = scr_gettext("SCR_TEXT_246")
        global.msg[1] = scr_gettext("SCR_TEXT_247")
        global.msg[2] = scr_gettext("SCR_TEXT_248")
        break
    case 31:
        if (global.choice == 0)
        {
            if (global.item[0] != 0 || global.flag[300] != 0)
            {
                if (instance_exists(obj_itemswapper) == 0)
                    instance_create(0, 0, obj_itemswapper)
                global.msg[0] = scr_gettext("SCR_TEXT_270")
            }
            else
            {
                gx = floor(random(3))
                if (gx == 0)
                    global.msg[0] = scr_gettext("SCR_TEXT_275")
                if (gx == 1)
                    global.msg[0] = scr_gettext("SCR_TEXT_276")
                if (gx == 2)
                    global.msg[0] = scr_gettext("SCR_TEXT_277")
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_282")
        break
    case 200:
        global.msg[0] = scr_gettext("SCR_TEXT_291")
        global.msg[1] = scr_gettext("SCR_TEXT_292")
        global.msg[2] = scr_gettext("SCR_TEXT_293")
        global.msg[3] = scr_gettext("SCR_TEXT_294")
        global.msg[4] = scr_gettext("SCR_TEXT_295")
        global.msg[5] = scr_gettext("SCR_TEXT_296")
        global.msg[6] = scr_gettext("SCR_TEXT_297")
        break
    case 201:
        global.msg[0] = scr_gettext("SCR_TEXT_301")
        global.msg[1] = scr_gettext("SCR_TEXT_302")
        break
    case 202:
        global.msg[0] = scr_gettext("SCR_TEXT_306")
        global.msg[1] = scr_gettext("SCR_TEXT_307")
        break
    case 203:
        global.msg[0] = scr_gettext("SCR_TEXT_311")
        global.msg[1] = scr_gettext("SCR_TEXT_312")
        global.msg[2] = scr_gettext("SCR_TEXT_313")
        global.msg[3] = scr_gettext("SCR_TEXT_314")
        break
    case 204:
        global.msg[0] = scr_gettext("SCR_TEXT_318")
        global.msg[1] = scr_gettext("SCR_TEXT_319")
        if (global.flag[6] == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_322")
            global.msg[2] = scr_gettext("SCR_TEXT_323")
            global.msg[3] = scr_gettext("SCR_TEXT_324")
            global.msg[4] = scr_gettext("SCR_TEXT_325")
        }
        break
    case 205:
        global.msg[0] = scr_gettext("SCR_TEXT_330")
        break
    case 206:
        global.msg[0] = scr_gettext("SCR_TEXT_334")
        global.msg[1] = scr_gettext("SCR_TEXT_335")
        break
    case 207:
        global.msg[0] = scr_gettext("SCR_TEXT_339")
        if (global.flag[6] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_342")
        break
    case 208:
        global.msg[0] = scr_gettext("SCR_TEXT_347")
        global.msg[1] = scr_gettext("SCR_TEXT_348")
        if (global.flag[6] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_352")
        break
    case 209:
        global.msg[0] = scr_gettext("SCR_TEXT_357")
        global.msg[1] = scr_gettext("SCR_TEXT_358")
        break
    case 210:
        global.msg[0] = scr_gettext("SCR_TEXT_362")
        global.msg[1] = scr_gettext("SCR_TEXT_363")
        break
    case 211:
        global.msg[0] = scr_gettext("SCR_TEXT_367")
        global.msg[1] = scr_gettext("SCR_TEXT_368")
        global.msg[2] = scr_gettext("SCR_TEXT_369")
        global.msg[3] = scr_gettext("SCR_TEXT_370")
        global.msg[4] = scr_gettext("SCR_TEXT_371")
        global.msg[5] = scr_gettext("SCR_TEXT_372")
        global.msg[6] = scr_gettext("SCR_TEXT_373")
        break
    case 212:
        if (global.flag[12] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_379")
            global.msg[1] = scr_gettext("SCR_TEXT_380")
        }
        if (global.flag[10] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_384")
        if (global.flag[11] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_388")
            global.msg[1] = scr_gettext("SCR_TEXT_389")
            global.msg[2] = scr_gettext("SCR_TEXT_390")
            global.msg[3] = scr_gettext("SCR_TEXT_391")
            global.msg[4] = scr_gettext("SCR_TEXT_392")
            global.msg[5] = scr_gettext("SCR_TEXT_393")
            global.msg[6] = scr_gettext("SCR_TEXT_394")
        }
        if (global.flag[13] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_398")
            global.msg[1] = scr_gettext("SCR_TEXT_399")
            global.msg[2] = scr_gettext("SCR_TEXT_400")
        }
        break
    case 213:
        global.msg[0] = scr_gettext("SCR_TEXT_405")
        global.msg[1] = scr_gettext("SCR_TEXT_406")
        break
    case 214:
        global.msg[0] = scr_gettext("SCR_TEXT_411")
        global.msg[1] = scr_gettext("SCR_TEXT_412")
        global.msg[2] = scr_gettext("SCR_TEXT_413")
        global.msg[3] = scr_gettext("SCR_TEXT_414")
        global.msg[4] = scr_gettext("SCR_TEXT_415")
        global.msg[5] = scr_gettext("SCR_TEXT_416")
        global.msg[6] = scr_gettext("SCR_TEXT_417")
        global.msg[7] = scr_gettext("SCR_TEXT_418")
        global.msg[8] = scr_gettext("SCR_TEXT_419")
        break
    case 215:
        global.msg[0] = scr_gettext("SCR_TEXT_423")
        global.msg[1] = scr_gettext("SCR_TEXT_424")
        break
    case 216:
        global.msg[0] = scr_gettext("SCR_TEXT_428")
        global.msg[1] = scr_gettext("SCR_TEXT_429")
        break
    case 217:
        global.msg[0] = scr_gettext("SCR_TEXT_433")
        global.msg[1] = scr_gettext("SCR_TEXT_434")
        break
    case 218:
        global.msg[0] = scr_gettext("SCR_TEXT_438")
        break
    case 219:
        global.msg[0] = scr_gettext("SCR_TEXT_442")
        break
    case 220:
        global.msg[0] = scr_gettext("SCR_TEXT_446")
        global.msg[1] = scr_gettext("SCR_TEXT_447")
        global.msg[2] = scr_gettext("SCR_TEXT_448")
        global.msg[3] = scr_gettext("SCR_TEXT_449")
        global.msg[4] = scr_gettext("SCR_TEXT_450")
        global.msg[5] = scr_gettext("SCR_TEXT_451")
        global.msg[6] = scr_gettext("SCR_TEXT_452")
        global.msg[7] = scr_gettext("SCR_TEXT_453")
        global.msg[8] = scr_gettext("SCR_TEXT_454")
        global.msg[9] = scr_gettext("SCR_TEXT_455")
        break
    case 221:
        global.msg[0] = scr_gettext("SCR_TEXT_459")
        global.msg[1] = scr_gettext("SCR_TEXT_460")
        global.msg[2] = scr_gettext("SCR_TEXT_461")
        global.msg[3] = scr_gettext("SCR_TEXT_462")
        global.msg[4] = scr_gettext("SCR_TEXT_463")
        break
    case 222:
        global.msg[0] = scr_gettext("SCR_TEXT_467")
        global.msg[1] = scr_gettext("SCR_TEXT_468")
        global.msg[2] = scr_gettext("SCR_TEXT_469")
        global.msg[3] = scr_gettext("SCR_TEXT_470")
        global.msg[4] = scr_gettext("SCR_TEXT_471")
        global.msg[5] = scr_gettext("SCR_TEXT_472")
        break
    case 223:
        if (global.choice == 0)
        {
            global.flag[46] = 0
            ossafe_ini_open("undertale.ini")
            ini_write_real("Toriel", "Bscotch", 2)
            ossafe_ini_close()
            ossafe_savedata_save()
        }
        if (global.choice == 1)
        {
            global.flag[46] = 1
            ossafe_ini_open("undertale.ini")
            ini_write_real("Toriel", "Bscotch", 1)
            ossafe_ini_close()
            ossafe_savedata_save()
        }
        global.msg[0] = scr_gettext("SCR_TEXT_490")
        global.msg[1] = scr_gettext("SCR_TEXT_491")
        global.msg[2] = scr_gettext("SCR_TEXT_492")
        global.msg[5] = scr_gettext("SCR_TEXT_493")
        break
    case 224:
        global.msg[0] = scr_gettext("SCR_TEXT_497")
        global.msg[1] = scr_gettext("SCR_TEXT_498")
        global.msg[2] = scr_gettext("SCR_TEXT_499")
        break
    case 225:
        if (global.choice == 0)
        {
            if instance_exists(obj_ladiesfishingrod)
            {
                obj_ladiesfishingrod.reeled = 1
                obj_ladiesfishingrod.image_index = 1
            }
            global.msg[0] = scr_gettext("SCR_TEXT_511")
            global.msg[1] = scr_gettext("SCR_TEXT_512")
            global.msg[2] = scr_gettext("SCR_TEXT_513")
            if (global.flag[7] == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_516")
                global.msg[1] = scr_gettext("SCR_TEXT_517")
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_522")
        break
    case 226:
        if (global.flag[56] == 5)
            global.msg[0] = scr_gettext("SCR_TEXT_529")
        if (global.flag[56] == 4)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_533")
            global.msg[1] = scr_gettext("SCR_TEXT_534")
            global.msg[2] = scr_gettext("SCR_TEXT_535")
            global.flag[56] = 5
        }
        if (global.flag[56] == 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_540")
            scr_itemcheck(16)
            scr_storagecheck(16)
            if (haveit == 0 && haveit2 == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_545")
                global.msg[1] = scr_gettext("SCR_TEXT_546")
                global.msg[2] = scr_gettext("SCR_TEXT_547")
                global.msg[3] = scr_gettext("SCR_TEXT_548")
                global.msg[4] = scr_gettext("SCR_TEXT_549")
            }
        }
        if (global.flag[56] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_554")
            scr_itemcheck(16)
            scr_storagecheck(16)
            if (haveit == 0 && haveit2 == 0)
            {
                global.msg[1] = scr_gettext("SCR_TEXT_559")
                scr_itemget(16)
                if (noroom == 1)
                    global.msg[2] = scr_gettext("SCR_TEXT_563")
                else
                {
                    global.msg[2] = scr_gettext("SCR_TEXT_567")
                    global.msg[3] = scr_gettext("SCR_TEXT_568")
                    global.flag[56] = 2
                }
            }
            else
                global.msg[0] += "%%"
        }
        if (global.flag[56] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_576")
            global.msg[1] = scr_gettext("SCR_TEXT_577")
            global.msg[2] = scr_gettext("SCR_TEXT_578")
            global.msg[3] = scr_gettext("SCR_TEXT_579")
            global.msg[4] = scr_gettext("SCR_TEXT_580")
        }
        break
    case 227:
        if (global.choice == 0)
        {
            scr_itemget(16)
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_590")
            else
            {
                global.msg[0] = scr_gettext("SCR_TEXT_593")
                global.msg[1] = scr_gettext("SCR_TEXT_594")
                global.flag[56] = 1
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_600")
        break
    case 228:
        global.msg[0] = scr_gettext("SCR_TEXT_605")
        if (global.flag[254] == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_606")
        scr_sansface(1, 0)
        global.msg[2] = scr_gettext("SCR_TEXT_608")
        global.msg[3] = scr_gettext("SCR_TEXT_609")
        scr_papface(4, 1)
        global.msg[5] = scr_gettext("SCR_TEXT_611")
        global.msg[6] = scr_gettext("SCR_TEXT_612")
        global.msg[7] = scr_gettext("SCR_TEXT_613")
        global.msg[8] = scr_gettext("SCR_TEXT_614")
        scr_sansface(9, 0)
        global.msg[10] = scr_gettext("SCR_TEXT_616")
        global.msg[11] = scr_gettext("SCR_TEXT_617")
        scr_papface(12, 3)
        global.msg[13] = scr_gettext("SCR_TEXT_619")
        global.msg[14] = scr_gettext("SCR_TEXT_620")
        global.msg[15] = scr_gettext("SCR_TEXT_621")
        global.msg[16] = scr_gettext("SCR_TEXT_622")
        global.msg[17] = scr_gettext("SCR_TEXT_623")
        break
    case 229:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.flag[58] = 0
            global.msg[1] = scr_gettext("SCR_TEXT_631")
            global.msg[2] = scr_gettext("SCR_TEXT_632")
            global.msg[3] = scr_gettext("SCR_TEXT_633")
            global.msg[4] = scr_gettext("SCR_TEXT_634")
        }
        if (global.choice == 1)
        {
            global.flag[58] = 1
            global.msg[1] = scr_gettext("SCR_TEXT_639")
            global.msg[2] = scr_gettext("SCR_TEXT_640")
            global.msg[3] = scr_gettext("SCR_TEXT_641")
            global.msg[4] = scr_gettext("SCR_TEXT_642")
            global.msg[5] = scr_gettext("SCR_TEXT_643")
            global.msg[6] = scr_gettext("SCR_TEXT_644")
            global.msg[7] = scr_gettext("SCR_TEXT_645")
        }
        break
    case 230:
        doak = 0
        noroom = 0
        if (global.flag[60] < 5)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_654")
            global.msg[1] = scr_gettext("SCR_TEXT_655")
            global.msg[2] = scr_gettext("SCR_TEXT_656")
            global.msg[3] = scr_gettext("SCR_TEXT_657")
        }
        if (global.flag[60] >= 5)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_661")
            global.msg[1] = scr_gettext("SCR_TEXT_662")
            global.msg[2] = scr_gettext("SCR_TEXT_663")
            global.msg[3] = scr_gettext("SCR_TEXT_664")
        }
        break
    case 231:
        script_execute(scr_cost, 15)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 17)
                    if (noroom == 0)
                    {
                        global.gold -= 15
                        global.flag[60] += 15
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_682")
            if (afford == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_684")
                global.msg[1] = scr_gettext("SCR_TEXT_685")
            }
        }
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_688")
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_691")
            global.msg[1] = scr_gettext("SCR_TEXT_692")
        }
        break
    case 232:
        doak = 0
        noroom = 0
        scr_itemcheck(26)
        if (global.flag[85] == 0)
        {
            if (global.flag[60] < 5)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_704")
                global.msg[1] = scr_gettext("SCR_TEXT_705")
                global.msg[2] = scr_gettext("SCR_TEXT_706")
                global.msg[3] = scr_gettext("SCR_TEXT_707")
            }
            if (global.flag[60] >= 5)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_711")
                global.msg[1] = scr_gettext("SCR_TEXT_712")
                global.msg[2] = scr_gettext("SCR_TEXT_713")
                global.msg[3] = scr_gettext("SCR_TEXT_714")
            }
        }
        if (global.flag[85] == 1)
        {
            if (global.flag[60] < 5)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_721")
                global.msg[1] = scr_gettext("SCR_TEXT_722")
                global.msg[2] = scr_gettext("SCR_TEXT_723")
                global.msg[3] = scr_gettext("SCR_TEXT_724")
            }
            if (global.flag[60] >= 5)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_728")
                global.msg[1] = scr_gettext("SCR_TEXT_729")
                global.msg[2] = scr_gettext("SCR_TEXT_730")
                global.msg[3] = scr_gettext("SCR_TEXT_731")
            }
        }
        if (itemcount > 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_736")
            global.msg[1] = scr_gettext("SCR_TEXT_737")
            global.msg[2] = scr_gettext("SCR_TEXT_738")
            global.msg[3] = scr_gettext("SCR_TEXT_739")
        }
        break
    case 233:
        scr_itemcheck(26)
        if (itemcount < 3)
        {
            if (global.flag[85] == 0)
                script_execute(scr_cost, 25)
            if (global.flag[85] == 1)
                script_execute(scr_cost, 15)
            if (global.choice == 0)
            {
                if (afford == 1)
                {
                    if (doak == 0)
                    {
                        doak = 1
                        script_execute(scr_itemget, 17)
                        if (noroom == 0)
                        {
                            if (global.flag[85] == 0)
                            {
                                global.gold -= 25
                                global.flag[60] += 25
                            }
                            else
                            {
                                global.gold -= 15
                                global.flag[60] += 15
                            }
                            global.flag[80] += 1
                        }
                    }
                }
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_777")
                if (afford == 0)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_779")
                    if (global.flag[85] == 1)
                        global.msg[0] = scr_gettext("SCR_TEXT_781")
                }
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_784")
        }
        else if (global.choice == 0)
        {
            rem = 0
            g = 0
            for (n = 0; n < 8; n += 1)
            {
                if (g == 1)
                {
                    n -= 1
                    g = 0
                }
                if (global.item[n] == 26 && rem < 3)
                {
                    scr_itemshift(n, 0)
                    rem += 1
                    g = 1
                }
            }
            scr_itemget(17)
            global.msg[0] = scr_gettext("SCR_TEXT_803")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_809")
            global.msg[1] = scr_gettext("SCR_TEXT_810")
        }
        break
    case 235:
        global.msg[0] = scr_gettext("SCR_TEXT_815")
        global.msg[1] = scr_gettext("SCR_TEXT_816")
        global.msg[2] = scr_gettext("SCR_TEXT_817")
        break
    case 236:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_823")
            global.msg[1] = scr_gettext("SCR_TEXT_824")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_828")
            global.msg[1] = scr_gettext("SCR_TEXT_829")
        }
        break
    case 237:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_836")
            global.msg[1] = scr_gettext("SCR_TEXT_837")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_841")
            global.msg[1] = scr_gettext("SCR_TEXT_842")
        }
        break
    case 238:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_849")
            global.msg[1] = scr_gettext("SCR_TEXT_850")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_854")
        break
    case 239:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_861")
            global.msg[1] = scr_gettext("SCR_TEXT_862")
            global.msg[2] = scr_gettext("SCR_TEXT_863")
            if (global.gold >= 50000)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_867")
                global.msg[1] = scr_gettext("SCR_TEXT_868")
                global.msg[2] = scr_gettext("SCR_TEXT_869")
                global.msg[3] = scr_gettext("SCR_TEXT_870")
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_875")
        break
    case 240:
        global.msg[0] = scr_gettext("SCR_TEXT_880")
        global.msg[1] = scr_gettext("SCR_TEXT_881")
        global.msg[2] = scr_gettext("SCR_TEXT_882")
        global.msg[3] = scr_gettext("SCR_TEXT_883")
        global.msg[4] = scr_gettext("SCR_TEXT_884")
        global.msg[5] = scr_gettext("SCR_TEXT_885")
        break
    case 241:
        scr_papface(0, 0)
        global.msg[1] = scr_gettext("SCR_TEXT_890")
        if (global.choice == 0)
        {
            global.msg[2] = scr_gettext("SCR_TEXT_893")
            global.msg[3] = scr_gettext("SCR_TEXT_894")
            global.msg[4] = scr_gettext("SCR_TEXT_895")
            global.flag[62] = 1
        }
        if (global.choice == 1)
        {
            global.msg[2] = scr_gettext("SCR_TEXT_900")
            global.msg[3] = scr_gettext("SCR_TEXT_901")
            global.msg[4] = scr_gettext("SCR_TEXT_902")
            global.flag[62] = 2
        }
        global.msg[5] = scr_gettext("SCR_TEXT_905")
        global.msg[6] = scr_gettext("SCR_TEXT_906")
        global.msg[7] = scr_gettext("SCR_TEXT_907")
        break
    case 243:
        global.msg[0] = scr_gettext("SCR_TEXT_911")
        global.msg[1] = scr_gettext("SCR_TEXT_912")
        global.msg[2] = scr_gettext("SCR_TEXT_913")
        global.msg[3] = scr_gettext("SCR_TEXT_914")
        global.msg[4] = scr_gettext("SCR_TEXT_915")
        global.msg[5] = scr_gettext("SCR_TEXT_916")
        global.msg[6] = scr_gettext("SCR_TEXT_917")
        global.msg[7] = scr_gettext("SCR_TEXT_918")
        global.msg[8] = scr_gettext("SCR_TEXT_919")
        global.msg[9] = scr_gettext("SCR_TEXT_920")
        global.msg[10] = scr_gettext("SCR_TEXT_921")
        global.msg[11] = scr_gettext("SCR_TEXT_922")
        global.msg[12] = scr_gettext("SCR_TEXT_923")
        global.msg[13] = scr_gettext("SCR_TEXT_924")
        global.msg[14] = scr_gettext("SCR_TEXT_925")
        global.msg[15] = scr_gettext("SCR_TEXT_926")
        global.msg[16] = scr_gettext("SCR_TEXT_927")
        global.msg[17] = scr_gettext("SCR_TEXT_928")
        global.msg[18] = scr_gettext("SCR_TEXT_929")
        global.msg[19] = scr_gettext("SCR_TEXT_930")
        global.msg[20] = scr_gettext("SCR_TEXT_931")
        global.msg[21] = scr_gettext("SCR_TEXT_932")
        global.msg[22] = scr_gettext("SCR_TEXT_933")
        global.msg[23] = scr_gettext("SCR_TEXT_934")
        global.msg[24] = scr_gettext("SCR_TEXT_935")
        global.msg[25] = scr_gettext("SCR_TEXT_936")
        global.msg[26] = scr_gettext("SCR_TEXT_937")
        global.msg[27] = scr_gettext("SCR_TEXT_938")
        global.msg[28] = scr_gettext("SCR_TEXT_939")
        global.msg[29] = scr_gettext("SCR_TEXT_940")
        global.msg[30] = scr_gettext("SCR_TEXT_941")
        global.msg[31] = scr_gettext("SCR_TEXT_942")
        global.msg[32] = scr_gettext("SCR_TEXT_943")
        global.msg[33] = scr_gettext("SCR_TEXT_944")
        global.msg[34] = scr_gettext("SCR_TEXT_945")
        global.msg[35] = scr_gettext("SCR_TEXT_946")
        break
    case 244:
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_953")
            global.msg[2] = scr_gettext("SCR_TEXT_954")
            global.msg[3] = scr_gettext("SCR_TEXT_955")
            global.msg[4] = scr_gettext("SCR_TEXT_956")
            global.msg[5] = scr_gettext("SCR_TEXT_957")
            global.msg[6] = scr_gettext("SCR_TEXT_958")
            global.msg[7] = scr_gettext("SCR_TEXT_959")
            obj_papyrus4.conversation = 50
        }
        if (global.choice == 1)
        {
            scr_papface(0, 3)
            global.msg[1] = scr_gettext("SCR_TEXT_965")
            global.msg[2] = scr_gettext("SCR_TEXT_966")
            global.msg[3] = scr_gettext("SCR_TEXT_967")
            global.msg[4] = scr_gettext("SCR_TEXT_968")
            global.msg[5] = scr_gettext("SCR_TEXT_969")
            global.msg[6] = scr_gettext("SCR_TEXT_970")
            global.msg[7] = scr_gettext("SCR_TEXT_971")
            global.msg[8] = scr_gettext("SCR_TEXT_972")
            global.msg[9] = scr_gettext("SCR_TEXT_973")
            global.msg[10] = scr_gettext("SCR_TEXT_974")
            global.msg[11] = scr_gettext("SCR_TEXT_975")
            global.msg[12] = scr_gettext("SCR_TEXT_976")
            global.msg[13] = scr_gettext("SCR_TEXT_977")
            global.msg[14] = scr_gettext("SCR_TEXT_978")
            global.msg[15] = scr_gettext("SCR_TEXT_979")
            global.msg[16] = scr_gettext("SCR_TEXT_980")
            global.msg[17] = scr_gettext("SCR_TEXT_981")
            global.msg[18] = scr_gettext("SCR_TEXT_982")
            global.msg[19] = scr_gettext("SCR_TEXT_983")
            global.msg[20] = scr_gettext("SCR_TEXT_984")
            global.msg[21] = scr_gettext("SCR_TEXT_985")
            global.msg[22] = scr_gettext("SCR_TEXT_986")
        }
        break
    case 245:
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_994")
            global.msg[2] = scr_gettext("SCR_TEXT_995")
            global.msg[3] = scr_gettext("SCR_TEXT_996")
            global.msg[4] = scr_gettext("SCR_TEXT_997")
            global.msg[5] = scr_gettext("SCR_TEXT_998")
            global.msg[6] = scr_gettext("SCR_TEXT_999")
            global.msg[7] = scr_gettext("SCR_TEXT_1000")
            obj_papyrus4.conversation = 50
        }
        if (global.choice == 1)
        {
            scr_papface(0, 3)
            global.msg[1] = scr_gettext("SCR_TEXT_1006")
            global.msg[2] = scr_gettext("SCR_TEXT_1007")
            global.msg[3] = scr_gettext("SCR_TEXT_1008")
            global.msg[4] = scr_gettext("SCR_TEXT_1009")
            global.msg[5] = scr_gettext("SCR_TEXT_1010")
            global.msg[6] = scr_gettext("SCR_TEXT_1011")
            global.msg[7] = scr_gettext("SCR_TEXT_1012")
            global.msg[8] = scr_gettext("SCR_TEXT_1013")
            global.msg[9] = scr_gettext("SCR_TEXT_1014")
            global.msg[10] = scr_gettext("SCR_TEXT_1015")
            global.msg[11] = scr_gettext("SCR_TEXT_1016")
            obj_papyrus4.conversation = 80
        }
        break
    case 246:
        if (global.flag[104] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1024")
            global.msg[1] = scr_gettext("SCR_TEXT_1025")
            global.msg[2] = scr_gettext("SCR_TEXT_1026")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1029")
        break
    case 247:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 23)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1045")
                global.flag[104] = 1
            }
            if (noroom == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1048")
                global.msg[1] = scr_gettext("SCR_TEXT_1049")
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1053")
        break
    case 248:
        if (global.flag[105] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1060")
            global.msg[1] = scr_gettext("SCR_TEXT_1061")
            global.msg[2] = scr_gettext("SCR_TEXT_1062")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1065")
        break
    case 249:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 24)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1081")
                global.flag[105] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1084")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1088")
        break
    case 250:
        if (global.flag[80] > 0)
        {
            card = string(global.flag[80])
            global.msg[0] = scr_gettext("SCR_TEXT_1096", card)
            if (global.flag[80] > 2)
                global.msg[0] = scr_gettext("SCR_TEXT_1097", card)
            if (global.flag[80] > 4)
                global.msg[0] = scr_gettext("SCR_TEXT_1098", card)
            if (global.flag[80] > 6)
                global.msg[0] = scr_gettext("SCR_TEXT_1099", card)
            if (global.flag[80] > 12)
                global.msg[0] = scr_gettext("SCR_TEXT_1100", card)
            global.msg[1] = scr_gettext("SCR_TEXT_1101")
            global.msg[2] = scr_gettext("SCR_TEXT_1102")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1105")
        break
    case 251:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 26)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1120")
                if (global.flag[80] > 2)
                    global.msg[0] = scr_gettext("SCR_TEXT_1121")
                if (global.flag[80] > 4)
                    global.msg[0] = scr_gettext("SCR_TEXT_1122")
                if (global.flag[80] > 6)
                    global.msg[0] = scr_gettext("SCR_TEXT_1123")
                if (global.flag[80] > 12)
                    global.msg[0] = scr_gettext("SCR_TEXT_1124")
                global.flag[80] -= 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1127")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1131")
        break
    case 252:
        if (global.flag[106] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1139")
            global.msg[1] = scr_gettext("SCR_TEXT_1140")
            global.msg[2] = scr_gettext("SCR_TEXT_1141")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1144")
        break
    case 253:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 25)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1160")
                global.flag[106] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1163")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1167")
        break
    case 254:
        global.msg[0] = scr_gettext("SCR_TEXT_1172")
        global.msg[1] = scr_gettext("SCR_TEXT_1173")
        global.msg[2] = scr_gettext("SCR_TEXT_1174")
        if (global.flag[85] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1177")
        break
    case 255:
        global.msg[0] = scr_gettext("SCR_TEXT_1182")
        if (global.choice == 0 && global.flag[85] == 0)
        {
            global.interact = 1
            if instance_exists(obj_carrybird)
            {
                obj_carrybird.carry = 1
                with (obj_mainchara)
                    uncan = 1
            }
        }
        break
    case 256:
        if (global.flag[107] != 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1197")
            global.msg[1] = scr_gettext("SCR_TEXT_1198")
            global.msg[2] = scr_gettext("SCR_TEXT_1199")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1202")
        break
    case 257:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 0)
                for (i = 0; i < 8; i += 1)
                {
                    if (global.item[i] == 27)
                        noroom = 2
                }
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1221")
                global.flag[107] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1224")
            if (noroom == 2)
                global.msg[0] = scr_gettext("SCR_TEXT_1225")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1229")
        break
    case 258:
        if (global.flag[109] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1237")
            global.msg[1] = scr_gettext("SCR_TEXT_1238")
            global.msg[2] = scr_gettext("SCR_TEXT_1239")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1242")
        break
    case 259:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 36)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1258")
                global.flag[109] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1261")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1265")
        break
    case 260:
        if (global.flag[110] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1273")
            global.msg[1] = scr_gettext("SCR_TEXT_1274")
            global.msg[2] = scr_gettext("SCR_TEXT_1275")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1278")
        break
    case 261:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 47)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1294")
                global.flag[110] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1297")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1301")
        break
    case 262:
        if (global.flag[111] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1308")
            global.msg[1] = scr_gettext("SCR_TEXT_1309")
            global.msg[2] = scr_gettext("SCR_TEXT_1310")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1313")
        break
    case 263:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 46)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1329")
                global.flag[111] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1332")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1336")
        break
    case 264:
        if (global.flag[112] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1343")
            global.msg[1] = scr_gettext("SCR_TEXT_1344")
            global.msg[2] = scr_gettext("SCR_TEXT_1345")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1348")
        break
    case 265:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 40)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1363")
                global.flag[112] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1366")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1370")
        break
    case 266:
        if (global.flag[113] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1378")
            global.msg[1] = scr_gettext("SCR_TEXT_1379")
            global.msg[2] = scr_gettext("SCR_TEXT_1380")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1383")
        break
    case 267:
        if (global.choice == 0)
        {
            global.flag[113] = 1
            if (doak == 0)
                global.gold += 100
            doak = 1
            global.msg[0] = scr_gettext("SCR_TEXT_1393")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1397")
        break
    case 268:
        if (global.flag[114] == 0)
        {
            if (scr_murderlv() < 16)
                global.msg[0] = scr_gettext("SCR_TEXT_1406")
            else
                global.msg[0] = scr_gettext("SCR_TEXT_1408")
            global.msg[1] = scr_gettext("SCR_TEXT_1409")
            global.msg[2] = scr_gettext("SCR_TEXT_1410")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1413")
        break
    case 269:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                if (scr_murderlv() >= 16)
                    scr_itemget(52)
                else
                    script_execute(scr_itemget, 51)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1431")
                if (scr_murderlv() >= 16)
                    global.msg[0] = scr_gettext("SCR_TEXT_1432")
                global.flag[114] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1435")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1439")
        break
    case 270:
        if (global.flag[115] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1446")
            global.msg[1] = scr_gettext("SCR_TEXT_1447")
            global.msg[2] = scr_gettext("SCR_TEXT_1448")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1451")
        break
    case 271:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                if (scr_murderlv() < 16)
                    script_execute(scr_itemget, 50)
                else
                    script_execute(scr_itemget, 53)
            }
            if (noroom == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1470")
                if (scr_murderlv() >= 16)
                    global.msg[0] = scr_gettext("SCR_TEXT_1471")
                global.flag[115] = 1
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1474")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1478")
        break
    case 272:
        global.msg[0] = scr_gettext("SCR_TEXT_1483")
        break
    case 273:
        doak = 0
        noroom = 0
        if (global.flag[250] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1492")
            global.msg[1] = scr_gettext("SCR_TEXT_1493")
            global.msg[2] = scr_gettext("SCR_TEXT_1494")
            global.msg[3] = scr_gettext("SCR_TEXT_1495")
        }
        if (global.flag[250] >= 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1499")
            global.msg[1] = scr_gettext("SCR_TEXT_1500")
            global.msg[2] = scr_gettext("SCR_TEXT_1501")
            global.msg[3] = scr_gettext("SCR_TEXT_1502")
        }
        break
    case 274:
        script_execute(scr_cost, 12)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 17)
                    if (noroom == 0)
                    {
                        global.gold -= 12
                        global.flag[60] += 12
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1520")
            if (afford == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_1522")
        }
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1525")
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1528")
            global.msg[1] = scr_gettext("SCR_TEXT_1529")
        }
        break
    case 500:
        global.msg[0] = scr_gettext("SCR_TEXT_1535")
        global.msg[1] = scr_gettext("SCR_TEXT_1536")
        break
    case 501:
        global.msg[0] = scr_gettext("SCR_TEXT_1540")
        break
    case 502:
        global.msg[0] = scr_gettext("SCR_TEXT_1544")
        break
    case 503:
        global.msg[0] = scr_gettext("SCR_TEXT_1548")
        break
    case 504:
        global.msg[0] = scr_gettext("SCR_TEXT_1552")
        break
    case 505:
        conversation = obj_goofyrock.conversation
        if (global.flag[33] == 0)
        {
            if (conversation == 0)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = scr_gettext("SCR_TEXT_1562")
                global.msg[1] = scr_gettext("SCR_TEXT_1563")
                global.msg[2] = scr_gettext("SCR_TEXT_1564")
            }
            if (conversation == 3)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = scr_gettext("SCR_TEXT_1569")
                global.msg[1] = scr_gettext("SCR_TEXT_1570")
            }
            if (conversation == 6)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = scr_gettext("SCR_TEXT_1575")
                global.msg[1] = scr_gettext("SCR_TEXT_1576")
            }
            if (conversation == 9)
                global.msg[0] = scr_gettext("SCR_TEXT_1579")
            if (conversation == 12)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = scr_gettext("SCR_TEXT_1583")
                global.msg[1] = scr_gettext("SCR_TEXT_1584")
            }
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1589")
            if (global.flag[7] == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1592")
                global.msg[1] = scr_gettext("SCR_TEXT_1593")
            }
        }
        break
    case 508:
        doak = 0
        noroom = 0
        if (global.flag[34] < 4)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1603")
            if (global.flag[34] == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_1604")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_1606")
        global.msg[1] = scr_gettext("SCR_TEXT_1607")
        break
    case 509:
        if (global.choice == 0)
        {
            if (global.flag[34] < 4)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 1)
                    if (noroom == 0)
                        global.flag[34] += 1
                }
            }
            if (noroom == 0)
            {
                if (global.flag[34] == 1)
                    global.msg[0] = scr_gettext("SCR_TEXT_1624")
                if (global.flag[34] == 2)
                    global.msg[0] = scr_gettext("SCR_TEXT_1625")
                if (global.flag[34] == 3)
                    global.msg[0] = scr_gettext("SCR_TEXT_1626")
                if (global.flag[34] == 4)
                    global.msg[0] = scr_gettext("SCR_TEXT_1628")
                if (global.flag[34] == 3 && global.flag[6] == 1)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_1631")
                    global.flag[34] += 1
                }
            }
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1635")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1639")
        break
    case 510:
        global.msg[0] = scr_gettext("SCR_TEXT_1644")
        global.msg[1] = scr_gettext("SCR_TEXT_1645")
        global.msg[2] = scr_gettext("SCR_TEXT_1646")
        global.msg[3] = scr_gettext("SCR_TEXT_1647")
        global.msg[4] = scr_gettext("SCR_TEXT_1648")
        break
    case 511:
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
        }
        global.msg[0] = "%%"
        break
    case 512:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_1675")
        global.msg[1] = scr_gettext("SCR_TEXT_1676")
        break
    case 513:
        script_execute(scr_itemcheck, 5)
        if (global.choice == 0)
        {
            if (haveit == 0)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 5)
                }
            }
        }
        if (noroom == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_1692")
        if (haveit == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1694")
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1695")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1698")
        break
    case 514:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_1705")
        global.msg[1] = scr_gettext("SCR_TEXT_1706")
        break
    case 515:
        script_execute(scr_cost, 7)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 7)
                    if (noroom == 0)
                    {
                        global.gold -= 7
                        global.flag[59] += 7
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1723")
            if (afford == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_1724")
        }
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1726")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1729")
        break
    case 516:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_1736")
        global.msg[1] = scr_gettext("SCR_TEXT_1737")
        break
    case 517:
        script_execute(scr_cost, 18)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 10)
                    if (noroom == 0)
                    {
                        global.gold -= 18
                        global.flag[59] += 18
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1754")
            if (afford == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_1755")
        }
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1757")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1760")
        break
    case 518:
        if (doak == 0)
        {
            script_execute(scr_itemget, 12)
            if (noroom == 0)
                global.flag[100] = 1
            doak = 1
        }
        global.msg[0] = scr_gettext("SCR_TEXT_1771")
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1772")
        break
    case 519:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_1778")
        global.msg[1] = scr_gettext("SCR_TEXT_1779")
        break
    case 520:
        if (doak == 0)
        {
            global.flag[43] += 1
            doak = 1
        }
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_1788")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1789")
        if (global.flag[43] > 25)
            global.msg[0] = scr_gettext("SCR_TEXT_1790")
        break
    case 521:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_1796")
        global.msg[1] = scr_gettext("SCR_TEXT_1797")
        break
    case 522:
        if (doak == 0)
        {
            global.flag[43] += 1
            doak = 1
        }
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1806")
            if (room == room_ruins15B && global.plot < 14)
            {
                global.plot = 14
                snd_play(snd_switchpull_n)
            }
            if (room == room_ruins15C && global.plot < 15)
            {
                global.plot = 15
                snd_play(snd_switchpull_n)
            }
            if (room == room_ruins15D && global.plot < 16)
            {
                global.plot = 16
                snd_play(snd_switchpull_n)
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1807")
        break
    case 523:
        if (doak == 0)
        {
            script_execute(scr_itemget, 13)
            if (noroom == 0)
                global.flag[102] = 1
            doak = 1
        }
        global.msg[0] = scr_gettext("SCR_TEXT_1817")
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1818")
        break
    case 524:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_1824")
        global.msg[1] = scr_gettext("SCR_TEXT_1825")
        break
    case 525:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1830")
            global.msg[1] = scr_gettext("SCR_TEXT_1831")
            global.msg[2] = scr_gettext("SCR_TEXT_1832")
            global.msg[3] = scr_gettext("SCR_TEXT_1833")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1835")
        break
    case 526:
        if (global.flag[6] == 0)
        {
            if (doak == 0)
            {
                script_execute(scr_itemget, 11)
                if (noroom == 0)
                    global.flag[103] = 2
                doak = 1
            }
            global.msg[0] = scr_gettext("SCR_TEXT_1842")
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1843")
        }
        else
        {
            if (doak == 0)
            {
                script_execute(scr_itemget, 63)
                if (noroom == 0)
                    global.flag[103] = 2
                doak = 1
            }
            global.msg[0] = scr_gettext("SCR_TEXT_1853")
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_1854")
        }
        break
    case 527:
        global.msg[0] = scr_gettext("SCR_TEXT_1859")
        global.msg[1] = scr_gettext("SCR_TEXT_1860")
        global.msg[2] = scr_gettext("SCR_TEXT_1861")
        global.msg[3] = scr_gettext("SCR_TEXT_1862")
        break
    case 528:
        global.plot = 19.1
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_1867")
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1870")
            if (global.flag[103] > 0)
                global.msg[0] = scr_gettext("SCR_TEXT_1871")
            global.msg[1] = scr_gettext("SCR_TEXT_1872")
            global.msg[2] = scr_gettext("SCR_TEXT_1873")
            global.msg[3] = scr_gettext("SCR_TEXT_1874")
            global.msg[4] = scr_gettext("SCR_TEXT_1875")
            global.msg[5] = scr_gettext("SCR_TEXT_1876")
            global.msg[6] = scr_gettext("SCR_TEXT_1877")
            global.msg[7] = scr_gettext("SCR_TEXT_1878")
            global.msg[8] = scr_gettext("SCR_TEXT_1879")
            global.msg[9] = scr_gettext("SCR_TEXT_1880")
            global.msg[10] = scr_gettext("SCR_TEXT_1881")
            global.msg[11] = scr_gettext("SCR_TEXT_1882")
            global.msg[12] = scr_gettext("SCR_TEXT_1883")
        }
        break
    case 529:
        global.plot = 19.2
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_1891")
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1895")
            global.msg[1] = scr_gettext("SCR_TEXT_1896")
            if (global.choice == -1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1897")
                global.msg[1] = scr_gettext("SCR_TEXT_1898")
            }
            global.msg[2] = scr_gettext("SCR_TEXT_1899")
            global.msg[3] = scr_gettext("SCR_TEXT_1900")
            global.msg[4] = scr_gettext("SCR_TEXT_1901")
        }
        break
    case 530:
        global.plot = 19.3
        global.msg[0] = scr_gettext("SCR_TEXT_1907")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_1908")
        global.msg[1] = scr_gettext("SCR_TEXT_1909")
        r = round(random(3))
        if (r == 0)
            global.msg[2] = scr_gettext("SCR_TEXT_1911")
        if (r == 1)
            global.msg[2] = scr_gettext("SCR_TEXT_1912")
        if (r == 2)
            global.msg[2] = scr_gettext("SCR_TEXT_1913")
        if (r == 3)
            global.msg[2] = scr_gettext("SCR_TEXT_1914")
        global.msg[3] = scr_gettext("SCR_TEXT_1915")
        global.msg[4] = scr_gettext("SCR_TEXT_1916")
        break
    case 531:
        global.plot = 19.4
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_1921")
        else
        {
            if (global.choice == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_1924")
                global.plot = 19.9
                with (obj_mainchara)
                    uncan = 1
            }
            if (global.choice == -1)
                global.msg[0] = scr_gettext("SCR_TEXT_1927")
        }
        global.msg[1] = scr_gettext("SCR_TEXT_1930")
        break
    case 532:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_1934")
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_1935")
            global.plot = 19.9
            with (obj_mainchara)
                uncan = 1
        }
        global.msg[1] = scr_gettext("SCR_TEXT_1936")
        break
    case 540:
        global.msg[0] = scr_gettext("SCR_TEXT_1940")
        global.msg[1] = scr_gettext("SCR_TEXT_1941")
        global.msg[2] = scr_gettext("SCR_TEXT_1942")
        global.msg[3] = scr_gettext("SCR_TEXT_1943")
        global.msg[4] = scr_gettext("SCR_TEXT_1944")
        global.msg[5] = scr_gettext("SCR_TEXT_1945")
        global.msg[6] = scr_gettext("SCR_TEXT_1946")
        global.msg[7] = scr_gettext("SCR_TEXT_1947")
        if (scr_murderlv() >= 7)
        {
            if instance_exists(obj_papyrus8)
            {
                if (obj_papyrus8.murder == 1)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_1956")
                    global.msg[1] = scr_gettext("SCR_TEXT_1957")
                    global.msg[2] = scr_gettext("SCR_TEXT_1958")
                    global.msg[3] = scr_gettext("SCR_TEXT_1959")
                    global.msg[4] = scr_gettext("SCR_TEXT_1960")
                    global.msg[5] = scr_gettext("SCR_TEXT_1961")
                    global.msg[6] = scr_gettext("SCR_TEXT_1962")
                    global.msg[7] = scr_gettext("SCR_TEXT_1963")
                    global.msg[8] = scr_gettext("SCR_TEXT_1964")
                    global.msg[9] = scr_gettext("SCR_TEXT_1965")
                    global.msg[10] = scr_gettext("SCR_TEXT_1966")
                    global.msg[11] = scr_gettext("SCR_TEXT_1967")
                    global.msg[12] = scr_gettext("SCR_TEXT_1968")
                }
            }
        }
        break
    case 541:
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_1978")
            global.msg[2] = scr_gettext("SCR_TEXT_1979")
            global.msg[3] = scr_gettext("SCR_TEXT_1980")
            global.msg[4] = scr_gettext("SCR_TEXT_1981")
            global.msg[5] = scr_gettext("SCR_TEXT_1982")
        }
        if (global.choice == 1)
        {
            scr_papface(0, 3)
            global.msg[1] = scr_gettext("SCR_TEXT_1987")
            global.msg[2] = scr_gettext("SCR_TEXT_1988")
            global.msg[3] = scr_gettext("SCR_TEXT_1989")
            global.msg[4] = scr_gettext("SCR_TEXT_1990")
            global.msg[5] = scr_gettext("SCR_TEXT_1991")
        }
        if (global.flag[66] == 1)
        {
            global.msg[6] = scr_gettext("SCR_TEXT_1996")
            global.msg[7] = scr_gettext("SCR_TEXT_1997")
            global.msg[8] = scr_gettext("SCR_TEXT_1998")
        }
        else
        {
            global.msg[6] = scr_gettext("SCR_TEXT_2002")
            global.msg[7] = scr_gettext("SCR_TEXT_2003")
            global.msg[8] = scr_gettext("SCR_TEXT_2004")
        }
        global.msg[9] = scr_gettext("SCR_TEXT_2006")
        global.msg[10] = scr_gettext("SCR_TEXT_2007")
        global.msg[11] = scr_gettext("SCR_TEXT_2008")
        global.msg[12] = scr_gettext("SCR_TEXT_2009")
        global.msg[13] = scr_gettext("SCR_TEXT_2010")
        global.msg[14] = scr_gettext("SCR_TEXT_2011")
        global.msg[15] = scr_gettext("SCR_TEXT_2012")
        global.msg[16] = scr_gettext("SCR_TEXT_2013")
        global.msg[17] = scr_gettext("SCR_TEXT_2014")
        global.msg[18] = scr_gettext("SCR_TEXT_2015")
        global.msg[19] = scr_gettext("SCR_TEXT_2016")
        global.msg[20] = scr_gettext("SCR_TEXT_2017")
        global.msg[21] = scr_gettext("SCR_TEXT_2018")
        break
    case 544:
        global.msg[0] = scr_gettext("SCR_TEXT_2023")
        global.msg[1] = scr_gettext("SCR_TEXT_2024")
        global.msg[2] = scr_gettext("SCR_TEXT_2025")
        global.msg[3] = scr_gettext("SCR_TEXT_2026")
        global.msg[4] = scr_gettext("SCR_TEXT_2027")
        global.msg[5] = scr_gettext("SCR_TEXT_2028")
        global.msg[6] = scr_gettext("SCR_TEXT_2029")
        global.msg[6] = scr_gettext("SCR_TEXT_2030")
        global.msg[7] = scr_gettext("SCR_TEXT_2031")
        global.msg[8] = scr_gettext("SCR_TEXT_2032")
        break
    case 545:
        global.msg[0] = scr_gettext("SCR_TEXT_2036")
        if (global.choice == 0)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_2039")
            global.msg[2] = scr_gettext("SCR_TEXT_2040")
            global.msg[3] = scr_gettext("SCR_TEXT_2041")
            global.msg[4] = scr_gettext("SCR_TEXT_2042")
            obj_papyrus8.conversation = 14
        }
        if (global.choice == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_2047")
            global.msg[2] = scr_gettext("SCR_TEXT_2048")
            obj_papyrus8.conversation = 17
            obj_fogmaker.s = 1
            global.flag[67] = 0
            global.flag[68] = 1
        }
        break
    case 547:
        global.msg[0] = scr_gettext("SCR_TEXT_2058")
        global.msg[1] = scr_gettext("SCR_TEXT_2059")
        if (global.flag[72] == 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2062")
            global.msg[1] = scr_gettext("SCR_TEXT_2063")
        }
        global.msg[2] = scr_gettext("SCR_TEXT_2065")
        if (obj_townnpc_innlady.jtext == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_2069")
        if (global.flag[7] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2074")
            global.msg[1] = scr_gettext("SCR_TEXT_2075")
            if (global.flag[72] == 2)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2078")
                global.msg[1] = scr_gettext("SCR_TEXT_2079")
                global.msg[2] = scr_gettext("SCR_TEXT_2080")
                global.msg[3] = scr_gettext("SCR_TEXT_2081")
            }
        }
        break
    case 548:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2089")
            if (global.flag[72] == 2)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2092")
                obj_townnpc_innlady.conversation = 2
                with (obj_townnpc_innlady)
                    jtext = 1
            }
            if (global.gold < 80 && global.flag[72] == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2098")
                global.msg[1] = scr_gettext("SCR_TEXT_2099")
                global.msg[2] = scr_gettext("SCR_TEXT_2100")
                global.msg[3] = scr_gettext("SCR_TEXT_2101")
                obj_townnpc_innlady.conversation = 2
                with (obj_townnpc_innlady)
                    jtext = 1
                global.flag[72] = 2
            }
            if (global.gold >= 80)
            {
                if (global.flag[72] == 0 || global.flag[72] == 1)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_2109")
                    obj_townnpc_innlady.conversation = 2
                    with (obj_townnpc_innlady)
                        jtext = 1
                    global.flag[72] = 1
                }
            }
            if (global.gold < 80 && global.flag[72] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2116")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_2121")
        break
    case 549:
        global.msg[0] = scr_gettext("SCR_TEXT_2125")
        global.msg[1] = scr_gettext("SCR_TEXT_2126")
        break
    case 550:
        global.msg[0] = scr_gettext("SCR_TEXT_2130")
        global.msg[1] = scr_gettext("SCR_TEXT_2131")
        if (global.choice == 0)
        {
            if (instance_exists(obj_starchecker) == 0)
                instance_create(view_xview[0], view_yview[0], obj_starchecker)
        }
        break
    case 551:
        if (obj_mainchara.dsprite != spr_maincharad_pranked)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2139")
            global.msg[1] = scr_gettext("SCR_TEXT_2140")
            global.msg[2] = scr_gettext("SCR_TEXT_2141")
            global.msg[3] = scr_gettext("SCR_TEXT_2142")
            global.msg[4] = scr_gettext("SCR_TEXT_2143")
            global.msg[5] = scr_gettext("SCR_TEXT_2144")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2148")
            global.msg[1] = scr_gettext("SCR_TEXT_2149")
            global.msg[2] = scr_gettext("SCR_TEXT_2150")
        }
        global.msg[6] = scr_gettext("SCR_TEXT_2152")
        break
    case 552:
        global.msg[0] = scr_gettext("SCR_TEXT_2157")
        global.msg[1] = scr_gettext("SCR_TEXT_2158")
        if (global.choice == 0)
        {
            if (instance_exists(obj_starchecker) == 0)
                instance_create(view_xview[0], view_yview[0], obj_starchecker)
            obj_mainchara.dsprite = spr_maincharad_pranked
            obj_mainchara.lsprite = spr_maincharal_pranked
        }
        else
        {
            scr_sansface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_2169")
        }
        break
    case 553:
        armor = scr_gettext("papyrus_armor_0")
        if (global.armor == 4)
            armor = scr_gettext("papyrus_armor_4")
        if (global.armor == 12)
            armor = scr_gettext("papyrus_armor_12")
        if (global.armor == 15)
            armor = scr_gettext("papyrus_armor_15")
        if (global.armor == 24)
            armor = scr_gettext("papyrus_armor_24")
        global.flag[75] = global.armor
        global.msg[0] = scr_gettext("SCR_TEXT_2180", armor)
        scr_papface(1, 0)
        global.msg[2] = scr_gettext("SCR_TEXT_2182", armor)
        global.msg[3] = scr_gettext("SCR_TEXT_2183", armor)
        global.msg[4] = scr_gettext("SCR_TEXT_2184", armor)
        global.msg[5] = scr_gettext("SCR_TEXT_2185", armor)
        global.msg[6] = scr_gettext("SCR_TEXT_2186", armor)
        global.msg[7] = scr_gettext("SCR_TEXT_2187", armor)
        global.msg[8] = scr_gettext("SCR_TEXT_2188", armor)
        global.msg[9] = scr_gettext("SCR_TEXT_2189", armor)
        global.msg[10] = scr_gettext("SCR_TEXT_2190", armor)
        global.msg[11] = scr_gettext("SCR_TEXT_2191", armor)
        global.msg[12] = scr_gettext("SCR_TEXT_2192", armor)
        global.msg[13] = scr_gettext("SCR_TEXT_2193", armor)
        break
    case 554:
        armor = scr_gettext("papyrus_armor_0")
        if (global.armor == 4)
            armor = scr_gettext("papyrus_armor_4")
        if (global.armor == 12)
            armor = scr_gettext("papyrus_armor_12")
        if (global.armor == 15)
            armor = scr_gettext("papyrus_armor_15")
        if (global.armor == 24)
            armor = scr_gettext("papyrus_armor_24")
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.flag[76] = 0
            global.msg[1] = scr_gettext("SCR_TEXT_2206", armor)
            global.msg[2] = scr_gettext("SCR_TEXT_2207", armor)
            global.msg[3] = scr_gettext("SCR_TEXT_2208", armor)
            global.msg[4] = scr_gettext("SCR_TEXT_2209", armor)
            global.msg[5] = scr_gettext("SCR_TEXT_2210", armor)
        }
        else
        {
            global.flag[76] = 1
            global.msg[1] = scr_gettext("SCR_TEXT_2215", armor)
            global.msg[2] = scr_gettext("SCR_TEXT_2216", armor)
            global.msg[3] = scr_gettext("SCR_TEXT_2217", armor)
            global.msg[4] = scr_gettext("SCR_TEXT_2218", armor)
            global.msg[5] = scr_gettext("SCR_TEXT_2219", armor)
            global.msg[6] = scr_gettext("SCR_TEXT_2220", armor)
        }
        break
    case 556:
        global.msg[0] = scr_gettext("SCR_TEXT_2227")
        if instance_exists(obj_papyrusparent)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_2231")
            global.msg[2] = scr_gettext("SCR_TEXT_2232")
            global.msg[3] = scr_gettext("SCR_TEXT_2233")
            global.msg[4] = scr_gettext("SCR_TEXT_2234")
            global.msg[5] = scr_gettext("SCR_TEXT_2235")
        }
        break
    case 557:
        global.msg[0] = scr_gettext("SCR_TEXT_2240")
        global.msg[1] = scr_gettext("SCR_TEXT_2241")
        global.msg[2] = scr_gettext("SCR_TEXT_2242")
        break
    case 558:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2248")
            global.msg[1] = scr_gettext("SCR_TEXT_2249")
            global.msg[2] = scr_gettext("SCR_TEXT_2250")
            global.msg[3] = scr_gettext("SCR_TEXT_2251")
            global.msg[4] = scr_gettext("SCR_TEXT_2252")
            global.msg[5] = scr_gettext("SCR_TEXT_2253")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_2257")
        break
    case 559:
        if instance_exists(obj_papyrusparent)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_2265")
            global.msg[2] = scr_gettext("SCR_TEXT_2266")
            global.msg[3] = scr_gettext("SCR_TEXT_2267")
            global.msg[4] = scr_gettext("SCR_TEXT_2268")
            if (global.flag[66] == 1)
                global.msg[4] = scr_gettext("SCR_TEXT_2269")
            global.msg[5] = scr_gettext("SCR_TEXT_2270")
            global.msg[6] = scr_gettext("SCR_TEXT_2271")
            global.msg[7] = scr_gettext("SCR_TEXT_2272")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2276")
            global.msg[1] = scr_gettext("SCR_TEXT_2277")
            global.msg[2] = scr_gettext("SCR_TEXT_2278")
            global.msg[3] = scr_gettext("SCR_TEXT_2279")
            global.msg[4] = scr_gettext("SCR_TEXT_2280")
            global.msg[5] = scr_gettext("SCR_TEXT_2281")
        }
        break
    case 560:
        global.msg[0] = scr_gettext("SCR_TEXT_2286")
        if (global.choice == 0)
        {
            with (obj_papdoor)
                event_user(2)
        }
        break
    case 561:
        if instance_exists(obj_papyrusparent)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_2297")
            global.msg[2] = scr_gettext("SCR_TEXT_2298")
            global.msg[3] = scr_gettext("SCR_TEXT_2299")
            global.msg[4] = scr_gettext("SCR_TEXT_2300")
            global.msg[5] = scr_gettext("SCR_TEXT_2301")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2305")
            global.msg[1] = scr_gettext("SCR_TEXT_2306")
        }
        break
    case 562:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_2313")
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2317")
            if instance_exists(obj_papyrusparent)
            {
                scr_papface(0, 0)
                global.msg[1] = scr_gettext("SCR_TEXT_2322")
            }
        }
        break
    case 563:
        global.msg[0] = scr_gettext("SCR_TEXT_2328")
        global.msg[1] = scr_gettext("SCR_TEXT_2329")
        global.msg[2] = scr_gettext("SCR_TEXT_2330")
        global.msg[3] = scr_gettext("SCR_TEXT_2331")
        global.msg[4] = scr_gettext("SCR_TEXT_2332")
        global.msg[5] = scr_gettext("SCR_TEXT_2333")
        if (global.flag[66] == 1)
        {
            global.msg[2] = scr_gettext("SCR_TEXT_2336")
            global.msg[3] = scr_gettext("SCR_TEXT_2337")
            global.msg[4] = scr_gettext("SCR_TEXT_2338")
            global.msg[5] = scr_gettext("SCR_TEXT_2339")
        }
        break
    case 564:
        if (global.choice == 0)
        {
            obj_papyrus_hisroom.intro = 4
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_2348")
            if (global.flag[66] == 0)
                global.msg[1] = scr_gettext("SCR_TEXT_2349")
        }
        else
        {
            scr_papface(0, 2)
            global.msg[1] = scr_gettext("SCR_TEXT_2354")
        }
        break
    case 565:
        global.msg[0] = scr_gettext("SCR_TEXT_2359")
        global.msg[1] = scr_gettext("SCR_TEXT_2360")
        global.msg[2] = scr_gettext("SCR_TEXT_2361")
        break
    case 566:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_2366")
        else
            global.msg[0] = scr_gettext("SCR_TEXT_2368")
        break
    case 567:
        global.msg[0] = scr_gettext("SCR_TEXT_2372")
        global.msg[1] = scr_gettext("SCR_TEXT_2373")
        global.msg[2] = scr_gettext("SCR_TEXT_2374")
        global.msg[3] = scr_gettext("SCR_TEXT_2375")
        if (global.flag[67] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_2376")
        if (scr_murderlv() >= 7)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2380")
            global.msg[1] = scr_gettext("SCR_TEXT_2381")
            global.msg[2] = scr_gettext("SCR_TEXT_2382")
        }
        break
    case 568:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2389")
            if instance_exists(obj_sans_sentry2)
                obj_sans_sentry2.con = 1
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_2394")
        break
    case 570:
        global.msg[0] = scr_gettext("SCR_TEXT_2399")
        global.msg[1] = scr_gettext("SCR_TEXT_2400")
        global.msg[2] = scr_gettext("SCR_TEXT_2401")
        global.msg[3] = scr_gettext("SCR_TEXT_2402")
        global.msg[4] = scr_gettext("SCR_TEXT_2403")
        break
    case 571:
        global.msg[0] = scr_gettext("SCR_TEXT_2407")
        if (global.choice == 0)
        {
            global.flag[391] = 1
            global.msg[0] = scr_gettext("SCR_TEXT_2411")
            global.msg[1] = scr_gettext("SCR_TEXT_2412")
            obj_grillbynpc_sansdate.burg = 0
        }
        if (global.choice == 1)
        {
            global.flag[391] = 2
            global.msg[0] = scr_gettext("SCR_TEXT_2418")
            global.msg[1] = scr_gettext("SCR_TEXT_2419")
            obj_grillbynpc_sansdate.burg = 1
        }
        break
    case 572:
        global.msg[0] = scr_gettext("SCR_TEXT_2426")
        global.msg[1] = scr_gettext("SCR_TEXT_2427")
        global.msg[2] = scr_gettext("SCR_TEXT_2428")
        break
    case 573:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2434")
            global.msg[1] = scr_gettext("SCR_TEXT_2435")
            global.msg[2] = scr_gettext("SCR_TEXT_2436")
            global.msg[3] = scr_gettext("SCR_TEXT_2437")
            global.msg[4] = scr_gettext("SCR_TEXT_2438")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2442")
            global.msg[1] = scr_gettext("SCR_TEXT_2443")
            global.msg[2] = scr_gettext("SCR_TEXT_2444")
            global.msg[3] = scr_gettext("SCR_TEXT_2445")
            global.msg[4] = scr_gettext("SCR_TEXT_2446")
            global.msg[5] = scr_gettext("SCR_TEXT_2447")
            global.msg[6] = scr_gettext("SCR_TEXT_2448")
        }
        break
    case 574:
        global.msg[0] = scr_gettext("SCR_TEXT_2453")
        global.msg[1] = scr_gettext("SCR_TEXT_2454")
        global.msg[2] = scr_gettext("SCR_TEXT_2455")
        break
    case 575:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_2461")
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2465")
            if instance_exists(obj_grillbynpc_sansdate)
                obj_grillbynpc_sansdate.burg = 2
        }
        break
    case 576:
        global.msg[0] = scr_gettext("SCR_TEXT_2472")
        global.msg[1] = scr_gettext("SCR_TEXT_2473")
        break
    case 577:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_2477")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_2478")
        global.msg[1] = scr_gettext("SCR_TEXT_2479")
        global.msg[2] = scr_gettext("SCR_TEXT_2480")
        global.msg[3] = scr_gettext("SCR_TEXT_2481")
        global.msg[4] = scr_gettext("SCR_TEXT_2482")
        global.msg[5] = scr_gettext("SCR_TEXT_2483")
        global.msg[6] = scr_gettext("SCR_TEXT_2484")
        global.msg[7] = scr_gettext("SCR_TEXT_2485")
        global.msg[8] = scr_gettext("SCR_TEXT_2486")
        global.msg[9] = scr_gettext("SCR_TEXT_2487")
        global.msg[10] = scr_gettext("SCR_TEXT_2488")
        global.msg[11] = scr_gettext("SCR_TEXT_2489")
        global.msg[12] = scr_gettext("SCR_TEXT_2490")
        global.msg[13] = scr_gettext("SCR_TEXT_2491")
        break
    case 578:
        global.msg[0] = scr_gettext("SCR_TEXT_2495")
        global.msg[1] = scr_gettext("SCR_TEXT_2496")
        global.msg[2] = scr_gettext("SCR_TEXT_2497")
        global.msg[3] = scr_gettext("SCR_TEXT_2498")
        break
    case 579:
        global.msg[0] = scr_gettext("SCR_TEXT_2502")
        break
    case 580:
        if (global.flag[84] == 4)
            global.msg[0] = scr_gettext("SCR_TEXT_2506")
        if (global.flag[84] == 5)
            global.msg[0] = scr_gettext("SCR_TEXT_2507")
        if (global.flag[84] < 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2510")
            global.msg[1] = scr_gettext("SCR_TEXT_2511")
            global.msg[2] = scr_gettext("SCR_TEXT_2512")
            global.msg[3] = scr_gettext("SCR_TEXT_2513")
            global.msg[4] = scr_gettext("SCR_TEXT_2514")
        }
        if (global.flag[84] == 2)
            global.msg[0] = scr_gettext("SCR_TEXT_2518")
        if (global.flag[84] == 3)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2522")
            global.msg[1] = scr_gettext("SCR_TEXT_2523")
            global.msg[2] = scr_gettext("SCR_TEXT_2524")
        }
        break
    case 581:
        if (global.flag[84] < 3)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2535")
                global.msg[1] = scr_gettext("SCR_TEXT_2536")
                global.msg[2] = scr_gettext("SCR_TEXT_2537")
                global.msg[3] = scr_gettext("SCR_TEXT_2538")
                global.msg[4] = scr_gettext("SCR_TEXT_2539")
                global.msg[5] = scr_gettext("SCR_TEXT_2540")
                global.msg[6] = scr_gettext("SCR_TEXT_2541")
                global.msg[7] = scr_gettext("SCR_TEXT_2542")
                global.msg[8] = scr_gettext("SCR_TEXT_2543")
                global.msg[9] = scr_gettext("SCR_TEXT_2544")
                if (doak == 0)
                {
                    if (global.flag[84] == 0)
                        global.flag[84] = 2
                    doak = 1
                }
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2553")
        }
        else
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2561")
                global.msg[1] = scr_gettext("SCR_TEXT_2562")
                global.flag[84] = 4
            }
            if (global.choice == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2567")
                global.msg[1] = scr_gettext("SCR_TEXT_2568")
                global.msg[2] = scr_gettext("SCR_TEXT_2569")
                global.msg[3] = scr_gettext("SCR_TEXT_2570")
                global.msg[4] = scr_gettext("SCR_TEXT_2571")
                global.flag[84] = 5
            }
        }
        break
    case 583:
        if (global.flag[85] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2580")
            global.msg[1] = scr_gettext("SCR_TEXT_2581")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2585")
            global.msg[1] = scr_gettext("SCR_TEXT_2586")
        }
        break
    case 584:
        if (doak == 0)
        {
            if (global.flag[85] == 0)
            {
                if (global.choice == 0)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_2597")
                    global.flag[85] = 1
                    if instance_exists(obj_umbrellabox)
                        obj_umbrellabox.image_index = 1
                    obj_mainchara.dsprite = spr_maincharad_umbrella
                    obj_mainchara.rsprite = spr_maincharar_umbrella
                    obj_mainchara.lsprite = spr_maincharal_umbrella
                    obj_mainchara.usprite = spr_maincharau_umbrella
                }
                else
                    global.msg[0] = scr_gettext("SCR_TEXT_2608")
            }
            else if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2615")
                global.flag[85] = 0
                if instance_exists(obj_umbrellabox)
                    obj_umbrellabox.image_index = 0
                obj_mainchara.dsprite = spr_maincharad
                obj_mainchara.rsprite = spr_maincharar
                obj_mainchara.usprite = spr_maincharau
                obj_mainchara.lsprite = spr_maincharal
            }
            else
                global.msg[0] = scr_gettext("SCR_TEXT_2626")
            doak = 1
        }
        break
    case 585:
        global.msg[0] = scr_gettext("SCR_TEXT_2634")
        if (global.flag[85] == 1 && global.flag[86] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2637")
            global.msg[1] = scr_gettext("SCR_TEXT_2638")
        }
        if (global.flag[86] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_2642")
        break
    case 586:
        if (doak == 0)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2651")
                global.msg[1] = scr_gettext("SCR_TEXT_2652")
                global.flag[85] = 0
                global.flag[86] = 1
                if instance_exists(obj_musicstatue)
                {
                    obj_musicstatue.image_index = 1
                    obj_musicstatue.con = 1
                }
                obj_mainchara.dsprite = spr_maincharad
                obj_mainchara.rsprite = spr_maincharar
                obj_mainchara.usprite = spr_maincharau
                obj_mainchara.lsprite = spr_maincharal
            }
            else
                global.msg[0] = scr_gettext("SCR_TEXT_2667")
            doak = 1
        }
        break
    case 587:
        global.msg[0] = scr_gettext("SCR_TEXT_2674")
        global.msg[1] = scr_gettext("SCR_TEXT_2675")
        global.msg[2] = scr_gettext("SCR_TEXT_2676")
        break
    case 588:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2682")
            global.msg[1] = scr_gettext("SCR_TEXT_2683")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_2686")
        break
    case 589:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_2692")
        if (global.flag[108] > 2)
            global.msg[0] = scr_gettext("SCR_TEXT_2694")
        if (global.flag[108] == 1 || global.flag[108] == 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2698")
            global.msg[1] = scr_gettext("SCR_TEXT_2699")
        }
        if (global.flag[108] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2704")
            global.msg[1] = scr_gettext("SCR_TEXT_2705")
            global.msg[2] = scr_gettext("SCR_TEXT_2706")
            global.msg[3] = scr_gettext("SCR_TEXT_2707")
            global.flag[108] = 1
        }
        break
    case 590:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = 0
                doak = 1
                script_execute(scr_itemget, 35)
                if (noroom == 0)
                    global.flag[108] += 1
            }
            if (noroom == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_2726")
            if (noroom == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2728")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_2732")
        break
    case 591:
        if (global.flag[355] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2739")
            global.msg[1] = scr_gettext("SCR_TEXT_2740")
            global.msg[2] = scr_gettext("SCR_TEXT_2741")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_2744")
        break
    case 592:
        if (global.choice == 0)
        {
            if (global.lv == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2752")
                global.msg[1] = scr_gettext("SCR_TEXT_2753")
            }
            if (global.lv > 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2757")
                global.msg[1] = scr_gettext("SCR_TEXT_2758")
            }
            if (global.lv > 4)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2762")
                global.msg[1] = scr_gettext("SCR_TEXT_2763")
            }
            if (global.lv > 7)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2767")
                global.msg[1] = scr_gettext("SCR_TEXT_2768")
            }
            global.flag[355] = 1
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2774")
            global.flag[355] = 2
        }
        break
    case 593:
        global.msg[0] = scr_gettext("SCR_TEXT_2780")
        global.msg[1] = scr_gettext("SCR_TEXT_2781")
        global.msg[2] = scr_gettext("SCR_TEXT_2782")
        break
    case 594:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2788")
            global.msg[1] = scr_gettext("SCR_TEXT_2789")
        }
        if (global.choice == 1)
            global.msg[0] = "%%"
        break
    case 595:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 596:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 597:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 598:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 599:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 600:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 601:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 606:
        global.msg[0] = scr_gettext("SCR_TEXT_2837")
        global.msg[1] = scr_gettext("SCR_TEXT_2838")
        global.msg[2] = scr_gettext("SCR_TEXT_2839")
        if (global.flag[94] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2842")
            global.msg[1] = scr_gettext("SCR_TEXT_2843")
            global.msg[2] = scr_gettext("SCR_TEXT_2844")
        }
        break
    case 607:
        if (global.flag[94] != 1)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2853")
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 1
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2859")
        }
        if (global.flag[94] == 1)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2867")
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 2
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2873")
        }
        break
    case 608:
        global.msg[0] = scr_gettext("SCR_TEXT_2881")
        global.msg[1] = scr_gettext("SCR_TEXT_2882")
        global.msg[2] = scr_gettext("SCR_TEXT_2883")
        if (global.flag[94] == 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2886")
            global.msg[1] = scr_gettext("SCR_TEXT_2887")
            global.msg[2] = scr_gettext("SCR_TEXT_2888")
        }
        break
    case 609:
        if (global.flag[94] != 2)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2897")
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 3
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2903")
        }
        if (global.flag[94] == 2)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2911")
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 2
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2917")
        }
        break
    case 610:
        global.msg[0] = scr_gettext("SCR_TEXT_2924")
        global.msg[1] = scr_gettext("SCR_TEXT_2925")
        global.msg[2] = scr_gettext("SCR_TEXT_2926")
        if (global.flag[94] == 3)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_2929")
            global.msg[1] = scr_gettext("SCR_TEXT_2930")
            global.msg[2] = scr_gettext("SCR_TEXT_2931")
        }
        break
    case 611:
        if (global.flag[94] != 3)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2940")
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 4
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2946")
        }
        if (global.flag[94] == 3)
        {
            if (global.choice == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2954")
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 2
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_2960")
        }
        break
    case 612:
        global.msg[0] = scr_gettext("SCR_TEXT_2968")
        global.msg[1] = scr_gettext("SCR_TEXT_2969")
        break
    case 613:
        if (global.choice == 0)
        {
            if (global.flag[93] < 2)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_2977")
                if instance_exists(obj_napstablookdate)
                    obj_napstablookdate.con = 11
            }
            else
                global.msg[0] = scr_gettext("SCR_TEXT_2983")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_2988")
        break
    case 615:
        global.msg[0] = scr_gettext("SCR_TEXT_2993")
        global.msg[1] = scr_gettext("SCR_TEXT_2994")
        global.msg[2] = scr_gettext("SCR_TEXT_2995")
        break
    case 616:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3001")
            global.msg[1] = scr_gettext("SCR_TEXT_3002")
            global.msg[2] = scr_gettext("SCR_TEXT_3003")
            global.msg[3] = scr_gettext("SCR_TEXT_3004")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3008")
        break
    case 617:
        global.msg[0] = scr_gettext("SCR_TEXT_3014")
        global.msg[1] = scr_gettext("SCR_TEXT_3015")
        global.msg[2] = scr_gettext("SCR_TEXT_3016")
        global.msg[3] = scr_gettext("SCR_TEXT_3017")
        break
    case 618:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_3023")
        if (global.choice == 1)
        {
            global.flag[93] = 9
            global.msg[0] = scr_gettext("SCR_TEXT_3028")
            obj_napstablookdate.con = 80
        }
        break
    case 619:
        global.msg[0] = scr_gettext("SCR_TEXT_3034")
        global.msg[1] = scr_gettext("SCR_TEXT_3035")
        global.msg[2] = scr_gettext("SCR_TEXT_3036")
        global.msg[3] = scr_gettext("SCR_TEXT_3037")
        break
    case 620:
        if (global.choice == 0)
        {
            if (global.gold == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_3045")
                global.msg[1] = scr_gettext("SCR_TEXT_3046")
                global.msg[2] = scr_gettext("SCR_TEXT_3047")
                global.msg[3] = scr_gettext("SCR_TEXT_3048")
            }
            if (global.gold < 10 && global.gold > 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_3052")
                global.msg[1] = scr_gettext("SCR_TEXT_3053")
                global.msg[2] = scr_gettext("SCR_TEXT_3054")
                global.msg[3] = scr_gettext("SCR_TEXT_3055")
                global.gold = 0
            }
            if (global.gold >= 10)
            {
                global.gold -= 10
                global.msg[0] = scr_gettext("SCR_TEXT_3061")
                global.msg[1] = scr_gettext("SCR_TEXT_3062")
            }
            obj_napstablook_farm2.con = 1
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3068")
        break
    case 621:
        global.msg[0] = scr_gettext("SCR_TEXT_3073")
        global.msg[1] = scr_gettext("SCR_TEXT_3074")
        global.msg[2] = scr_gettext("SCR_TEXT_3075")
        global.msg[3] = scr_gettext("SCR_TEXT_3076")
        global.msg[4] = scr_gettext("SCR_TEXT_3077")
        global.msg[5] = scr_gettext("SCR_TEXT_3078")
        global.msg[6] = scr_gettext("SCR_TEXT_3079")
        global.msg[7] = scr_gettext("SCR_TEXT_3080")
        global.msg[8] = scr_gettext("SCR_TEXT_3081")
        global.msg[9] = scr_gettext("SCR_TEXT_3082")
        global.msg[10] = scr_gettext("SCR_TEXT_3083")
        global.msg[11] = scr_gettext("SCR_TEXT_3084")
        global.msg[12] = scr_gettext("SCR_TEXT_3085")
        global.msg[13] = scr_gettext("SCR_TEXT_3086")
        break
    case 623:
        global.msg[0] = scr_gettext("SCR_TEXT_3104")
        global.msg[1] = scr_gettext("SCR_TEXT_3105")
        global.msg[2] = scr_gettext("SCR_TEXT_3106")
        global.msg[3] = scr_gettext("SCR_TEXT_3107")
        global.msg[4] = scr_gettext("SCR_TEXT_3108")
        global.msg[5] = scr_gettext("SCR_TEXT_3109")
        global.msg[6] = scr_gettext("SCR_TEXT_3110")
        global.msg[7] = scr_gettext("SCR_TEXT_3111")
        global.msg[8] = scr_gettext("SCR_TEXT_3112")
        global.msg[9] = scr_gettext("SCR_TEXT_3113")
        global.msg[10] = scr_gettext("SCR_TEXT_3114")
        global.msg[11] = scr_gettext("SCR_TEXT_3115")
        global.msg[12] = scr_gettext("SCR_TEXT_3116")
        global.msg[13] = scr_gettext("SCR_TEXT_3117")
        global.msg[14] = scr_gettext("SCR_TEXT_3118")
        break
    case 624:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3124")
            global.msg[1] = scr_gettext("SCR_TEXT_3125")
            global.msg[2] = scr_gettext("SCR_TEXT_3126")
            global.msg[3] = scr_gettext("SCR_TEXT_3127")
            global.msg[4] = scr_gettext("SCR_TEXT_3128")
            global.msg[5] = scr_gettext("SCR_TEXT_3129")
            global.msg[6] = scr_gettext("SCR_TEXT_3130")
            global.msg[7] = scr_gettext("SCR_TEXT_3131")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3135")
            global.msg[1] = scr_gettext("SCR_TEXT_3136")
            global.msg[2] = scr_gettext("SCR_TEXT_3137")
            global.msg[3] = scr_gettext("SCR_TEXT_3138")
            global.msg[4] = scr_gettext("SCR_TEXT_3139")
            global.msg[5] = scr_gettext("SCR_TEXT_3140")
        }
        break
    case 625:
        if (global.flag[353] <= 19)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3148")
            global.msg[1] = scr_gettext("SCR_TEXT_3149")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3153")
            if instance_exists(obj_undynefall)
                global.msg[0] = scr_gettext("SCR_TEXT_3155")
        }
        break
    case 626:
        if (global.choice == 0)
        {
            global.flag[440] += 1
            global.msg[0] = scr_gettext("SCR_TEXT_3163")
            with (obj_watercooler)
                event_user(1)
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3171")
        break
    case 627:
        global.msg[0] = scr_gettext("SCR_TEXT_3177")
        global.msg[1] = scr_gettext("SCR_TEXT_3178")
        break
    case 628:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3184")
            with (obj_watercooler)
            {
                if instance_exists(obj_undynefall)
                    global.flag[441] += 1
                global.flag[353] += 1
                event_user(1)
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3196")
        break
    case 629:
        global.msg[0] = scr_gettext("SCR_TEXT_3201")
        if instance_exists(obj_watercooler)
        {
            if (obj_watercooler.havewater == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_3205")
                global.msg[1] = scr_gettext("SCR_TEXT_3206")
            }
            else
                global.msg[0] = scr_gettext("SCR_TEXT_3210")
        }
        break
    case 630:
        global.msg[0] = scr_gettext("SCR_TEXT_3215")
        if (global.choice == 0)
        {
            global.interact = 1
            with (obj_undynefall)
                event_user(1)
        }
        break
    case 632:
        armor1 = scr_gettext("papyrus_armor_0")
        armor2 = scr_gettext("papyrus_armor_0")
        if (global.flag[75] == 4)
            armor1 = scr_gettext("papyrus_armor_4")
        if (global.flag[75] == 12)
            armor1 = scr_gettext("papyrus_armor_12")
        if (global.flag[75] == 15)
            armor1 = scr_gettext("papyrus_armor_15")
        if (global.flag[75] == 24)
            armor1 = scr_gettext("papyrus_armor_24")
        if (global.flag[77] == 4)
            armor2 = scr_gettext("papyrus_armor_4")
        if (global.flag[77] == 12)
            armor2 = scr_gettext("papyrus_armor_12")
        if (global.flag[77] == 15)
            armor2 = scr_gettext("papyrus_armor_15")
        if (global.flag[77] == 24)
            armor2 = scr_gettext("papyrus_armor_24")
        global.msg[0] = scr_gettext("SCR_TEXT_3238", armor1, armor2)
        scr_papface(1, 0)
        global.msg[2] = scr_gettext("SCR_TEXT_3240", armor1, armor2)
        global.msg[3] = scr_gettext("SCR_TEXT_3241", armor1, armor2)
        global.msg[4] = scr_gettext("SCR_TEXT_3242", armor1, armor2)
        global.msg[5] = scr_gettext("SCR_TEXT_3243", armor1, armor2)
        global.msg[6] = scr_gettext("SCR_TEXT_3244", armor1, armor2)
        global.msg[7] = scr_gettext("SCR_TEXT_3245", armor1, armor2)
        if (global.flag[75] == global.flag[77])
        {
            if (global.flag[76] == 0)
            {
                global.msg[7] = scr_gettext("SCR_TEXT_3250", armor1, armor2)
                global.msg[8] = scr_gettext("SCR_TEXT_3251", armor1, armor2)
                global.msg[9] = scr_gettext("SCR_TEXT_3252", armor1, armor2)
                global.msg[10] = scr_gettext("SCR_TEXT_3253", armor1, armor2)
                global.msg[11] = scr_gettext("SCR_TEXT_3254", armor1, armor2)
                global.msg[12] = scr_gettext("SCR_TEXT_3255", armor1, armor2)
                global.msg[13] = scr_gettext("SCR_TEXT_3256", armor1, armor2)
                global.msg[14] = scr_gettext("SCR_TEXT_3257", armor1, armor2)
                global.msg[15] = scr_gettext("SCR_TEXT_3258", armor1, armor2)
                global.msg[16] = scr_gettext("SCR_TEXT_3259", armor1, armor2)
                global.msg[17] = scr_gettext("SCR_TEXT_3260", armor1, armor2)
                global.msg[18] = scr_gettext("SCR_TEXT_3261", armor1, armor2)
                global.msg[19] = scr_gettext("SCR_TEXT_3262", armor1, armor2)
            }
        }
        if (global.flag[75] != global.flag[77])
        {
            if (global.flag[76] == 0)
            {
                global.msg[7] = scr_gettext("SCR_TEXT_3268", armor1, armor2)
                global.msg[8] = scr_gettext("SCR_TEXT_3269", armor1, armor2)
                global.msg[9] = scr_gettext("SCR_TEXT_3270", armor1, armor2)
                global.msg[10] = scr_gettext("SCR_TEXT_3271", armor1, armor2)
                global.msg[11] = scr_gettext("SCR_TEXT_3272", armor1, armor2)
                global.msg[12] = scr_gettext("SCR_TEXT_3273", armor1, armor2)
                global.msg[13] = scr_gettext("SCR_TEXT_3274", armor1, armor2)
                global.msg[14] = scr_gettext("SCR_TEXT_3275", armor1, armor2)
                global.msg[15] = scr_gettext("SCR_TEXT_3276", armor1, armor2)
                global.msg[16] = scr_gettext("SCR_TEXT_3277", armor1, armor2)
                global.msg[17] = scr_gettext("SCR_TEXT_3278", armor1, armor2)
                global.msg[18] = scr_gettext("SCR_TEXT_3279", armor1, armor2)
                global.msg[19] = scr_gettext("SCR_TEXT_3280", armor1, armor2)
                global.msg[20] = scr_gettext("SCR_TEXT_3281", armor1, armor2)
                global.msg[21] = scr_gettext("SCR_TEXT_3282", armor1, armor2)
                global.msg[22] = scr_gettext("SCR_TEXT_3283", armor1, armor2)
                global.msg[23] = scr_gettext("SCR_TEXT_3284", armor1, armor2)
                global.msg[24] = scr_gettext("SCR_TEXT_3285", armor1, armor2)
            }
        }
        if (global.flag[75] == global.flag[77])
        {
            if (global.flag[76] == 1)
            {
                global.msg[7] = scr_gettext("SCR_TEXT_3291", armor1, armor2)
                global.msg[8] = scr_gettext("SCR_TEXT_3292", armor1, armor2)
                global.msg[9] = scr_gettext("SCR_TEXT_3293", armor1, armor2)
                global.msg[10] = scr_gettext("SCR_TEXT_3294", armor1, armor2)
                global.msg[11] = scr_gettext("SCR_TEXT_3295", armor1, armor2)
                global.msg[12] = scr_gettext("SCR_TEXT_3296", armor1, armor2)
                global.msg[13] = scr_gettext("SCR_TEXT_3297", armor1, armor2)
                global.msg[14] = scr_gettext("SCR_TEXT_3298", armor1, armor2)
                global.msg[15] = scr_gettext("SCR_TEXT_3299", armor1, armor2)
                global.msg[16] = scr_gettext("SCR_TEXT_3300", armor1, armor2)
                global.msg[17] = scr_gettext("SCR_TEXT_3301", armor1, armor2)
                global.msg[18] = scr_gettext("SCR_TEXT_3302", armor1, armor2)
                global.msg[19] = scr_gettext("SCR_TEXT_3303", armor1, armor2)
            }
        }
        if (global.flag[75] != global.flag[77])
        {
            if (global.flag[76] == 1)
            {
                global.msg[7] = scr_gettext("SCR_TEXT_3309", armor1, armor2)
                global.msg[8] = scr_gettext("SCR_TEXT_3310", armor1, armor2)
                global.msg[9] = scr_gettext("SCR_TEXT_3311", armor1, armor2)
                global.msg[10] = scr_gettext("SCR_TEXT_3312", armor1, armor2)
                global.msg[11] = scr_gettext("SCR_TEXT_3313", armor1, armor2)
                global.msg[12] = scr_gettext("SCR_TEXT_3314", armor1, armor2)
                global.msg[13] = scr_gettext("SCR_TEXT_3315", armor1, armor2)
                global.msg[14] = scr_gettext("SCR_TEXT_3316", armor1, armor2)
                global.msg[15] = scr_gettext("SCR_TEXT_3317", armor1, armor2)
                global.msg[16] = scr_gettext("SCR_TEXT_3318", armor1, armor2)
                global.msg[17] = scr_gettext("SCR_TEXT_3319", armor1, armor2)
                global.msg[18] = scr_gettext("SCR_TEXT_3320", armor1, armor2)
                global.msg[19] = scr_gettext("SCR_TEXT_3321", armor1, armor2)
                global.msg[20] = scr_gettext("SCR_TEXT_3322", armor1, armor2)
                global.msg[21] = scr_gettext("SCR_TEXT_3323", armor1, armor2)
                global.msg[22] = scr_gettext("SCR_TEXT_3324", armor1, armor2)
                global.msg[23] = scr_gettext("SCR_TEXT_3325", armor1, armor2)
                global.msg[24] = scr_gettext("SCR_TEXT_3326", armor1, armor2)
                global.msg[25] = scr_gettext("SCR_TEXT_3327", armor1, armor2)
                global.msg[26] = scr_gettext("SCR_TEXT_3328", armor1, armor2)
                global.msg[27] = scr_gettext("SCR_TEXT_3329", armor1, armor2)
                global.msg[28] = scr_gettext("SCR_TEXT_3330", armor1, armor2)
                global.msg[29] = scr_gettext("SCR_TEXT_3331", armor1, armor2)
            }
        }
        break
    case 633:
        global.msg[0] = scr_gettext("SCR_TEXT_3337")
        scr_papface(1, 0)
        global.msg[2] = scr_gettext("SCR_TEXT_3339")
        global.msg[3] = scr_gettext("SCR_TEXT_3340")
        global.msg[4] = scr_gettext("SCR_TEXT_3341")
        global.msg[5] = scr_gettext("SCR_TEXT_3342")
        global.msg[6] = scr_gettext("SCR_TEXT_3343")
        global.msg[7] = scr_gettext("SCR_TEXT_3344")
        global.msg[8] = scr_gettext("SCR_TEXT_3345")
        if (global.flag[88] < 3)
        {
            global.msg[5] = scr_gettext("SCR_TEXT_3348")
            global.msg[6] = scr_gettext("SCR_TEXT_3349")
            global.msg[7] = scr_gettext("SCR_TEXT_3350")
            global.msg[8] = scr_gettext("SCR_TEXT_3351")
            global.msg[9] = scr_gettext("SCR_TEXT_3352")
        }
        break
    case 635:
        global.msg[0] = scr_gettext("SCR_TEXT_3358")
        global.msg[1] = scr_gettext("SCR_TEXT_3359")
        break
    case 636:
        global.msg[0] = scr_gettext("SCR_TEXT_3363")
        global.msg[1] = scr_gettext("SCR_TEXT_3364")
        global.msg[2] = scr_gettext("SCR_TEXT_3365")
        global.msg[3] = scr_gettext("SCR_TEXT_3366")
        global.msg[4] = scr_gettext("SCR_TEXT_3367")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3370")
        break
    case 637:
        global.msg[0] = scr_gettext("SCR_TEXT_3358")
        global.msg[1] = scr_gettext("SCR_TEXT_3359")
        break
    case 638:
        global.msg[0] = scr_gettext("SCR_TEXT_3363")
        global.msg[1] = scr_gettext("SCR_TEXT_3364")
        global.msg[2] = scr_gettext("SCR_TEXT_3365")
        global.msg[3] = scr_gettext("SCR_TEXT_3366")
        global.msg[4] = scr_gettext("SCR_TEXT_3367")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3370")
        break
    case 639:
        global.msg[0] = scr_gettext("SCR_TEXT_3393")
        global.msg[1] = scr_gettext("SCR_TEXT_3394")
        break
    case 640:
        global.msg[0] = scr_gettext("SCR_TEXT_3398")
        global.msg[1] = scr_gettext("SCR_TEXT_3399")
        global.msg[2] = scr_gettext("SCR_TEXT_3400")
        global.msg[3] = scr_gettext("SCR_TEXT_3401")
        global.msg[4] = scr_gettext("SCR_TEXT_3402")
        global.msg[5] = scr_gettext("SCR_TEXT_3403")
        global.msg[6] = scr_gettext("SCR_TEXT_3404")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3407")
        break
    case 641:
        global.msg[0] = scr_gettext("SCR_TEXT_3413")
        global.msg[1] = scr_gettext("SCR_TEXT_3414")
        break
    case 642:
        global.msg[0] = scr_gettext("SCR_TEXT_3418")
        global.msg[1] = scr_gettext("SCR_TEXT_3419")
        global.msg[2] = scr_gettext("SCR_TEXT_3420")
        global.msg[3] = scr_gettext("SCR_TEXT_3421")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3424")
        break
    case 643:
        global.msg[0] = scr_gettext("SCR_TEXT_3429")
        global.msg[1] = scr_gettext("SCR_TEXT_3430")
        break
    case 644:
        global.msg[0] = scr_gettext("SCR_TEXT_3434")
        global.msg[1] = scr_gettext("SCR_TEXT_3435")
        global.msg[2] = scr_gettext("SCR_TEXT_3436")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3441")
        break
    case 645:
        global.msg[0] = scr_gettext("SCR_TEXT_3446")
        global.msg[1] = scr_gettext("SCR_TEXT_3447")
        break
    case 646:
        global.msg[0] = scr_gettext("SCR_TEXT_3451")
        global.msg[1] = scr_gettext("SCR_TEXT_3452")
        global.msg[2] = scr_gettext("SCR_TEXT_3453")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3458")
        break
    case 647:
        global.msg[0] = scr_gettext("SCR_TEXT_3463")
        global.msg[1] = scr_gettext("SCR_TEXT_3464")
        break
    case 648:
        global.msg[0] = scr_gettext("SCR_TEXT_3469")
        global.msg[1] = scr_gettext("SCR_TEXT_3470")
        global.msg[2] = scr_gettext("SCR_TEXT_3471")
        global.msg[3] = scr_gettext("SCR_TEXT_3472")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3477")
        break
    case 660:
        global.msg[0] = scr_gettext("SCR_TEXT_3482")
        global.msg[1] = scr_gettext("SCR_TEXT_3483")
        break
    case 661:
        global.msg[0] = scr_gettext("SCR_TEXT_3487")
        global.msg[1] = scr_gettext("SCR_TEXT_3488")
        if (global.choice == 0)
        {
            if (instance_exists(obj_paino) == 0)
                instance_create(2, 2, obj_paino)
        }
        break
    case 666:
        global.msg[0] = scr_gettext("SCR_TEXT_3495")
        global.msg[1] = scr_gettext("SCR_TEXT_3496")
        global.msg[2] = scr_gettext("SCR_TEXT_3497")
        global.msg[3] = scr_gettext("SCR_TEXT_3498")
        global.msg[4] = scr_gettext("SCR_TEXT_3499")
        break
    case 667:
        global.msg[0] = scr_gettext("SCR_TEXT_3503")
        global.msg[1] = scr_gettext("SCR_TEXT_3504")
        global.msg[2] = scr_gettext("SCR_TEXT_3505")
        break
    case 668:
        global.msg[0] = scr_gettext("SCR_TEXT_3509")
        global.msg[1] = scr_gettext("SCR_TEXT_3510")
        break
    case 669:
        global.msg[0] = scr_gettext("SCR_TEXT_3514")
        global.msg[1] = scr_gettext("SCR_TEXT_3515")
        global.msg[2] = scr_gettext("SCR_TEXT_3516")
        break
    case 670:
        global.msg[0] = scr_gettext("SCR_TEXT_3520")
        break
    case 671:
        global.msg[0] = scr_gettext("SCR_TEXT_3524")
        global.msg[1] = scr_gettext("SCR_TEXT_3525")
        break
    case 672:
        global.msg[0] = scr_gettext("SCR_TEXT_3528")
        break
    case 673:
        global.msg[0] = scr_gettext("SCR_TEXT_3531")
        global.msg[1] = scr_gettext("SCR_TEXT_3532")
        break
    case 674:
        global.msg[0] = scr_gettext("SCR_TEXT_3535")
        global.msg[1] = scr_gettext("SCR_TEXT_3536")
        global.msg[2] = scr_gettext("SCR_TEXT_3537")
        global.msg[3] = scr_gettext("SCR_TEXT_3538")
        global.msg[4] = scr_gettext("SCR_TEXT_3539")
        global.msg[5] = scr_gettext("SCR_TEXT_3540")
        global.msg[5] = scr_gettext("SCR_TEXT_3541")
        global.msg[6] = scr_gettext("SCR_TEXT_3542")
        break
    case 680:
        global.msg[0] = scr_gettext("SCR_TEXT_3548")
        global.msg[1] = scr_gettext("SCR_TEXT_3549")
        break
    case 681:
        global.msg[0] = scr_gettext("SCR_TEXT_3553")
        global.msg[1] = scr_gettext("SCR_TEXT_3554")
        if (global.choice == 0)
        {
            if (instance_exists(obj_purpledude) == 1)
                obj_purpledude.con = 1
            global.msg[0] = scr_gettext("SCR_TEXT_3559")
        }
        break
    case 682:
        global.msg[0] = scr_gettext("SCR_TEXT_3564")
        global.msg[1] = scr_gettext("SCR_TEXT_3565")
        break
    case 683:
        if (global.flag[371] == 0 && doak == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3571")
            if (global.choice == 0)
            {
                sc = instance_create(0, 0, obj_soundcombo)
                with (sc)
                {
                    sound1 = snd_switchpull_n
                    sound2 = snd_spearappear
                    alarm[1] = 8
                }
                doak = 1
                global.msg[0] = scr_gettext("SCR_TEXT_3584")
                if instance_exists(obj_laserswitch1)
                {
                    with (obj_laserswitch1)
                        event_user(0)
                }
            }
        }
        if (global.flag[371] == 1 && doak == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3591")
            if (global.choice == 0)
            {
                sc = instance_create(0, 0, obj_soundcombo)
                with (sc)
                {
                    sound1 = snd_switchpull_n
                    sound2 = snd_spearappear
                    alarm[1] = 8
                }
                doak = 1
                global.msg[0] = scr_gettext("SCR_TEXT_3604")
                if instance_exists(obj_laserswitch1)
                {
                    with (obj_laserswitch1)
                        event_user(1)
                }
            }
        }
        break
    case 684:
        global.msg[0] = scr_gettext("SCR_TEXT_3612")
        global.msg[1] = scr_gettext("SCR_TEXT_3613")
        global.msg[2] = scr_gettext("SCR_TEXT_3614")
        global.msg[3] = scr_gettext("SCR_TEXT_3615")
        global.msg[4] = scr_gettext("SCR_TEXT_3616")
        global.msg[5] = scr_gettext("SCR_TEXT_3617")
        global.msg[6] = scr_gettext("SCR_TEXT_3618")
        global.msg[7] = scr_gettext("SCR_TEXT_3619")
        global.msg[8] = scr_gettext("SCR_TEXT_3620")
        global.msg[9] = scr_gettext("SCR_TEXT_3621")
        global.msg[10] = scr_gettext("SCR_TEXT_3622")
        global.msg[11] = scr_gettext("SCR_TEXT_3623")
        global.msg[12] = scr_gettext("SCR_TEXT_3624")
        break
    case 685:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_3630")
        global.msg[1] = scr_gettext("SCR_TEXT_3631")
        global.msg[2] = scr_gettext("SCR_TEXT_3632")
        if (global.flag[380] > 0 && global.item[7] != 0)
        {
            if (instance_number(obj_hotdog) < 30)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_3638")
                if (global.flag[380] == 1)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_3641")
                    global.msg[1] = scr_gettext("SCR_TEXT_3642")
                }
                if (doak == 0)
                {
                    with (obj_hotdoggen)
                        event_user(0)
                    global.flag[380] += 1
                }
            }
            else
            {
                global.msg[0] = scr_gettext("SCR_TEXT_3653")
                if (global.flag[381] == 0)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_3656")
                    global.msg[1] = scr_gettext("SCR_TEXT_3657")
                    global.msg[2] = scr_gettext("SCR_TEXT_3658")
                    global.msg[3] = scr_gettext("SCR_TEXT_3659")
                    global.msg[4] = scr_gettext("SCR_TEXT_3660")
                }
                global.flag[381] = 1
            }
        }
        break
    case 686:
        script_execute(scr_cost, 30)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    if (global.flag[379] != 1)
                        script_execute(scr_itemget, 38)
                    if (global.flag[379] == 1)
                        script_execute(scr_itemget, 39)
                    if (noroom == 0)
                    {
                        global.gold -= 30
                        global.flag[378] += 30
                    }
                }
            }
            if (noroom == 0)
            {
                if (afford == 1)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_3687")
                    if (global.flag[379] == 0)
                    {
                        global.msg[0] = scr_gettext("SCR_TEXT_3690")
                        global.msg[1] = scr_gettext("SCR_TEXT_3691")
                    }
                    if (global.flag[379] == 1)
                    {
                        global.msg[0] = scr_gettext("SCR_TEXT_3695")
                        global.msg[1] = scr_gettext("SCR_TEXT_3696")
                        global.msg[2] = scr_gettext("SCR_TEXT_3697")
                    }
                    if (global.flag[379] == 2)
                    {
                        global.msg[0] = scr_gettext("SCR_TEXT_3701")
                        global.msg[1] = scr_gettext("SCR_TEXT_3702")
                        global.msg[2] = scr_gettext("SCR_TEXT_3703")
                        global.msg[3] = scr_gettext("SCR_TEXT_3704")
                    }
                    if (global.flag[379] == 3)
                    {
                        global.msg[0] = scr_gettext("SCR_TEXT_3708")
                        global.msg[1] = scr_gettext("SCR_TEXT_3709")
                        global.msg[2] = scr_gettext("SCR_TEXT_3710")
                        global.msg[3] = scr_gettext("SCR_TEXT_3711")
                    }
                    if (global.flag[379] == 4)
                    {
                        global.msg[0] = scr_gettext("SCR_TEXT_3715")
                        global.msg[1] = scr_gettext("SCR_TEXT_3716")
                        global.msg[2] = scr_gettext("SCR_TEXT_3717")
                        global.msg[3] = scr_gettext("SCR_TEXT_3718")
                        global.msg[4] = scr_gettext("SCR_TEXT_3719")
                    }
                    global.flag[379] += 1
                }
                if (afford == 0)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_3725")
                    global.msg[1] = scr_gettext("SCR_TEXT_3726")
                }
            }
            if (noroom == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_3729")
                with (obj_hotdoggen)
                    event_user(0)
                global.flag[380] += 1
                noroom = 2
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_3738")
        break
    case 690:
        global.msg[0] = scr_gettext("SCR_TEXT_3743")
        global.msg[1] = scr_gettext("SCR_TEXT_3744")
        global.msg[2] = scr_gettext("SCR_TEXT_3745")
        global.msg[3] = scr_gettext("SCR_TEXT_3746")
        global.msg[4] = scr_gettext("SCR_TEXT_3747")
        global.msg[5] = scr_gettext("SCR_TEXT_3748")
        break
    case 691:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3754")
            global.msg[1] = scr_gettext("SCR_TEXT_3755")
            global.msg[2] = scr_gettext("SCR_TEXT_3756")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3760")
            global.msg[1] = scr_gettext("SCR_TEXT_3761")
            global.msg[2] = scr_gettext("SCR_TEXT_3762")
        }
        global.msg[3] = scr_gettext("SCR_TEXT_3765")
        global.msg[4] = scr_gettext("SCR_TEXT_3766")
        global.msg[5] = scr_gettext("SCR_TEXT_3767")
        global.msg[6] = scr_gettext("SCR_TEXT_3768")
        global.msg[7] = scr_gettext("SCR_TEXT_3769")
        global.msg[8] = scr_gettext("SCR_TEXT_3770")
        global.msg[9] = scr_gettext("SCR_TEXT_3771")
        global.msg[10] = scr_gettext("SCR_TEXT_3772")
        global.msg[11] = scr_gettext("SCR_TEXT_3773")
        global.msg[12] = scr_gettext("SCR_TEXT_3774")
        global.msg[13] = scr_gettext("SCR_TEXT_3775")
        global.msg[14] = scr_gettext("SCR_TEXT_3776")
        global.msg[15] = scr_gettext("SCR_TEXT_3777")
        global.msg[16] = scr_gettext("SCR_TEXT_3778")
        global.msg[17] = scr_gettext("SCR_TEXT_3779")
        global.msg[18] = scr_gettext("SCR_TEXT_3780")
        global.msg[19] = scr_gettext("SCR_TEXT_3781")
        global.msg[20] = scr_gettext("SCR_TEXT_3782")
        global.msg[21] = scr_gettext("SCR_TEXT_3783")
        break
    case 692:
        global.msg[0] = scr_gettext("SCR_TEXT_3787")
        global.msg[1] = scr_gettext("SCR_TEXT_3788")
        global.msg[2] = scr_gettext("SCR_TEXT_3789")
        global.msg[3] = scr_gettext("SCR_TEXT_3790")
        global.msg[4] = scr_gettext("SCR_TEXT_3791")
        global.msg[5] = scr_gettext("SCR_TEXT_3792")
        global.msg[6] = scr_gettext("SCR_TEXT_3793")
        break
    case 693:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_3800")
            global.msg[2] = scr_gettext("SCR_TEXT_3801")
            global.msg[3] = scr_gettext("SCR_TEXT_3802")
            global.msg[4] = scr_gettext("SCR_TEXT_3803")
        }
        if (global.choice == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_3807")
            global.msg[2] = scr_gettext("SCR_TEXT_3808")
            global.msg[3] = scr_gettext("SCR_TEXT_3809")
        }
        break
    case 694:
        global.msg[0] = scr_gettext("SCR_TEXT_3814")
        global.msg[1] = scr_gettext("SCR_TEXT_3815")
        global.msg[2] = scr_gettext("SCR_TEXT_3816")
        global.msg[3] = scr_gettext("SCR_TEXT_3817")
        global.msg[4] = scr_gettext("SCR_TEXT_3818")
        break
    case 695:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_3825")
            global.msg[2] = scr_gettext("SCR_TEXT_3826")
            global.msg[3] = scr_gettext("SCR_TEXT_3827")
            global.msg[4] = scr_gettext("SCR_TEXT_3828")
        }
        if (global.choice == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_3832")
            global.msg[2] = scr_gettext("SCR_TEXT_3833")
            global.msg[3] = scr_gettext("SCR_TEXT_3834")
        }
        break
    case 696:
        global.msg[0] = scr_gettext("SCR_TEXT_3839")
        global.msg[1] = scr_gettext("SCR_TEXT_3840")
        global.msg[2] = scr_gettext("SCR_TEXT_3841")
        break
    case 697:
        global.msg[0] = scr_gettext("SCR_TEXT_3845")
        if (global.choice == 0)
        {
            if (obj_xoxocontroller1.fvic != 1)
            {
                obj_xoxocontroller1.fvic = 1
                snd_play(snd_switchpull_n)
            }
        }
        break
    case 698:
        global.msg[0] = scr_gettext("SCR_TEXT_3859")
        global.msg[1] = scr_gettext("SCR_TEXT_3860")
        global.msg[2] = scr_gettext("SCR_TEXT_3861")
        global.msg[3] = scr_gettext("SCR_TEXT_3862")
        global.msg[4] = scr_gettext("SCR_TEXT_3863")
        global.msg[5] = scr_gettext("SCR_TEXT_3864")
        break
    case 699:
        scr_papface(0, 3)
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_3872")
            if instance_exists(obj_undynedate_outside)
                obj_undynedate_outside.con = 5
        }
        if (global.choice == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_3878")
            global.msg[2] = scr_gettext("SCR_TEXT_3879")
        }
        break
    case 700:
        global.msg[0] = scr_gettext("SCR_TEXT_3884")
        global.msg[1] = scr_gettext("SCR_TEXT_3885")
        global.msg[2] = scr_gettext("SCR_TEXT_3886")
        global.msg[3] = scr_gettext("SCR_TEXT_3887")
        break
    case 701:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_3895")
            if instance_exists(obj_undynedate_outside)
                obj_undynedate_outside.con = 5
        }
        if (global.choice == 1)
            global.msg[1] = scr_gettext("SCR_TEXT_3901")
        break
    case 702:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_3910")
            if instance_exists(obj_undynedate_outside)
                obj_undynedate_outside.con = 5
        }
        if (global.choice == 1)
            global.msg[1] = scr_gettext("SCR_TEXT_3916")
        break
    case 703:
        global.msg[0] = scr_gettext("SCR_TEXT_3921")
        global.msg[1] = scr_gettext("SCR_TEXT_3922")
        global.msg[2] = scr_gettext("SCR_TEXT_3923")
        global.msg[3] = scr_gettext("SCR_TEXT_3924")
        global.msg[4] = scr_gettext("SCR_TEXT_3925")
        global.msg[5] = scr_gettext("SCR_TEXT_3926")
        break
    case 704:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3932")
            global.msg[1] = scr_gettext("SCR_TEXT_3933")
            global.msg[2] = scr_gettext("SCR_TEXT_3934")
            global.msg[3] = scr_gettext("SCR_TEXT_3935")
            global.msg[4] = scr_gettext("SCR_TEXT_3936")
            global.msg[5] = scr_gettext("SCR_TEXT_3937")
            global.msg[6] = scr_gettext("SCR_TEXT_3938")
            global.msg[7] = scr_gettext("SCR_TEXT_3939")
            global.msg[8] = scr_gettext("SCR_TEXT_3940")
            global.msg[9] = scr_gettext("SCR_TEXT_3941")
            global.msg[10] = scr_gettext("SCR_TEXT_3942")
            global.msg[11] = scr_gettext("SCR_TEXT_3943")
            global.msg[12] = scr_gettext("SCR_TEXT_3944")
            global.msg[13] = scr_gettext("SCR_TEXT_3945")
            if instance_exists(obj_undynedate_inside)
                obj_undynedate_inside.con = 50
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3951")
            global.msg[1] = scr_gettext("SCR_TEXT_3952")
            global.msg[2] = scr_gettext("SCR_TEXT_3953")
            global.msg[3] = scr_gettext("SCR_TEXT_3954")
            global.msg[4] = scr_gettext("SCR_TEXT_3955")
            global.msg[5] = scr_gettext("SCR_TEXT_3956")
        }
        break
    case 705:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3963")
            global.msg[1] = scr_gettext("SCR_TEXT_3964")
            global.msg[2] = scr_gettext("SCR_TEXT_3965")
            global.msg[3] = scr_gettext("SCR_TEXT_3966")
            global.msg[4] = scr_gettext("SCR_TEXT_3967")
            global.msg[5] = scr_gettext("SCR_TEXT_3968")
            global.msg[6] = scr_gettext("SCR_TEXT_3969")
            global.msg[7] = scr_gettext("SCR_TEXT_3970")
            if instance_exists(obj_undynedate_inside)
                obj_undynedate_inside.con = 40
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_3976")
            global.msg[1] = scr_gettext("SCR_TEXT_3977")
            global.msg[2] = scr_gettext("SCR_TEXT_3978")
            global.msg[3] = scr_gettext("SCR_TEXT_3979")
            global.msg[4] = scr_gettext("SCR_TEXT_3980")
            global.msg[5] = scr_gettext("SCR_TEXT_3981")
            global.msg[6] = scr_gettext("SCR_TEXT_3982")
            global.msg[7] = scr_gettext("SCR_TEXT_3983")
            global.msg[8] = scr_gettext("SCR_TEXT_3984")
            global.msg[9] = scr_gettext("SCR_TEXT_3985")
            global.msg[10] = scr_gettext("SCR_TEXT_3986")
            global.msg[11] = scr_gettext("SCR_TEXT_3987")
            global.msg[12] = scr_gettext("SCR_TEXT_3988")
            global.msg[13] = scr_gettext("SCR_TEXT_3989")
            global.msg[14] = scr_gettext("SCR_TEXT_3990")
            global.msg[15] = scr_gettext("SCR_TEXT_3991")
            if instance_exists(obj_undynedate_inside)
                obj_undynedate_inside.con = 50
        }
        break
    case 706:
        global.msg[0] = scr_gettext("SCR_TEXT_3998")
        global.msg[1] = scr_gettext("SCR_TEXT_3999")
        break
    case 707:
        global.msg[0] = scr_gettext("SCR_TEXT_4003")
        if (global.choice == 0)
            obj_undynedate_inside.con = 60
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4010")
            global.msg[1] = scr_gettext("SCR_TEXT_4011")
        }
        break
    case 708:
        global.msg[0] = scr_gettext("SCR_TEXT_4016")
        global.msg[1] = scr_gettext("SCR_TEXT_4017")
        global.msg[2] = scr_gettext("SCR_TEXT_4018")
        global.msg[3] = scr_gettext("SCR_TEXT_4019")
        global.msg[4] = scr_gettext("SCR_TEXT_4020")
        global.msg[5] = scr_gettext("SCR_TEXT_4021")
        global.msg[6] = scr_gettext("SCR_TEXT_4022")
        break
    case 709:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4028")
            global.msg[1] = scr_gettext("SCR_TEXT_4029")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4033")
        break
    case 710:
        global.msg[0] = scr_gettext("SCR_TEXT_4038")
        global.msg[1] = scr_gettext("SCR_TEXT_4039")
        global.msg[2] = scr_gettext("SCR_TEXT_4041")
        global.msg[3] = scr_gettext("SCR_TEXT_4042")
        global.msg[4] = scr_gettext("SCR_TEXT_4043")
        break
    case 711:
        if (global.choice == 0)
        {
            obj_undynedate_inside.con = 140
            global.msg[0] = scr_gettext("SCR_TEXT_4050")
            scr_undface(1, 6)
            global.msg[2] = scr_gettext("SCR_TEXT_4052")
            global.msg[3] = scr_gettext("SCR_TEXT_4053")
            global.msg[4] = scr_gettext("SCR_TEXT_4054")
            global.msg[5] = scr_gettext("SCR_TEXT_4055")
        }
        if (global.choice == 1)
        {
            obj_undynedate_inside.con = 141
            global.msg[0] = scr_gettext("SCR_TEXT_4060")
            scr_undface(1, 1)
            global.msg[2] = scr_gettext("SCR_TEXT_4062")
            global.msg[3] = scr_gettext("SCR_TEXT_4063")
            global.msg[4] = scr_gettext("SCR_TEXT_4064")
        }
        break
    case 712:
        global.msg[0] = scr_gettext("SCR_TEXT_4071")
        global.msg[1] = scr_gettext("SCR_TEXT_4072")
        global.msg[2] = scr_gettext("SCR_TEXT_4073")
        global.msg[3] = scr_gettext("SCR_TEXT_4074")
        global.msg[4] = scr_gettext("SCR_TEXT_4075")
        global.msg[5] = scr_gettext("SCR_TEXT_4076")
        global.msg[6] = scr_gettext("SCR_TEXT_4077")
        global.msg[7] = scr_gettext("SCR_TEXT_4078")
        global.msg[8] = scr_gettext("SCR_TEXT_4079")
        break
    case 713:
        if (global.choice == 0)
        {
            obj_undynedate_inside.con = 199
            global.msg[0] = scr_gettext("SCR_TEXT_4086")
            global.msg[1] = scr_gettext("SCR_TEXT_4087")
            scr_undface(2, 6)
            global.msg[3] = scr_gettext("SCR_TEXT_4089")
        }
        if (global.choice == 1)
        {
            obj_undynedate_inside.con = 200
            global.msg[0] = scr_gettext("SCR_TEXT_4094")
            global.msg[1] = scr_gettext("SCR_TEXT_4095")
            scr_undface(2, 9)
            global.msg[3] = scr_gettext("SCR_TEXT_4097")
        }
        break
    case 714:
        global.msg[0] = scr_gettext("SCR_TEXT_4103")
        global.msg[1] = scr_gettext("SCR_TEXT_4104")
        global.msg[2] = scr_gettext("SCR_TEXT_4105")
        global.msg[3] = scr_gettext("SCR_TEXT_4106")
        global.msg[4] = scr_gettext("SCR_TEXT_4107")
        break
    case 715:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4113")
            global.msg[1] = scr_gettext("SCR_TEXT_4114")
            global.msg[2] = scr_gettext("SCR_TEXT_4115")
            global.msg[3] = scr_gettext("SCR_TEXT_4116")
            global.msg[4] = scr_gettext("SCR_TEXT_4117")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4121")
            global.msg[1] = scr_gettext("SCR_TEXT_4122")
            global.msg[2] = scr_gettext("SCR_TEXT_4123")
            global.msg[3] = scr_gettext("SCR_TEXT_4124")
            global.msg[4] = scr_gettext("SCR_TEXT_4125")
            global.msg[5] = scr_gettext("SCR_TEXT_4126")
        }
        break
    case 716:
        global.msg[0] = scr_gettext("SCR_TEXT_4131")
        global.msg[1] = scr_gettext("SCR_TEXT_4132")
        break
    case 717:
        global.msg[0] = scr_gettext("SCR_TEXT_4136")
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            obj_bonedrawer_check.con = 5
        }
        break
    case 720:
        global.msg[0] = scr_gettext("SCR_TEXT_4147")
        global.msg[1] = scr_gettext("SCR_TEXT_4148")
        global.msg[2] = scr_gettext("SCR_TEXT_4149")
        global.msg[3] = scr_gettext("SCR_TEXT_4150")
        global.msg[4] = scr_gettext("SCR_TEXT_4151")
        global.msg[5] = scr_gettext("SCR_TEXT_4152")
        break
    case 721:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4159")
            obj_mettnewsevent.eventchoice = 1
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4165")
        break
    case 722:
        global.msg[0] = scr_gettext("SCR_TEXT_4170")
        global.msg[1] = scr_gettext("SCR_TEXT_4171")
        global.msg[2] = scr_gettext("SCR_TEXT_4172")
        global.msg[3] = scr_gettext("SCR_TEXT_4173")
        break
    case 723:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4179")
            obj_mettnewsevent.eventchoice = 1
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4185")
        break
    case 724:
        global.msg[0] = scr_gettext("SCR_TEXT_4191")
        global.msg[1] = scr_gettext("SCR_TEXT_4192")
        global.msg[2] = scr_gettext("SCR_TEXT_4193")
        global.msg[3] = scr_gettext("SCR_TEXT_4194")
        global.msg[4] = scr_gettext("SCR_TEXT_4195")
        break
    case 725:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4201")
            obj_mettnewsevent.eventchoice = 2
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4207")
        break
    case 726:
        global.msg[0] = scr_gettext("SCR_TEXT_4214")
        global.msg[1] = scr_gettext("SCR_TEXT_4215")
        global.msg[2] = scr_gettext("SCR_TEXT_4216")
        break
    case 727:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4222")
            obj_mettnewsevent.eventchoice = 2
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4228")
        break
    case 728:
        global.msg[0] = scr_gettext("SCR_TEXT_4234")
        global.msg[1] = scr_gettext("SCR_TEXT_4235")
        global.msg[2] = scr_gettext("SCR_TEXT_4236")
        global.msg[3] = scr_gettext("SCR_TEXT_4237")
        global.msg[4] = scr_gettext("SCR_TEXT_4238")
        global.msg[5] = scr_gettext("SCR_TEXT_4239")
        global.msg[6] = scr_gettext("SCR_TEXT_4240")
        global.msg[7] = scr_gettext("SCR_TEXT_4241")
        global.msg[8] = scr_gettext("SCR_TEXT_4242")
        break
    case 729:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4248")
            obj_mettnewsevent.eventchoice = 3
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4254")
            global.msg[1] = scr_gettext("SCR_TEXT_4255")
        }
        break
    case 730:
        global.msg[0] = scr_gettext("SCR_TEXT_4260")
        global.msg[1] = scr_gettext("SCR_TEXT_4261")
        global.msg[2] = scr_gettext("SCR_TEXT_4262")
        global.msg[3] = scr_gettext("SCR_TEXT_4263")
        break
    case 731:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4270")
            obj_mettnewsevent.eventchoice = 3
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4276")
        break
    case 732:
        global.msg[0] = scr_gettext("SCR_TEXT_4282")
        global.msg[1] = scr_gettext("SCR_TEXT_4283")
        global.msg[2] = scr_gettext("SCR_TEXT_4284")
        global.msg[3] = scr_gettext("SCR_TEXT_4285")
        global.msg[4] = scr_gettext("SCR_TEXT_4286")
        global.msg[5] = scr_gettext("SCR_TEXT_4287")
        break
    case 733:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4293")
            obj_mettnewsevent.eventchoice = 4
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4299")
        break
    case 734:
        global.msg[0] = scr_gettext("SCR_TEXT_4304")
        global.msg[1] = scr_gettext("SCR_TEXT_4305")
        global.msg[2] = scr_gettext("SCR_TEXT_4306")
        break
    case 735:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4312")
            obj_mettnewsevent.eventchoice = 4
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4318")
        break
    case 736:
        global.msg[0] = scr_gettext("SCR_TEXT_4326")
        global.msg[1] = scr_gettext("SCR_TEXT_4327")
        global.msg[2] = scr_gettext("SCR_TEXT_4328")
        global.msg[3] = scr_gettext("SCR_TEXT_4329")
        global.msg[4] = scr_gettext("SCR_TEXT_4330")
        break
    case 737:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4336")
            obj_mettnewsevent.eventchoice = 5
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4342")
        break
    case 738:
        global.msg[0] = scr_gettext("SCR_TEXT_4347")
        global.msg[1] = scr_gettext("SCR_TEXT_4348")
        global.msg[2] = scr_gettext("SCR_TEXT_4349")
        global.msg[3] = scr_gettext("SCR_TEXT_4350")
        break
    case 739:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4356")
            obj_mettnewsevent.eventchoice = 5
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4362")
        break
    case 740:
        global.msg[0] = scr_gettext("SCR_TEXT_4369")
        global.msg[1] = scr_gettext("SCR_TEXT_4370")
        global.msg[2] = scr_gettext("SCR_TEXT_4371")
        global.msg[3] = scr_gettext("SCR_TEXT_4372")
        global.msg[4] = scr_gettext("SCR_TEXT_4373")
        global.msg[5] = scr_gettext("SCR_TEXT_4374")
        global.msg[6] = scr_gettext("SCR_TEXT_4375")
        break
    case 741:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4381")
            obj_mettnewsevent.eventchoice = 6
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4387")
        break
    case 742:
        global.msg[0] = scr_gettext("SCR_TEXT_4392")
        global.msg[1] = scr_gettext("SCR_TEXT_4393")
        global.msg[2] = scr_gettext("SCR_TEXT_4394")
        global.msg[3] = scr_gettext("SCR_TEXT_4395")
        break
    case 743:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4401")
            obj_mettnewsevent.eventchoice = 6
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4407")
        break
    case 744:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_4415")
        global.msg[1] = scr_gettext("SCR_TEXT_4416")
        break
    case 745:
        script_execute(scr_cost, 9999)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 10)
                    if (noroom == 0)
                    {
                        global.gold -= 9999
                        global.flag[59] += 9999
                        global.flag[403] = 1
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4433")
            if (afford == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_4434")
        }
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4436")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4439")
        break
    case 746:
        doak = 0
        noroom = 0
        global.msg[0] = scr_gettext("SCR_TEXT_4447")
        global.msg[1] = scr_gettext("SCR_TEXT_4448")
        break
    case 747:
        script_execute(scr_cost, 9999)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 7)
                    if (noroom == 0)
                    {
                        global.gold -= 9999
                        global.flag[59] += 9999
                        global.flag[403] = 1
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4465")
            if (afford == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_4466")
        }
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4468")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4471")
        break
    case 748:
        global.msg[0] = scr_gettext("SCR_TEXT_4477")
        global.msg[1] = scr_gettext("SCR_TEXT_4478")
        global.msg[2] = scr_gettext("SCR_TEXT_4479")
        global.msg[3] = scr_gettext("SCR_TEXT_4480")
        break
    case 749:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4486")
            global.msg[1] = scr_gettext("SCR_TEXT_4487")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4491")
            global.msg[1] = scr_gettext("SCR_TEXT_4492")
            global.msg[2] = scr_gettext("SCR_TEXT_4493")
            global.msg[3] = scr_gettext("SCR_TEXT_4494")
        }
        break
    case 750:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_4501")
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4505")
            global.flag[22] = 1
        }
        break
    case 751:
        global.msg[0] = scr_gettext("SCR_TEXT_4511")
        global.msg[1] = scr_gettext("SCR_TEXT_4512")
        global.msg[2] = scr_gettext("SCR_TEXT_4513")
        break
    case 752:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4519")
            global.msg[1] = scr_gettext("SCR_TEXT_4520")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4524")
            global.msg[1] = scr_gettext("SCR_TEXT_4525")
            global.msg[2] = scr_gettext("SCR_TEXT_4526")
            global.msg[2] = scr_gettext("SCR_TEXT_4527")
            global.msg[3] = scr_gettext("SCR_TEXT_4528")
            global.msg[4] = scr_gettext("SCR_TEXT_4529")
            global.flag[22] = 2
        }
        break
    case 753:
        global.msg[0] = scr_gettext("SCR_TEXT_4535")
        break
    case 754:
        global.msg[0] = scr_gettext("SCR_TEXT_4540")
        global.msg[1] = scr_gettext("SCR_TEXT_4541")
        global.msg[2] = scr_gettext("SCR_TEXT_4542")
        break
    case 755:
        global.flag[409] = 1
        global.msg[0] = scr_gettext("SCR_TEXT_4547")
        break
    case 756:
        global.msg[0] = scr_gettext("SCR_TEXT_4551")
        global.msg[1] = scr_gettext("SCR_TEXT_4552")
        global.msg[2] = scr_gettext("SCR_TEXT_4553")
        break
    case 757:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4559")
            global.msg[1] = scr_gettext("SCR_TEXT_4560")
            global.msg[2] = scr_gettext("SCR_TEXT_4561")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4565")
        break
    case 758:
        global.msg[0] = scr_gettext("SCR_TEXT_4571")
        global.msg[1] = scr_gettext("SCR_TEXT_4572")
        global.msg[2] = scr_gettext("SCR_TEXT_4573")
        break
    case 759:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_4579")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4583")
        break
    case 760:
        global.msg[0] = scr_gettext("SCR_TEXT_4588")
        global.msg[1] = scr_gettext("SCR_TEXT_4589")
        global.msg[2] = scr_gettext("SCR_TEXT_4590")
        if (global.flag[67] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4591")
        break
    case 761:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4597")
            if instance_exists(obj_sans_prefinaldate)
                obj_sans_prefinaldate.con = 1
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4602")
        break
    case 762:
        global.msg[0] = scr_gettext("SCR_TEXT_4609")
        global.msg[1] = scr_gettext("SCR_TEXT_4610")
        global.msg[2] = scr_gettext("SCR_TEXT_4611")
        global.msg[3] = scr_gettext("SCR_TEXT_4612")
        global.msg[4] = scr_gettext("SCR_TEXT_4613")
        global.msg[5] = scr_gettext("SCR_TEXT_4614")
        global.msg[6] = scr_gettext("SCR_TEXT_4615")
        global.msg[7] = scr_gettext("SCR_TEXT_4616")
        break
    case 763:
        if (global.choice == 0)
        {
            obj_barrierevent.con = 10
            global.msg[0] = scr_gettext("SCR_TEXT_4623")
            global.msg[1] = scr_gettext("SCR_TEXT_4624")
        }
        if (global.choice == 1)
        {
            global.flag[457] = 1
            obj_barrierevent.con = 40
            global.msg[0] = scr_gettext("SCR_TEXT_4630")
            global.msg[1] = scr_gettext("SCR_TEXT_4631")
            global.msg[2] = scr_gettext("SCR_TEXT_4632")
            global.msg[3] = scr_gettext("SCR_TEXT_4633")
        }
        break
    case 764:
        global.msg[0] = scr_gettext("SCR_TEXT_4639")
        global.msg[1] = scr_gettext("SCR_TEXT_4640")
        global.msg[2] = scr_gettext("SCR_TEXT_4641")
        global.msg[3] = scr_gettext("SCR_TEXT_4642")
        break
    case 765:
        if (global.choice == 0)
        {
            obj_barrierevent.con = 10
            global.msg[0] = scr_gettext("SCR_TEXT_4649")
            global.msg[1] = scr_gettext("SCR_TEXT_4650")
        }
        if (global.choice == 1)
        {
            obj_barrierevent.con = 40
            global.msg[0] = scr_gettext("SCR_TEXT_4655")
            global.msg[1] = scr_gettext("SCR_TEXT_4656")
        }
        break
    case 770:
        global.msg[0] = scr_gettext("SCR_TEXT_4662")
        global.msg[1] = scr_gettext("SCR_TEXT_4663")
        global.msg[2] = scr_gettext("SCR_TEXT_4664")
        global.msg[3] = scr_gettext("SCR_TEXT_4665")
        global.msg[4] = scr_gettext("SCR_TEXT_4666")
        if (global.flag[460] > 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4669")
            global.msg[1] = scr_gettext("SCR_TEXT_4670")
            global.msg[2] = scr_gettext("SCR_TEXT_4671")
        }
        break
    case 771:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4678")
            if (room == room_fire_dock)
                global.msg[0] = scr_gettext("SCR_TEXT_4680")
            if (room == room_water_dock)
                global.msg[0] = scr_gettext("SCR_TEXT_4682")
            if (room == room_tundra_dock)
                global.msg[0] = scr_gettext("SCR_TEXT_4684")
            global.msg[1] = scr_gettext("SCR_TEXT_4686")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4691")
        break
    case 772:
        if (global.choice == 0)
        {
            if (room == room_fire_dock || room == room_water_dock)
                global.flag[459] = 1
            if (room == room_tundra_dock)
                global.flag[459] = 2
        }
        if (global.choice == 1)
        {
            if (room == room_tundra_dock || room == room_water_dock)
                global.flag[459] = 3
            if (room == room_fire_dock)
                global.flag[459] = 2
        }
        if instance_exists(obj_dogboat_thing)
            obj_dogboat_thing.con = 0.1
        global.msg[0] = scr_gettext("SCR_TEXT_4713")
        break
    case 780:
        if (global.flag[490] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4720")
            global.msg[1] = scr_gettext("SCR_TEXT_4721")
            global.msg[2] = scr_gettext("SCR_TEXT_4722")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_4725")
        break
    case 781:
        global.msg[0] = scr_gettext("SCR_TEXT_4729")
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            global.flag[490] = 1
            if instance_exists(obj_amalgam_dogevent)
                obj_amalgam_dogevent.con = 50
        }
        break
    case 782:
        global.msg[0] = scr_gettext("SCR_TEXT_4743")
        global.msg[1] = scr_gettext("SCR_TEXT_4744")
        global.msg[2] = scr_gettext("SCR_TEXT_4745")
        break
    case 783:
        global.msg[0] = scr_gettext("SCR_TEXT_4749")
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            if instance_exists(obj_bed_watcher)
                obj_bed_watcher.con = 5
        }
        break
    case 784:
        if (global.flag[484] > 1)
            global.msg[0] = scr_gettext("SCR_TEXT_4763")
        if (global.flag[484] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4767")
            global.flag[484] = 2
        }
        if (global.flag[484] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4772")
            global.msg[1] = scr_gettext("SCR_TEXT_4773")
            global.msg[2] = scr_gettext("SCR_TEXT_4774")
        }
        break
    case 785:
        global.msg[0] = scr_gettext("SCR_TEXT_4779")
        if (global.choice == 0)
        {
            if (global.flag[484] == 0)
            {
                snd_play(snd_noise)
                global.flag[484] = 1
            }
        }
        break
    case 786:
        global.msg[0] = scr_gettext("SCR_TEXT_4791")
        if (global.flag[491] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4794")
            global.msg[1] = scr_gettext("SCR_TEXT_4795")
            global.msg[2] = scr_gettext("SCR_TEXT_4796")
        }
        break
    case 787:
        global.msg[0] = scr_gettext("SCR_TEXT_4801")
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            if instance_exists(obj_lab_powerswitch)
            {
                global.flag[491] = 1
                obj_lab_powerswitch.con = 5
            }
        }
        break
    case 800:
        global.msg[0] = scr_gettext("SCR_TEXT_4816")
        global.msg[1] = scr_gettext("SCR_TEXT_4817")
        global.msg[2] = scr_gettext("SCR_TEXT_4818")
        global.msg[3] = scr_gettext("SCR_TEXT_4819")
        global.msg[4] = scr_gettext("SCR_TEXT_4820")
        global.msg[5] = scr_gettext("SCR_TEXT_4821")
        global.msg[6] = scr_gettext("SCR_TEXT_4822")
        global.msg[7] = scr_gettext("SCR_TEXT_4823")
        global.msg[8] = scr_gettext("SCR_TEXT_4824")
        global.msg[9] = scr_gettext("SCR_TEXT_4825")
        global.msg[10] = scr_gettext("SCR_TEXT_4826")
        global.msg[11] = scr_gettext("SCR_TEXT_4827")
        global.msg[12] = scr_gettext("SCR_TEXT_4828")
        global.msg[13] = scr_gettext("SCR_TEXT_4829")
        global.msg[14] = scr_gettext("SCR_TEXT_4830")
        global.msg[15] = scr_gettext("SCR_TEXT_4831")
        global.msg[16] = scr_gettext("SCR_TEXT_4832")
        global.msg[17] = scr_gettext("SCR_TEXT_4833")
        global.msg[18] = scr_gettext("SCR_TEXT_4834")
        global.msg[19] = scr_gettext("SCR_TEXT_4835")
        global.msg[20] = scr_gettext("SCR_TEXT_4836")
        global.msg[21] = scr_gettext("SCR_TEXT_4837")
        global.msg[22] = scr_gettext("SCR_TEXT_4838")
        global.msg[23] = scr_gettext("SCR_TEXT_4839")
        global.msg[24] = scr_gettext("SCR_TEXT_4840")
        global.msg[25] = scr_gettext("SCR_TEXT_4841")
        global.msg[26] = scr_gettext("SCR_TEXT_4842")
        global.msg[27] = scr_gettext("SCR_TEXT_4843")
        global.msg[28] = scr_gettext("SCR_TEXT_4844")
        global.msg[29] = scr_gettext("SCR_TEXT_4845")
        global.msg[30] = scr_gettext("SCR_TEXT_4846")
        global.msg[31] = scr_gettext("SCR_TEXT_4847")
        global.msg[32] = scr_gettext("SCR_TEXT_4848")
        global.msg[33] = scr_gettext("SCR_TEXT_4849")
        global.msg[34] = scr_gettext("SCR_TEXT_4850")
        global.msg[35] = scr_gettext("SCR_TEXT_4851")
        global.msg[36] = scr_gettext("SCR_TEXT_4852")
        global.msg[37] = scr_gettext("SCR_TEXT_4853")
        global.msg[38] = scr_gettext("SCR_TEXT_4855")
        global.msg[39] = scr_gettext("SCR_TEXT_4856")
        break
    case 801:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4862")
            global.msg[1] = scr_gettext("SCR_TEXT_4863")
            global.msg[2] = scr_gettext("SCR_TEXT_4864")
            global.msg[3] = scr_gettext("SCR_TEXT_4865")
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_4869")
            global.msg[1] = scr_gettext("SCR_TEXT_4870")
            global.msg[2] = scr_gettext("SCR_TEXT_4871")
            global.msg[3] = scr_gettext("SCR_TEXT_4872")
        }
        global.msg[4] = scr_gettext("SCR_TEXT_4875")
        global.msg[5] = scr_gettext("SCR_TEXT_4876")
        global.msg[6] = scr_gettext("SCR_TEXT_4877")
        global.msg[7] = scr_gettext("SCR_TEXT_4878")
        global.msg[8] = scr_gettext("SCR_TEXT_4879")
        global.msg[9] = scr_gettext("SCR_TEXT_4880")
        global.msg[10] = scr_gettext("SCR_TEXT_4881")
        global.msg[11] = scr_gettext("SCR_TEXT_4882")
        global.msg[12] = scr_gettext("SCR_TEXT_4883")
        break
    case 803:
        global.msg[0] = scr_gettext("SCR_TEXT_4887")
        global.msg[1] = scr_gettext("SCR_TEXT_4888")
        global.msg[2] = scr_gettext("SCR_TEXT_4889")
        global.msg[3] = scr_gettext("SCR_TEXT_4890")
        global.msg[4] = scr_gettext("SCR_TEXT_4891")
        global.msg[5] = scr_gettext("SCR_TEXT_4892")
        global.msg[6] = scr_gettext("SCR_TEXT_4893")
        global.msg[7] = scr_gettext("SCR_TEXT_4894")
        global.msg[8] = scr_gettext("SCR_TEXT_4895")
        global.msg[9] = scr_gettext("SCR_TEXT_4896")
        global.msg[10] = scr_gettext("SCR_TEXT_4897")
        global.msg[11] = scr_gettext("SCR_TEXT_4898")
        global.msg[12] = scr_gettext("SCR_TEXT_4899")
        global.msg[13] = scr_gettext("SCR_TEXT_4900")
        break
    case 804:
        global.msg[0] = " %%"
        if instance_exists(obj_asriel_overworldanim)
        {
            if (global.choice == 0)
                obj_asriel_overworldanim.con = 28
            if (global.choice == 1)
                obj_asriel_overworldanim.con = 50
        }
        break
    case 806:
        global.msg[0] = scr_gettext("SCR_TEXT_4919")
        global.msg[1] = scr_gettext("SCR_TEXT_4920")
        global.msg[2] = scr_gettext("SCR_TEXT_4921")
        scr_alface(3, 3)
        global.msg[4] = scr_gettext("SCR_TEXT_4923")
        global.msg[5] = scr_gettext("SCR_TEXT_4924")
        global.msg[6] = scr_gettext("SCR_TEXT_4925")
        global.msg[7] = scr_gettext("SCR_TEXT_4926")
        scr_papface(8, 0)
        global.msg[9] = scr_gettext("SCR_TEXT_4928")
        global.msg[10] = scr_gettext("SCR_TEXT_4929")
        scr_undface(11, 9)
        global.msg[12] = scr_gettext("SCR_TEXT_4931")
        global.msg[13] = scr_gettext("SCR_TEXT_4932")
        global.msg[14] = scr_gettext("SCR_TEXT_4933")
        global.msg[15] = scr_gettext("SCR_TEXT_4934")
        scr_asgface(16, 2)
        global.msg[17] = scr_gettext("SCR_TEXT_4936")
        global.msg[18] = scr_gettext("SCR_TEXT_4937")
        scr_alface(19, 1)
        global.msg[20] = scr_gettext("SCR_TEXT_4939")
        global.msg[21] = scr_gettext("SCR_TEXT_4940")
        global.msg[22] = scr_gettext("SCR_TEXT_4941")
        global.msg[23] = scr_gettext("SCR_TEXT_4942")
        global.msg[24] = scr_gettext("SCR_TEXT_4943")
        global.msg[25] = scr_gettext("SCR_TEXT_4944")
        global.msg[26] = scr_gettext("SCR_TEXT_4945")
        break
    case 807:
        scr_asgface(0, 2)
        global.msg[1] = scr_gettext("SCR_TEXT_4950")
        if (global.choice == 1)
            global.msg[1] = scr_gettext("SCR_TEXT_4952")
        global.msg[2] = scr_gettext("SCR_TEXT_4953")
        global.msg[3] = scr_gettext("SCR_TEXT_4954")
        scr_alface(4, 3)
        global.msg[5] = scr_gettext("SCR_TEXT_4956")
        global.msg[6] = scr_gettext("SCR_TEXT_4957")
        global.msg[7] = scr_gettext("SCR_TEXT_4958")
        global.msg[8] = scr_gettext("SCR_TEXT_4959")
        global.msg[9] = scr_gettext("SCR_TEXT_4960")
        global.msg[10] = scr_gettext("SCR_TEXT_4961")
        scr_asgface(11, 1)
        global.msg[12] = scr_gettext("SCR_TEXT_4963")
        scr_undface(13, 9)
        global.msg[14] = scr_gettext("SCR_TEXT_4965")
        scr_asgface(15, 0)
        global.msg[16] = scr_gettext("SCR_TEXT_4967")
        scr_alface(17, 5)
        global.msg[18] = scr_gettext("SCR_TEXT_4969")
        break
    case 808:
        global.msg[0] = scr_gettext("SCR_TEXT_4973")
        global.msg[1] = scr_gettext("SCR_TEXT_4974")
        global.msg[2] = scr_gettext("SCR_TEXT_4975")
        global.msg[3] = scr_gettext("SCR_TEXT_4976")
        global.msg[4] = scr_gettext("SCR_TEXT_4977")
        global.msg[5] = scr_gettext("SCR_TEXT_4978")
        global.msg[6] = scr_gettext("SCR_TEXT_4979")
        break
    case 809:
        global.msg[0] = scr_gettext("SCR_TEXT_4983")
        if (global.choice == 0)
        {
            scr_alface(0, 7)
            global.msg[1] = scr_gettext("SCR_TEXT_4987")
            global.msg[2] = scr_gettext("SCR_TEXT_4988")
            global.msg[3] = scr_gettext("SCR_TEXT_4989")
            global.msg[4] = scr_gettext("SCR_TEXT_4990")
            global.msg[5] = scr_gettext("SCR_TEXT_4991")
            global.msg[6] = scr_gettext("SCR_TEXT_4992")
        }
        if (global.choice == 1)
        {
            scr_alface(0, 9)
            global.msg[1] = scr_gettext("SCR_TEXT_4998")
            global.msg[2] = scr_gettext("SCR_TEXT_4999")
            global.msg[3] = scr_gettext("SCR_TEXT_5000")
            global.msg[4] = scr_gettext("SCR_TEXT_5001")
        }
        break
    case 810:
        global.msg[0] = scr_gettext("SCR_TEXT_5007")
        global.msg[1] = scr_gettext("SCR_TEXT_5008")
        global.msg[2] = scr_gettext("SCR_TEXT_5009")
        global.msg[3] = scr_gettext("SCR_TEXT_5010")
        global.msg[4] = scr_gettext("SCR_TEXT_5011")
        global.msg[5] = scr_gettext("SCR_TEXT_5012")
        global.msg[6] = scr_gettext("SCR_TEXT_5013")
        global.msg[7] = scr_gettext("SCR_TEXT_5014")
        global.msg[8] = scr_gettext("SCR_TEXT_5015")
        break
    case 811:
        global.msg[0] = scr_gettext("SCR_TEXT_5019")
        if (global.choice == 0)
        {
            scr_asgface(0, 0)
            global.msg[1] = scr_gettext("SCR_TEXT_5023")
            global.msg[2] = scr_gettext("SCR_TEXT_5024")
            global.msg[3] = scr_gettext("SCR_TEXT_5025")
            global.msg[4] = scr_gettext("SCR_TEXT_5026")
            global.msg[5] = scr_gettext("SCR_TEXT_5027")
            global.msg[6] = scr_gettext("SCR_TEXT_5028")
        }
        if (global.choice == 1)
        {
            scr_asgface(0, 3)
            global.msg[1] = scr_gettext("SCR_TEXT_5034")
            global.msg[2] = scr_gettext("SCR_TEXT_5035")
            scr_undface(3, 2)
            global.msg[4] = scr_gettext("SCR_TEXT_5037")
            scr_asgface(5, 2)
            global.msg[6] = scr_gettext("SCR_TEXT_5039")
            global.msg[7] = scr_gettext("SCR_TEXT_5040")
            scr_undface(8, 6)
            global.msg[9] = scr_gettext("SCR_TEXT_5042")
            scr_alface(10, 9)
            global.msg[11] = scr_gettext("SCR_TEXT_5044")
            global.msg[12] = scr_gettext("SCR_TEXT_5045")
            scr_undface(13, 2)
            global.msg[14] = scr_gettext("SCR_TEXT_5047")
            scr_papface(15, 0)
            global.msg[16] = scr_gettext("SCR_TEXT_5049")
            scr_undface(17, 9)
            global.msg[18] = scr_gettext("SCR_TEXT_5051")
            scr_sansface(19, 1)
            global.msg[20] = scr_gettext("SCR_TEXT_5053")
            scr_torface(21, 0)
            global.msg[22] = scr_gettext("SCR_TEXT_5055")
            scr_undface(23, 1)
            global.msg[24] = scr_gettext("SCR_TEXT_5057")
            scr_asgface(25, 0)
            global.msg[26] = scr_gettext("SCR_TEXT_5059")
            scr_undface(27, 6)
            global.msg[28] = scr_gettext("SCR_TEXT_5061")
            scr_torface(29, 1)
            global.msg[30] = scr_gettext("SCR_TEXT_5063")
            scr_asgface(31, 5)
            global.msg[32] = scr_gettext("SCR_TEXT_5065")
            scr_undface(33, 1)
            global.msg[34] = scr_gettext("SCR_TEXT_5067")
        }
        break
    case 812:
        global.msg[0] = scr_gettext("SCR_TEXT_5073")
        global.msg[1] = scr_gettext("SCR_TEXT_5074")
        global.msg[2] = scr_gettext("SCR_TEXT_5075")
        global.msg[3] = scr_gettext("SCR_TEXT_5076")
        global.msg[4] = scr_gettext("SCR_TEXT_5077")
        break
    case 813:
        global.msg[0] = scr_gettext("SCR_TEXT_5081")
        if (global.choice == 0)
            obj_underground_exit.con = 2
        if (global.choice == 1)
            obj_underground_exit.con = 10
        break
    case 814:
        global.msg[0] = scr_gettext("SCR_TEXT_5095")
        scr_asgface(1, 0)
        global.msg[2] = scr_gettext("SCR_TEXT_5097")
        scr_alface(3, 3)
        global.msg[4] = scr_gettext("SCR_TEXT_5099")
        global.msg[5] = scr_gettext("SCR_TEXT_5100")
        scr_undface(6, 1)
        global.msg[7] = scr_gettext("SCR_TEXT_5102")
        global.msg[8] = scr_gettext("SCR_TEXT_5103")
        global.msg[9] = scr_gettext("SCR_TEXT_5104")
        scr_papface(10, 0)
        global.msg[11] = scr_gettext("SCR_TEXT_5106")
        global.msg[12] = scr_gettext("SCR_TEXT_5107")
        scr_sansface(13, 1)
        global.msg[14] = scr_gettext("SCR_TEXT_5109")
        scr_papface(15, 0)
        global.msg[16] = scr_gettext("SCR_TEXT_5111")
        global.msg[17] = scr_gettext("SCR_TEXT_5112")
        scr_asgface(18, 0)
        global.msg[19] = scr_gettext("SCR_TEXT_5114")
        scr_torface(20, 0)
        global.msg[21] = scr_gettext("SCR_TEXT_5116")
        global.msg[22] = scr_gettext("SCR_TEXT_5117")
        scr_asgface(23, 3)
        global.msg[24] = scr_gettext("SCR_TEXT_5119")
        global.msg[25] = scr_gettext("SCR_TEXT_5120")
        global.msg[26] = scr_gettext("SCR_TEXT_5121")
        global.msg[27] = scr_gettext("SCR_TEXT_5122")
        global.msg[28] = scr_gettext("SCR_TEXT_5123")
        global.msg[29] = scr_gettext("SCR_TEXT_5124")
        global.msg[30] = scr_gettext("SCR_TEXT_5125")
        global.msg[31] = scr_gettext("SCR_TEXT_5126")
        global.msg[32] = scr_gettext("SCR_TEXT_5127")
        global.msg[33] = scr_gettext("SCR_TEXT_5128")
        break
    case 815:
        scr_papface(0, 0)
        global.msg[1] = scr_gettext("SCR_TEXT_5133")
        if (global.choice == 0)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_5136")
            global.msg[2] = scr_gettext("SCR_TEXT_5137")
            global.msg[3] = scr_gettext("SCR_TEXT_5138")
            global.msg[4] = scr_gettext("SCR_TEXT_5139")
        }
        if (global.choice == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_5143")
            global.msg[2] = scr_gettext("SCR_TEXT_5144")
            global.msg[3] = scr_gettext("SCR_TEXT_5145")
            global.msg[4] = scr_gettext("SCR_TEXT_5146")
        }
        break
    case 820:
        global.msg[0] = scr_gettext("SCR_TEXT_5151")
        global.msg[1] = scr_gettext("SCR_TEXT_5152")
        global.msg[2] = scr_gettext("SCR_TEXT_5153")
        global.msg[3] = scr_gettext("SCR_TEXT_5154")
        global.msg[4] = scr_gettext("SCR_TEXT_5155")
        global.msg[5] = scr_gettext("SCR_TEXT_5156")
        global.msg[6] = scr_gettext("SCR_TEXT_5157")
        break
    case 821:
        global.msg[0] = scr_gettext("SCR_TEXT_5161")
        if (global.choice == 0)
        {
            if instance_exists(obj_outsideworld_event)
                obj_outsideworld_event.con = 100
        }
        if (global.choice == 1)
        {
            if instance_exists(obj_outsideworld_event)
                obj_outsideworld_event.con = 200
        }
        break
    case 825:
        global.msg[0] = scr_gettext("SCR_TEXT_5182")
        global.msg[1] = scr_gettext("SCR_TEXT_5183")
        global.msg[2] = scr_gettext("SCR_TEXT_5184")
        break
    case 826:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                snd_play(snd_knock)
                doak = 1
            }
            global.msg[0] = scr_gettext("SCR_TEXT_5197")
            if instance_exists(obj_alabdoor_l)
            {
                obj_alabdoor_l.myinteract = 5
                obj_alabdoor_l.con = 2
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_5206")
        break
    case 827:
        global.msg[0] = scr_gettext("SCR_TEXT_5212")
        global.msg[1] = scr_gettext("SCR_TEXT_5213")
        global.msg[2] = scr_gettext("SCR_TEXT_5214")
        break
    case 828:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5220")
            global.msg[1] = scr_gettext("SCR_TEXT_5221")
            global.msg[2] = scr_gettext("SCR_TEXT_5222")
            global.msg[3] = scr_gettext("SCR_TEXT_5223")
            global.msg[4] = scr_gettext("SCR_TEXT_5224")
            global.msg[5] = scr_gettext("SCR_TEXT_5225")
            global.msg[6] = scr_gettext("SCR_TEXT_5226")
            global.msg[7] = scr_gettext("SCR_TEXT_5227")
            global.msg[8] = scr_gettext("SCR_TEXT_5228")
            global.msg[9] = scr_gettext("SCR_TEXT_5229")
            global.msg[10] = scr_gettext("SCR_TEXT_5230")
            global.msg[11] = scr_gettext("SCR_TEXT_5231")
            global.msg[12] = scr_gettext("SCR_TEXT_5232")
            global.msg[13] = scr_gettext("SCR_TEXT_5233")
            global.msg[14] = scr_gettext("SCR_TEXT_5234")
            global.msg[15] = scr_gettext("SCR_TEXT_5235")
            global.msg[16] = scr_gettext("SCR_TEXT_5236")
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_5240")
        break
    case 829:
        doak = 0
        noroom = 0
        if (global.flag[495] < 8)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5250")
            global.msg[1] = scr_gettext("SCR_TEXT_5251")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5255")
            global.msg[1] = scr_gettext("SCR_TEXT_5256")
        }
        break
    case 830:
        script_execute(scr_cost, 25)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 58)
                    if (noroom == 0)
                    {
                        global.gold -= 25
                        global.flag[495] += 1
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5274")
            if (afford == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_5275")
        }
        if (noroom == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_5277")
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_5280")
        break
    case 831:
        global.msg[0] = scr_gettext("SCR_TEXT_5285")
        global.msg[1] = scr_gettext("SCR_TEXT_5286")
        ossafe_ini_open("undertale.ini")
        bs = ini_read_real("Toriel", "Bscotch", 0)
        ossafe_ini_close()
        global.msg[2] = scr_gettext("SCR_TEXT_5290")
        global.msg[3] = scr_gettext("SCR_TEXT_5291")
        global.msg[4] = scr_gettext("SCR_TEXT_5292")
        global.msg[5] = scr_gettext("SCR_TEXT_5293")
        if (bs == 1)
            global.msg[6] = scr_gettext("SCR_TEXT_5295")
        else if (bs == 2)
            global.msg[6] = scr_gettext("SCR_TEXT_5296")
        else
            global.msg[6] = scr_gettext("SCR_TEXT_5294")
        global.msg[7] = scr_gettext("SCR_TEXT_5297")
        break
    case 832:
        ossafe_ini_open("undertale.ini")
        bs = ini_read_real("Toriel", "Bscotch", 0)
        ossafe_ini_close()
        if (global.choice == 0)
        {
            if (bs == 1)
                global.flag[46] = 1
            else
                global.flag[46] = 0
            global.msg[0] = scr_gettext("SCR_TEXT_5309")
            global.msg[1] = scr_gettext("SCR_TEXT_5310")
            global.msg[2] = scr_gettext("SCR_TEXT_5311")
            global.msg[3] = scr_gettext("SCR_TEXT_5312")
            global.msg[4] = scr_gettext("SCR_TEXT_5313")
            global.msg[5] = scr_gettext("SCR_TEXT_5314")
            global.msg[6] = scr_gettext("SCR_TEXT_5315")
            global.msg[7] = scr_gettext("SCR_TEXT_5316")
            global.msg[8] = scr_gettext("SCR_TEXT_5317")
        }
        if (global.choice == 1)
        {
            if (bs == 1)
                global.flag[46] = 0
            else
                global.flag[46] = 1
            global.msg[0] = scr_gettext("SCR_TEXT_5323")
            global.msg[1] = scr_gettext("SCR_TEXT_5324")
            global.msg[2] = scr_gettext("SCR_TEXT_5325")
            global.msg[3] = scr_gettext("SCR_TEXT_5326")
            ossafe_ini_open("undertale.ini")
            if (bs == 1)
                ini_write_real("Toriel", "Bscotch", 2)
            else
                ini_write_real("Toriel", "Bscotch", 1)
            ossafe_savedata_save()
            ossafe_ini_close()
        }
        break
    case 833:
        global.msg[0] = scr_gettext("SCR_TEXT_5337")
        global.msg[1] = scr_gettext("SCR_TEXT_5338")
        global.msg[2] = scr_gettext("SCR_TEXT_5339")
        break
    case 834:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5345")
            if instance_exists(obj_mettboss_event)
                obj_mettboss_event.con = 4.5
        }
        if (global.choice == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5353")
            obj_mettboss_event.con = 6
        }
        break
    case 835:
        global.msg[0] = scr_gettext("SCR_TEXT_5359")
        global.msg[1] = scr_gettext("SCR_TEXT_5360")
        global.msg[2] = scr_gettext("SCR_TEXT_5361")
        global.msg[3] = scr_gettext("SCR_TEXT_5362")
        global.msg[4] = scr_gettext("SCR_TEXT_5363")
        global.msg[5] = scr_gettext("SCR_TEXT_5364")
        global.msg[6] = scr_gettext("SCR_TEXT_5365")
        global.msg[7] = scr_gettext("SCR_TEXT_5366")
        global.msg[8] = scr_gettext("SCR_TEXT_5367")
        global.msg[9] = scr_gettext("SCR_TEXT_5368")
        global.msg[10] = scr_gettext("SCR_TEXT_5369")
        global.msg[11] = scr_gettext("SCR_TEXT_5370")
        global.msg[12] = scr_gettext("SCR_TEXT_5371")
        global.msg[13] = scr_gettext("SCR_TEXT_5372")
        break
    case 836:
        if (global.choice == 0)
        {
            scr_sansface(0, 1)
            global.msg[1] = scr_gettext("SCR_TEXT_5379")
            global.msg[2] = scr_gettext("SCR_TEXT_5380")
            global.msg[3] = scr_gettext("SCR_TEXT_5381")
            if instance_exists(obj_lastsans_trigger)
                obj_lastsans_trigger.con = 20
        }
        if (global.choice == 1)
        {
            scr_sansface(0, 1)
            global.msg[1] = scr_gettext("SCR_TEXT_5390")
            global.msg[2] = scr_gettext("SCR_TEXT_5391")
            global.msg[3] = scr_gettext("SCR_TEXT_5392")
            global.msg[4] = scr_gettext("SCR_TEXT_5393")
            if instance_exists(obj_lastsans_trigger)
                obj_lastsans_trigger.con = 21
        }
        break
    case 837:
        global.msg[0] = scr_gettext("SCR_TEXT_5402")
        scr_alface(1, 0)
        global.msg[2] = scr_gettext("SCR_TEXT_5404")
        global.msg[3] = scr_gettext("SCR_TEXT_5405")
        global.msg[4] = scr_gettext("SCR_TEXT_5406")
        global.msg[5] = scr_gettext("SCR_TEXT_5407")
        global.msg[6] = scr_gettext("SCR_TEXT_5408")
        global.msg[7] = scr_gettext("SCR_TEXT_5409")
        global.msg[8] = scr_gettext("SCR_TEXT_5410")
        global.msg[9] = scr_gettext("SCR_TEXT_5411")
        global.msg[10] = scr_gettext("SCR_TEXT_5412")
        break
    case 838:
        scr_alface(0, 5)
        if (global.choice == 0)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_5420")
            global.msg[2] = scr_gettext("SCR_TEXT_5421")
            global.msg[3] = scr_gettext("SCR_TEXT_5422")
            global.msg[4] = scr_gettext("SCR_TEXT_5423")
            global.msg[5] = scr_gettext("SCR_TEXT_5424")
            global.msg[6] = scr_gettext("SCR_TEXT_5425")
            global.msg[7] = scr_gettext("SCR_TEXT_5426")
            global.msg[8] = scr_gettext("SCR_TEXT_5427")
            global.msg[9] = scr_gettext("SCR_TEXT_5428")
            global.msg[10] = scr_gettext("SCR_TEXT_5429")
            global.msg[11] = scr_gettext("SCR_TEXT_5430")
            global.msg[12] = scr_gettext("SCR_TEXT_5431")
            global.msg[13] = scr_gettext("SCR_TEXT_5432")
            global.msg[14] = scr_gettext("SCR_TEXT_5433")
            global.msg[15] = scr_gettext("SCR_TEXT_5434")
            global.msg[16] = scr_gettext("SCR_TEXT_5435")
            global.msg[17] = scr_gettext("SCR_TEXT_5436")
            global.msg[18] = scr_gettext("SCR_TEXT_5437")
            global.msg[19] = scr_gettext("SCR_TEXT_5438")
            global.msg[20] = scr_gettext("SCR_TEXT_5439")
            global.msg[21] = scr_gettext("SCR_TEXT_5440")
        }
        if (global.choice == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_5445")
            global.msg[2] = scr_gettext("SCR_TEXT_5446")
            global.msg[3] = scr_gettext("SCR_TEXT_5447")
            global.msg[4] = scr_gettext("SCR_TEXT_5448")
            global.msg[5] = scr_gettext("SCR_TEXT_5449")
            global.msg[6] = scr_gettext("SCR_TEXT_5450")
        }
        break
    case 839:
        global.msg[0] = scr_gettext("SCR_TEXT_5455")
        global.msg[1] = scr_gettext("SCR_TEXT_5456")
        global.msg[2] = scr_gettext("SCR_TEXT_5457")
        global.msg[3] = scr_gettext("SCR_TEXT_5458")
        global.msg[4] = scr_gettext("SCR_TEXT_5459")
        global.msg[5] = scr_gettext("SCR_TEXT_5460")
        global.msg[6] = scr_gettext("SCR_TEXT_5461")
        global.msg[7] = scr_gettext("SCR_TEXT_5462")
        global.msg[8] = scr_gettext("SCR_TEXT_5463")
        break
    case 840:
        if (global.choice == 0)
        {
            global.flag[496] = 6
            global.msg[0] = scr_gettext("SCR_TEXT_5471")
        }
        if (global.choice == 1)
        {
            global.flag[496] = -1
            with (obj_onionsan_event)
                con = 25
            global.msg[0] = scr_gettext("SCR_TEXT_5477")
        }
        break
    case 845:
        global.msg[0] = scr_gettext("SCR_TEXT_5485")
        global.msg[1] = scr_gettext("SCR_TEXT_5486")
        global.msg[2] = scr_gettext("SCR_TEXT_5487")
        break
    case 846:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5493")
            global.msg[1] = scr_gettext("SCR_TEXT_5494")
            global.msg[2] = scr_gettext("SCR_TEXT_5495")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_5499")
        break
    case 847:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_5506")
        else
            global.msg[0] = scr_gettext("SCR_TEXT_5510")
        break
    case 850:
        with (obj_heatsflamesman)
            sprite_index = spr_heatsf_remember
        global.msg[0] = scr_gettext("SCR_TEXT_5517")
        global.msg[1] = scr_gettext("SCR_TEXT_5518")
        global.msg[2] = scr_gettext("SCR_TEXT_5519")
        break
    case 851:
        with (obj_heatsflamesman)
            sprite_index = spr_heatsf_shock
        if (global.choice == 0)
        {
            global.flag[434] = 1
            global.msg[0] = scr_gettext("SCR_TEXT_5527")
            global.msg[1] = scr_gettext("SCR_TEXT_5528")
        }
        if (global.choice == 1)
        {
            global.flag[434] = 2
            global.msg[0] = scr_gettext("SCR_TEXT_5533")
            global.msg[1] = scr_gettext("SCR_TEXT_5534")
        }
        break
    case 853:
        global.msg[0] = scr_gettext("SCR_TEXT_5541")
        global.msg[1] = scr_gettext("SCR_TEXT_5542")
        global.msg[2] = scr_gettext("SCR_TEXT_5543")
        break
    case 854:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5550")
            global.msg[1] = scr_gettext("SCR_TEXT_5551")
            global.msg[2] = scr_gettext("SCR_TEXT_5552")
            global.msg[3] = scr_gettext("SCR_TEXT_5553")
            global.msg[4] = scr_gettext("SCR_TEXT_5554")
            global.msg[5] = scr_gettext("SCR_TEXT_5555")
            global.msg[6] = scr_gettext("SCR_TEXT_5556")
            global.msg[7] = scr_gettext("SCR_TEXT_5557")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_5561")
        break
    case 860:
        global.msg[0] = scr_gettext("SCR_TEXT_5566")
        if (global.flag[262] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5569")
            global.msg[1] = scr_gettext("SCR_TEXT_5570")
            global.msg[2] = scr_gettext("SCR_TEXT_5571")
            if (global.flag[7] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_5575")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5581")
            if (global.flag[7] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_5585")
        }
        break
    case 861:
        if (global.choice == 0)
        {
            scr_itemremove(41)
            if (removed == 1)
            {
                global.gold += 99
                global.flag[262] = 1
                global.msg[0] = scr_gettext("SCR_TEXT_5598")
                global.msg[1] = scr_gettext("SCR_TEXT_5599")
                global.msg[2] = scr_gettext("SCR_TEXT_5600")
            }
            else
                global.msg[0] = scr_gettext("SCR_TEXT_5604")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_5609")
        break
    case 862:
        global.msg[0] = scr_gettext("SCR_TEXT_5614")
        if (global.flag[263] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5617")
            global.msg[1] = scr_gettext("SCR_TEXT_5618")
            global.msg[2] = scr_gettext("SCR_TEXT_5619")
            if (global.flag[7] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_5623")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5628")
            if (global.flag[7] == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_5632")
        }
        break
    case 863:
        if (global.choice == 0)
        {
            scr_itemremove(21)
            if (removed == 1)
            {
                global.gold += 99
                global.flag[263] = 1
                global.msg[0] = scr_gettext("SCR_TEXT_5646")
                global.msg[1] = scr_gettext("SCR_TEXT_5647")
                global.msg[2] = scr_gettext("SCR_TEXT_5648")
            }
            else
                global.msg[0] = scr_gettext("SCR_TEXT_5652")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_5657")
        break
    case 864:
        global.msg[0] = scr_gettext("SCR_TEXT_5662")
        global.msg[1] = scr_gettext("SCR_TEXT_5663")
        global.msg[2] = scr_gettext("SCR_TEXT_5664")
        global.msg[3] = scr_gettext("SCR_TEXT_5665")
        if (global.flag[7] == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_5669")
        break
    case 865:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5676")
            global.msg[1] = scr_gettext("SCR_TEXT_5677")
            type = 0
            scr_itemcheck(38)
            if (haveit == 1 && global.flag[264] == 0)
                type = 1
            scr_itemcheck(39)
            if (haveit == 1)
                type = 2
            scr_itemcheck(28)
            if (haveit == 1)
                type = 3
            scr_itemcheck(29)
            if (haveit == 1)
                type = 4
            scr_itemcheck(30)
            if (haveit == 1)
                type = 4
            scr_itemcheck(31)
            if (haveit == 1)
                type = 4
            scr_itemcheck(32)
            if (haveit == 1)
                type = 4
            scr_itemcheck(33)
            if (haveit == 1)
                type = 4
            scr_itemcheck(34)
            if (haveit == 1)
                type = 4
            if (type == 1 && global.flag[264] == 0)
            {
                scr_itemremove(38)
                scr_itemget(62)
                global.msg[0] = scr_gettext("SCR_TEXT_5705")
                global.msg[1] = scr_gettext("SCR_TEXT_5706")
                global.msg[2] = scr_gettext("SCR_TEXT_5707")
                global.msg[3] = scr_gettext("SCR_TEXT_5708")
                global.msg[4] = scr_gettext("SCR_TEXT_5709")
                global.msg[5] = scr_gettext("SCR_TEXT_5710")
                global.msg[6] = scr_gettext("SCR_TEXT_5711")
                global.msg[7] = scr_gettext("SCR_TEXT_5712")
                global.msg[8] = scr_gettext("SCR_TEXT_5713")
                global.msg[9] = scr_gettext("SCR_TEXT_5714")
                global.flag[264] = 1
            }
            if (type == 2)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_5720")
                global.msg[1] = scr_gettext("SCR_TEXT_5721")
            }
            if (type == 3)
            {
                scr_itemremove(28)
                global.msg[0] = scr_gettext("SCR_TEXT_5727")
                global.msg[1] = scr_gettext("SCR_TEXT_5728")
                global.msg[2] = scr_gettext("SCR_TEXT_5729")
            }
            if (type == 4)
            {
                rr = choose(29, 30, 31, 32, 33, 34)
                scr_itemget(rr)
                global.msg[0] = scr_gettext("SCR_TEXT_5736")
                global.msg[1] = scr_gettext("SCR_TEXT_5737")
                global.msg[2] = scr_gettext("SCR_TEXT_5738")
                if (noroom == 1)
                    global.msg[3] = scr_gettext("SCR_TEXT_5743")
                else
                    global.msg[3] = scr_gettext("SCR_TEXT_5747")
            }
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_5755")
        break
    case 866:
        global.msg[0] = scr_gettext("SCR_TEXT_5761")
        global.msg[1] = scr_gettext("SCR_TEXT_5762")
        global.msg[2] = scr_gettext("SCR_TEXT_5763")
        global.msg[3] = scr_gettext("SCR_TEXT_5764")
        if (global.flag[267] == 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5768")
            global.msg[1] = scr_gettext("SCR_TEXT_5769")
        }
        if (global.flag[267] == 1)
        {
            global.flag[267] = 2
            global.msg[0] = scr_gettext("SCR_TEXT_5774")
            global.msg[1] = scr_gettext("SCR_TEXT_5775")
            global.msg[2] = scr_gettext("SCR_TEXT_5776")
            global.msg[3] = scr_gettext("SCR_TEXT_5777")
            global.msg[4] = scr_gettext("SCR_TEXT_5778")
            global.msg[5] = scr_gettext("SCR_TEXT_5779")
        }
        break
    case 867:
        if (global.choice == 0)
        {
            if (global.gold >= 200)
            {
                global.gold -= 200
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = scr_gettext("SCR_TEXT_5791")
                if (global.flag[267] < 1)
                    global.flag[267] = 1
                obj_hotelreceptionist.con = 1
            }
            else
                global.msg[0] = scr_gettext("SCR_TEXT_5797")
        }
        else
            global.msg[0] = scr_gettext("SCR_TEXT_5802")
        break
    case 870:
        global.msg[0] = scr_gettext("SCR_TEXT_5807")
        global.msg[1] = scr_gettext("SCR_TEXT_5808")
        global.msg[2] = scr_gettext("SCR_TEXT_5809")
        global.msg[3] = scr_gettext("SCR_TEXT_5810")
        global.msg[4] = scr_gettext("SCR_TEXT_5811")
        global.msg[5] = scr_gettext("SCR_TEXT_5812")
        global.msg[6] = scr_gettext("SCR_TEXT_5813")
        break
    case 871:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_5819")
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5823")
            with (obj_playmovement)
                con = 240
        }
        break
    case 888:
        global.msg[0] = scr_gettext("SCR_TEXT_5829")
        global.msg[1] = scr_gettext("SCR_TEXT_5830")
        global.msg[2] = scr_gettext("SCR_TEXT_5831")
        global.msg[3] = scr_gettext("SCR_TEXT_5832")
        global.msg[4] = scr_gettext("SCR_TEXT_5833")
        global.msg[5] = scr_gettext("SCR_TEXT_5834")
        global.msg[6] = scr_gettext("SCR_TEXT_5835")
        global.msg[7] = scr_gettext("SCR_TEXT_5836")
        break
    case 889:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_5842")
        else
            global.msg[0] = scr_gettext("SCR_TEXT_5846")
        break
    case 890:
        global.msg[0] = scr_gettext("SCR_TEXT_5851")
        global.msg[1] = scr_gettext("SCR_TEXT_5852")
        global.msg[2] = scr_gettext("SCR_TEXT_5853")
        global.msg[3] = scr_gettext("SCR_TEXT_5854")
        global.msg[4] = scr_gettext("SCR_TEXT_5855")
        global.msg[5] = scr_gettext("SCR_TEXT_5856")
        global.msg[6] = scr_gettext("SCR_TEXT_5857")
        break
    case 891:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5863")
            global.msg[1] = scr_gettext("SCR_TEXT_5864")
            global.msg[2] = scr_gettext("SCR_TEXT_5865")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5869")
            global.msg[1] = scr_gettext("SCR_TEXT_5870")
        }
        break
    case 892:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5877")
            global.msg[1] = scr_gettext("SCR_TEXT_5878")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5882")
            global.msg[1] = scr_gettext("SCR_TEXT_5883")
        }
        break
    case 900:
        global.msg[0] = scr_gettext("SCR_TEXT_5888")
        global.msg[1] = scr_gettext("SCR_TEXT_5889")
        global.msg[2] = scr_gettext("SCR_TEXT_5890")
        global.msg[3] = scr_gettext("SCR_TEXT_5891")
        break
    case 901:
        if (global.choice == 0)
            global.msg[0] = scr_gettext("SCR_TEXT_5897")
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_5901")
            global.msg[1] = scr_gettext("SCR_TEXT_5902")
            global.msg[2] = scr_gettext("SCR_TEXT_5903")
        }
        break
    case 950:
        global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5913")
        global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5914")
        global.msg[2] = " "
        if (global.flag[295] >= 1)
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5919", string(global.flag[292]), string(global.flag[293]))
        if (global.flag[295] == 2 && global.flag[294] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5924")
            global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5925")
            global.msg[2] = scr_gettext("SCR_TEXT_dogshrine_5919", string(global.flag[292]), string(global.flag[293]))
            global.msg[3] = " "
            global.flag[295] = 3
        }
        if (global.flag[295] == 3 && global.flag[294] == 5)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5933")
            global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5919", string(global.flag[292]), string(global.flag[293]))
            global.msg[2] = " "
            global.flag[295] = 4
        }
        if (global.flag[295] == 4 && global.flag[294] == 10)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5941")
            global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5942")
            global.msg[2] = scr_gettext("SCR_TEXT_dogshrine_5943")
            global.msg[3] = scr_gettext("SCR_TEXT_dogshrine_5944")
            global.msg[4] = scr_gettext("SCR_TEXT_dogshrine_5919", string(global.flag[292]), string(global.flag[293]))
            global.msg[5] = " "
            global.flag[295] = 5
        }
        if (global.flag[295] >= 6)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5954")
            global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5955")
            if (global.flag[7] == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5958")
                global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5959")
            }
        }
        break
    case 951:
        if (doak == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5970")
            if (global.choice == 0)
            {
                if (global.flag[292] >= global.flag[293])
                    global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5977")
                if (global.gold <= 0)
                {
                    global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5982")
                    global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5983")
                }
                if (global.flag[292] < global.flag[293])
                {
                    if (global.gold >= 1)
                    {
                        snd_play(snd_item)
                        global.gold -= 1
                        global.flag[292] += 1
                        global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5993")
                        if (global.flag[295] == 0)
                        {
                            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_5997")
                            global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_5998")
                        }
                        if (global.flag[292] >= global.flag[293])
                        {
                            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6003")
                            global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_6004")
                            global.flag[294] += 1
                            trophy_unlock(("donate_" + string(global.flag[294])))
                        }
                    }
                }
                if (global.flag[295] == 0)
                    global.flag[295] = 1
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6015")
            doak = 1
        }
        break
    case 952:
        global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6024")
        global.msg[1] = " "
        break
    case 953:
        if (global.choice == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6031")
            global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_6032")
            global.flag[296] = 1
        }
        if (global.choice == 1)
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6037")
        break
    case 955:
        global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6045")
        global.msg[1] = scr_gettext("SCR_TEXT_dogshrine_6046")
        global.msg[2] = scr_gettext("SCR_TEXT_dogshrine_6047")
        global.msg[3] = scr_gettext("SCR_TEXT_dogshrine_6048")
        global.msg[4] = " "
        break
    case 956:
        p = caster_get_pitch(global.currentsong)
        p2 = (p + 0.1)
        if (p2 >= 1.5)
            p2 = 1.5
        if (global.choice == 0)
        {
            caster_set_pitch(global.currentsong, p2)
            global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6059")
        }
        if (global.choice == 1)
        {
            if (p == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6065")
            else
            {
                caster_set_pitch(global.currentsong, 1)
                global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6070")
            }
        }
        break
    case 960:
        global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6076", string(global.flag[292]))
        global.msg[1] = " "
        break
    case 961:
        if (doak == 0)
        {
            if (global.choice == 0)
            {
                if (global.flag[292] < 350)
                {
                    if (global.gold >= 1)
                    {
                        global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6095")
                        global.flag[292] += 1
                        if (global.flag[292] >= 2)
                            trophy_unlock("donate_1")
                        if (global.flag[292] >= 6)
                            trophy_unlock("donate_2")
                        if (global.flag[292] >= 12)
                            trophy_unlock("donate_3")
                        if (global.flag[292] >= 20)
                            trophy_unlock("donate_4")
                        if (global.flag[292] >= 30)
                            trophy_unlock("donate_5")
                        if (global.flag[292] >= 43)
                            trophy_unlock("donate_6")
                        if (global.flag[292] >= 59)
                            trophy_unlock("donate_7")
                        if (global.flag[292] >= 78)
                            trophy_unlock("donate_8")
                        if (global.flag[292] >= 100)
                            trophy_unlock("donate_9")
                        if (global.flag[292] >= 125)
                            trophy_unlock("donate_10")
                        if (global.flag[292] >= 155)
                            trophy_unlock("donate_11")
                        if (global.flag[292] >= 190)
                            trophy_unlock("donate_12")
                        if (global.flag[292] >= 230)
                            trophy_unlock("donate_13")
                        if (global.flag[292] >= 280)
                            trophy_unlock("donate_14")
                        if (global.flag[292] >= 350)
                            trophy_unlock("donate_15")
                        global.gold -= 1
                    }
                    else
                        global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6101")
                }
                else
                    global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6106")
            }
            if (global.choice == 1)
                global.msg[0] = scr_gettext("SCR_TEXT_dogshrine_6112")
            doak = 1
        }
        break
    case 1001:
        global.msg[0] = scr_gettext("SCR_TEXT_5937")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1002:
        global.msg[0] = scr_gettext("SCR_TEXT_5948")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1003:
        global.msg[0] = scr_gettext("SCR_TEXT_5959")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1004:
        global.msg[0] = scr_gettext("SCR_TEXT_5970")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1005:
        global.msg[0] = scr_gettext("SCR_TEXT_5981")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1006:
        global.msg[0] = scr_gettext("SCR_TEXT_5991")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1007:
        global.msg[0] = scr_gettext("SCR_TEXT_6001")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1008:
        global.msg[0] = scr_gettext("SCR_TEXT_6011")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1009:
        global.msg[0] = scr_gettext("SCR_TEXT_6021")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1010:
        global.msg[0] = scr_gettext("SCR_TEXT_6031")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1011:
        global.msg[0] = scr_gettext("SCR_TEXT_6041")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1012:
        global.msg[0] = scr_gettext("SCR_TEXT_6051")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1013:
        global.msg[0] = scr_gettext("SCR_TEXT_6061")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1014:
        global.msg[0] = scr_gettext("SCR_TEXT_6071")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 1
        break
    case 1015:
        global.msg[0] = scr_gettext("SCR_TEXT_6081")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1016:
        global.msg[0] = scr_gettext("SCR_TEXT_6091")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1017:
        global.msg[0] = scr_gettext("SCR_TEXT_6101")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1018:
        if (global.flag[57] == 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_6114")
            global.choices[0] = 1
            global.choices[1] = 1
            global.choices[2] = 0
            global.choices[3] = 1
            global.choices[4] = 1
            global.choices[5] = 0
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_6124")
            global.choices[0] = 1
            global.choices[1] = 1
            global.choices[2] = 0
            global.choices[3] = 1
            global.choices[4] = 1
            global.choices[5] = 0
        }
        break
    case 1019:
        global.msg[0] = scr_gettext("SCR_TEXT_6135")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1020:
        global.msg[0] = scr_gettext("SCR_TEXT_6145")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1021:
        global.msg[0] = scr_gettext("SCR_TEXT_6155")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1022:
        global.msg[0] = scr_gettext("SCR_TEXT_6165")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1023:
        global.msg[0] = scr_gettext("SCR_TEXT_6175")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1024:
        global.msg[0] = scr_gettext("SCR_TEXT_6185")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1025:
        global.msg[0] = scr_gettext("SCR_TEXT_6196")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        if (scr_murderlv() >= 7)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_6206")
            global.choices[0] = 1
            global.choices[1] = 0
            global.choices[2] = 0
            global.choices[3] = 0
            global.choices[4] = 0
            global.choices[5] = 0
        }
        break
    case 1026:
        if instance_exists(obj_moldsmalx)
        {
            if (obj_moldsmalx.stage == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_6221")
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 0
                global.choices[5] = 0
            }
            else if (global.flag[74] == 0)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_6234")
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 1
                global.choices[5] = 0
            }
            else
            {
                global.msg[0] = scr_gettext("SCR_TEXT_6244")
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 1
                global.choices[5] = 0
            }
        }
        break
    case 1028:
        global.msg[0] = scr_gettext("SCR_TEXT_6259")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1029:
        global.msg[0] = scr_gettext("SCR_TEXT_6269")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1031:
        global.msg[0] = scr_gettext("SCR_TEXT_6279")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1032:
        global.msg[0] = scr_gettext("SCR_TEXT_6289")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        if instance_exists(obj_undyneboss)
        {
            if (obj_undyneboss.con >= 50)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_6300")
                global.choices[0] = 1
                global.choices[1] = 0
                global.choices[2] = 0
                global.choices[3] = 0
                global.choices[4] = 0
                global.choices[5] = 0
            }
        }
        break
    case 1033:
        global.msg[0] = scr_gettext("SCR_TEXT_6313")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1034:
        global.msg[0] = scr_gettext("SCR_TEXT_6323")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1035:
        global.msg[0] = scr_gettext("SCR_TEXT_6333")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1036:
        global.msg[0] = scr_gettext("SCR_TEXT_6343")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1037:
        global.msg[0] = scr_gettext("SCR_TEXT_6353")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1038:
        global.msg[0] = scr_gettext("SCR_TEXT_6363")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1039:
        cashfactor = "NaN"
        global.msg[0] = scr_gettext("SCR_TEXT_6374", string(global.flag[382]))
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1040:
        global.msg[0] = scr_gettext("SCR_TEXT_6384")
        if (global.flag[385] > 0)
            global.msg[0] = scr_gettext("SCR_TEXT_6387")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1041:
        global.msg[0] = scr_gettext("SCR_TEXT_6398")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1042:
        global.msg[0] = scr_gettext("SCR_TEXT_6408")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1043:
        global.msg[0] = scr_gettext("SCR_TEXT_6418")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1044:
        global.msg[0] = scr_gettext("SCR_TEXT_6428")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1045:
        global.msg[0] = scr_gettext("SCR_TEXT_6438")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1046:
        global.msg[0] = scr_gettext("SCR_TEXT_6449")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1047:
        global.msg[0] = scr_gettext("SCR_TEXT_6460")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1048:
        global.msg[0] = scr_gettext("SCR_TEXT_6470")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1049:
        global.msg[0] = scr_gettext("SCR_TEXT_6480")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1050:
        global.msg[0] = scr_gettext("SCR_TEXT_6490")
        if (global.flag[424] > 0)
            global.msg[0] = scr_gettext("SCR_TEXT_6493")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1051:
        global.msg[0] = scr_gettext("SCR_TEXT_6504")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1052:
        global.msg[0] = scr_gettext("SCR_TEXT_6514")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1053:
        global.msg[0] = scr_gettext("SCR_TEXT_6524")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 1
        break
    case 1054:
        global.msg[0] = scr_gettext("SCR_TEXT_6534")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 1
        break
    case 1055:
        global.msg[0] = scr_gettext("SCR_TEXT_6544")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1056:
        global.msg[0] = scr_gettext("SCR_TEXT_6554")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        if instance_exists(obj_memoryhead)
        {
            if (obj_memoryhead.coherent == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_6565")
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 0
                global.choices[5] = 0
            }
        }
        break
    case 1057:
        global.msg[0] = scr_gettext("SCR_TEXT_6579")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1058:
        global.msg[0] = scr_gettext("SCR_TEXT_6589")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1059:
        global.msg[0] = scr_gettext("SCR_TEXT_6599")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1060:
        global.msg[0] = scr_gettext("SCR_TEXT_6609")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1061:
        global.msg[0] = scr_gettext("SCR_TEXT_6619")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1062:
        global.msg[0] = scr_gettext("SCR_TEXT_6629")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1063:
        global.msg[0] = scr_gettext("SCR_TEXT_6639")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1064:
        global.msg[0] = scr_gettext("SCR_TEXT_6649")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1065:
        global.msg[0] = scr_gettext("SCR_TEXT_6659")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1066:
        global.msg[0] = scr_gettext("SCR_TEXT_6669")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1067:
        global.msg[0] = scr_gettext("SCR_TEXT_6679")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1068:
        global.msg[0] = scr_gettext("SCR_TEXT_6689")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1070:
        global.msg[0] = scr_gettext("SCR_TEXT_6699")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1071:
        global.msg[0] = scr_gettext("SCR_TEXT_6709")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1072:
        global.msg[0] = scr_gettext("SCR_TEXT_6720")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1073:
        global.msg[0] = scr_gettext("SCR_TEXT_6731")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1074:
        global.msg[0] = scr_gettext("SCR_TEXT_6741")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1075:
        global.msg[0] = scr_gettext("SCR_TEXT_6751")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1076:
        global.msg[0] = scr_gettext("SCR_TEXT_6761")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1080:
        global.msg[0] = scr_gettext("SCR_TEXT_6772")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1081:
        global.msg[0] = scr_gettext("SCR_TEXT_6783")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1099:
        global.msg[0] = scr_gettext("SCR_TEXT_6793")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1100:
        global.msg[0] = scr_gettext("SCR_TEXT_6804")
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        if (global.flag[501] == 0)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_6814")
            global.choices[0] = 1
            global.choices[1] = 0
            global.choices[2] = 0
            global.choices[3] = 0
            global.choices[4] = 0
            global.choices[5] = 0
        }
        if (global.flag[501] == 1)
        {
            if (global.flag[505] == 0)
                global.msg[0] = scr_gettext("SCR_TEXT_6825")
            else
                global.msg[0] = scr_gettext("SCR_TEXT_6826")
            if (global.flag[506] == 0)
                global.msg[0] += scr_gettext("SCR_TEXT_6828")
            else
                global.msg[0] += scr_gettext("SCR_TEXT_6829")
            if (global.flag[507] == 0)
                global.msg[0] += scr_gettext("SCR_TEXT_6831")
            else
                global.msg[0] += scr_gettext("SCR_TEXT_6832")
            if (global.flag[508] == 0)
                global.msg[0] += scr_gettext("SCR_TEXT_6834")
            else
                global.msg[0] += scr_gettext("SCR_TEXT_6835")
            global.choices[0] = 1
            global.choices[1] = 1
            global.choices[2] = 1
            global.choices[3] = 1
            global.choices[4] = 1
            global.choices[5] = 1
        }
        if (global.flag[501] > 1)
        {
            if (global.flag[501] == 2)
                global.msg[0] = scr_gettext("SCR_TEXT_6848")
            if (global.flag[501] == 3)
                global.msg[0] = scr_gettext("SCR_TEXT_6849")
            global.choices[0] = 1
            global.choices[1] = 0
            global.choices[2] = 0
            global.choices[3] = 0
            global.choices[4] = 0
            global.choices[5] = 0
        }
        break
    case 1501:
        global.msg[0] = scr_gettext("SCR_TEXT_6868")
        if (doak == 0)
        {
            doak = 1
            global.flag[40] += 1
        }
        if (global.flag[40] == 1)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_6876")
            global.msg[2] = scr_gettext("SCR_TEXT_6877")
            global.msg[3] = scr_gettext("SCR_TEXT_6878")
            global.msg[4] = scr_gettext("SCR_TEXT_6879")
            global.msg[5] = scr_gettext("SCR_TEXT_6880")
            global.msg[6] = scr_gettext("SCR_TEXT_6881")
            global.msg[7] = scr_gettext("SCR_TEXT_6882")
        }
        if (global.flag[40] == 2)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_6886")
            global.msg[2] = scr_gettext("SCR_TEXT_6887")
            global.msg[3] = scr_gettext("SCR_TEXT_6888")
            global.msg[4] = scr_gettext("SCR_TEXT_6889")
            global.msg[5] = scr_gettext("SCR_TEXT_6890")
            global.msg[6] = scr_gettext("SCR_TEXT_6891")
            global.msg[7] = scr_gettext("SCR_TEXT_6892")
        }
        if (global.flag[40] == 3)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_6896")
            global.msg[2] = scr_gettext("SCR_TEXT_6897")
            global.msg[3] = scr_gettext("SCR_TEXT_6898")
            global.msg[4] = scr_gettext("SCR_TEXT_6899")
            global.msg[5] = scr_gettext("SCR_TEXT_6900")
            global.msg[6] = scr_gettext("SCR_TEXT_6901")
            global.msg[7] = scr_gettext("SCR_TEXT_6902")
            global.msg[8] = scr_gettext("SCR_TEXT_6903")
            global.msg[9] = scr_gettext("SCR_TEXT_6904")
            global.msg[10] = scr_gettext("SCR_TEXT_6905")
        }
        if (global.flag[40] > 3)
        {
            global.msg[1] = scr_gettext("SCR_TEXT_6909")
            global.msg[2] = scr_gettext("SCR_TEXT_6910")
            global.msg[3] = scr_gettext("SCR_TEXT_6911")
            global.msg[4] = scr_gettext("SCR_TEXT_6912")
            global.msg[5] = scr_gettext("SCR_TEXT_6913")
            global.msg[6] = scr_gettext("SCR_TEXT_6914")
            global.msg[7] = scr_gettext("SCR_TEXT_6915")
        }
        break
    case 1502:
        global.msg[0] = scr_gettext("SCR_TEXT_6920")
        global.msg[1] = scr_gettext("SCR_TEXT_6921")
        global.msg[2] = scr_gettext("SCR_TEXT_6922")
        global.msg[3] = scr_gettext("SCR_TEXT_6923")
        global.msg[4] = scr_gettext("SCR_TEXT_6924")
        global.msg[5] = scr_gettext("SCR_TEXT_6925")
        global.msg[6] = scr_gettext("SCR_TEXT_6926")
        global.msg[7] = scr_gettext("SCR_TEXT_6927")
        break
    case 1503:
        global.msg[0] = scr_gettext("SCR_TEXT_6931")
        global.msg[1] = scr_gettext("SCR_TEXT_6932")
        global.msg[2] = scr_gettext("SCR_TEXT_6933")
        global.msg[3] = scr_gettext("SCR_TEXT_6934")
        global.msg[4] = scr_gettext("SCR_TEXT_6935")
        global.msg[5] = scr_gettext("SCR_TEXT_6936")
        global.msg[6] = scr_gettext("SCR_TEXT_6937")
        global.msg[7] = scr_gettext("SCR_TEXT_6938")
        break
    case 1504:
        global.flag[42] = 1
        global.msg[0] = scr_gettext("SCR_TEXT_6943")
        global.msg[1] = scr_gettext("SCR_TEXT_6944")
        global.msg[2] = scr_gettext("SCR_TEXT_6945")
        global.msg[3] = scr_gettext("SCR_TEXT_6946")
        global.msg[4] = scr_gettext("SCR_TEXT_6947")
        global.msg[5] = scr_gettext("SCR_TEXT_6948")
        global.msg[6] = scr_gettext("SCR_TEXT_6949")
        global.msg[7] = scr_gettext("SCR_TEXT_6950")
        global.msg[8] = scr_gettext("SCR_TEXT_6951")
        global.msg[9] = scr_gettext("SCR_TEXT_6952")
        break
    case 1505:
        if (doak == 0)
        {
            doak = 1
            global.flag[41] += 1
        }
        if (global.flag[41] == 1)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_6963")
            global.msg[1] = scr_gettext("SCR_TEXT_6964")
            global.msg[2] = scr_gettext("SCR_TEXT_6965")
            global.msg[3] = scr_gettext("SCR_TEXT_6966")
            global.msg[4] = scr_gettext("SCR_TEXT_6967")
            global.msg[5] = scr_gettext("SCR_TEXT_6968")
            global.msg[6] = scr_gettext("SCR_TEXT_6969")
            global.msg[7] = scr_gettext("SCR_TEXT_6970")
        }
        if (global.flag[41] == 2)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_6974")
            global.msg[1] = scr_gettext("SCR_TEXT_6975")
            global.msg[2] = scr_gettext("SCR_TEXT_6976")
            global.msg[3] = scr_gettext("SCR_TEXT_6977")
            global.msg[4] = scr_gettext("SCR_TEXT_6978")
            global.msg[5] = scr_gettext("SCR_TEXT_6979")
            if (global.flag[42] == 1)
            {
                global.msg[3] = scr_gettext("SCR_TEXT_6982")
                global.msg[4] = scr_gettext("SCR_TEXT_6983")
                global.msg[5] = scr_gettext("SCR_TEXT_6984")
                global.msg[6] = scr_gettext("SCR_TEXT_6985")
            }
        }
        break
    case 1506:
        if (global.flag[45] == 4)
        {
            global.msg[0] = scr_gettext("SCR_TEXT_6994")
            global.msg[1] = scr_gettext("SCR_TEXT_6995")
            global.msg[2] = scr_gettext("SCR_TEXT_6996")
        }
        else
        {
            global.msg[0] = scr_gettext("SCR_TEXT_7000")
            global.msg[1] = scr_gettext("SCR_TEXT_7001")
            global.msg[2] = scr_gettext("SCR_TEXT_7002")
            scr_itemcheck(27)
            if (haveit == 1)
            {
                global.msg[0] = scr_gettext("SCR_TEXT_7007")
                global.msg[1] = scr_gettext("SCR_TEXT_7008")
                global.msg[2] = scr_gettext("SCR_TEXT_7009")
            }
        }
        break
    case 1507:
        global.faceemotion = 99
        global.msg[0] = scr_gettext("SCR_TEXT_7016")
        global.msg[1] = scr_gettext("SCR_TEXT_7017")
        global.msg[2] = scr_gettext("SCR_TEXT_7018")
        global.msg[3] = scr_gettext("SCR_TEXT_7019")
        global.msg[4] = scr_gettext("SCR_TEXT_7020")
        global.msg[5] = scr_gettext("SCR_TEXT_7021")
        break
    case 1508:
        global.msg[0] = scr_gettext("SCR_TEXT_7025")
        global.msg[1] = scr_gettext("SCR_TEXT_7026")
        global.msg[2] = scr_gettext("SCR_TEXT_7027")
        break
    case 1510:
        if (global.flag[7] == 0)
            scr_papcall()
        else
            global.msg[0] = scr_gettext("SCR_TEXT_7040")
        break
    case 1515:
        scr_torcall()
        break
    case 1520:
        global.msg[0] = scr_gettext("SCR_TEXT_7052")
        break
    case 2001:
        global.msg[0] = scr_gettext("SCR_TEXT_306")
        global.msg[1] = scr_gettext("SCR_TEXT_307")
        break
    case 2002:
        global.faceplate = 1
        global.msg[0] = scr_gettext("SCR_TEXT_6965")
        global.msg[1] = scr_gettext("SCR_TEXT_6966")
        global.msg[2] = scr_gettext("SCR_TEXT_6967")
        global.msg[3] = (scr_gettext("SCR_TEXT_6968") + "%")
        break
    case 3002:
        global.msg[0] = scr_gettext("SCR_TEXT_7079")
        global.msg[1] = scr_gettext("SCR_TEXT_7080")
        break
    case 3003:
        global.msg[0] = scr_gettext("SCR_TEXT_7084")
        global.msg[1] = scr_gettext("SCR_TEXT_7085")
        break
    case 3004:
        global.msg[0] = scr_gettext("SCR_TEXT_7089")
        global.msg[1] = scr_gettext("SCR_TEXT_7090")
        break
    case 3005:
        global.msg[0] = scr_gettext("SCR_TEXT_7094")
        global.msg[1] = scr_gettext("SCR_TEXT_7095")
        break
    case 3006:
        global.msg[0] = scr_gettext("SCR_TEXT_7099")
        global.msg[1] = scr_gettext("SCR_TEXT_7100")
        break
    case 3007:
        global.msg[0] = scr_gettext("SCR_TEXT_7104")
        global.msg[1] = scr_gettext("SCR_TEXT_7105")
        break
    case 9999:
        i = 0
        fileid = file_text_open_read("testlines.txt")
        while (file_text_eof(fileid) == 0)
        {
            global.msg[i] = file_text_read_string(fileid)
            file_text_readln(fileid)
            i += 1
        }
        file_text_close(fileid)
        break
}

