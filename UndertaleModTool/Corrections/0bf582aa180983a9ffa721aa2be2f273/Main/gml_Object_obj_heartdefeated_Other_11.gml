var msgid_base, i, text, xx;
if (floor((msgnum / 100)) == 1)
    global.typer = 32
else if (floor((msgnum / 100)) == 3)
    global.typer = 110
else
    global.typer = 30
msgid_base = (("obj_heartdefeated_" + string(msgnum)) + "_")
for (i = 0; true; i++)
{
    text = scr_gettext((msgid_base + string(i)))
    if (text == "")
        break
    global.msg[i] = text
}
dingus = 1
script_execute(SCR_TEXTTYPE, global.typer)
global.msc = 0
xx = 100
if (global.language == "ja")
    xx = 88
instance_create(xx, 300, OBJ_WRITER)
