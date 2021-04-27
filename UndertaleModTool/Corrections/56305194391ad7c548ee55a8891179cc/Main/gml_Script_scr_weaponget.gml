i = 0
loop = true
noroom = false
global.weapon[12] = 999
while (loop == true)
{
    if (global.weapon[i] == 0)
    {
        global.weapon[i] = argument0
        break
    }
    if (i == 12)
    {
        noroom = true
        break
    }
    i += 1
}
script_execute(scr_weaponinfo_all)
