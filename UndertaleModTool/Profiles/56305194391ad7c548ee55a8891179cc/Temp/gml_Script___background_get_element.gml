var __bind = argument0;

var __result;
__result[0] = -1;
__result[1] = -1;
__result[2] = -1;


// Look at the existing layers in the room to see if we have any foregrounds or backgrounds
var __fgstring = "Compatibility_Foreground_";
var __bgstring = "Compatibility_Background_";
var __fglen = string_length(__fgstring);
var __bglen = string_length(__bgstring);
var __layerlist = layer_get_all();
var __layerlistlength = array_length_1d(__layerlist);
var __i;

var __layerid = -1;
var __isforeground = false;

// Try and find the appropriate background in the list of layers
for(__i = 0; __i < __layerlistlength; __i++)
{
	var __layername = layer_get_name(__layerlist[__i]);	
	if (string_pos(__fgstring, __layername) > 0)
	{
		var __slotchr = string_char_at(__layername, __fglen + 1);
		if (__slotchr == "")
			continue;
			
		var __slot = real( __slotchr );
		
		if (__slot == __bind)
		{
			__layerid = __layerlist[__i];
			__isforeground = true;
			
			break;
		}		
	}
	else if (string_pos(__bgstring, __layername) > 0)
	{
		var __slotchr = string_char_at(__layername, __bglen + 1);
		if (__slotchr == "")
			continue;
			
		var __slot = real( __slotchr );
		
		if (__slot == __bind)
		{
			__layerid = __layerlist[__i];
			__isforeground = false;
			
			break;
		}		
	}	
}
if (__layerid != -1)
{
	// We have a winner!
	// Get id of background element on this layer
	var __els = layer_get_all_elements(__layerid);
	var __elslength = array_length_1d(__els);
	for(__i = 0; __i < __elslength; __i++)
	{
		if (layer_get_element_type(__els[__i]) == layerelementtype_background)
		{				
			__result[0] = __els[__i];
			__result[1] = __layerid;
			__result[2] = __isforeground;
		}
	}			
}
else
{
	// Need to construct a new layer with a new background
	var __newback = __background_set_element(__bind, false, false, -1, 0, 0, true, true, 1, 1, true, 0, 0, $ffffff, 1)
	__result[0] = __newback[0];
	__result[1] = __newback[1];
	__result[2] = false;
}

return __result;