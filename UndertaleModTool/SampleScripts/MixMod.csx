EnsureDataLoaded();

/* TODO: Respect loop/no loop setting properly */

var browserext = new UndertaleExtension() {
	Name = Data.Strings.MakeString("GMWebBrowser"),
	ClassName = Data.Strings.MakeString(""),
	EmptyString = Data.Strings.MakeString("")
};
browserext.Files.Add(new UndertaleExtension.ExtensionFile() {
	Filename = Data.Strings.MakeString("GMWebExtension.dll"),
	Kind = UndertaleExtension.ExtensionKind.DLL,
	InitScript = Data.Strings.MakeString("__webextension_native_init"),
	CleanupScript = Data.Strings.MakeString("__webextension_native_exit"),
});
// 0xc = cdecl?
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 1, 0xc, "RegisterCallbacks", UndertaleExtension.ExtensionVarType.Double, "RegisterCallbacks", UndertaleExtension.ExtensionVarType.String, UndertaleExtension.ExtensionVarType.String, UndertaleExtension.ExtensionVarType.String, UndertaleExtension.ExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 2, 0xc, "__webextension_native_init", UndertaleExtension.ExtensionVarType.Double, "__webextension_native_init");
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 3, 0xc, "__webextension_native_exit", UndertaleExtension.ExtensionVarType.Double, "__webextension_native_exit");
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 4, 0xc, "__webextension_set_device", UndertaleExtension.ExtensionVarType.Double, "__webextension_set_device", UndertaleExtension.ExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 5, 0xc, "browser_create", UndertaleExtension.ExtensionVarType.Double, "browser_create", UndertaleExtension.ExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 6, 0xc, "browser_destroy", UndertaleExtension.ExtensionVarType.Double, "browser_destroy", UndertaleExtension.ExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 7, 0xc, "browser_load", UndertaleExtension.ExtensionVarType.Double, "browser_load", UndertaleExtension.ExtensionVarType.Double, UndertaleExtension.ExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 8, 0xc, "browser_load_html", UndertaleExtension.ExtensionVarType.Double, "browser_load_html", UndertaleExtension.ExtensionVarType.Double, UndertaleExtension.ExtensionVarType.String);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 9, 0xc, "browser_resize", UndertaleExtension.ExtensionVarType.Double, "browser_resize", UndertaleExtension.ExtensionVarType.Double, UndertaleExtension.ExtensionVarType.Double, UndertaleExtension.ExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 10, 0xc, "browser_draw", UndertaleExtension.ExtensionVarType.Double, "browser_draw", UndertaleExtension.ExtensionVarType.Double, UndertaleExtension.ExtensionVarType.Double, UndertaleExtension.ExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 11, 0xc, "browser_is_initialized", UndertaleExtension.ExtensionVarType.Double, "browser_is_initialized", UndertaleExtension.ExtensionVarType.Double);
browserext.Files[0].Functions.DefineExtensionFunction(Data.Functions, Data.Strings, 12, 0xc, "browser_js", UndertaleExtension.ExtensionVarType.Double, "browser_js", UndertaleExtension.ExtensionVarType.Double, UndertaleExtension.ExtensionVarType.String);
Data.Extensions.Add(browserext);

Data.Functions.EnsureDefined("window_device", Data.Strings);
Data.Functions.EnsureDefined("http_get", Data.Strings);
Data.Functions.EnsureDefined("ds_list_size", Data.Strings);
Data.Functions.EnsureDefined("ds_list_find_value", Data.Strings);
Data.Functions.EnsureDefined("show_message", Data.Strings);
Data.Functions.EnsureDefined("show_debug_message", Data.Strings);
Data.Variables.EnsureDefined("my_browser", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_current_song", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_request", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_song_title", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_last_song", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
Data.Variables.EnsureDefined("youtube_cache", UndertaleInstruction.InstanceType.Self, false, Data.Strings, Data);
var var_song = Data.Variables.IndexOf(Data.Variables.DefineLocal(1, "song", Data.Strings, Data));
var var_w = Data.Variables.IndexOf(Data.Variables.DefineLocal(1, "w", Data.Strings, Data));
var var_h = Data.Variables.IndexOf(Data.Variables.DefineLocal(2, "h", Data.Strings, Data));
var var_data = Data.Variables.IndexOf(Data.Variables.DefineLocal(1, "data", Data.Strings, Data));
var var_items = Data.Variables.IndexOf(Data.Variables.DefineLocal(1, "items", Data.Strings, Data));
var var_item = Data.Variables.IndexOf(Data.Variables.DefineLocal(2, "item", Data.Strings, Data));
var var_type = Data.Variables.IndexOf(Data.Variables.DefineLocal(1, "type", Data.Strings, Data));
Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
call.i window_device(argc=0)
call.i __webextension_set_device(argc=1)
popz.v

push.s """"
conv.s.v
call.i browser_create(argc=1)
pop.v.v self.my_browser

push.s """"
pop.v.s self.youtube_current_song

pushi.e -4
pop.v.i self.youtube_request

push.s """"
pop.v.s self.youtube_song_title

push.s """"
pop.v.s self.youtube_last_song

call.i ds_map_create(argc=0)
pop.v.v self.youtube_cache
", Data.Functions, Data.Variables, Data.Strings));

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.PostDraw, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
.localvar 1 w " + var_w + @"
.localvar 2 h " + var_h + @"
call.i window_get_width(argc=0)
push.d 6.5
div.d.v
pop.v.v local.w
pushloc.v local.w
pushi.e 16
conv.i.d
div.d.v
pushi.e 9
mul.i.v
pop.v.v local.h
pushloc.v local.h
pushloc.v local.w
push.v self.my_browser
call.i browser_resize(argc=3)
popz.v

pushi.e 5
conv.i.v
pushi.e 5
conv.i.v
push.v self.my_browser
call.i browser_draw(argc=3)
popz.v

push.v self.youtube_current_song
call.i window_set_caption(argc=1)
popz.v
", Data.Functions, Data.Variables, Data.Strings));

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Draw, EventSubtypeDraw.DrawGUI, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e 2
conv.i.v
call.i draw_set_font(argc=1)
popz.v

push.i " + 0x00FFFF.ToString() + @"
conv.i.v
call.i draw_set_color(argc=1)
popz.v

; why is it so hard to get something centered horizontally
; it's almost like trying to do vertical centering in CSS
; no, actually CSS is worse
; I hope nobody tries to play Undertale in portrait orientation or this is going to break :P
push.v self.youtube_song_title
pushi.e 0
conv.i.v
pushvar.v self.application_surface
call.i surface_get_width(argc=1)
pushglb.v global.window_scale
mul.v.v
push.v self.youtube_song_title
call.i string_width(argc=1)
sub.v.v
pushi.e 2
div.i.v
pushglb.v global.window_yofs
add.v.v
call.i draw_text(argc=3)
popz.v
", Data.Functions, Data.Variables, Data.Strings));

var MOD_get_mus_query = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_MOD_get_mus_query") };
MOD_get_mus_query.Append(Assembler.Assemble(@"
; yeah, I compiled that in GameMaker: Studio :P
00000: pushvar.v self.argument0
00002: pushi.e 213
00003: cmp.i.v EQ
00004: bf 00009
00005: push.s ""Ghost+Fight""
00007: conv.s.v
00008: ret.v

00009: pushvar.v self.argument0
00011: pushi.e 214
00012: cmp.i.v EQ
00013: bf 00018
00014: push.s ""Once+Upon+a+Time""
00016: conv.s.v
00017: ret.v

00018: pushvar.v self.argument0
00020: pushi.e 216
00021: cmp.i.v EQ
00022: bf 00027
00023: push.s ""Fallen+Down""
00025: conv.s.v
00026: ret.v

00027: pushvar.v self.argument0
00029: pushi.e 217
00030: cmp.i.v EQ
00031: bf 00036
00032: push.s ""Your+Best+Friend""
00034: conv.s.v
00035: ret.v

00036: pushvar.v self.argument0
00038: pushi.e 218
00039: cmp.i.v EQ
00040: bf 00045
00041: push.s ""Anticipation""
00043: conv.s.v
00044: ret.v

00045: pushvar.v self.argument0
00047: pushi.e 219
00048: cmp.i.v EQ
00049: bf 00054
00050: push.s ""Unnecessary+Tension""
00052: conv.s.v
00053: ret.v

00054: pushvar.v self.argument0
00056: pushi.e 220
00057: cmp.i.v EQ
00058: bf 00063
00059: push.s ""Start+Menu""
00061: conv.s.v
00062: ret.v

00063: pushvar.v self.argument0
00065: pushi.e 221
00066: cmp.i.v EQ
00067: bf 00072
00068: push.s ""Start+Menu""
00070: conv.s.v
00071: ret.v

00072: pushvar.v self.argument0
00074: pushi.e 222
00075: cmp.i.v EQ
00076: bf 00081
00077: push.s ""Start+Menu""
00079: conv.s.v
00080: ret.v

00081: pushvar.v self.argument0
00083: pushi.e 223
00084: cmp.i.v EQ
00085: bf 00090
00086: push.s ""Start+Menu""
00088: conv.s.v
00089: ret.v

00090: pushvar.v self.argument0
00092: pushi.e 224
00093: cmp.i.v EQ
00094: bf 00099
00095: push.s ""Start+Menu""
00097: conv.s.v
00098: ret.v

00099: pushvar.v self.argument0
00101: pushi.e 225
00102: cmp.i.v EQ
00103: bf 00108
00104: push.s ""Start+Menu""
00106: conv.s.v
00107: ret.v

00108: pushvar.v self.argument0
00110: pushi.e 226
00111: cmp.i.v EQ
00112: bf 00117
00113: push.s ""Menu+%28Full%29""
00115: conv.s.v
00116: ret.v

00117: pushvar.v self.argument0
00119: pushi.e 227
00120: cmp.i.v EQ
00121: bf 00126
00122: push.s ""Home""
00124: conv.s.v
00125: ret.v

00126: pushvar.v self.argument0
00128: pushi.e 232
00129: cmp.i.v EQ
00130: bf 00135
00131: push.s ""Heartache""
00133: conv.s.v
00134: ret.v

00135: pushvar.v self.argument0
00137: pushi.e 233
00138: cmp.i.v EQ
00139: bf 00144
00140: push.s ""Home+%28Music+Box%29""
00142: conv.s.v
00143: ret.v

00144: pushvar.v self.argument0
00146: pushi.e 234
00147: cmp.i.v EQ
00148: bf 00153
00149: push.s ""Ruins""
00151: conv.s.v
00152: ret.v

00153: pushvar.v self.argument0
00155: pushi.e 235
00156: cmp.i.v EQ
00157: bf 00162
00158: push.s ""Enemy+Approaching""
00160: conv.s.v
00161: ret.v

00162: pushvar.v self.argument0
00164: pushi.e 236
00165: cmp.i.v EQ
00166: bf 00171
;00167: push.s ""Determination""
00167: push.s ""game+over+theme""
00169: conv.s.v
00170: ret.v

00171: pushvar.v self.argument0
00173: pushi.e 241
00174: cmp.i.v EQ
00175: bf 00180
00176: push.s ""Dogsong""
00178: conv.s.v
00179: ret.v

00180: pushvar.v self.argument0
00182: pushi.e 242
00183: cmp.i.v EQ
00184: bf 00189
00185: push.s ""Bonetrousle""
00187: conv.s.v
00188: ret.v

00189: pushvar.v self.argument0
00191: pushi.e 243
00192: cmp.i.v EQ
00193: bf 00198
00194: push.s ""Shop""
00196: conv.s.v
00197: ret.v

00198: pushvar.v self.argument0
00200: pushi.e 244
00201: cmp.i.v EQ
00202: bf 00216
00203: push.s ""Snowdin+Town""
00205: conv.s.v
00206: ret.v

;00207: pushvar.v self.argument0
;00209: pushi.e 246
;00210: cmp.i.v EQ
;00211: bf 00216
;00212: push.s ""Mysterious+Place""
;00214: conv.s.v
;00215: ret.v

00216: pushvar.v self.argument0
00218: pushi.e 251
00219: cmp.i.v EQ
00220: bf 00225
00221: push.s ""%22Dating+Start%21%22""
00223: conv.s.v
00224: ret.v

00225: pushvar.v self.argument0
00227: pushi.e 252
00228: cmp.i.v EQ
00229: bf 00234
00230: push.s ""%22Dating+Tense%21%22""
00232: conv.s.v
00233: ret.v

00234: pushvar.v self.argument0
00236: pushi.e 253
00237: cmp.i.v EQ
00238: bf 00243
00239: push.s ""%22Dating+Fight%21%22""
00241: conv.s.v
00242: ret.v

00243: pushvar.v self.argument0
00245: pushi.e 254
00246: cmp.i.v EQ
00247: bf 00252
00248: push.s ""Premonition""
00250: conv.s.v
00251: ret.v

00252: pushvar.v self.argument0
00254: pushi.e 255
00255: cmp.i.v EQ
00256: bf 00270
00257: push.s ""Snowy""
00259: conv.s.v
00260: ret.v

;00261: pushvar.v self.argument0
;00263: pushi.e 256
;00264: cmp.i.v EQ
;00265: bf 00270
;00266: push.s ""sans.""
; OH PLEASE MEGALOVANIA STOP IT
; (-megalovania just totally breaks the results for some reason)
;00266: push.s ""sans+theme""
;00268: conv.s.v
;00269: ret.v

00270: pushvar.v self.argument0
00272: pushi.e 257
00273: cmp.i.v EQ
00274: bf 00297
00275: push.s ""Nyeh+Heh+Heh%21""
00277: conv.s.v
00278: ret.v

;00279: pushvar.v self.argument0
;00281: pushi.e 260
;00282: cmp.i.v EQ
;00283: bf 00288
;00284: push.s ""Dogbass""
;00286: conv.s.v
;00287: ret.v

;00288: pushvar.v self.argument0
;00290: pushi.e 261
;00291: cmp.i.v EQ
;00292: bf 00297
;00293: push.s ""Danger+Mystery""
;00295: conv.s.v
;00296: ret.v

00297: pushvar.v self.argument0
00299: pushi.e 262
00300: cmp.i.v EQ
00301: bf 00315
00302: push.s ""Bird+That+Carries+You+Over+A+Disproportionately+Small+Gap""
00304: conv.s.v
00305: ret.v

;00306: pushvar.v self.argument0
;00308: pushi.e 263
;00309: cmp.i.v EQ
;00310: bf 00315
;00311: push.s ""The+Choice""
;00313: conv.s.v
;00314: ret.v

00315: pushvar.v self.argument0
00317: pushi.e 264
00318: cmp.i.v EQ
00319: bf 00324
00320: push.s ""Dummy%21""
00322: conv.s.v
00323: ret.v

00324: pushvar.v self.argument0
00326: pushi.e 267
00327: cmp.i.v EQ
00328: bf 00333
00329: push.s ""Thundersnail""
00331: conv.s.v
00332: ret.v

00333: pushvar.v self.argument0
00335: pushi.e 268
00336: cmp.i.v EQ
00337: bf 00342
00338: push.s ""NGAHHH%21%21""
00340: conv.s.v
00341: ret.v

00342: pushvar.v self.argument0
00344: pushi.e 269
00345: cmp.i.v EQ
00346: bf 00351
00347: push.s ""She%27s+Playing+Piano""
00349: conv.s.v
00350: ret.v

00351: pushvar.v self.argument0
00353: pushi.e 270
00354: cmp.i.v EQ
00355: bf 00360
00356: push.s ""Waterfall""
00358: conv.s.v
00359: ret.v

00360: pushvar.v self.argument0
00362: pushi.e 271
00363: cmp.i.v EQ
00364: bf 00369
00365: push.s ""Quiet+Water""
00367: conv.s.v
00368: ret.v

00369: pushvar.v self.argument0
00371: pushi.e 273
00372: cmp.i.v EQ
00373: bf 00387
00374: push.s ""Run%21""
00376: conv.s.v
00377: ret.v

;00378: pushvar.v self.argument0
;00380: pushi.e 274
;00381: cmp.i.v EQ
;00382: bf 00387
;00383: push.s ""Undyne""
;00385: conv.s.v
;00386: ret.v

00387: pushvar.v self.argument0
00389: pushi.e 276
00390: cmp.i.v EQ
00391: bf 00396
00392: push.s ""Memory""
00394: conv.s.v
00395: ret.v

00396: pushvar.v self.argument0
00398: pushi.e 279
00399: cmp.i.v EQ
00400: bf 00405
00401: push.s ""Pathetic+House""
00403: conv.s.v
00404: ret.v

00405: pushvar.v self.argument0
00407: pushi.e 282
00408: cmp.i.v EQ
00409: bf 00414
00410: push.s ""Chill""
00412: conv.s.v
00413: ret.v

00414: pushvar.v self.argument0
00416: pushi.e 283
00417: cmp.i.v EQ
00418: bf 00423
00419: push.s ""Spooktune""
00421: conv.s.v
00422: ret.v

00423: pushvar.v self.argument0
00425: pushi.e 284
00426: cmp.i.v EQ
00427: bf 00432
00428: push.s ""Spookwave""
00430: conv.s.v
00431: ret.v

00432: pushvar.v self.argument0
00434: pushi.e 285
00435: cmp.i.v EQ
00436: bf 00441
00437: push.s ""Ghouliday""
00439: conv.s.v
00440: ret.v

00441: pushvar.v self.argument0
00443: pushi.e 286
00444: cmp.i.v EQ
;00445: bf 00450
00445: bf 00468
00446: push.s ""Spear+of+Justice""
00448: conv.s.v
00449: ret.v

; this one just like never works
; people remix it please
;00450: pushvar.v self.argument0
;00452: pushi.e 289
;00453: cmp.i.v EQ
;00454: bf 00459
;00455: push.s ""Alphys""
;00457: conv.s.v
;00458: ret.v

00459: pushvar.v self.argument0
00461: pushi.e 290
00462: cmp.i.v EQ
00463: bf 00468
00464: push.s ""It%27s+Showtime%21""
00466: conv.s.v
00467: ret.v

00468: pushvar.v self.argument0
00470: pushi.e 291
00471: cmp.i.v EQ
00472: bf 00477
00473: push.s ""Metal+Crusher""
00475: conv.s.v
00476: ret.v

00477: pushvar.v self.argument0
00479: pushi.e 292
00480: cmp.i.v EQ
00481: bf 00495
00482: push.s ""Hotel""
00484: conv.s.v
00485: ret.v

;00486: pushvar.v self.argument0
;00488: pushi.e 293
;00489: cmp.i.v EQ
;00490: bf 00495
;00491: push.s ""For+The+Fans""
;00493: conv.s.v
;00494: ret.v

00495: pushvar.v self.argument0
00497: pushi.e 294
00498: cmp.i.v EQ
00499: bf 00504
00500: push.s ""Spider+Dance""
00502: conv.s.v
00503: ret.v

00504: pushvar.v self.argument0
00506: pushi.e 295
00507: cmp.i.v EQ
00508: bf 00513
00509: push.s ""It%27s+Raining+Somewhere+Else""
00511: conv.s.v
00512: ret.v

00513: pushvar.v self.argument0
00515: pushi.e 297
00516: cmp.i.v EQ
00517: bf 00522
00518: push.s ""Live+Report""
00520: conv.s.v
00521: ret.v

00522: pushvar.v self.argument0
00524: pushi.e 298
00525: cmp.i.v EQ
00526: bf 00531
00527: push.s ""Death+Report""
00529: conv.s.v
00530: ret.v

00531: pushvar.v self.argument0
00533: pushi.e 299
00534: cmp.i.v EQ
00535: bf 00540
00536: push.s ""Can+You+Really+Call+This+A+Hotel%2C+I+Didn%27t+Receive+A+Mint+On+My+Pillow+Or+Anything""
00538: conv.s.v
00539: ret.v

00540: pushvar.v self.argument0
00542: pushi.e 300
00543: cmp.i.v EQ
00544: bf 00549
00545: push.s ""CORE""
00547: conv.s.v
00548: ret.v

00549: pushvar.v self.argument0
00551: pushi.e 301
00552: cmp.i.v EQ
00553: bf 00576
00554: push.s ""Death+by+Glamour""
00556: conv.s.v
00557: ret.v

;00558: pushvar.v self.argument0
;00560: pushi.e 302
;00561: cmp.i.v EQ
;00562: bf 00567
;00563: push.s ""Oh+My...""
;00565: conv.s.v
;00566: ret.v

;00567: pushvar.v self.argument0
;00569: pushi.e 303
;00570: cmp.i.v EQ
;00571: bf 00576
;00572: push.s ""Ooo""
;00574: conv.s.v
;00575: ret.v

00576: pushvar.v self.argument0
00578: pushi.e 304
00579: cmp.i.v EQ
00580: bf 00585
00581: push.s ""Another+Medium""
00583: conv.s.v
00584: ret.v

00585: pushvar.v self.argument0
00587: pushi.e 312
00588: cmp.i.v EQ
00589: bf 00594
00590: push.s ""Confession""
00592: conv.s.v
00593: ret.v

00594: pushvar.v self.argument0
00596: pushi.e 313
00597: cmp.i.v EQ
00598: bf 00603
00599: push.s ""Oh%21+One+True+Love""
00601: conv.s.v
00602: ret.v

00603: pushvar.v self.argument0
00605: pushi.e 314
00606: cmp.i.v EQ
00607: bf 00612
00608: push.s ""Oh%21+One+True+Love""
00610: conv.s.v
00611: ret.v

00612: pushvar.v self.argument0
00614: pushi.e 315
00615: cmp.i.v EQ
00616: bf 00621
00617: push.s ""Oh%21+One+True+Love""
00619: conv.s.v
00620: ret.v

00621: pushvar.v self.argument0
00623: pushi.e 316
00624: cmp.i.v EQ
00625: bf 00684
00626: push.s ""Oh%21+One+True+Love""
00628: conv.s.v
00629: ret.v

;00630: pushvar.v self.argument0
;00632: pushi.e 321
;00633: cmp.i.v EQ
;00634: bf 00639
;00635: push.s ""Long+Elevator""
;00637: conv.s.v
;00638: ret.v

;00639: pushvar.v self.argument0
;00641: pushi.e 322
;00642: cmp.i.v EQ
;00643: bf 00648
;00644: push.s ""Oh%21+Dungeon""
;00646: conv.s.v
;00647: ret.v

;00648: pushvar.v self.argument0
;00650: pushi.e 323
;00651: cmp.i.v EQ
;00652: bf 00657
;00653: push.s ""Last+Episode%21""
;00655: conv.s.v
;00656: ret.v

;00657: pushvar.v self.argument0
;00659: pushi.e 332
;00660: cmp.i.v EQ
;00661: bf 00684
;00662: push.s ""Bergentr%C3%BCckung""
;00664: conv.s.v
;00665: ret.v

; Too hard to query for that, and all remixes I found don't match the mood too well
;00666: pushvar.v self.argument0
;00668: pushi.e 333
;00669: cmp.i.v EQ
;00670: bf 00675
;00671: push.s ""Undertale+theme""
;00673: conv.s.v
;00674: ret.v

;00675: pushvar.v self.argument0
;00677: pushi.e 337
;00678: cmp.i.v EQ
;00679: bf 00684
;00680: push.s ""Barrier""
;00682: conv.s.v
;00683: ret.v

00684: pushvar.v self.argument0
00686: pushi.e 338
00687: cmp.i.v EQ
00688: bf 00720
00689: push.s ""ASGORE""
00691: conv.s.v
00692: ret.v

; Too hard to query for that, and all remixes I found don't match the mood too well
;00693: pushvar.v self.argument0
;00695: pushi.e 339
;00696: cmp.i.v EQ
;00697: bf 00702
;00698: push.s ""Undertale+theme""
;00700: conv.s.v
;00701: ret.v

;00702: pushvar.v self.argument0
;00704: pushi.e 340
;00705: cmp.i.v EQ
;00706: bf 00711
;00707: push.s ""CORE+Approach""
;00709: conv.s.v
;00710: ret.v

;00711: pushvar.v self.argument0
;00713: pushi.e 349
;00714: cmp.i.v EQ
;00715: bf 00720
;00716: push.s ""You+Idiot""
;00718: conv.s.v
;00719: ret.v

00720: pushvar.v self.argument0
00722: pushi.e 383
00723: cmp.i.v EQ
00724: bf 00729
00725: push.s ""Amalgam""
00727: conv.s.v
00728: ret.v

00729: pushvar.v self.argument0
00731: pushi.e 387
00732: cmp.i.v EQ
00733: bf 00738
00734: push.s ""Temmie+Village""
00736: conv.s.v
00737: ret.v

00738: pushvar.v self.argument0
00740: pushi.e 388
00741: cmp.i.v EQ
00742: bf 00747
00743: push.s ""Tem+Shop""
00745: conv.s.v
00746: ret.v

00747: pushvar.v self.argument0
00749: pushi.e 390
00750: cmp.i.v EQ
00751: bf 00756
00752: push.s ""Here+We+Are""
00754: conv.s.v
00755: ret.v

00756: pushvar.v self.argument0
00758: pushi.e 391
00759: cmp.i.v EQ
00760: bf 00765
00761: push.s ""%22An+Ending%22""
00763: conv.s.v
00764: ret.v

00765: pushvar.v self.argument0
00767: pushi.e 392
00768: cmp.i.v EQ
00769: bf 00774
00770: push.s ""Battle+Against+a+True+Hero""
00772: conv.s.v
00773: ret.v

00774: pushvar.v self.argument0
00776: pushi.e 393
00777: cmp.i.v EQ
00778: bf 00783
00779: push.s ""But+the+Earth+Refused+to+Die""
00781: conv.s.v
00782: ret.v

00783: pushvar.v self.argument0
00785: pushi.e 394
00786: cmp.i.v EQ
00787: bf 00792
00788: push.s ""Power+of+NEO""
00790: conv.s.v
00791: ret.v

00792: pushvar.v self.argument0
00794: pushi.e 395
00795: cmp.i.v EQ
00796: bf 00801
00797: push.s ""MEGALOVANIA""
00799: conv.s.v
00800: ret.v

00801: pushvar.v self.argument0
00803: pushi.e 404
00804: cmp.i.v EQ
00805: bf 00810
00806: push.s ""Fallen+Down+%28Reprise%29""
00808: conv.s.v
00809: ret.v

00810: pushvar.v self.argument0
00812: pushi.e 405
00813: cmp.i.v EQ
00814: bf 00819
00815: push.s ""Don%27t+Give+Up""
00817: conv.s.v
00818: ret.v

00819: pushvar.v self.argument0
00821: pushi.e 408
00822: cmp.i.v EQ
00823: bf 00828
00824: push.s ""Hopes+and+Dreams""
00826: conv.s.v
00827: ret.v

00828: pushvar.v self.argument0
00830: pushi.e 409
00831: cmp.i.v EQ
00832: bf 00846
00833: push.s ""SAVE+the+World""
00835: conv.s.v
00836: ret.v

;00837: pushvar.v self.argument0
;00839: pushi.e 410
;00840: cmp.i.v EQ
;00841: bf 00846
;00842: push.s ""Final+Power""
;00844: conv.s.v
;00845: ret.v

00846: pushvar.v self.argument0
00848: pushi.e 411
00849: cmp.i.v EQ
00850: bf 00855
00851: push.s ""Reunited""
00853: conv.s.v
00854: ret.v

00855: pushvar.v self.argument0
00857: pushi.e 412
00858: cmp.i.v EQ
00859: bf 00864
00860: push.s ""Respite""
00862: conv.s.v
00863: ret.v

00864: pushvar.v self.argument0
00866: pushi.e 413
00867: cmp.i.v EQ
00868: bf 00873
00869: push.s ""Burn+in+Despair%21""
00871: conv.s.v
00872: ret.v

00873: pushvar.v self.argument0
00875: pushi.e 415
00876: cmp.i.v EQ
00877: bf 00882
00878: push.s ""Wrong+Enemy+%21%3F""
00880: conv.s.v
00881: ret.v

00882: pushvar.v self.argument0
00884: pushi.e 421
00885: cmp.i.v EQ
00886: bf 00891
00887: push.s ""Uwa%21%21+So+Holiday""
00889: conv.s.v
00890: ret.v

00891: pushvar.v self.argument0
00893: pushi.e 422
00894: cmp.i.v EQ
00895: bf 00900
00896: push.s ""Uwa%21%21+So+Temperate""
00898: conv.s.v
00899: ret.v

00900: pushvar.v self.argument0
00902: pushi.e 424
00903: cmp.i.v EQ
00904: bf 00909
00905: push.s ""Uwa%21%21+So+HEATS%21%21%E2%99%AB""
00907: conv.s.v
00908: ret.v

00909: pushvar.v self.argument0
00911: pushi.e 425
00912: cmp.i.v EQ
00913: bf 00918
00914: push.s ""Stronger+Monsters""
00916: conv.s.v
00917: ret.v

00918: pushvar.v self.argument0
00920: pushi.e 432
00921: cmp.i.v EQ
00922: bf 00927
00923: push.s ""Bring+It+In%2C+Guys%21""
00925: conv.s.v
00926: ret.v

00927: pushvar.v self.argument0
00929: pushi.e 433
00930: cmp.i.v EQ
00931: bf 00936
00932: push.s ""Bring+It+In%2C+Guys%21""
00934: conv.s.v
00935: ret.v

00936: pushvar.v self.argument0
00938: pushi.e 434
00939: cmp.i.v EQ
00940: bf 00945
00941: push.s ""Bring+It+In%2C+Guys%21""
00943: conv.s.v
00944: ret.v

00945: pushvar.v self.argument0
00947: pushi.e 435
00948: cmp.i.v EQ
00949: bf 00954
00950: push.s ""Bring+It+In%2C+Guys%21""
00952: conv.s.v
00953: ret.v

00954: pushvar.v self.argument0
00956: pushi.e 436
00957: cmp.i.v EQ
00958: bf 00963
00959: push.s ""Bring+It+In%2C+Guys%21""
00961: conv.s.v
00962: ret.v

00963: pushvar.v self.argument0
00965: pushi.e 437
00966: cmp.i.v EQ
00967: bf 00972
00968: push.s ""Bring+It+In%2C+Guys%21""
00970: conv.s.v
00971: ret.v

00972: pushvar.v self.argument0
00974: pushi.e 438
00975: cmp.i.v EQ
00976: bf 00981
00977: push.s ""Bring+It+In%2C+Guys%21""
00979: conv.s.v
00980: ret.v

00981: pushvar.v self.argument0
00983: pushi.e 442
00984: cmp.i.v EQ
00985: bf 00990
00986: push.s ""Last+Goodbye""
00988: conv.s.v
00989: ret.v

00990: push.s """"
00992: conv.s.v
00993: ret.v
", Data.Functions, Data.Variables, Data.Strings));
Data.Code.Add(MOD_get_mus_query);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = MOD_get_mus_query.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("MOD_get_mus_query"), Code = MOD_get_mus_query });
Data.Functions.EnsureDefined("MOD_get_mus_query", Data.Strings);

var MOD_get_mus_count = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_MOD_get_mus_count") };
MOD_get_mus_count.Append(Assembler.Assemble(@"
; i can't decide if there are more songs that should be unlocked to 50 (or more if i had pagination support) entries or those which should be limited to ~15 :P

pushvar.v self.argument0
push.s ""game+over+theme""
cmp.s.v EQ
bf dating
pushi.e 5
conv.i.v
ret.v
b func_end

dating: pushvar.v self.argument0
push.s ""%22Dating+Tense%21%22""
cmp.s.v EQ
bf confess
pushi.e 5
conv.i.v
ret.v
b func_end

confess: pushvar.v self.argument0
push.s ""Confession""
cmp.s.v EQ
bf premo
pushi.e 5
conv.i.v
ret.v
b func_end

premo: pushvar.v self.argument0
push.s ""Premonition""
cmp.s.v EQ
bf dogs
pushi.e 3
conv.i.v
ret.v
b func_end

dogs: pushvar.v self.argument0
push.s ""Dogsong""
cmp.s.v EQ
bf run
pushi.e 5
conv.i.v
ret.v
b func_end

run: pushvar.v self.argument0
push.s ""Run%21""
cmp.s.v EQ
bf respite
pushi.e 3
conv.i.v
ret.v
b func_end

respite: pushvar.v self.argument0
push.s ""Respite""
cmp.s.v EQ
bf ending
pushi.e 3
conv.i.v
ret.v
b func_end

ending: pushvar.v self.argument0
push.s ""%22An+Ending%22""
cmp.s.v EQ
bf undyne
pushi.e 5
conv.i.v
ret.v
b func_end

undyne: pushvar.v self.argument0
push.s ""Battle+Against+a+True+Hero""
cmp.s.v EQ
bf waterfall
pushi.e 50
conv.i.v
ret.v
b func_end

waterfall: pushvar.v self.argument0
push.s ""Waterfall""
cmp.s.v EQ
bf waterfall2
pushi.e 50
conv.i.v
ret.v
b func_end

waterfall2: pushvar.v self.argument0
push.s ""Quiet+Water""
cmp.s.v EQ
bf snowy
pushi.e 30
conv.i.v
ret.v
b func_end

snowy: pushvar.v self.argument0
push.s ""Snowy""
cmp.s.v EQ
bf core
pushi.e 50
conv.i.v
ret.v
b func_end

core: pushvar.v self.argument0
push.s ""CORE""
cmp.s.v EQ
bf spider
pushi.e 50
conv.i.v
ret.v
b func_end

spider: pushvar.v self.argument0
push.s ""Spider+Dance""
cmp.s.v EQ
bf memory
pushi.e 50
conv.i.v
ret.v
b func_end

memory: pushvar.v self.argument0
push.s ""Memory""
cmp.s.v EQ
bf intro
pushi.e 50
conv.i.v
ret.v
b func_end

intro: pushvar.v self.argument0
push.s ""Once+Upon+a+Time""
cmp.s.v EQ
bf enemy
pushi.e 50
conv.i.v
ret.v
b func_end

enemy: pushvar.v self.argument0
push.s ""Enemy+Approaching""
cmp.s.v EQ
bf toriel
pushi.e 50
conv.i.v
ret.v
b func_end

toriel: pushvar.v self.argument0
push.s ""Heatache""
cmp.s.v EQ
bf asgore
pushi.e 50
conv.i.v
ret.v
b func_end

asgore: pushvar.v self.argument0
push.s ""ASGORE""
cmp.s.v EQ
bf itsthesanssong
pushi.e 50
conv.i.v
ret.v
b func_end

itsthesanssong: pushvar.v self.argument0
push.s ""MEGALOVANIA""
cmp.s.v EQ
bf ihopeanddreamthisisthelastone
pushi.e 50
conv.i.v
ret.v
b func_end

ihopeanddreamthisisthelastone: pushvar.v self.argument0
push.s ""Hopes+and+Dreams""
cmp.s.v EQ
bf normal
pushi.e 50
conv.i.v
ret.v
b func_end

normal: pushi.e 15
conv.i.v
ret.v
", Data.Functions, Data.Variables, Data.Strings));
Data.Code.Add(MOD_get_mus_count);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = MOD_get_mus_count.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("MOD_get_mus_count"), Code = MOD_get_mus_count });
Data.Functions.EnsureDefined("MOD_get_mus_count", Data.Strings);

var youtube_load_song = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_load_song") };
youtube_load_song.Append(Assembler.Assemble(@"
.localvar 1 items " + var_items + @"
.localvar 2 item " + var_item + @"
pushi.e " + Data.GameObjects.IndexOf(Data.GameObjects.ByName("obj_time")) + @"
pushenv func_end
00000: push.v self.youtube_current_song
00002: push.v self.youtube_cache
00004: call.i ds_map_find_value(argc=2)
00006: pop.v.v local.items
00008: pushloc.v local.items
00010: call.i ds_list_size(argc=1)
00012: pushi.e 1
00013: sub.i.v
00014: call.i irandom(argc=1)
00016: pushloc.v local.items
00018: call.i ds_list_find_value(argc=2)
00020: pop.v.v local.item
00022: push.s ""change_song('""
00024: push.s ""videoId""
00026: conv.s.v
00027: push.s ""id""
00029: conv.s.v
00030: pushloc.v local.item
00032: call.i ds_map_find_value(argc=2)
00034: call.i ds_map_find_value(argc=2)
00036: add.v.s
00037: push.s ""')""
00039: add.s.v
00040: push.v self.my_browser
00042: call.i browser_js(argc=2)
00044: popz.v
00045: push.s ""channelTitle""
00047: conv.s.v
00048: push.s ""snippet""
00050: conv.s.v
00051: pushloc.v local.item
00053: call.i ds_map_find_value(argc=2)
00055: call.i ds_map_find_value(argc=2)
00057: push.s "" - ""
00059: add.s.v
00060: push.s ""title""
00062: conv.s.v
00063: push.s ""snippet""
00065: conv.s.v
00066: pushloc.v local.item
00068: call.i ds_map_find_value(argc=2)
00070: call.i ds_map_find_value(argc=2)
00072: add.v.v
00073: pop.v.v self.youtube_song_title
popenv 00000
", Data.Functions, Data.Variables, Data.Strings));
Data.Code.Add(youtube_load_song);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_load_song.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_load_song"), Code = youtube_load_song });
Data.Functions.EnsureDefined("youtube_load_song", Data.Strings);

var youtube_play = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_play") };
youtube_play.Append(Assembler.Assemble(@"
.localvar 1 song " + var_song + @"
pushi.e " + Data.GameObjects.IndexOf(Data.GameObjects.ByName("obj_time")) + @"
pushenv func_end
00000: pushvar.v self.argument0
00002: call.i MOD_get_mus_query(argc=1)
00004: pop.v.v local.song
00006: pushloc.v local.song
00008: push.s """"
00010: cmp.s.v NEQ
00011: bf 00095
00012: pushloc.v local.song
00014: push.v self.youtube_current_song
00016: cmp.v.v NEQ
00017: bf 00091
00018: pushloc.v local.song
00020: pop.v.v self.youtube_current_song
00022: push.v self.my_browser
00024: call.i browser_is_initialized(argc=1)
00026: conv.v.b
00027: bf 00091
00028: push.v self.youtube_current_song
00030: push.v self.youtube_last_song
00032: cmp.v.v NEQ
00033: bf 00083
00034: pushloc.v local.song
00036: pop.v.v self.youtube_last_song
00038: push.s ""change_song(null)""
00040: conv.s.v
00041: push.v self.my_browser
00043: call.i browser_js(argc=2)
00045: popz.v
00046: push.v self.youtube_current_song
00048: push.v self.youtube_cache
00050: call.i ds_map_find_value(argc=2)
00052: call.i is_undefined(argc=1)
00054: conv.v.b
00055: bf 00079
00056: push.s ""https://www.googleapis.com/youtube/v3/search?part=snippet&maxResults=""
00058: push.v self.youtube_current_song
00060: call.i MOD_get_mus_count(argc=1)
00062: call.i string(argc=1)
00064: add.v.s
00065: push.s ""&type=video&videoEmbeddable=true&fields=items(id%2FvideoId%2Csnippet(channelId%2CchannelTitle%2Ctitle))&q=""
00067: add.s.v
00068: push.v self.youtube_current_song
00070: add.v.v
00071: push.s ""+%22undertale+remix%22&key=AIzaSyANCxd-4e8cXdOx99SFiF24j2GF0Nid0Lc""
00073: add.s.v
00074: call.i http_get(argc=1)
00076: pop.v.v self.youtube_request
00078: b 00082
00079: call.i youtube_load_song(argc=0)
00081: popz.v
00082: b 00091
00083: push.s ""resume_song()""
00085: conv.s.v
00086: push.v self.my_browser
00088: call.i browser_js(argc=2)
00090: popz.v
00091: pushi.e 1337
00092: conv.i.v
00093: ret.v
00094: b func_end
00095: pushvar.v self.argument4
00097: pushvar.v self.argument3
00099: pushvar.v self.argument0
00101: call.i audio_play_sound(argc=3)
00103: pop.v.v self.this_song_i
00105: pushvar.v self.argument2
00107: pushvar.v self.argument0
00109: call.i audio_sound_pitch(argc=2)
00111: popz.v
00112: pushi.e 0
00113: conv.i.v
00114: pushvar.v self.argument1
00116: pushvar.v self.argument0
00118: call.i audio_sound_gain(argc=3)
00120: popz.v
00121: push.v self.this_song_i
00123: ret.v
popenv 00000
", Data.Functions, Data.Variables, Data.Strings));
Data.Code.Add(youtube_play);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_play.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_play"), Code = youtube_play });
Data.Functions.EnsureDefined("youtube_play", Data.Strings);

var youtube_stop = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_stop") };
youtube_stop.Append(Assembler.Assemble(@"
pushi.e " + Data.GameObjects.IndexOf(Data.GameObjects.ByName("obj_time")) + @"
pushenv func_end
00000: push.s ""change_song(null)""
conv.s.v
push.v self.my_browser
call.i browser_js(argc=2)
popz.v
00008: push.s """"
00010: pop.v.s self.youtube_current_song
00012: push.s """"
00014: pop.v.s self.youtube_song_title
pushi.e -4
pop.v.i self.youtube_request
popenv 00000
", Data.Functions, Data.Variables, Data.Strings));
Data.Code.Add(youtube_stop);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_stop.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_stop"), Code = youtube_stop });
Data.Functions.EnsureDefined("youtube_stop", Data.Strings);

var youtube_is_playing = new UndertaleCode() { Name = Data.Strings.MakeString("gml_Script_youtube_is_playing") };
youtube_is_playing.Append(Assembler.Assemble(@"
pushi.e " + Data.GameObjects.IndexOf(Data.GameObjects.ByName("obj_time")) + @"
pushenv func_end

00000: push.v self.youtube_current_song
00002: push.s """"
00004: cmp.s.v NEQ
00005: bf 00012
00006: pushvar.v self.argument0
00008: push.v self.youtube_current_song
00010: cmp.v.v EQ
00011: b 00013
00012: push.e 0
00013: conv.b.v
00014: ret.v

popenv 00000
", Data.Functions, Data.Variables, Data.Strings));
Data.Code.Add(youtube_is_playing);
Data.CodeLocals.Add(new UndertaleCodeLocals() { Name = youtube_is_playing.Name });
Data.Scripts.Add(new UndertaleScript() { Name = Data.Strings.MakeString("youtube_is_playing"), Code = youtube_is_playing });
Data.Functions.EnsureDefined("youtube_is_playing", Data.Strings);

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Other, (uint)62u, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
.localvar 1 data " + var_data + @"
00000: push.s ""id""
00002: conv.s.v
00003: pushvar.v self.async_load
00005: call.i ds_map_find_value(argc=2)
00007: push.v self.youtube_request
00009: cmp.v.v EQ
00010: bf func_end
00011: push.s ""status""
00013: conv.s.v
00014: pushvar.v self.async_load
00016: call.i ds_map_find_value(argc=2)
00018: pushi.e 0
00019: cmp.i.v EQ
00020: bf func_end
00021: push.s ""result""
00023: conv.s.v
00024: pushvar.v self.async_load
00026: call.i ds_map_find_value(argc=2)
00028: call.i json_decode(argc=1)
00030: pop.v.v local.data
00032: push.s ""items""
00034: conv.s.v
00035: pushloc.v local.data
00037: call.i ds_map_find_value(argc=2)
00039: push.v self.youtube_current_song
00041: push.v self.youtube_cache
00043: call.i ds_map_add(argc=3)
00045: popz.v
00046: pushi.e -4
00047: pop.v.i self.youtube_request
00049: call.i youtube_load_song(argc=0)
00051: popz.v
", Data.Functions, Data.Variables, Data.Strings));

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.Other, (uint)70u, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
.localvar 1 type " + var_type + @"

00000: push.s ""id""
00002: conv.s.v
00003: pushvar.v self.async_load
00005: call.i ds_map_find_value(argc=2)
00011: pushi.e 1337
00012: cmp.i.v EQ
00013: bf func_end
00014: push.s ""type""
00016: conv.s.v
00017: pushvar.v self.async_load
00019: call.i ds_map_find_value(argc=2)
00021: pop.v.v local.type

00032: pushloc.v local.type
00034: push.s ""browser_initialized""
00036: cmp.s.v EQ
00037: bf func_end
00038: push.s ""Browser initialized""
00040: conv.s.v
00041: call.i show_debug_message(argc=1)
00043: popz.v
00044: push.s ""<style>head, body, div, iframe { width: 100%; height: 100%; margin: 0; background-color: black; }</style><script src='https://www.youtube.com/iframe_api' async='async'></script><script>var current_song; var loaded = false; var player, prev_player; var last_change; var last_song; function change_song(songid) { if (player && current_song == songid) return; if (!current_song && songid != last_song) { last_change = new Date().getTime(); } current_song = songid; if (current_song != null) { last_song = current_song; } if (player) { if (current_song != null) { if (prev_player != null) { document.body.removeChild(player.getIframe()); } else { prev_player = player; prev_player.getIframe().style.display = 'none'; } player = null; } else { document.body.removeChild(player.getIframe()); player = null; if (prev_player != null) { document.body.removeChild(prev_player.getIframe()); prev_player = null; } } } if (loaded && current_song != null) { var container = document.createElement('div'); document.body.appendChild(container); player = new YT.Player(container, { height: '100%', width: '100%', videoId: current_song, events: { 'onReady': onPlayerReady, 'onStateChange': onPlayerStateChange } }); } } function resume_song() { change_song(last_song); } function onYouTubeIframeAPIReady() { loaded = true; if (current_song) change_song(current_song); } function onPlayerReady(event) { var desiredPos = (new Date().getTime() - last_change)/1000 % player.getDuration(); event.target.seekTo(desiredPos, true); event.target.playVideo(); } function onPlayerStateChange(event) { if (event.target == player) { if (player.getDuration() != 0) { var desiredPos = (new Date().getTime() - last_change)/1000 % player.getDuration(); if (Math.abs(desiredPos - player.getCurrentTime()) > 1) player.seekTo(desiredPos, true); } if (event.data == YT.PlayerState.PLAYING) { if (prev_player) { document.body.removeChild(prev_player.getIframe()); prev_player = null; } } if (event.data == YT.PlayerState.PAUSED || event.data == YT.PlayerState.ENDED) { player.playVideo(); } } }</script>""
00046: conv.s.v
00047: push.v self.my_browser
00049: call.i browser_load_html(argc=2)
00051: popz.v
", Data.Functions, Data.Variables, Data.Strings));

Data.GameObjects.ByName("obj_time").EventHandlerFor(EventType.KeyPress, 32, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
push.v self.youtube_current_song
push.s """"
cmp.s.v NEQ
bf func_end
call.i youtube_load_song(argc=0)
popz.v
", Data.Functions, Data.Variables, Data.Strings));

Data.Scripts.ByName("caster_play").Code.Replace(Assembler.Assemble(@"
00000: pushi.e 0
00001: conv.i.v
00002: pushi.e 100
00003: conv.i.v
00004: pushvar.v self.argument2
00006: pushvar.v self.argument1
00008: pushvar.v self.argument0
00010: call.i youtube_play(argc=5)
00012: ret.v
", Data.Functions, Data.Variables, Data.Strings));

Data.Scripts.ByName("caster_play_l").Code.Replace(Assembler.Assemble(@"
00000: pushi.e 0
00001: conv.i.v
00002: pushi.e 100
00003: conv.i.v
00004: pushvar.v self.argument2
00006: pushvar.v self.argument1
00008: pushvar.v self.argument0
00010: call.i youtube_play(argc=5)
00012: ret.v
", Data.Functions, Data.Variables, Data.Strings));

Data.Scripts.ByName("caster_loop").Code.Replace(Assembler.Assemble(@"
00000: pushi.e 1
00001: conv.i.v
00002: pushi.e 120
00003: conv.i.v
00004: pushvar.v self.argument2
00006: pushvar.v self.argument1
00008: pushvar.v self.argument0
00010: call.i youtube_play(argc=5)
00012: ret.v
", Data.Functions, Data.Variables, Data.Strings));

Data.Scripts.ByName("caster_resume").Code.Replace(Assembler.Assemble(@"
00000: pushvar.v self.argument0
00002: call.i MOD_get_mus_query(argc=1)
00004: push.s """"
00006: cmp.s.v NEQ
00007: bf 00022
00008: pushi.e 0
00009: conv.i.v
00010: pushi.e 0
00011: conv.i.v
00012: pushi.e 0
00013: conv.i.v
00014: pushi.e 0
00015: conv.i.v
00016: pushvar.v self.argument0
00018: call.i youtube_play(argc=5)
00020: popz.v
00021: b func_end
00022: pushvar.v self.argument0
00024: call.i audio_resume_sound(argc=1)
00026: popz.v
", Data.Functions, Data.Variables, Data.Strings));

Data.Scripts.ByName("caster_pause").Code.Replace(Assembler.Assemble(@"
00000: pushvar.v self.argument0
00002: call.i MOD_get_mus_query(argc=1)
00004: call.i youtube_is_playing(argc=1)
00006: conv.v.b
00007: bf 00012
00008: call.i youtube_stop(argc=0)
00010: popz.v
00011: b func_end
00012: pushvar.v self.argument0
00014: call.i audio_pause_sound(argc=1)
00016: popz.v
", Data.Functions, Data.Variables, Data.Strings));

Data.Scripts.ByName("caster_free").Code.Replace(Assembler.Assemble(@"
00000: pushvar.v self.argument0
00002: pushi.e -3
00003: cmp.i.v NEQ
00004: bf 00023
00005: pushvar.v self.argument0
00007: call.i MOD_get_mus_query(argc=1)
00009: call.i youtube_is_playing(argc=1)
00011: conv.v.b
00012: bf 00017
00013: call.i youtube_stop(argc=0)
00015: popz.v
00016: b 00022
00017: pushvar.v self.argument0
00019: call.i audio_stop_sound(argc=1)
00021: popz.v
00022: b func_end
00023: call.i audio_stop_all(argc=0)
00025: popz.v
00026: call.i youtube_stop(argc=0)
00028: popz.v
", Data.Functions, Data.Variables, Data.Strings));

Data.GameObjects.ByName("obj_titleimage").EventHandlerFor(EventType.Draw, EventSubtypeDraw.Draw, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
push.i " + 0x00FFFF.ToString() + @"
conv.i.v
call.i draw_set_color(argc=1)
popz.v
pushi.e 3
conv.i.v
call.i scr_setfont(argc=1)
popz.v

push.d 1
conv.d.v
push.d 1
conv.d.v
push.s ""But Every Time A Song Plays#Its A Random Remix From YouTube Instead""
conv.s.v
pushi.e 150
conv.i.v
pushi.e 160
conv.i.v
call.i scr_drawtext_centered_scaled(argc=5)
popz.v

push.i " + 0x808080.ToString() + @"
conv.i.v
call.i draw_set_color(argc=1)

push.d 1
conv.d.v
push.d 1
conv.d.v
push.s ""mod by krzys_h""
conv.s.v
pushi.e 160
conv.i.v
pushi.e 240
conv.i.v
call.i scr_drawtext_centered_scaled(argc=5)
popz.v
", Data.Functions, Data.Variables, Data.Strings));

ScriptMessage("Finished! Enjoy!");