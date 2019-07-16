EnsureDataLoaded();

ScriptMessage("Adds... a new room?\nJust start playing the game as usual and you'll see\nFor Undertale 1.08\nby krzys_h");

var room_ruins1 = Data.Rooms.ByName("room_ruins1");
var room_water_dogroom = Data.Rooms.ByName("room_water_dogroom");
var room_of_dog = Data.Rooms.ByName("room_of_dog");
var bg_ruinsplaceholder = Data.Backgrounds.ByName("bg_ruinsplaceholder");
var bg_ruinseasynam1 = Data.Backgrounds.ByName("bg_ruinseasynam1");
var bg_ruinseasynam2 = Data.Backgrounds.ByName("bg_ruinseasynam2");
var obj_solidsmall = Data.GameObjects.ByName("obj_solidsmall");
var obj_door_ruins13 = Data.GameObjects.ByName("obj_door_ruins13");
var obj_markerX = Data.GameObjects.ByName("obj_markerX");
var obj_mainchara = Data.GameObjects.ByName("obj_mainchara");
var obj_overworldcontroller = Data.GameObjects.ByName("obj_overworldcontroller");
var obj_solidtall = Data.GameObjects.ByName("obj_solidtall");
var obj_solidlong = Data.GameObjects.ByName("obj_solidlong");
var obj_dialoguer = Data.GameObjects.ByName("obj_dialoguer");
var OBJ_WRITER = Data.GameObjects.ByName("OBJ_WRITER");
var obj_readable = Data.GameObjects.ByName("obj_readable");
var obj_ruinsdoor1 = Data.GameObjects.ByName("obj_ruinsdoor1");
var obj_doorA = Data.GameObjects.ByName("obj_doorA");
var obj_markerB = Data.GameObjects.ByName("obj_markerB");
var obj_rarependant = Data.GameObjects.ByName("obj_rarependant");
var SCR_TEXT = Data.Scripts.ByName("SCR_TEXT");
var spr_event = Data.Sprites.ByName("spr_event");
var spr_interactable = Data.Sprites.ByName("spr_interactable");

// Move the left collision box a little
room_ruins1.GameObjects.ByInstanceID(100085).X = 20;
room_ruins1.GameObjects.ByInstanceID(100086).X = 20;
room_ruins1.GameObjects.ByInstanceID(100087).X = 20;
room_ruins1.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_solidsmall,
    X = 40, Y = 220
});

// Fix depth of the left pillar so that we can go behind it
room_ruins1.Tiles.ByInstanceID(10000302).TileDepth = 99;
room_ruins1.Tiles.ByInstanceID(10000303).TileDepth = 99;
room_ruins1.Tiles.ByInstanceID(10000304).TileDepth = 99;
room_ruins1.Tiles.ByInstanceID(10000305).TileDepth = 99;
room_ruins1.Tiles.ByInstanceID(10000306).TileDepth = 99;
room_ruins1.Tiles.ByInstanceID(10000307).TileDepth = 99;
room_ruins1.Tiles.ByInstanceID(10000209).TileDepth = 99;

// Create the door
room_ruins1.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_door_ruins13,
    X = 25, Y = 320
});
room_ruins1.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_door_ruins13,
    X = 25, Y = 300
});

// The fun part begins. It's the ROOM OF DERERMINATION!
var room_of_determination = new UndertaleRoom()
{
    Name = Data.Strings.MakeString("room_of_determination"),
    Caption = Data.Strings.MakeString(""),
    Width = 960, Height = 180
};
room_of_determination.Views[0].ViewWidth = 320;
room_of_determination.Views[0].ViewHeight = 240;
room_of_determination.Views[0].PortWidth = 640;
room_of_determination.Views[0].PortHeight = 480;
room_of_determination.Views[0].ObjectId = obj_mainchara;
Data.Rooms.Add(room_of_determination);
Data.GeneralInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room_of_determination });

// All game rooms need an overworldcontroller and Frisk... or Chara? :P
// (by the way, is the name Chara just a shortened version of 'main character'...)
// (why not obj_mainfrisk instead? :P)
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_mainchara,
    X = 940, Y = 105
});
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_overworldcontroller,
    X = 20, Y = 20
});
// Add door entrypoint marker
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_markerX,
    X = 940, Y = 100
});

// Actually link the door
obj_door_ruins13.EventHandlerFor(EventType.Alarm, 2, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushbltn.v self.room
pushi.e room_ruins1
cmp.i.v EQ
bf func_end
pushi.e room_of_determination
conv.i.v
call.i room_goto(argc=1)
popz.v
", Data));

// A floor would be nice
for(int x = 0; x <= 940; x += 20)
{
    for (int y = 0; y <= 160; y += 20)
    {
        // bg_ruinsplaceholder, 0, 0 = ruins ground tile
        // bg_ruinsplaceholder, 20, 20 = ruins path tile
        // bg_ruinseasynam1, 40, 20 = ruins wall bottom
        // bg_ruinseasynam1, 40, 10 = ruins wall middle
        // bg_ruinseasynam1, 120, 40 = ruins line top
        // bg_ruinseasynam1, 140, 20 = ruins line left
        // bg_ruinseasynam1, 100, 60 = ruins line top left
        // bg_ruinseasynam1, 100, 80 = ruins line bottom left
        // bg_ruinseasynam1, 120, 0 = ruins line bottom
        var bg = bg_ruinsplaceholder;
        uint sx = 0, sy = 0; // ruins ground tile
        if (y == 100 || y == 120) // And a road in the middle would be cool
        {
            sx = 20; sy = 20; // ruins path file
        }
        else if (y == 60) // Let there...
        {
            bg = bg_ruinseasynam1;
            sx = 40; sy = 20;
        }
        else if (y == 40 || y == 20) // ... be walls!!!!
        {
            bg = bg_ruinseasynam1;
            sx = 40; sy = 10;
        }
        else if (y == 0) // ... please?
        {
            bg = bg_ruinseasynam1;
            sx = 120; sy = 40;
        }
        else if (y == 160) // bottom border
        {
            bg = bg_ruinseasynam1;
            sx = 120; sy = 0;
        }
        if (x == 0) // left border
        {
            bg = bg_ruinseasynam1;
            sx = 140; sy = 20;
            if (y == 0) // upper-left corner
            {
                sx = 100; sy = 60;
            }
            if (y == 160) // stay determined! almost done!
            {
                sx = 100; sy = 80;
            }
        }
        if (x >= 80 && x <= 100 && y >= 20 && y <= 60) // Leave some space for the door
            continue;
        room_of_determination.Tiles.Add(new UndertaleRoom.Tile()
        {
            InstanceID = Data.GeneralInfo.LastTile++,
            BackgroundDefinition = bg,
            X = x, Y = y,
            SourceX = sx, SourceY = sy, Width = 20, Height = 20,
            TileDepth = 999999
        });
    }
}

// I think these collision thingies seem important
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_solidtall,
    X = 0, Y = 0
});
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_solidlong,
    X = 0, Y = 60
});
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_solidlong,
    X = 0, Y = 160
});

// Okay, I lied before. THIS is where real fun begins. CONTENT!

var obj_something_changed_trigger = new UndertaleGameObject()
{
    Name = Data.Strings.MakeString("obj_something_changed_trigger"),
    Sprite = spr_event,
    Visible = false
};
obj_something_changed_trigger.EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e 0
pop.v.i self.con

pushi.e 30
pushi.e -1
pushi.e 0
pop.v.i [array]alarm
", Data));
obj_something_changed_trigger.EventHandlerFor(EventType.Alarm, (uint)0, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e 5
pop.v.i global.typer
pushi.e 0
pop.v.i global.msc
pushi.e 0
pop.v.i global.facechoice
pushi.e 0
pop.v.i global.faceemotion

push.v self.room
pushi.e room_of_determination
cmp.i.v NEQ
bf enter_the_determination

push.s ""* You see a weird door hidden&  behind the left pillar/""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

push.s ""* You wonder how you missed it&  the last time you played/""
conv.s.v
pushi.e -5
pushi.e 1
pop.v.v [array]msg

push.s ""* You don't even remember&  seeing this door in any&  other timeline/%%""
conv.s.v
pushi.e -5
pushi.e 2
pop.v.v [array]msg

b go_display

enter_the_determination: push.s ""* You wonder how you managed& to miss such a long corridor/%%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

go_display: pushi.e obj_dialoguer
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
call.i instance_create(argc=3)
popz.v

pushi.e 1
pop.v.i global.interact
pushi.e 1
pop.v.i self.con
", Data));
obj_something_changed_trigger.EventHandlerFor(EventType.Step, EventSubtypeStep.Step, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
; Toby, please, why
; why can't you just make the obj_dialoguer block movement automatically
; writing this every time will get really painful really fast
; but DETERMINATION will save me

; Actual comment to explain what this is doing:
; movement block is managed via global variable 'interact', but
; you have to set and unset it manually every time you want
; to invoke the dialog box

push.v self.con
pushi.e 1
cmp.i.v EQ
bf func_end
pushi.e OBJ_WRITER
conv.i.v
call.i instance_exists(argc=1)
pushi.e 0
cmp.i.v EQ
bf func_end

pushi.e 0
pop.v.i global.interact
pushi.e 0
pop.v.i self.con
", Data));
Data.GameObjects.Add(obj_something_changed_trigger);

room_ruins1.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_something_changed_trigger,
    X = 80, Y = 380
});
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_something_changed_trigger,
    X = 900, Y = 20
});

// And MORE CONTENT!
var obj_readable_determination = new UndertaleGameObject()
{
    Name = Data.Strings.MakeString("obj_readable_determination"),
    Sprite = spr_interactable,
    ParentId = obj_readable,
    Visible = false
};
obj_readable_determination.EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e 0
pop.v.i self.myinteract
pushi.e 0
pop.v.i self.specialread
pushi.e 1
pop.v.i self.cantalk
; wtf is this value
push.i 438274832
pop.v.i self.mydialoguer
pushi.e 1
pop.v.i self.image_xscale
pushi.e 1
pop.v.i self.image_yscale
", Data));
obj_readable_determination.EventHandlerFor(EventType.Alarm, (uint)0, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e 3
pop.v.i self.myinteract
pushi.e 0
pop.v.i global.msc
pushi.e 5
pop.v.i global.typer
pushi.e 0
pop.v.i global.facechoice
pushi.e 0
pop.v.i global.faceemotion

push.v self.x
pushi.e 700
cmp.i.v GTE
bf not_1

push.s ""* This is a special room/""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

push.s ""* I call it the ROOM OF&  DETERMINATION/%%""
conv.s.v
pushi.e -5
pushi.e 1
pop.v.v [array]msg

b finish

not_1: push.v self.x
pushi.e 600
cmp.i.v GTE
bf not_2

push.s ""* This room shows just how much&  you can modify a game if you&  are DETERMINED enough/%%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

b finish

not_2: push.v self.x
pushi.e 500
cmp.i.v GTE
bf not_3

push.s ""* A month ago, all we could do&  was modify some sprites&  and text/""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

push.s ""* And now? I'm putting code,&  rooms and objects in the game!/%%""
conv.s.v
pushi.e -5
pushi.e 1
pop.v.v [array]msg

b finish

not_3: push.v self.x
pushi.e 400
cmp.i.v GTE
bf not_4

push.s ""* Pretty cool, huh?/%%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

b finish

not_4: push.v self.x
pushi.e 300
cmp.i.v GTE
bf not_5

push.s ""* So it's finally time/%%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

b finish

not_5: push.v self.x
pushi.e 200
cmp.i.v GTE
bf not_6

push.s ""* We will now finally see what&  happens if you pick up this orb&  without the annoying dog.../%%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

b finish

not_6: push.s ""* It's broken%  Assembly scripting is hard/%%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

finish: pushi.e obj_dialoguer
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
call.i instance_create(argc=3)
pop.v.v self.mydialoguer
", Data));
Data.GameObjects.Add(obj_readable_determination);

for (int i = 0; i < 6; i++)
{
    room_of_determination.Tiles.Add(new UndertaleRoom.Tile()
    {
        InstanceID = Data.GeneralInfo.LastTile++,
        BackgroundDefinition = bg_ruinseasynam2,
        X = 700 - i * 100, Y = 40,
        SourceX = 80, SourceY = 100, Width = 40, Height = 20,
        TileDepth = 999998
    });
    room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
    {
        InstanceID = Data.GeneralInfo.LastObj++,
        ObjectDefinition = obj_readable_determination,
        X = 700 - i * 100, Y = 60
    });
    room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
    {
        InstanceID = Data.GeneralInfo.LastObj++,
        ObjectDefinition = obj_readable_determination,
        X = 700 - i * 100 + 20, Y = 60
    });
}

// Prepare the orb v2
var obj_determined_rarependant = new UndertaleGameObject()
{
    Name = Data.Strings.MakeString("obj_determined_rarependant"),
    Sprite = obj_rarependant.Sprite,
    Visible = obj_rarependant.Visible,
    Solid = obj_rarependant.Solid,
    ParentId = obj_rarependant.ParentId
};
obj_determined_rarependant.EventHandlerFor(EventType.Create, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e 0
pop.v.i self.myinteract
pushi.e 0
pop.v.i self.facing
pushi.e 270
pop.v.i self.direction
pushi.e 0
pop.v.i self.image_speed
pushi.e 0
pop.v.i self.con
", Data));
obj_determined_rarependant.EventHandlerFor(EventType.Alarm, (uint)0, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e 3
pop.v.i self.myinteract
; this is using SCR_TEXT because you are basically forced to do that with dialog prompts
pushi.e 11337
pop.v.i global.msc
pushi.e 5
pop.v.i global.typer
pushi.e 0
pop.v.i global.facechoice
pushi.e 0
pop.v.i global.faceemotion
pushi.e 1
pop.v.i self.con

pushi.e obj_dialoguer
conv.i.v
pushi.e 0
conv.i.v
pushi.e 0
conv.i.v
call.i instance_create(argc=3)
pop.v.v self.mydialoguer
", Data));
obj_determined_rarependant.EventHandlerFor(EventType.Step, EventSubtypeStep.Step, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
push.v self.con
pushi.e 1
cmp.i.v EQ
bf end
pushi.e OBJ_WRITER
conv.i.v
call.i instance_exists(argc=1)
pushi.e 0
cmp.i.v EQ
bf end

pushglb.v global.choice
pushi.e 0
cmp.i.v EQ
bf end

pushi.e 40
pushi.e -1
pushi.e 1
pop.v.i [array]alarm

pushi.e 0
pop.v.i self.con

end: call.i event_inherited(argc=0)
popz.i
", Data));
obj_determined_rarependant.EventHandlerFor(EventType.Alarm, (uint)1, Data.Strings, Data.Code, Data.CodeLocals).Append(Assembler.Assemble(@"
pushi.e room_of_dog
conv.i.v
call.i room_goto(argc=1)
popz.v
", Data));
Data.GameObjects.Add(obj_determined_rarependant);

SCR_TEXT.Code.Append(Assembler.Assemble(@"
; Toby, why did you think this was a good idea
; actually, it's not as bad as I thought it was going to be :P

pushbltn.v self.argument0
pushi.e 11337
cmp.i.v EQ
bf not_11337

push.s ""* (It's a legendary artifact.)/""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

push.s ""* (Will you take it?)& &         Take it     Leave it  \C ""
conv.s.v
pushi.e -5
pushi.e 1
pop.v.v [array]msg

push.s "" ""
conv.s.v
pushi.e -5
pushi.e 2
pop.v.v [array]msg

b func_end

not_11337: pushbltn.v self.argument0
pushi.e 11338
cmp.i.v EQ
bf func_end

push.s ""* (You took the legendary&  artifact.)/%%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg

pushglb.v global.choice
pushi.e 1
cmp.i.v EQ
bf func_end

push.s "" %%""
conv.s.v
pushi.e -5
pushi.e 0
pop.v.v [array]msg
", Data));

// Okay, now for some copying
var room_of_determined_dog = new UndertaleRoom()
{
    Name = Data.Strings.MakeString("room_of_determined_dog"),
    Caption = Data.Strings.MakeString(""),
    Width = room_water_dogroom.Width,
    Height = room_water_dogroom.Height,
};
room_of_determined_dog.Views[0].ViewWidth = room_water_dogroom.Views[0].ViewWidth;
room_of_determined_dog.Views[0].ViewHeight = room_water_dogroom.Views[0].ViewHeight;
room_of_determined_dog.Views[0].PortWidth = room_water_dogroom.Views[0].PortWidth;
room_of_determined_dog.Views[0].PortHeight = room_water_dogroom.Views[0].PortHeight;
room_of_determined_dog.Views[0].ObjectId = room_water_dogroom.Views[0].ObjectId;
Data.Rooms.Add(room_of_determined_dog);
Data.GeneralInfo.RoomOrder.Add(new UndertaleResourceById<UndertaleRoom, UndertaleChunkROOM>() { Resource = room_of_determined_dog });

foreach (var other in room_water_dogroom.Tiles)
{
    room_of_determined_dog.Tiles.Add(new UndertaleRoom.Tile()
    {
        X = other.X,
        Y = other.Y,
        BackgroundDefinition = other.BackgroundDefinition,
        SourceX = other.SourceX,
        SourceY = other.SourceY,
        Width = other.Width,
        Height = other.Height,
        TileDepth = other.TileDepth,
        InstanceID = Data.GeneralInfo.LastTile++,
        ScaleX = other.ScaleX,
        ScaleY = other.ScaleY,
        Color = other.Color
    });
}
foreach(var other in room_water_dogroom.GameObjects)
{
    var obj = new UndertaleRoom.GameObject()
    {
        X = other.X,
        Y = other.Y,
        ObjectDefinition = other.ObjectDefinition,
        InstanceID = Data.GeneralInfo.LastObj++,
        CreationCode = other.CreationCode,
        ScaleX = other.ScaleX,
        ScaleY = other.ScaleY,
        Color = other.Color,
        Rotation = other.Rotation,
        PreCreateCode = other.PreCreateCode
    };
    if (obj.ObjectDefinition == obj_rarependant)
        obj.ObjectDefinition = obj_determined_rarependant;
    room_of_determined_dog.GameObjects.Add(obj);
}

// hOI! I need a door to that room!
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_ruinsdoor1,
    X = 80, Y = 20
});
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_doorA,
    X = 80, Y = 60
});
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_doorA,
    X = 100, Y = 60
});
room_of_determination.GameObjects.Add(new UndertaleRoom.GameObject()
{
    InstanceID = Data.GeneralInfo.LastObj++,
    ObjectDefinition = obj_markerB,
    X = 90, Y = 70
});

ChangeSelection(room_of_determination);
