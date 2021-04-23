var __layers = layer_get_all()
var __numlayers = array_length_1d(__layers)
for (var __i = 0; __i < __numlayers; __i++)
{
    var __layer_id = __layers[__i]
    var __els = layer_get_all_elements(__layer_id)
    var __numels = array_length_1d(__els)
    var __name = layer_get_name(__layer_id)
    show_debug_message((((((("layer: " + string(__i)) + " ") + __name) + "[") + string(__numels)) + "]"))
    var __pos = string_pos("_lang_", __name)
    if (__pos > 0 && string_length(__name) > (__pos + 8))
    {
        var __lang = string_copy(__name, (__pos + 6), 2)
        if (__lang != global.lang)
        {
            layer_set_visible(__layer_id, 0)
            continue
        }
    }
    for (var __j = 0; __j < __numels; __j++)
    {
        var __id = __els[__j]
        var __eltype = layer_get_element_type(__id)
        if (__eltype == 2)
        {
            var __inst = layer_instance_get_instance(__id)
            __name = object_get_name(__inst.object_index)
            show_debug_message(((("  instance:" + string(__j)) + ": ") + __name))
        }
        else if (__eltype == 4)
        {
            var __spr = layer_sprite_get_sprite(__id)
            __name = sprite_get_name(__spr)
            show_debug_message(((("  sprite: " + string(__j)) + ": ") + __name))
            __pos = string_pos("_lang_", __name)
            if (__pos > 0 && string_length(__name) > (__pos + 8))
            {
                __lang = string_copy(__name, (__pos + 6), 2)
                if (__lang != global.lang)
                    layer_sprite_destroy(__id)
            }
        }
        else if (__eltype == 1)
            show_debug_message(("  background: " + string(__j)))
        else if (__eltype == 5)
            show_debug_message(("  tilemap: " + string(__j)))
        else if (__eltype == 6)
            show_debug_message(("  particlesystem: " + string(__j)))
        else if (__eltype == 7)
            show_debug_message(("  tile: " + string(__j)))
        else
            show_debug_message(("  unknown: " + string(__j)))
    }
}
