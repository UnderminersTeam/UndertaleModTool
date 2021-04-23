// NOTE: this script will only work properly if you're using the standard depth range (-16000 to 16000)
var __bind = argument0;
var __vis = argument1;
var __fore = argument2;
var __back = argument3;
var __x = argument4;
var __y = argument5;
var __htiled = argument6;
var __vtiled = argument7;
var __xscale = argument8;
var __yscale = argument9;
var __stretch = argument10;
var __hspeed = argument11;
var __vspeed = argument12;
var __blend = argument13;
var __alpha = argument14;

//var __nearestdepth = -15000;
//var __farthestdepth = 15999;
var __nearestdepth = 1000000000;
var __farthestdepth = -1000000000;
var __depthinc = 100;

var __result;
__result[0] = -1;
__result[1] = -1;


// Now look at the existing layers in the room to see if we have any foregrounds or backgrounds
var __fgstring = "Compatibility_Foreground_";
var __bgstring = "Compatibility_Background_";
var __colstring = "Compatibility_Colour";
var __fglen = string_length(__fgstring);
var __bglen = string_length(__bgstring);
var __layerlist = layer_get_all();
var __layerlistlength = array_length_1d(__layerlist);
var __collayer = -1;
var __slots;
var __isforeground;
var __i;
for(__i = 0; __i < 8; __i++)
{
	__slots[__i] = -1;
	__isforeground[__i] = false;
}


for(__i = 0; __i < __layerlistlength; __i++)
{
	var __layername = layer_get_name(__layerlist[__i]);	
	if (string_pos(__fgstring, __layername) > 0)
	{
		var __slotchr = string_char_at(__layername, __fglen + 1);
		if (__slotchr == "")
			continue;
			
		var __slot = real( __slotchr );
		__slots[__slot] = __layerlist[__i];
		
		__isforeground[__slot] = true;
		
		// Could check the contents of this layer to see if it has a single background element on it but that's probably overkill		
	}
	else if (string_pos(__bgstring, __layername) > 0)
	{
		var __slotchr = string_char_at(__layername, __bglen + 1);
		if (__slotchr == "")
			continue;
			
		var __slot = real( __slotchr );
		__slots[__slot] = __layerlist[__i];
		
		__isforeground[__slot] = false;
		
		// Could check the contents of this layer to see if it has a single background element on it but that's probably overkill		
	}
	else if (string_pos(__colstring, __layername) > 0)
	{
		// Make sure colour is as deep as it can be
		__collayer = __layerlist[__i];
		layer_depth(__layerlist[__i], __farthestdepth);
	}
	else
	{
		var __currdepth = layer_get_depth(__layerlist[__i]);
		
		if (__currdepth < __nearestdepth)
			__nearestdepth = __currdepth;
			
		if (__currdepth > __farthestdepth)
			__farthestdepth = __currdepth;
	}
}

// Reassign depths
__farthestdepth += __depthinc + 1000;		// add some margin for the background layers
__nearestdepth -= __depthinc;

//__farthestdepth = max(__farthestdepth, 15999);
//__nearestdepth = min(__nearestdepth, -15000);
__farthestdepth = max(__farthestdepth, 2147483600);
__nearestdepth = min(__nearestdepth, -2147482000);

for(__i = 0; __i < 8; __i++)
{
	if (__slots[__i] != -1)
	{
		var __depth = 0;
		if (__isforeground[__i] == true)
		{
			__depth = __nearestdepth - (__i * __depthinc);
		}
		else
		{
			__depth = (__farthestdepth - __depthinc) - (__slot * __depthinc);
		}
		
		layer_depth(__slots[__i], __depth);
	}
}
if (__collayer != -1)
{
	layer_depth(__collayer, __farthestdepth);
}


// Construct our layer name and depth
var __layername;
var __layerdepth;
if (__bind == -1)
{
	// This is the background colour layer
	__layername = __colstring;
	__layerdepth = __farthestdepth;
}
else
{
	if (__fore == true)
	{
		__layername = __fgstring + string(__bind);
		__layerdepth = __nearestdepth - (__bind * __depthinc);
	}
	else
	{
		__layername = __bgstring + string(__bind);
		__layerdepth = (__farthestdepth - __depthinc) - (__bind * __depthinc);	// reserve 16000 for imported colour
	}
}

// If we already have a background in this slot, nuke it
var __layerid;
if (__bind == -1)
{
	__layerid = __collayer;
}
else
{
	__layerid = __slots[__bind];
}
	
if (__layerid != -1)
{
	layer_destroy(__layerid);
}
__layerid = layer_create(__layerdepth, __layername);

// Set our layer properties
layer_x(__layerid, __x);
layer_y(__layerid, __y);
layer_hspeed(__layerid, __hspeed);
layer_vspeed(__layerid, __vspeed);

// Construct our new background element
var __backel = layer_background_create(__layerid, __back);
layer_background_visible(__backel, __vis);
layer_background_htiled(__backel, __htiled);
layer_background_vtiled(__backel, __vtiled);
layer_background_xscale(__backel, __xscale);
layer_background_yscale(__backel, __yscale);
layer_background_stretch(__backel, __stretch);
layer_background_blend(__backel, __blend);
layer_background_alpha(__backel, __alpha);

__result[0] = __backel;
__result[1] = __layerid;

return __result;