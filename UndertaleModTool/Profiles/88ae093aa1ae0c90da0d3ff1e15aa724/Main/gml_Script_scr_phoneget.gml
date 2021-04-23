i = 0
loop = 1
global.phone[8] = 999
while (loop == 1)
{
    if (global.phone[i] == 0)
    {
        global.phone[i] = argument0
        break
    }
	if (i == 8)
    {
        script_execute(scr_itemnospace)
        break
    }
    i += 1
}
script_execute(scr_phonename)
