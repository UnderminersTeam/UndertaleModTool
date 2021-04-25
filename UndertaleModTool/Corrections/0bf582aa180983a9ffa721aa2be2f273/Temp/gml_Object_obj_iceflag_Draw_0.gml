draw_self_custom(0, 9999, 0, (ystart + 40))
if (yoff > 1)
    y -= 1
yoff -= 1
if (x > (view_xview + view_wview[0]))
    instance_destroy()
if (x < (view_xview - 10))
    instance_destroy()
if (y > ((view_yview + view_hview) + 30))
    instance_destroy()
if (y < view_yview)
    instance_destroy()
