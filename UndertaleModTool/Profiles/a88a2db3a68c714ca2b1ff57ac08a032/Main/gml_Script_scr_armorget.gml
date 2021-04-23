i = 0
loop = true
noroom = false
global.armor[12] = 999
while (loop == true)
{
    if (global.armor[i] == 0)
    {
        global.armor[i] = argument0
        break
    }
    if (i == 12)
    {
        noroom = true
        break
    }
	i += 1
}
script_execute(scr_armorinfo_all)


