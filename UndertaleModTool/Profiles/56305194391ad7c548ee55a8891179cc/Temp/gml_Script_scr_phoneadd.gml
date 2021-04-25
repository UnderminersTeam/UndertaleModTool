i = 0
loop = true
noroom = false
global.phone[8] = 999
while (loop == true)
{
    if (global.phone[i] == 0)
    {
        global.phone[i] = argument0
        break
    }
    if (i == 8)
    {
        noroom = true
        break
    }
    i += 1
}
scr_phonename()
