var xx, yy, use_font, i;
xx = argument0
yy = argument1
if (global.language == "ja")
{
    use_font = fnt_ja_curs
    yy += 3
}
else
{
    use_font = fnt_curs
    for (i = 1; i < strlen(global.charname); i++)
    {
        if (ord(string_char_at(global.charname, i)) >= 12288)
        {
            use_font = 12
            yy += 3
            break
        }
    }
}
draw_set_font(use_font)
draw_text(xx, yy, global.charname)
return string_width(global.charname);
