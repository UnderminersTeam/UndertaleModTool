var name, file, data, num_lines, newline_pos, nextline_pos, line, lines;
if (global.osflavor <= 2)
    return file_text_open_read(argument0);
else
{
    name = string_lower(argument0)
    file = ds_map_find_value(global.savedata, name)
    if is_undefined(file)
        return undefined;
    data = file
    num_lines = 0
    while (string_byte_length(data) > 0)
    {
        newline_pos = string_pos("
", data)
        if (newline_pos > 0)
        {
            nextline_pos = (newline_pos + 1)
            if (newline_pos > 1 && string_char_at(data, (newline_pos - 1)) == "")
                newline_pos--
            if (newline_pos > 1)
                line = substr(data, 1, (newline_pos - 1))
            else
                line = ""
            if (nextline_pos <= strlen(data))
                data = substr(data, nextline_pos)
            else
                data = ""
        }
        else
        {
            line = data
            data = ""
        }
        lines[num_lines++] = line
    }
    handle = ds_map_create()
    ds_map_set(handle, "is_write", 0)
    ds_map_set(handle, "text", lines)
    ds_map_set(handle, "num_lines", num_lines)
    ds_map_set(handle, "line", 0)
    ds_map_set(handle, "line_read", 0)
    return handle;
}
