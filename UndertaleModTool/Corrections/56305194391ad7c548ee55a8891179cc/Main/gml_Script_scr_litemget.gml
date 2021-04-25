i = 0
loop = true
noroom = false
global.litem[8] = 999
while (loop == true)
{
    if (global.litem[i] == 0)
    {
        global.litem[i] = argument0
        break
    }
    if (i == 8)
    {
        noroom = true
        break
    }
    i += 1
}
scr_litemname()
