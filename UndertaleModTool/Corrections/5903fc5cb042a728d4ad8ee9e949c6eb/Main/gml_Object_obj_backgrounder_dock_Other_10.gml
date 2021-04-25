gg = (room_width - view_wview[0])
hh = (room_height - view_hview[0])
if (view_xview[0] >= 0 && view_xview < gg)
{
    x = (xhome + floor((view_xview[0] - (view_xview[0] * scrollspeed))))
    g = (x - xprevious)
    tile_layer_shift(1000100, g, 0)
    tile_layer_shift(1000002, (g / 2), 0)
}
if (view_yview[0] >= 0 && view_yview[0] < hh)
{
    y = (yhome + floor((view_yview[0] - (view_yview[0] * scrollspeed))))
    h = (y - yprevious)
    tile_layer_shift(1000100, 0, h)
    tile_layer_shift(1000002, 0, (h / 2))
}
