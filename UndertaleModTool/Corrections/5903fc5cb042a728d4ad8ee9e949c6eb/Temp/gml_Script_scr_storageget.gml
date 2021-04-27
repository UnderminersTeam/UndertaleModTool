i = 0
loop = 1
noroom = 0
global.flag[(argument1 + 10)] = 999
while (loop == 1)
{
    if (global.flag[(i + argument1)] == 0)
    {
        global.flag[(i + argument1)] = argument0
        break
    }
    if (i == 10)
    {
        script_execute(scr_itemnospace)
        break
    }
    i += 1
}
script_execute(scr_itemnameb)
script_execute(scr_storagename, 300)
