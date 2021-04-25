var bc;
i = 0
loop = 1
noroom = 0
global.item[8] = 999
while (loop == 1)
{
    if (global.item[i] == 0)
    {
        global.item[i] = argument0
        break
    }
    if (i == 8)
    {
        script_execute(scr_itemnospace)
        break
    }
    i += 1
}
ossafe_ini_open("undertale.ini")
bc = ini_read_real("General", "BC", 0)
if (bc < 4)
{
    bc++
    ini_write_real("General", "BC", bc)
    ossafe_ini_close()
    ossafe_savedata_save()
}
else
    ossafe_ini_close()
if (bc >= 1)
    trophy_unlock("item_1")
if (bc >= 2)
    trophy_unlock("item_2")
if (bc >= 3)
    trophy_unlock("item_3")
if (bc >= 4)
    trophy_unlock("item_4")
script_execute(scr_itemnameb)
script_execute(scr_itemname)
