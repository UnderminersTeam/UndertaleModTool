i = 0
loop = true
noroom = false
global.item[12] = 999
while (loop == true)
{
    if (global.item[i] == 0)
    {
        global.item[i] = argument0
        break
    }
    if (i == 12)
    {
        noroom = true
        break
    }
    i += 1
}
script_execute(scr_iteminfo_all)
