i = 0
loop = true
noroom = false
global.item[12] = 999
while (loop == true)
{
    if (global.keyitem[i] == 0)
    {
        global.keyitem[i] = argument0
        break
    }
    if (i == 12)
    {
        noroom = true
        break
    }
    i += 1
}
script_execute(scr_keyiteminfo_all)
