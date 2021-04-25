spell = argument0
caster = argument1
star = global.chartarget[argument1]
spelltext = " "
switch spell
{
    case 0:
        break
    case 1:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_14_0"), global.charname[global.char[caster]])
        break
    case 2:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_18_0"), global.charname[global.char[caster]])
        break
    case 3:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_23_0"), global.charname[global.char[caster]])
        scr_retarget_spell()
        if (global.monster[star] == true)
        {
            if (global.monsterstatus[star] == true)
            {
            }
            else
            {
                global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_33_0"), global.charname[global.char[caster]])
                if (global.mercymod[star] >= 100)
                    global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_36_0"), global.charname[global.char[caster]])
            }
        }
        break
    case 4:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_43_0"), global.charname[global.char[caster]])
        break
    case 5:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_47_0"), global.charname[global.char[caster]])
        break
    case 6:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_51_0"), global.charname[global.char[caster]])
        break
    case 100:
        cancelattack = false
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_58_0"), global.charname[global.char[caster]], global.monstername[star])
        scr_retarget_spell()
        if (global.mercymod[star] >= 100)
            global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_62_0"), global.charname[global.char[caster]], global.monstername[star])
        else
        {
            global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_66_0"), global.charname[global.char[caster]], global.monstername[star])
            if (global.monsterstatus[star] == true)
            {
                global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_69_0"), global.charname[global.char[caster]], global.monstername[star])
                global.msg[1] = scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_70_0")
            }
        }
        if (cancelattack == true)
            global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_73_0"), global.charname[global.char[caster]])
        break
    case 201:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_78_0"), global.charname[global.char[caster]])
        break
    case 202:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_82_0"), global.charname[global.char[caster]])
        break
    case 203:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_86_0"), global.charname[global.char[caster]])
        global.msg[1] = scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_87_0")
        spec_shard = 0
        for (_en = 0; _en < 3; _en += 1)
        {
            shardtest[_en] = 0
            if (global.monster[_en] == true)
            {
                if (global.monstertype[_en] == 5)
                {
                    spec_shard = 1
                    shardtest[_en] = 1
                    global.mercymod[_en] = 200
                }
            }
        }
        if (spec_shard > 0)
        {
            scr_itemshift(global.bmenucoord[4, argument1], 0)
            global.msg[1] = ""
            global.msg[2] = scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_109_0")
            for (_j = 0; _j < 3; _j += 1)
            {
                if (shardtest[_j] == 1)
                    global.msg[1] += scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_112_0"), global.monstername[_j])
            }
            global.msg[1] += "/"
        }
        break
    case 204:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_122_0"), global.charname[global.char[caster]])
        global.msg[1] = scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_123_0")
        spec_shard = 0
        for (_en = 0; _en < 3; _en += 1)
        {
            shardtest[_en] = 0
            if (global.monster[_en] == true)
            {
                if (global.monstertype[_en] == 5)
                {
                    spec_shard = 1
                    shardtest[_en] = 1
                    global.monsterstatus[_en] = true
                    global.monstercomment[_en] = scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_135_0")
                }
                if (global.monstertype[_en] == 6)
                {
                    spec_shard = 1
                    shardtest[_en] = 2
                }
                if (global.monstertype[_en] == 7 || global.monstertype[_en] == 16)
                {
                    spec_shard = 1
                    shardtest[_en] = 3
                    with (global.monsterinstance[_en])
                    {
                        battlecancel = 1
                        if (manual == 0)
                        {
                            manual = 1
                            scr_mercyadd(myself, 50)
                        }
                    }
                }
            }
        }
        if (spec_shard > 0)
        {
            global.msg[1] = ""
            for (_j = 0; _j < 3; _j += 1)
            {
                if (shardtest[_j] == 1)
                    global.msg[1] += scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_168_0"), global.monstername[_j])
                if (shardtest[_j] == 2)
                    global.msg[1] += scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_169_0"), global.monstername[_j])
                if (shardtest[_j] == 3)
                    global.msg[1] += scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_170_0"), global.monstername[_j])
            }
            global.msg[1] += scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_172_0")
        }
        break
    case 205:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_179_0"), global.charname[global.char[caster]])
        break
    case 206:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_183_0"), global.charname[global.char[caster]])
        break
    case 207:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_187_0"), global.charname[global.char[caster]])
        break
    case 208:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_191_0"), global.charname[global.char[caster]])
        break
    case 209:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_194_0"), global.charname[global.char[caster]])
        break
    case 210:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_197_0"), global.charname[global.char[caster]])
        break
    case 211:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_200_0"), global.charname[global.char[caster]])
        break
    case 212:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_203_0"), global.charname[global.char[caster]])
        break
    case 213:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_206_0"), global.charname[global.char[caster]])
        break
    case 214:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_209_0"), global.charname[global.char[caster]])
        break
    case 215:
        global.msg[0] = scr_84_get_subst_string(scr_84_get_lang_string("scr_spelltext_slash_scr_spelltext_gml_212_0"), global.charname[global.char[caster]])
        break
}

