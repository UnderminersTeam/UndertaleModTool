var i, desc;
global.msg[0] = scr_gettext("scr_itemdesc_2")
i = 0
while (true)
{
    desc = scr_gettext(((("item_desc_" + string(argument0)) + "_") + string(i)))
    if (string_length(desc) == 0)
        break
    global.msg[i] = desc
    i++
}
