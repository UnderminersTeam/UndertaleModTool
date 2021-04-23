switch argument0
{
    case 0:
        break
    case 1:
        global.msg[0] = "\XLa, la.^3 &Time to wake&up and\R smell\X &the^4 pain./"
        global.msg[1] = "* Though^2.^4.^6.^8.&It's still a&little shaky./"
        global.msg[2] = "fhuehfuehfuehfuheufhe/%"
        global.msg[3] = "%%%"
        break
    case 2:
        global.msg[0] = "* TestMonster and its cohorts&draw near!"
        global.msg[1] = "%%%"
        break
    case 3:
        global.msg[0] = " "
        if (global.monster[0] == true)
        {
            with (global.monsterinstance[0])
                script_execute(scr_mercystandard)
            adder = " "
            adder = "\W "
            if (global.monsterinstance[0].mercy < 0)
            {
                if (global.flag[22] == 0)
                    adder = "\Y "
                if (global.flag[22] == 2)
                    adder = "\p "
            }
            global.msg[0] = adder
            global.msg[0] += ("  * " + global.monstername[0])
            if (global.monstertype[0] == global.monstertype[1] || global.monstertype[0] == global.monstertype[2])
                global.msg[0] += " A"
        }
        global.msg[0] += "\W &"
        if (global.monster[1] == true)
        {
            with (global.monsterinstance[1])
                script_execute(scr_mercystandard)
            if (global.monsterinstance[1].mercy < 0 && global.flag[22] == 0)
                global.msg[0] += "\Y"
            if (global.monsterinstance[1].mercy < 0 && global.flag[22] == 2)
                global.msg[0] += "\p"
            global.msg[0] += ("   * " + global.monstername[1])
            if (global.monstertype[1] == global.monstertype[0])
                global.msg[0] += " B"
        }
        global.msg[0] += "\W &"
        if (global.monster[2] == true)
        {
            with (global.monsterinstance[2])
                script_execute(scr_mercystandard)
            if (global.monsterinstance[2].mercy < 0 && global.flag[22] == 0)
                global.msg[0] += "\Y"
            if (global.monsterinstance[2].mercy < 0 && global.flag[22] == 2)
                global.msg[0] += "\p"
            global.msg[0] += ("   * " + global.monstername[2])
            if (global.monstertype[2] == global.monstertype[1])
                global.msg[0] += " C"
        }
        global.msg[1] = "%%%"
        break
    case 7:
        global.msg[0] = " "
        for (i = 0; i < 3; i += 1)
        {
            if (global.monster[i] == true)
            {
                with (global.monsterinstance[i])
                    script_execute(scr_mercystandard)
                if (global.monsterinstance[i].mercy < 0 && global.flag[22] == 0)
                    global.msg[0] = "\Y "
                if (global.monsterinstance[i].mercy < 0 && global.flag[22] == 2)
                    global.msg[0] = "\p "
            }
        }
        global.msg[0] += "  * Spare"
        if (global.mercy == 0)
            global.msg[0] += "& \W  * Flee"
        break
    case 9:
        global.msg[0] = (("   * " + global.itemnameb[0]) + "     ")
        if (global.item[1] != 0)
            global.msg[0] += ("* " + global.itemnameb[1])
        global.msg[0] += " &"
        if (global.item[2] != 0)
            global.msg[0] += (("   * " + global.itemnameb[2]) + "     ")
        if (global.item[3] != 0)
            global.msg[0] += ("* " + global.itemnameb[3])
        global.msg[0] += " &                     PAGE 1"
        global.msg[1] = "%%%"
        break
    case 10:
        global.msg[0] = (("   * " + global.itemnameb[4]) + "     ")
        if (global.item[5] != 0)
            global.msg[0] += ("* " + global.itemnameb[5])
        global.msg[0] += " &"
        if (global.item[6] != 0)
            global.msg[0] += (("   * " + global.itemnameb[6]) + "     ")
        if (global.item[7] != 0)
            global.msg[0] += ("* " + global.itemnameb[7])
        global.msg[0] += " &                     PAGE 2"
        global.msg[1] = "%%%"
        break
    case 11:
        global.msg[0] += " &"
        if (global.item[8] < 99)
            global.msg[0] += (("* You recovered " + string(global.item[8])) + " HP!/%")
        else
            global.msg[0] += "* Your HP was maxed out./%"
        break
    case 12:
        i = round(random(18))
        if (i == 0)
            global.msg[0] = (("* You bid a quiet farewell&  to the " + global.itemname[global.menucoord[1]]) + ".")
        if (i == 1)
            global.msg[0] = (("* You put the " + global.itemname[global.menucoord[1]]) + "&  on the ground and gave it a&  little pat.")
        if (i == 2)
            global.msg[0] = (("* You threw the " + global.itemname[global.menucoord[1]]) + "&  on the ground like the piece&  of trash it is.")
        if (i == 3)
            global.msg[0] = (("* You abandoned the &  " + global.itemname[global.menucoord[1]]) + ".")
        if (i > 3)
            global.msg[0] = (("* The " + global.itemname[global.menucoord[1]]) + " was&  thrown away.")
        global.msg[0] += "/%"
        break
    case 14:
        i = round(random(20))
        if (i == 0 || i == 1)
            global.msg[0] = "   * I'm outta here."
        if (i == 2)
            global.msg[0] = "   * I've got better to do."
        if (i > 3)
            global.msg[0] = "   * Escaped..."
        if (i == 3)
            global.msg[0] = "   * Don't slow me down."
        if (global.xpreward[3] > 0 || global.goldreward[3] > 0)
            global.msg[0] = (((("   * Ran away with " + string(global.xpreward[3])) + " EXP&     and ") + string(global.goldreward[3])) + " GOLD.")
        break
    case 15:
        if (room == room_ruins1)
        {
            global.msg[0] = "* (The shadow of the ruins&  looms above^1, filling you with&  determination.)/"
            global.msg[1] = "* (HP fully restored.)/%%"
        }
        if (room == room_ruins7)
        {
            global.msg[0] = "* (Playfully crinkling through&  the leaves fills you with&  determination.)/"
            global.msg[1] = "* (HP fully restored.)/%%"
        }
        if (room == room_ruins12A)
        {
            global.msg[0] = "* (Knowing the mouse might one&  day leave its hole and&  get the cheese...)/"
            global.msg[1] = "* (It fills you with&  determination.)/%%"
        }
        if (room == room_ruins19)
            global.msg[0] = "* (Seeing such a cute^1, tidy&  house in the RUINS gives&  you determination.)/%%"
        if (global.flag[202] >= 20)
            global.msg[0] = "* Determination./%%"
        if (room == room_tundra1)
            global.msg[0] = "* (The cold atmosphere of a&  new land... it fills you&  with determination.)/%%"
        if (room == room_tundra3)
            global.msg[0] = "* (The convenience of that&  lamp still fills you&  with determination.)/%%"
        if (room == room_tundra_spaghetti)
        {
            global.msg[0] = "* (Knowing the mouse might one&  day find a way to heat&  up the spaghetti...)/"
            global.msg[1] = "* (It fills you with&  determination.)/%%"
        }
        if (room == room_tundra_lesserdog)
        {
            global.msg[0] = "* (Knowing that dog will&  never give up trying to&  make the perfect snowdog...)/"
            global.msg[1] = "* (It fills you with&  determination.)/%%"
            if (global.flag[55] == 1)
            {
                global.msg[0] = "* (Snow can always be broken&  down and rebuilt into&  something more useful.)/"
                global.msg[1] = "* (This simple fact fills&  you with determination.)/%%"
            }
        }
        if (room == room_tundra_town)
            global.msg[0] = "* (The sight of such a friendly&  town fills you with&  determination.)/%%"
        if (room == room_water2)
            global.msg[0] = "* (The sound of rushing water&  fills you with&  determination.)/%%"
        if (room == room_water4)
            global.msg[0] = "* (A feeling of dread hangs&  over you...^1)&* (But you stay determined.)/%%"
        if (room == room_water_savepoint1)
        {
            global.msg[0] = "* (Knowing the mouse might one&  day extract the cheese from&  the mystical crystal...)/"
            global.msg[1] = "* (It fills you with&  determination.)/%%"
        }
        if (room == room_water_preundyne)
        {
            global.msg[0] = "* (The sound of muffled rain&  on the cavetop...)/"
            if (global.flag[86] == 1)
                global.msg[0] = "* (The serene sound of a&  distant music box...)/"
            global.msg[1] = "* (It fills you with&  determination.)/%%"
        }
        if (room == room_water_trashzone2)
            global.msg[0] = "* (The feeling of your socks&  squishing as you step&  gives you determination.)/%%"
        if (room == room_water_trashsavepoint)
        {
            global.msg[0] = "* (The waterfall here seems&  to flow from the&  ceiling of the cavern...)/"
            global.msg[1] = "* (Occasionally^1, a piece of&  trash will flow&  through...)/"
            global.msg[2] = "* (... and fall into the&  bottomless abyss below.)/"
            global.msg[3] = "* (Viewing this endless&  cycle of worthless&  garbage...)/"
            global.msg[4] = "* (It fills you with&  determination.)/%%"
            if (global.flag[91] == 1)
                global.msg[0] = "* (Partaking in worthless&  garbage fills you&  with determination.)/%%"
            global.flag[91] = 1
        }
        if (room == room_water_friendlyhub)
            global.msg[0] = "* (You feel a calming&  tranquility^1. You're filled&  with determination...)/%%"
        if (room == room_water_temvillage)
            global.msg[0] = "* (You feel..^1. something.)&* (You're filled with&  detemmienation.)/%%"
        if (room == room_water_undynefinal)
        {
            global.msg[0] = "* (The wind is howling^1.&* You're filled with&  determination...)/%%"
            if (global.flag[99] > 0)
                global.msg[0] = "* (The howling wind is&  now a breeze^1. This gives&  you determination...)/%%"
            if (global.flag[350] == 1)
                global.msg[0] = "* (The wind has stopped^1.&* You're filled with&  determination...)/%%"
        }
        if (room == room_fire_prelab)
        {
            global.msg[0] = "* (Seeing such a strange&  laboratory in a place like&  this...)/"
            global.msg[1] = "* (You're filled with&  determination.)/%%"
        }
        if (room == room_fire6)
            global.msg[0] = "* (The wooshing sound of steam&  and cogs..^1. it fills you&  with determination.)/%%"
        if (room == room_fire_savepoint1)
        {
            global.msg[0] = "* (An ominous structure looms&  in the distance...)/"
            global.msg[1] = "* (You're filled with&  determination.)/%%"
        }
        if (room == room_fire_mewmew2)
        {
            global.msg[0] = "* (Knowing the mouse might one&  day hack the computerized&  safe and get the cheese...)/"
            global.msg[1] = "* (It fills you with&  determination.)/%%"
        }
        if (room == room_fire_hotelfront_1)
            global.msg[0] = "* (A huge structure lies north.^1)&* (You're filled with&  determination.)/%%"
        if (room == room_fire_hotellobby)
            global.msg[0] = "* (The relaxing atmosphere&  of this hotel..^1. it fills&  you with determination.)/%%"
        if (room == room_fire_core_branch)
            global.msg[0] = "* (The air is filled with&  the smell of ozone..^1. it fills&  you with determination.)/%%"
        if (room == room_fire_core_premett)
        {
            global.msg[0] = "* (Behind this door must be&  the elevator to the King's&  castle.)/"
            global.msg[1] = "* (You're filled with&  determination.)/%%"
        }
        if (room == room_fire_savepoint2)
        {
            global.msg[0] = "* (The smell of cobwebs fills&  the air...)/"
            global.msg[1] = "* (You're filled with&  determination.)/%%"
        }
        break
    case 16:
        i = round(random(14))
        script_execute(scr_itemname)
        if (i <= 12)
            global.msg[0] = (("* The " + global.itemname[global.menucoord[6]]) + " was&  put away.")
        if (i > 12)
            global.msg[0] = (("* The " + global.itemname[global.menucoord[6]]) + " was&  tossed inside recklessly.")
        if (i > 13)
            global.msg[0] = (("* The " + global.itemname[global.menucoord[6]]) + " was&  placed thoughtfully inside.")
        global.msg[0] += "/%"
        break
    case 17:
        i = round(random(14))
        script_execute(scr_storagename, 300)
        if (i <= 12)
            global.msg[0] = (("* The " + global.itemname[global.menucoord[7]]) + " was&  taken out.")
        if (i > 12)
            global.msg[0] = (("* The " + global.itemname[global.menucoord[7]]) + " was&  grabbed impatiently.")
        if (i > 13)
            global.msg[0] = (("* The " + global.itemname[global.menucoord[7]]) + " was&  taken out and held like a&  small puppy.")
        global.msg[0] += "/%"
        break
    case 18:
        global.msg[0] = "* You can't carry any more./%%"
        break
    case 19:
        global.msg[0] = "* The box is full./%%"
        break
    case 23:
        global.msg[0] = "* You leave the Quiche on the&  ground and tell it you'll&  be right back./%%"
        break
    case 30:
        global.msg[0] = "* Use the box?& &         Yes         No      \C "
        global.msg[1] = " "
        global.msg[2] = " "
        break
    case 31:
        if (global.choice == 0)
        {
            if (global.item[0] != 0 || global.flag[300] != 0)
            {
                if (instance_exists(obj_itemswapper) == 0)
                    instance_create(0, 0, obj_itemswapper)
                global.msg[0] = "  %%"
            }
            else
            {
                gx = floor(random(3))
                if (gx == 0)
                    global.msg[0] = "* You have no items^1.&* You put a little time into&  the box./%%"
                if (gx == 1)
                    global.msg[0] = "* You have no items^1.&* You put a little effort&  into the box./%%"
                if (gx == 2)
                    global.msg[0] = "* You have no items^1.&* You put a little feeling&  into the box./%%"
            }
        }
        if (global.choice == 1)
            global.msg[0] = "  %%"
        break
    case 200:
        global.msg[0] = "\W* Howdy^2!&* I'm\Y FLOWEY\W.^2 &* \YFLOWEY\W the \YFLOWER\W!/"
        global.msg[1] = "* Hmmm.../"
        global.msg[2] = "* You're new to the&  UNDERGROUND^2, aren'tcha?/"
        global.msg[3] = "* Golly^1, you must be&  so confused./"
        global.msg[4] = "* Someone ought to teach&  you how things work&  around here!/"
        global.msg[5] = "* I guess little old me&  will have to do./"
        global.msg[6] = "* Ready^2?&* Here we go!/%%"
        break
    case 201:
        global.msg[0] = "\E2* This way./%%"
        global.msg[1] = "%%%"
        break
    case 202:
        global.msg[0] = "\E2* Welcome to your new&  home^1, innocent one./"
        global.msg[1] = "* Allow me to educate you&  in the operation of the&  RUINS./%%"
        break
    case 203:
        global.msg[0] = "\E2* The RUINS are full of&  puzzles./"
        global.msg[1] = "* Ancient fusions between &  diversions and doorkeys./"
        global.msg[2] = "* One must solve them&  to move from room to&  room./"
        global.msg[3] = "* Please adjust yourself    to the sight of them./%"
        break
    case 204:
        global.msg[0] = "\E2* To make progress here,^1 &  you will need to trigger&  several switches./"
        global.msg[1] = "* Do not worry,^1 I have &  labelled the ones that&  you need to flip./%"
        if (global.flag[6] == 1)
        {
            global.msg[1] = "* Do not worry,^1 I have &  labelled the ones that&  you need to flip./"
            global.msg[2] = "* ... eh^1?&* It seems that the&  labelling has worn away./"
            global.msg[3] = "* Oh dear./"
            global.msg[4] = "* This might be far more&  challenging than I&  anticipated.../%%"
        }
        break
    case 205:
        global.msg[0] = "\E2* The first switch is over&  on the wall./%"
        break
    case 206:
        global.msg[0] = "\E1* Do you need some help..^1?&* Press the switch on the   wall./"
        global.msg[1] = "\E0* Come on^1, you can do it!/%"
        break
    case 207:
        global.msg[0] = "\E2* Go on^1, press the switch&  on the left./%"
        if (global.flag[6] == 1)
            global.msg[0] = "\E2* I believe it was&  the switch on the&  left./%"
        break
    case 208:
        global.msg[0] = "\E1* You do know which way&  left is^1, do you not?/"
        global.msg[1] = "\E0* Press the switch that I&  labelled for you./%"
        if (global.flag[6] == 1)
            global.msg[0] = "\E1* You do know which way&  left is^1, do you not?/%%"
        break
    case 209:
        global.msg[0] = "\E1* You are very curious,^1 &  are you not?/"
        global.msg[1] = "\E1* Please understand.^2 & \E0I only want the best&  for you./%"
        break
    case 210:
        global.msg[0] = "\E0* Splendid!^2 &* I am proud of you,^1 &  little one./"
        global.msg[1] = "* Let us move to the&  next room./%"
        break
    case 211:
        global.msg[0] = "\E1* As a human living in&  the UNDERGROUND,^1 &  monsters may attack you./"
        global.msg[1] = "\E2* You will need to be&  prepared for this&  situation./"
        global.msg[2] = "\E0* However, worry not!^2 &* The process is simple./"
        global.msg[3] = "\E2* When you encounter a &  monster,^1 you will enter&  a FIGHT./"
        global.msg[4] = "* While you are in a&  FIGHT^1, strike up a&  friendly conversation./"
        global.msg[5] = "\E2* Stall for time.&  I will come to resolve&  the conflict./"
        global.msg[6] = "\E2* Practice talking to&  the dummy./%"
        break
    case 212:
        if (global.flag[12] == 1)
        {
            global.msg[0] = "\E1* Ahh,^1 the dummies are&  not for fighting!^2 &* They are for talking!/"
            global.msg[1] = "* We do not want to hurt&  anybody, do we...?^2 \E0 &* Come now./%"
        }
        if (global.flag[10] == 1)
            global.msg[0] = "\E0* Ah,^1 very good!^2 &* You are very good./%"
        if (global.flag[11] == 1)
        {
            global.msg[0] = "\E1* .../"
            global.msg[1] = "\E1* ... you ran away.../"
            global.msg[2] = "\E0* Truthfully^1, that was&  not a poor choice./"
            global.msg[3] = "\E0* It is better to&  avoid conflict&  whenever possible./"
            global.msg[4] = "\E1* That..^1. however^1, is&  only a dummy^2.&* It cannot harm you./"
            global.msg[5] = "\E1* It is made of cotton^1.&* It has no desire&  for revenge.../"
            global.msg[6] = "\E0* Nevermind^2.&* Stay close to me and&  I will keep you safe./%"
        }
        if (global.flag[13] == 1)
        {
            global.msg[0] = "\E3* ^1.^1.^1./"
            global.msg[1] = "\E4* ^1.^1.^1./"
            global.msg[2] = "\E0* The next room awaits./%"
        }
        break
    case 213:
        global.msg[0] = "\E2* Practice talking to&  the dummy./"
        global.msg[1] = "\E1* You can say anything..^2.\E2 &* I do not think the dummy&  will be bothered./%"
        break
    case 214:
        global.msg[0] = "\E0* Do you need some&  ideas for conversation&  topics?/"
        global.msg[1] = "* Well^1, I often start with&  a simple 'how do you&  do...'/"
        global.msg[2] = "* You could ask them about&  their favorite books.../"
        global.msg[3] = "* Jokes can be useful for&  'breaking the ice.'/"
        global.msg[4] = "* Listen to this one.../"
        global.msg[5] = "* What did the skeleton&  tile his roof with?/"
        global.msg[6] = "* ... SHIN-gles!/"
        global.msg[7] = "\E1* .../"
        global.msg[8] = "\E0* Well^1, I thought it&  was amusing./%"
        break
    case 215:
        global.msg[0] = "\E2* Practice talking to&  the dummy./"
        global.msg[1] = "\E1* You can say anything..^2.\E0 &* The dummy will not&  be bothered./%"
        break
    case 216:
        global.msg[0] = "\E1* There is another puzzle&  in this room.../"
        global.msg[1] = "\E0* I wonder if you can&  solve it?/%"
        break
    case 217:
        global.msg[0] = "\E1* This is the puzzle^1,&  but.../"
        global.msg[1] = "\E0* Here^1, take my hand&  for a moment./%"
        break
    case 218:
        global.msg[0] = "\E1* Puzzles seem a little&  too dangerous for&  now./%"
        break
    case 219:
        global.msg[0] = ".../%"
        break
    case 220:
        global.msg[0] = "\E2* Greetings,^1 my child^2.&* Do not worry^1, I did&  not leave you./"
        global.msg[1] = "\E0* I was merely behind this&  pillar the whole time./"
        global.msg[2] = "* Thank you for trusting&  me./"
        global.msg[3] = "\E2* However^1, there was an&  important reason for&  this exercise./"
        global.msg[4] = "* ... to test your&  independence./"
        global.msg[5] = "\E1* I must attend to some&  business^1, and you must&  stay alone for a while./"
        global.msg[6] = "\E0* Please remain here^2.&*\E1 It's dangerous to&  explore by yourself./"
        global.msg[7] = "\E0* I have an idea^2.&* I will give you a&  CELL PHONE./"
        global.msg[8] = "* If you have a need for&  anything^1, just call./"
        global.msg[9] = "\E1* Be good^1, alright?/%"
        break
    case 221:
        global.msg[0] = "\E0* You have done&  excellently thus&  far^1, my child./"
        global.msg[1] = "\E2* However... I have a&  difficult request to ask&  of you./"
        global.msg[2] = "* .../"
        global.msg[3] = "* I would like you to walk&  to the end of the room&  by yourself./"
        global.msg[4] = "\E1* Forgive me for this./%"
        break
    case 222:
        global.msg[0] = "* Ring..\E0.\TT /"
        global.msg[1] = "\F1 %"
        global.msg[2] = "* Hello?&* This is TORIEL./"
        global.msg[3] = "* For no reason in&  particular...&* Which do you prefer?/"
        global.msg[4] = "* Cinnamon or&  butterscotch?&  Cinnamon    Bscotch \C"
        global.msg[5] = " "
        break
    case 223:
        if (global.choice == 0)
        {
            global.flag[46] = 0
            ini_open("undertale.ini")
            ini_write_real("Toriel", "Bscotch", 2)
            ini_close()
        }
        if (global.choice == 1)
        {
            global.flag[46] = 1
            ini_open("undertale.ini")
            ini_write_real("Toriel", "Bscotch", 1)
            ini_close()
        }
        global.msg[0] = "* Oh^1, I see.&* Thank you very much!/"
        global.msg[1] = "\TS \F0 \T0 %"
        global.msg[2] = "* Click.../%%"
        global.msg[5] = "* /"
        break
    case 224:
        global.msg[0] = "* It's a fishing rod affixed&  to the ground.../"
        global.msg[1] = "* Reel it in?& &         Yes         No      \C "
        global.msg[2] = " "
        break
    case 225:
        if (global.choice == 0)
        {
            if instance_exists(obj_ladiesfishingrod)
            {
                obj_ladiesfishingrod.reeled = 1
                obj_ladiesfishingrod.image_index = 1
            }
            global.msg[0] = "* All that's attached to&  the end is a photo of a&  weird-looking monster.../"
            global.msg[1] = "* (Call Me!&  Here's my number!)/"
            global.msg[2] = "* You decide not to call./%%"
            if (global.flag[7] == 1)
            {
                global.msg[0] = "* All that's attached to&  the end is a note./"
                global.msg[1] = "* (Nevermind^1, guys!)/%%"
            }
        }
        if (global.choice == 1)
            global.msg[0] = "* You leave it alone./%%"
        break
    case 226:
        if (global.flag[56] == 5)
            global.msg[0] = "* I shouldn't have given myself&  away so easily.../%%"
        if (global.flag[56] == 4)
        {
            global.msg[0] = "* Did you just...&* Consume the part of me&  I had given you?/"
            global.msg[1] = "* In front of my very eyes!?/"
            global.msg[2] = "* I have no words for you..^1.&* Begone!/%%"
            global.flag[56] = 5
        }
        if (global.flag[56] == 2)
        {
            global.msg[0] = "* Thank you for taking care&  of me.../%%"
            scr_itemcheck(16)
            scr_storagecheck(16)
            if (haveit == 0 && haveit2 == 0)
            {
                global.msg[0] = "* Huh^1? Again...?/"
                global.msg[1] = "* I'm sorry..^1. if I give you&  any more^1, there will be&  nothing left of me./"
                global.msg[2] = "* I suppose it is true^1.&* Travelling beyond our limits&  is but a fantasy./"
                global.msg[3] = "* It's no different for&  anyone else./"
                global.msg[4] = "* All of monsterkind&  are doomed to stay&  underground^1, forever.../%%"
            }
        }
        if (global.flag[56] == 1)
        {
            global.msg[0] = "* How am I doing^1?&* By " + chr(34) + "I" + chr(34) + " I mean the piece&  of me I gave you.../"
            scr_itemcheck(16)
            scr_storagecheck(16)
            if (haveit == 0 && haveit2 == 0)
            {
                global.msg[1] = "* Huh^1? You lost it...^1?&* ... I suppose I can give&  you another piece.../"
                scr_itemget(16)
                if (noroom == 1)
                    global.msg[2] = "* You don't have any room^1?&* OK..^1. I understand I am not&  a priority for you^1, then./%%"
                else
                {
                    global.msg[2] = "* Please be careful this&  time./"
                    global.msg[3] = "* (You got the Snowman&  Piece.)/%%"
                    global.flag[56] = 2
                }
            }
            else
                global.msg[0] += "%%"
        }
        if (global.flag[56] == 0)
        {
            global.msg[0] = "* Hello^1.&* I am a snowman./"
            global.msg[1] = "* I want to see the world..^1.&* But I cannot move./"
            global.msg[2] = "* If you would be so kind^1,&  traveller^1, please.../"
            global.msg[3] = "* Take a piece of me and&  bring it very far away.&         Yes         No      \C "
            global.msg[4] = " "
        }
        break
    case 227:
        if (global.choice == 0)
        {
            scr_itemget(16)
            if (noroom == 1)
                global.msg[0] = "* It seems you do not&  have enough room for me./%%"
            else
            {
                global.msg[0] = "* Thank you... good luck!/"
                global.msg[1] = "* (You got the Snowman&  Piece.)/%%"
                global.flag[56] = 1
            }
        }
        if (global.choice == 1)
            global.msg[0] = "* I see^1.&* Good journey^1, then./%%"
        break
    case 228:
        global.msg[0] = "SANS!!^1!&THAT DIDN'T DO&ANYTHING!/"
        if (global.flag[254] == 0)
            global.msg[0] = "SANS!!^1!&THEY DIDN'T EVEN&LOOK AT IT!/"
        scr_sansface(1, 0)
        global.msg[2] = "* whoops./"
        global.msg[3] = "* i knew i should have&  used today's crossword&  instead./"
        scr_papface(4, 1)
        global.msg[5] = "WHAT!^1?&CROSSWORD!?/"
        global.msg[6] = "I CAN'T BELIEVE&YOU SAID THAT!!/"
        global.msg[7] = "IN MY OPINION.../"
        global.msg[8] = "\E3JUNIOR JUMBLE&IS EASILY THE&HARDEST./"
        scr_sansface(9, 0)
        global.msg[10] = "* what^1? really^1, dude^1?&* that easy-peasy word&  scramble?/"
        global.msg[11] = "\E1* that's for baby bones./"
        scr_papface(12, 3)
        global.msg[13] = "UN^1. BELIEVABLE./"
        global.msg[14] = "\E0HUMAN!!^1!&SOLVE THIS DISPUTE!/"
        global.msg[15] = "\TS \F0 \E0 \T0 %"
        global.msg[16] = "* (Which is harder?)& &         Jumble      Crossword \C "
        global.msg[17] = "   "
        break
    case 229:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.flag[58] = 0
            global.msg[1] = "HA^1! HA^1! YES!/"
            global.msg[2] = "HUMANS MUST BE&VERY INTELLIGENT!/"
            global.msg[3] = "IF THEY ALSO FIND&JUNIOR JUMBLE&SO DIFFICULT!/"
            global.msg[4] = "NYEH^1! HEH^1! HEH HEH!/%%"
        }
        if (global.choice == 1)
        {
            global.flag[58] = 1
            global.msg[1] = "YOU TWO ARE WEIRD!/"
            global.msg[2] = "\E3CROSSWORDS ARE SO&EASY./"
            global.msg[3] = "IT'S THE SAME&SOLUTION EVERY&TIME./"
            global.msg[4] = "\E0I JUST FILL ALL&THE BOXES IN WITH&THE LETTER " + chr(34) + "Z" + chr(34) + ".../"
            global.msg[5] = "BECAUSE EVERY TIME&I LOOK AT A&CROSSWORD.../"
            global.msg[6] = "ALL I CAN DO IS&SNORE!!!/"
            global.msg[7] = "NYEH HEH HEH!!!/%%"
        }
        break
    case 230:
        doak = 0
        noroom = 0
        if (global.flag[60] < 5)
        {
            global.msg[0] = "* Hello^1!&* Would you like some&  Nice Cream?/"
            global.msg[1] = "* It's the frozen treat&  that warms your heart!/"
            global.msg[2] = "* Now just 15G!& &         Yes         No \C"
            global.msg[3] = " "
        }
        if (global.flag[60] >= 5)
        {
            global.msg[0] = " %"
            global.msg[1] = "* Nice Cream^1!&* It's the frozen treat&  that warms your heart!/"
            global.msg[2] = "* Now just 15G!& &         Yes         No \C"
            global.msg[3] = " "
        }
        break
    case 231:
        script_execute(scr_cost, 15)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 17)
                    if (noroom == 0)
                    {
                        global.gold -= 15
                        global.flag[60] += 15
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = "* Here you go^1!&* Have a super-duper day^1!&* (You got the Nice Cream.)/%%"
            if (afford == 0)
            {
                global.msg[0] = "* Huh^1?&* You don't have enough&  money.../"
                global.msg[1] = "* I wish I could make&  Nice Cream for free.../%%"
            }
        }
        if (noroom == 1)
            global.msg[0] = "* It looks like you're&  holding too much stuff^1!&* Oh well!/%%"
        if (global.choice == 1)
        {
            global.msg[0] = "* Well then..^1.&* Tell your friends.../"
            global.msg[1] = "* There's ice cream..^1.&* Out in the middle of&  the woods.../%%"
        }
        break
    case 232:
        doak = 0
        noroom = 0
        scr_itemcheck(26)
        if (global.flag[85] == 0)
        {
            if (global.flag[60] < 5)
            {
                global.msg[0] = "* Hello^1!&* Would you like some&  Nice Cream?/"
                global.msg[1] = "* It's the frozen treat&  that warms your heart!/"
                global.msg[2] = "* Now just 25G!& &         Yes         No \C"
                global.msg[3] = " "
            }
            if (global.flag[60] >= 5)
            {
                global.msg[0] = " %"
                global.msg[1] = "* Nice Cream^1!&* It's the frozen treat&  that warms your heart!/"
                global.msg[2] = "* Now just 25G!& &         Yes         No \C"
                global.msg[3] = " "
            }
        }
        if (global.flag[85] == 1)
        {
            if (global.flag[60] < 5)
            {
                global.msg[0] = "* Hey^1, you have an umbrella^1,&  just like my cart.../"
                global.msg[1] = "* Such solidarity^1!&* I have no choice but to&  give you a deal!/"
                global.msg[2] = "* Discount Ice Cream! 15G!& &         Yes         No \C"
                global.msg[3] = " "
            }
            if (global.flag[60] >= 5)
            {
                global.msg[0] = " %"
                global.msg[1] = "* Umbrella solidarity!^1?&* I guess I have to give&  you a deal.../"
                global.msg[2] = "* Discount Ice Cream! 15G!& &         Yes         No \C"
                global.msg[3] = " "
            }
        }
        if (itemcount > 2)
        {
            global.msg[0] = "* Hey^1!&* You have 3 Punch Cards!/"
            global.msg[1] = "* How about redeeming them&  for some Nice Cream!?/"
            global.msg[2] = "* It's free!& &         Yes         No \C"
            global.msg[3] = " "
        }
        break
    case 233:
        scr_itemcheck(26)
        if (itemcount < 3)
        {
            if (global.flag[85] == 0)
                script_execute(scr_cost, 25)
            if (global.flag[85] == 1)
                script_execute(scr_cost, 15)
            if (global.choice == 0)
            {
                if (afford == 1)
                {
                    if (doak == 0)
                    {
                        doak = 1
                        script_execute(scr_itemget, 17)
                        if (noroom == 0)
                        {
                            if (global.flag[85] == 0)
                            {
                                global.gold -= 25
                                global.flag[60] += 25
                            }
                            else
                            {
                                global.gold -= 15
                                global.flag[60] += 15
                            }
                            global.flag[80] += 1
                        }
                    }
                }
            }
            if (noroom == 0)
            {
                global.msg[0] = "* Super^1! Here you go^1!&* Your Card's in the box!&* (You got the Nice Cream.)/%%"
                if (afford == 0)
                {
                    global.msg[0] = "* Huh^1? No money^1?&* Sorry^1, I can't give it to&  you for free./%%"
                    if (global.flag[85] == 1)
                        global.msg[0] = "* Huh^1? You can't afford it&  even with the discount^1?&* I.../%%"
                }
            }
            if (noroom == 1)
                global.msg[0] = "* It looks like you're&  holding too much stuff^1!&* Oh well!/%%"
        }
        else if (global.choice == 0)
        {
            rem = 0
            g = 0
            for (n = 0; n < 8; n += 1)
            {
                if (g == 1)
                {
                    n -= 1
                    g = 0
                }
                if (global.item[n] == 26 && rem < 3)
                {
                    scr_itemshift(n, 0)
                    rem += 1
                    g = 1
                }
            }
            scr_itemget(17)
            global.msg[0] = "* Here^1! A free Nice Cream^1!&* (You lost 3 cards and got&  a Nice Cream.)/%%"
        }
        if (global.choice == 1)
        {
            global.msg[0] = "* Well then..^1.&* Tell your friends.../"
            global.msg[1] = "* There's ice cream..^1.&* Hidden in the depths of a&  watery cavern.../%%"
        }
        break
    case 235:
        global.msg[0] = "* i've been thinking&  about selling treats&  too./"
        global.msg[1] = "* want some fried snow^1?&* it's just 5G.&  Buy         No\C"
        global.msg[2] = " "
        break
    case 236:
        if (global.choice == 0)
        {
            global.msg[0] = "* did i say 5G^1?&* i meant 50G^1.&  Buy         No\C"
            global.msg[1] = "  "
        }
        if (global.choice == 1)
        {
            global.msg[0] = "* you're right./"
            global.msg[1] = "* i should charge way&  more than than that./%%"
        }
        break
    case 237:
        if (global.choice == 0)
        {
            global.msg[0] = "* really^1?&* how about 5000G?&  Buy         No\C"
            global.msg[1] = "  "
        }
        if (global.choice == 1)
        {
            global.msg[0] = "* you're right./"
            global.msg[1] = "* that's still too low./%%"
        }
        break
    case 238:
        if (global.choice == 0)
        {
            global.msg[0] = "* 50000G^1.&* that's my final offer.&  Buy         No\C"
            global.msg[1] = "  "
        }
        if (global.choice == 1)
            global.msg[0] = "* i have to pay for&  the raw materials&  somehow./%%"
        break
    case 239:
        if (global.choice == 0)
        {
            global.msg[0] = "* what^1?&* you don't have the&  money?/"
            global.msg[1] = "* hey^1, that's okay./"
            global.msg[2] = "\E1* i don't have any snow./%%"
            if (global.gold >= 50000)
            {
                global.msg[0] = "* wow^1, that's a lot&  of cash./"
                global.msg[1] = "\E3* that's why i'm sorry&  to say.../"
                global.msg[2] = "\E3* i can't sell you&  this fried snow./"
                global.msg[3] = "\E2* it's got too much&  sentimental value./%%"
            }
        }
        if (global.choice == 1)
            global.msg[0] = "* don't you know a&  good deal when you&  hear one?/%%"
        break
    case 240:
        global.msg[0] = "WHAT!^1?&HOW DID YOU&AVOID MY TRAP?/"
        global.msg[1] = "\E3AND^1, MORE&IMPORTANTLY.../"
        global.msg[2] = "\E0IS THERE ANY&LEFT FOR ME???/"
        global.msg[3] = "\TS \F0 \E0 \T0 %"
        global.msg[4] = "* (What do you tell Papyrus&  about his spaghetti?)&         Ate it      Left it   \C "
        global.msg[5] = "   "
        break
    case 241:
        scr_papface(0, 0)
        global.msg[1] = "REALLY!?/"
        if (global.choice == 0)
        {
            global.msg[2] = "\E2WOWIE.../"
            global.msg[3] = "NO ONE'S EVER&ENJOYED MY&COOKING BEFORE.../"
            global.msg[4] = "\E0WELL THEN!!/"
            global.flag[62] = 1
        }
        if (global.choice == 1)
        {
            global.msg[2] = "\E2WOWIE.../"
            global.msg[3] = "YOU RESISTED THE&FLAVOR OF MY&HOMECOOKED PASTA.../"
            global.msg[4] = "JUST SO YOU&COULD SHARE&IT WITH ME???/"
            global.flag[62] = 2
        }
        global.msg[5] = "\E0FRET NOT HUMAN^1!&I^1, MASTER CHEF&PAPYRUS.../"
        global.msg[6] = "WILL MAKE YOU ALL&THE PASTA YOU&COULD EVER WANT!/"
        global.msg[7] = "HEH HEH HEH HEH&HEH HEH NYEH!/%%"
        break
    case 243:
        global.msg[0] = "HEY!/"
        global.msg[1] = "IT'S THE HUMAN!/"
        global.msg[2] = "\E0YOU'RE GONNA&LOVE THIS&PUZZLE!/"
        global.msg[3] = "IT WAS MADE&BY THE GREAT&DR. ALPHYS!/"
        global.msg[4] = "YOU SEE&THESE TILES&!?/"
        global.msg[5] = "ONCE I THROW&THIS SWITCH.../"
        global.msg[6] = "THEY WILL&BEGIN TO&CHANGE COLOR!/"
        global.msg[7] = "EACH COLOR HAS&A DIFFERENT&FUNCTION!/"
        global.msg[8] = "RED TILES ARE&IMPASSABLE!/"
        global.msg[9] = "YOU CANNOT&WALK ON THEM!/"
        global.msg[10] = "YELLOW TILES&ARE ELECTRIC!/"
        global.msg[11] = "THEY WILL&ELECTROCUTE&YOU!/"
        global.msg[12] = "GREEN TILES&ARE ALARM&TILES!/"
        global.msg[13] = "IF YOU STEP&ON THEM.../"
        global.msg[14] = "YOU WILL HAVE&TO FIGHT A&MONSTER!!/"
        global.msg[15] = "ORANGE TILES&ARE ORANGE-&SCENTED./"
        global.msg[16] = "THEY WILL MAKE&YOU SMELL&DELICIOUS!/"
        global.msg[17] = "BLUE TILES ARE&WATER TILES./"
        global.msg[18] = "SWIM THROUGH&IF YOU LIKE^1,&BUT.../"
        global.msg[19] = "IF YOU SMELL&LIKE ORANGES!/"
        global.msg[20] = "THE PIRANHAS&WILL BITE&YOU./"
        global.msg[21] = "ALSO^1, IF A&BLUE TILE IS&NEXT TO A,/"
        global.msg[22] = "YELLOW TILE^1,&THE WATER WILL&ALSO ZAP YOU!/"
        global.msg[23] = "PURPLE TILES&ARE SLIPPERY!/"
        global.msg[24] = "YOU WILL SLIDE&TO THE NEXT&TILE!/"
        global.msg[25] = "HOWEVER^1, THE&SLIPPERY&SOAP.../"
        global.msg[26] = "SMELLS LIKE&LEMONS!!/"
        global.msg[27] = "WHICH PIRANHAS&DO NOT LIKE!/"
        global.msg[28] = "PURPLE AND&BLUE ARE OK!/"
        global.msg[29] = "FINALLY^1,&PINK TILES./"
        global.msg[30] = "THEY DON'T DO&ANYTHING./"
        global.msg[31] = "STEP ON THEM&ALL YOU LIKE./"
        global.msg[32] = "HOW WAS THAT!^1?&UNDERSTAND???/"
        global.msg[33] = "\TS \F0 \E0 \T0 %"
        global.msg[34] = "* (Understand the explanation?)& &         Of course   No        \C "
        global.msg[35] = "   "
        break
    case 244:
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = "GREAT!^1!&THEN THERE'S ONE&LAST THING.../"
            global.msg[2] = "THIS PUZZLE.../"
            global.msg[3] = "IS ENTIRELY RANDOM&!!!!!!/"
            global.msg[4] = "WHEN I PULL THIS&SWITCH^1, IT WILL&MAKE A PUZZLE.../"
            global.msg[5] = "THAT HAS NEVER&BEEN SEEN BEFORE!/"
            global.msg[6] = "NOT EVEN I WILL&KNOW THE SOLUTION!/"
            global.msg[7] = "NYEH HEH HEH^1!&GET READY...!/%%"
            obj_papyrus4.conversation = 50
        }
        if (global.choice == 1)
        {
            scr_papface(0, 3)
            global.msg[1] = "OKAY..^1.&I GUESS I'LL&REPEAT MYSELF.../"
            global.msg[2] = "\E0RED TILES ARE&IMPASSABLE./"
            global.msg[3] = "YELLOW TILES ARE&ELECTRIC AND&DANGEROUS./"
            global.msg[4] = "BLUE TILES MAKE&YOU FIGHT A&MONSTER./"
            global.msg[5] = "GREEN TILES ARE&WATER TILES./"
            global.msg[6] = "ORANGE TILES ARE&ORANGE SCENTED./"
            global.msg[7] = "IF YOU STEP ON&ORANGE^1, DON'T STEP&ON GREEN./"
            global.msg[8] = "\E3BROWN TILES ARE.../"
            global.msg[9] = "\E1WAIT!!^1!&THERE ARE NO&BROWN TILES.../"
            global.msg[10] = "\E0PURPLE TILES&SMELL LIKE&LEMONS.../"
            global.msg[11] = "\E3WHY DON'T THE&YELLOW ONES&SMELL LEMONY?/"
            global.msg[12] = "\E3UMM.../"
            global.msg[13] = "\E1WAIT!^1!&DID I MIX UP&GREEN AND BLUE!?/"
            global.msg[14] = "THE BLUE ONES&ARE WATER ONES!/"
            global.msg[15] = "\E3PINK TILES.../"
            global.msg[16] = "I DON'T..^1.&REMEMBER???/"
            global.msg[17] = "\E1WAIT!!!/"
            global.msg[18] = "\E3THOSE ONES DON'T&DO ANYTHING./"
            global.msg[19] = "\E0OKAY^1!&DO YOU UNDERSTAND&BETTER NOW!?/"
            global.msg[20] = "\TS \F0 \E0 \T0 %"
            global.msg[21] = "* (Understand the explanation?)& &         Yes         Even less \C "
            global.msg[22] = "   "
        }
        break
    case 245:
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = "GREAT!^1!&THEN THERE'S ONE&LAST THING.../"
            global.msg[2] = "THIS PUZZLE.../"
            global.msg[3] = "IS ENTIRELY RANDOM&!!!!!!/"
            global.msg[4] = "WHEN I PULL THIS&SWITCH^1, IT WILL&MAKE A PUZZLE.../"
            global.msg[5] = "THAT HAS NEVER&BEEN SEEN BEFORE!/"
            global.msg[6] = "NOT EVEN I WILL&KNOW THE SOLUTION!/"
            global.msg[7] = "NYEH HEH HEH^1!&GET READY...!/%%"
            obj_papyrus4.conversation = 50
        }
        if (global.choice == 1)
        {
            scr_papface(0, 3)
            global.msg[1] = "........../"
            global.msg[2] = "OK^1, YOU KNOW&WHAT???/"
            global.msg[3] = "HOW ABOUT..^1.&YOU JUST.../"
            global.msg[4] = "DO THIS PUZZLE..^1.&ON YOUR OWN.../"
            global.msg[5] = "I'LL LEAVE THE&INSTRUCTIONS.../"
            global.msg[6] = "JUST READ THEM./"
            global.msg[7] = "THEN WHEN YOU&UNDERSTAND IT.../"
            global.msg[8] = "YOU CAN THROW&THE SWITCH./"
            global.msg[9] = "AND DO IT AT&YOUR OWN PACE./"
            global.msg[10] = "GOOD LUCK./"
            global.msg[11] = "NYEH..^1.&HEH..^1.&HEH!/%%"
            obj_papyrus4.conversation = 80
        }
        break
    case 246:
        if (global.flag[104] == 0)
        {
            global.msg[0] = "* (There's a lone quiche&  sitting underneath&  this bench.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* It's just a bench./%%"
        break
    case 247:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 23)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Abandoned&  Quiche.)/%%"
                global.flag[104] = 1
            }
            if (noroom == 1)
            {
                global.msg[0] = "* (You're carrying too much.)/"
                global.msg[1] = "* (You aren't ready for the&  responsibility.)/%%"
            }
        }
        if (global.choice == 1)
            global.msg[0] = "* (The quiche was left all&  alone...)/%%"
        break
    case 248:
        if (global.flag[105] == 0)
        {
            global.msg[0] = "* (There's a tutu lying on&  the ground here.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* Nothing's here!!!/%%"
        break
    case 249:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 24)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Old Tutu.)/%%"
                global.flag[105] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 250:
        if (global.flag[80] > 0)
        {
            card = string(global.flag[80])
            global.msg[0] = (("* (The box contains " + card) + " cards.)/")
            if (global.flag[80] > 2)
                global.msg[0] = (("* (Two bugs in the box are&  playing a " + card) + "-card game.)/")
            if (global.flag[80] > 4)
                global.msg[0] = (("* (There's a smiley face made&  of " + card) + " cards in the box.)/")
            if (global.flag[80] > 6)
                global.msg[0] = (("* (A house made of " + card) + " cards&  sits in the box.)/")
            if (global.flag[80] > 12)
                global.msg[0] = "* (The box is overstuffed with&  cards.)/"
            global.msg[1] = "* (Take a card?)& &         Take        Leave \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* (It's a box for storing Punch&  Cards^1.)&* (It's empty right now.)/%%"
        break
    case 251:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 26)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got a Punch Card.)/%%"
                if (global.flag[80] > 2)
                    global.msg[0] = "* (All games must end one day.^1)&* (You got a Punch Card.)/%%"
                if (global.flag[80] > 4)
                    global.msg[0] = "* (Happiness is fleeting.^1)&* (You got a Punch Card.)/%%"
                if (global.flag[80] > 6)
                    global.msg[0] = "* (The house collapses.^1)&* (You got a Punch Card.)/%%"
                if (global.flag[80] > 12)
                    global.msg[0] = "* (You got a Punch Card.)/%%"
                global.flag[80] -= 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 252:
        if (global.flag[106] == 0)
        {
            global.msg[0] = "* (It's a pair of ballet shoes.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* Nothing's here!!!/%%"
        break
    case 253:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 25)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Ballet Shoes.)/%%"
                global.flag[106] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 254:
        global.msg[0] = "* (This little bird wants to&  carry you across.)/"
        global.msg[1] = "* (Accept the bird's offer?)& &         Get ride    No        \C "
        global.msg[2] = "  "
        if (global.flag[85] == 1)
            global.msg[0] = "* (Umbrellas and birds...^1)&* (A bad combination.)/%%"
        break
    case 255:
        global.msg[0] = " %%"
        if (global.choice == 0 && global.flag[85] == 0)
        {
            global.interact = 1
            if instance_exists(obj_carrybird)
            {
                obj_carrybird.carry = 1
                with (obj_mainchara)
                    uncan = 1
            }
        }
        break
    case 256:
        if (global.flag[107] != 1)
        {
            global.msg[0] = "* (It's a legendary artifact.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* (The artifact is gone.)/%%"
        break
    case 257:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 0)
                for (i = 0; i < 8; i += 1)
                {
                    if (global.item[i] == 27)
                        noroom = 2
                }
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (This will never happen.)/%%"
                global.flag[107] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
            if (noroom == 2)
                global.msg[0] = "* (You're carrying too many&  dogs.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 258:
        if (global.flag[109] == 0)
        {
            global.msg[0] = "* (The fridge is filled with&  instant noodles and soda.)/"
            global.msg[1] = "* (Take a package of noodles?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* (The fridge is filled with&  instant noodles and soda.)/%%"
        break
    case 259:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 36)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Instant Noodles.)/%%"
                global.flag[109] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = "* (You decide to stay healthy.)/%%"
        break
    case 260:
        if (global.flag[110] == 0)
        {
            global.msg[0] = "* (There's a frypan lying on&  the ground.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* Nothing's here!!!/%%"
        break
    case 261:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 47)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Burnt Pan.)/%%"
                global.flag[110] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 262:
        if (global.flag[111] == 0)
        {
            global.msg[0] = "* (There's an apron lying on&  the ground.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* Nothing's here!!!/%%"
        break
    case 263:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 46)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Stained Apron.)/%%"
                global.flag[111] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 264:
        if (global.flag[112] == 0)
        {
            global.msg[0] = "* (There's a Glamburger inside&  the trash can.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* (It's an empty trashcan.)/%%"
        break
    case 265:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                script_execute(scr_itemget, 40)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Glamburger.)/%%"
                global.flag[112] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 266:
        if (global.flag[113] == 0)
        {
            global.msg[0] = "* (There's 100G inside&  the trash can.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* (It's an empty trashcan.)/%%"
        break
    case 267:
        if (global.choice == 0)
        {
            global.flag[113] = 1
            if (doak == 0)
                global.gold += 100
            doak = 1
            global.msg[0] = "* (You got 100G.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 268:
        if (global.flag[114] == 0)
        {
            if (scr_murderlv() < 16)
                global.msg[0] = "* (There's a worn dagger&  inside the box.)/"
            else
                global.msg[0] = "* (Knife inside the box.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* Nothing's here!!!/%%"
        break
    case 269:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                if (scr_murderlv() >= 16)
                    scr_itemget(52)
                else
                    script_execute(scr_itemget, 51)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Worn Dagger.)/%%"
                if (scr_murderlv() >= 16)
                    global.msg[0] = "* (You got the Real Knife.)/%%"
                global.flag[114] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 270:
        if (global.flag[115] == 0)
        {
            global.msg[0] = "* (There's a heart-shaped&  locket inside the box.)/"
            global.msg[1] = "* (Will you take it?)& &         Take it     Leave it  \C "
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* Nothing's here!!!/%%"
        break
    case 271:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = -1
                doak = 1
                if (scr_murderlv() < 16)
                    script_execute(scr_itemget, 50)
                else
                    script_execute(scr_itemget, 53)
            }
            if (noroom == 0)
            {
                global.msg[0] = "* (You got the Heart-shaped&  Locket.)/%%"
                if (scr_murderlv() >= 16)
                    global.msg[0] = "* (You got The Locket.)/%%"
                global.flag[115] = 1
            }
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 272:
        global.msg[0] = "* (The box is empty.)/%%"
        break
    case 273:
        doak = 0
        noroom = 0
        if (global.flag[250] == 0)
        {
            global.msg[0] = "* Hello...&* Would you like some&  Nice Cream...?/"
            global.msg[1] = "* It's the frozen treat...&* That warms your heart./"
            global.msg[2] = "* Now only 12G!& &         Yes         No \C"
            global.msg[3] = " "
        }
        if (global.flag[250] >= 1)
        {
            global.msg[0] = " %"
            global.msg[1] = "* Nice Cream^1.&* It's the frozen treat^1.&* That warms your heart./"
            global.msg[2] = "* Now just 12G.& &         Yes         No \C"
            global.msg[3] = " "
        }
        break
    case 274:
        script_execute(scr_cost, 12)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 17)
                    if (noroom == 0)
                    {
                        global.gold -= 12
                        global.flag[60] += 12
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = "* Here^1.&* (You got the Nice Cream.)/%%"
            if (afford == 0)
                global.msg[0] = "* You don't have enough&  money.../%%"
        }
        if (noroom == 1)
            global.msg[0] = "* Drop something./%%"
        if (global.choice == 1)
        {
            global.msg[0] = "* Well then..^1.&* Tell your friends.../"
            global.msg[1] = "* Life..^1. is suffering./%%"
        }
        break
    case 500:
        global.msg[0] = "* (Golden flowers.^1)&* (They must have&  broken your fall.)/%%"
        global.msg[1] = "%%%"
        break
    case 501:
        global.msg[0] = "* " + chr(34) + "Press [Z] to read signs!" + chr(34) + "/%%"
        break
    case 502:
        global.msg[0] = "* Just a regular old pillar./%%"
        break
    case 503:
        global.msg[0] = "* Please don't step on the&  leaves./%%"
        break
    case 504:
        global.msg[0] = "* Didn't you read the sign&  downstairs?/%%"
        break
    case 505:
        conversation = obj_goofyrock.conversation
        if (global.flag[33] == 0)
        {
            if (conversation == 0)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = "* WHOA there^1, pardner^2!&* Who said you could push&  me around?/"
                global.msg[1] = "* HMM^2?&* So you're ASKIN' me to&  move over?/"
                global.msg[2] = "* Okay^1, just for you^1,&  pumpkin./%%"
            }
            if (conversation == 3)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = "* HMM^2?&* You want me to move some&  more?/"
                global.msg[1] = "* Alrighty^1, how's this?/%%"
            }
            if (conversation == 6)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = "* HMM^2?&* That was the wrong direction?/"
                global.msg[1] = "* Okay^1, think I got it./%%"
            }
            if (conversation == 9)
                global.msg[0] = "* Was that helpful?/%%"
            if (conversation == 12)
            {
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = "* HMM^2?&* You wanted me to STAY there?/"
                global.msg[1] = "* You're giving me a real&  workout./%%"
            }
        }
        else
        {
            global.msg[0] = "* Aren't things easier when&  you just ask?/%%"
            if (global.flag[7] == 1)
            {
                global.msg[0] = "* The exit's open^1?&* Guess I better roll out.../"
                global.msg[1] = "* Hey^1, y'mind giving me a&  push^1, pumpkin?/%%"
            }
        }
        break
    case 508:
        doak = 0
        noroom = 0
        if (global.flag[34] < 4)
        {
            global.msg[0] = "*'Take one.'&* Take a candy?&         Yes         No \C"
            if (global.flag[34] == 0)
                global.msg[0] = "* It says 'take one.'&* Take a piece of candy?&         Yes         No \C"
        }
        else
            global.msg[0] = "* Look at what you've done./%%"
        global.msg[1] = " "
        break
    case 509:
        if (global.choice == 0)
        {
            if (global.flag[34] < 4)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 1)
                    if (noroom == 0)
                        global.flag[34] += 1
                }
            }
            if (noroom == 0)
            {
                if (global.flag[34] == 1)
                    global.msg[0] = "* You took a piece of candy.&* (Press [C] to open the menu.)/%%"
                if (global.flag[34] == 2)
                    global.msg[0] = "* You took more candy^1.&* How disgusting../%%"
                if (global.flag[34] == 3)
                    global.msg[0] = "* You take another piece.&* You feel like the&  scum of the earth.../%%"
                if (global.flag[34] == 4)
                    global.msg[0] = "* You took too much too fast.&* The candy spills onto&  the floor./%%"
                if (global.flag[34] == 3 && global.flag[6] == 1)
                {
                    global.msg[0] = "* In this hellish world^1, you&  can only take 3 pieces&  of candy.../%%"
                    global.flag[34] += 1
                }
            }
            if (noroom == 1)
                global.msg[0] = "* You tried to take a piece&  of candy^1, but you didn't&  have any room./%%"
        }
        if (global.choice == 1)
            global.msg[0] = "* You decided not to take some./%%"
        break
    case 510:
        global.msg[0] = "* zzzzzzzzzzzzzzz..^1.&* zzzzzzzzzzzzzz.../"
        global.msg[1] = "* zzzzzzzzzz..^1.&* (are they gone yet^1)&* zzzzzzzzzzzzzzz.../"
        global.msg[2] = "* (This ghost keeps saying 'z'&  out loud repeatedly^1,&  pretending to sleep.)/"
        global.msg[3] = "* Move it with force?& &         Yes         No \C"
        global.msg[4] = " "
        break
    case 511:
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
        }
        global.msg[0] = "%%"
        break
    case 512:
        doak = 0
        noroom = 0
        global.msg[0] = "* It says 'Take them all.'&* Take a candy?&         Yes         No \C"
        global.msg[1] = " "
        break
    case 513:
        script_execute(scr_itemcheck, 5)
        if (global.choice == 0)
        {
            if (haveit == 0)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 5)
                }
            }
        }
        if (noroom == 0)
            global.msg[0] = "* You took a piece.&* Boy^1, that's heavy./%%"
        if (haveit == 1)
            global.msg[0] = "* You can't carry more.&* It's just too heavy./%%"
        if (noroom == 1)
            global.msg[0] = "* You tried to take a piece&  of candy^1, but you didn't&  have any room./%%"
        if (global.choice == 1)
            global.msg[0] = "* You decided not to take some./%%"
        break
    case 514:
        doak = 0
        noroom = 0
        global.msg[0] = "* Leave 7G in the web?& &         Yes         No \C"
        global.msg[1] = " "
        break
    case 515:
        script_execute(scr_cost, 7)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 7)
                    if (noroom == 0)
                    {
                        global.gold -= 7
                        global.flag[59] += 7
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = "* Some spiders crawled down&  and gave you a donut./%%"
            if (afford == 0)
                global.msg[0] = "* You didn't have enough&  gold./%%"
        }
        if (noroom == 1)
            global.msg[0] = "* You are carrying too&  many items./%%"
        if (global.choice == 1)
            global.msg[0] = "*%%"
        break
    case 516:
        doak = 0
        noroom = 0
        global.msg[0] = "* Leave 18G in the web?& &         Yes         No \C"
        global.msg[1] = " "
        break
    case 517:
        script_execute(scr_cost, 18)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 10)
                    if (noroom == 0)
                    {
                        global.gold -= 18
                        global.flag[59] += 18
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = "* Some spiders crawled down&  and gave you a jug./%%"
            if (afford == 0)
                global.msg[0] = "* You didn't have enough&  gold./%%"
        }
        if (noroom == 1)
            global.msg[0] = "* You are carrying too&  many items./%%"
        if (global.choice == 1)
            global.msg[0] = "*%%"
        break
    case 518:
        if (doak == 0)
        {
            script_execute(scr_itemget, 12)
            if (noroom == 0)
                global.flag[100] = 1
            doak = 1
        }
        global.msg[0] = "* You found a Faded Ribbon./%%"
        if (noroom == 1)
            global.msg[0] = "* You are carrying too&  much./%%"
        break
    case 519:
        doak = 0
        noroom = 0
        global.msg[0] = "* It's a switch.&* Press it?&         Yes         No \C"
        global.msg[1] = " "
        break
    case 520:
        if (doak == 0)
        {
            global.flag[43] += 1
            doak = 1
        }
        if (global.choice == 0)
            global.msg[0] = "* Nothing happened./%%"
        if (global.choice == 1)
            global.msg[0] = "%%"
        if (global.flag[43] > 25)
            global.msg[0] = "* You're making the switches&  uncomfortable with all&  this attention./%%"
        break
    case 521:
        doak = 0
        noroom = 0
        global.msg[0] = "* It's a switch.&* Press it?&         Yes         No \C"
        global.msg[1] = " "
        break
    case 522:
        if (doak == 0)
        {
            global.flag[43] += 1
            doak = 1
        }
        if (global.choice == 0)
        {
            global.msg[0] = "* You hear a clicking sound./%%"
            if (room == room_ruins15B && global.plot < 14)
            {
                global.plot = 14
                snd_play(snd_switchpull_n)
            }
            if (room == room_ruins15C && global.plot < 15)
            {
                global.plot = 15
                snd_play(snd_switchpull_n)
            }
            if (room == room_ruins15D && global.plot < 16)
            {
                global.plot = 16
                snd_play(snd_switchpull_n)
            }
        }
        if (global.choice == 1)
            global.msg[0] = "%%"
        break
    case 523:
        if (doak == 0)
        {
            script_execute(scr_itemget, 13)
            if (noroom == 0)
                global.flag[102] = 1
            doak = 1
        }
        global.msg[0] = "* You found the Toy Knife./%%"
        if (noroom == 1)
            global.msg[0] = "* You are carrying too&  much./%%"
        break
    case 524:
        doak = 0
        noroom = 0
        global.msg[0] = "* It's TORIEL's diary.&* Read the circled passage?&         Yes         No \C"
        global.msg[1] = " "
        break
    case 525:
        if (global.choice == 0)
        {
            global.msg[0] = "* You read the passage.../"
            global.msg[1] = "* " + chr(34) + "Why did the skeleton want&  a friend?" + chr(34) + "/"
            global.msg[2] = "* " + chr(34) + "Because she was feeling&  BONELY..." + chr(34) + "/"
            global.msg[3] = "* The rest of the page is&  filled with jokes of&  a similar caliber./%%"
        }
        if (global.choice == 1)
            global.msg[0] = "%%"
        break
    case 526:
        if (global.flag[6] == 0)
        {
            if (doak == 0)
            {
                script_execute(scr_itemget, 11)
                if (noroom == 0)
                    global.flag[103] = 2
                doak = 1
            }
            global.msg[0] = "* You found a slice of&  butterscotch-cinnamon&  pie./%%"
            if (noroom == 1)
                global.msg[0] = "* You are carrying too&  much./%%"
        }
        else
        {
            if (doak == 0)
            {
                script_execute(scr_itemget, 63)
                if (noroom == 0)
                    global.flag[103] = 2
                doak = 1
            }
            global.msg[0] = "* You found a slice of&  snail pie.../%%"
            if (noroom == 1)
                global.msg[0] = "* You are carrying too&  much./%%"
        }
        break
    case 527:
        global.msg[0] = "* Hello there^1,&  little one!/"
        global.msg[1] = "* The pie has not&  cooled down yet./"
        global.msg[2] = "* Perhaps you should&  take a nap.&  Yes         No      \C "
        global.msg[3] = " "
        break
    case 528:
        global.plot = 19.1
        if (global.choice == 0)
            global.msg[0] = "* Sweet dreams./%%"
        else
        {
            global.msg[0] = "\E1* You'd rather stay&  up and chat with&  me^1, then?/"
            if (global.flag[103] > 0)
                global.msg[0] = "* Up already^1, I see?/"
            global.msg[1] = "\E0* Um^1, I want you to know&  how glad I am to&  have someone here./"
            global.msg[2] = "* There are so many&  old books I want&  to share./"
            global.msg[3] = "* I want to show you&  my favorite bug-&  hunting spot./"
            global.msg[4] = "* I've also prepared&  a curriculum for&  your education./"
            global.msg[5] = "* This may come as&  a surprise to you.../"
            global.msg[6] = "* But I have always&  wanted to be a&  teacher./"
            global.msg[7] = "\E1* ... actually^1, perhaps&  that isn't very&  surprising./"
            global.msg[8] = "\E5* STILL./"
            global.msg[9] = "\E0* I am glad to have&  you living here./"
            global.msg[10] = "\E1* Oh^1, did you&  want something?/"
            global.msg[11] = "* What is it?&               When can&  Nothing      I go home?\C "
            global.msg[12] = " "
        }
        break
    case 529:
        global.plot = 19.2
        if (global.choice == 0)
            global.msg[0] = "* Well^1, talk to me&  again if you&  need anything./%%"
        else
        {
            global.msg[0] = "\E1* What^1?&* This..^1. this IS your&  home now./"
            global.msg[1] = "* Um..^1. would you like&  to hear about this&  book I am reading?/"
            if (global.choice == -1)
            {
                global.msg[0] = "\E0* Oh^1, hello!/"
                global.msg[1] = "* Did you want to&  hear about the&  book I am reading?/"
            }
            global.msg[2] = "\E0* It is called&  " + chr(34) + "72 Uses for Snails." + chr(34) + "/"
            global.msg[3] = "* How about it?&              How to exit&  Sure        the RUINS\C "
            global.msg[4] = " "
        }
        break
    case 530:
        global.plot = 19.3
        global.msg[0] = "* Here is an exciting&  snail fact./"
        if (global.choice == 1)
            global.msg[0] = "\E1* Um^1.^1.^1.&*\E0 How about an exciting&  snail fact?/"
        global.msg[1] = "\E2* Did you know&  that snails.../"
        r = round(random(3))
        if (r == 0)
            global.msg[2] = "\E0* Have a chainsaw-like&  tongue called a&  radula?/"
        if (r == 1)
            global.msg[2] = "\E0* Sometimes flip their&  digestive systems&  as they mature?/"
        if (r == 2)
            global.msg[2] = "\E0* Make terrible&  shoelaces?/"
        if (r == 3)
            global.msg[2] = "\E0* Talk^2. Really^2. Slowly^2?&* Just kidding^1, snails&  don't talk./"
        global.msg[3] = "\E0* Interesting.&              How to exit&  Yeah        the RUINS\C "
        global.msg[4] = " "
        break
    case 531:
        global.plot = 19.4
        if (global.choice == 0)
            global.msg[0] = "* Well^1, bother me&  if you need anything&  else./%%"
        else
        {
            if (global.choice == 1)
            {
                global.msg[0] = "\E1* ... I have to do&  something^1.&* Stay here./%%"
                global.plot = 19.9
                with (obj_mainchara)
                    uncan = 1
            }
            if (global.choice == -1)
                global.msg[0] = "\E0* What is it?&              How to exit&  Nothing     the RUINS\C "
        }
        global.msg[1] = " "
        break
    case 532:
        if (global.choice == 0)
            global.msg[0] = "* If you need anything^1,&  just ask./%%"
        if (global.choice == 1)
        {
            global.msg[0] = "\E1* ... I have to do&  something.&* Stay here./%%."
            global.plot = 19.9
            with (obj_mainchara)
                uncan = 1
        }
        global.msg[1] = " "
        break
    case 540:
        global.msg[0] = "\E2NYOO HOO HOO.../"
        global.msg[1] = "I CAN'T EVEN&STOP SOMEONE AS&WEAK AS YOU.../"
        global.msg[2] = "UNDYNE'S GOING TO&BE DISAPPOINTED&IN ME./"
        global.msg[3] = "I'LL NEVER JOIN THE&ROYAL GUARD..^1.&AND.../"
        global.msg[4] = "MY FRIEND QUANTITY&WILL REMAIN&STAGNANT!/"
        global.msg[5] = "\TS \F0 \T0 %"
        global.msg[6] = "* (What should you say?)&         Let's be    What a&         friends     loser\C"
        global.msg[7] = " "
        if (scr_murderlv() >= 7)
        {
            if instance_exists(obj_papyrus8)
            {
                if (obj_papyrus8.murder == 1)
                {
                    global.msg[0] = "\E0WOWIE!^1!&YOU DID IT!!!/"
                    global.msg[1] = "YOU DIDN'T DO A&VIOLENCE!!!/"
                    global.msg[2] = "\E5TO BE HONEST^1,&I WAS A LITTLE&AFRAID.../"
                    global.msg[3] = "\E0BUT YOU'RE ALREADY&BECOMING A GREAT&PERSON!/"
                    global.msg[4] = "\E7I'M SO PROUD I&COULD CRY!!!/"
                    global.msg[5] = "\E3... WAIT^1, WASN'T I&SUPPOSED TO&CAPTURE YOU...?/"
                    global.msg[6] = "\E0WELL^1, FORGET IT!/"
                    global.msg[7] = "I JUST WANT YOU&TO BE THE BEST&PERSON YOU CAN BE./"
                    global.msg[8] = "SO LET'S LET&BYBONES BE&BYBONES./"
                    global.msg[9] = "I'LL EVEN TELL YOU&HOW TO LEAVE THE&UNDERGROUND!/"
                    global.msg[10] = "JUST KEEP GOING&EAST!/"
                    global.msg[11] = "EVENTUALLY YOU'LL&REACH THE KING'S&CASTLE./"
                    global.msg[12] = "THEN YOU CAN&LEAVE!/%%"
                }
            }
        }
        break
    case 541:
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = "REALLY!^1?&YOU WANT TO BE&FRIENDS^1, WITH ME???/"
            global.msg[2] = "WELL THEN.../"
            global.msg[3] = "\E0I GUESS..^1./"
            global.msg[4] = "I GUESS&I CAN MAKE AN&ALLOWANCE FOR YOU!/"
            global.msg[5] = " %"
        }
        if (global.choice == 1)
        {
            scr_papface(0, 3)
            global.msg[1] = "HUH^1?&WHY WOULD YOU.../"
            global.msg[2] = "BERATE YOURSELF&SO LOUDLY???/"
            global.msg[3] = "IS IT BECAUSE.../"
            global.msg[4] = "\E3YOU DON'T THINK&YOU'RE GOOD ENOUGH&TO BE MY FRIEND?/"
            global.msg[5] = "\E0NO!!^1!&YOU'RE GREAT!!^1!&I'LL BE YOUR FRIEND!/"
        }
        if (global.flag[66] == 1)
        {
            global.msg[6] = "WOWIE!^1!&WE HAVEN'T EVEN HAD&OUR FIRST DATE.../"
            global.msg[7] = "AND I'VE ALREADY&MANAGED TO HIT&THE FRIEND ZONE!!!/"
            global.msg[8] = "WHO KNEW THAT&ALL I NEEDED TO&MAKE PALS.../"
        }
        else
        {
            global.msg[6] = "WOW!!!/"
            global.msg[7] = "I HAVE FRIENDS!!!/"
            global.msg[8] = "AND WHO KNEW THAT&ALL I NEEDED TO&MAKE THEM.../"
        }
        global.msg[9] = "WAS TO GIVE PEOPLE&AWFUL PUZZLES AND&THEN FIGHT THEM??/"
        global.msg[10] = "YOU TAUGHT ME A&LOT^1, HUMAN./"
        global.msg[11] = "I HEREBY GRANT&YOU PERMISSION&TO PASS THROUGH!/"
        global.msg[12] = "AND I'LL GIVE&YOU DIRECTIONS&TO THE SURFACE./"
        global.msg[13] = "CONTINUE FORWARD&UNTIL YOU REACH THE&END OF THE CAVERN./"
        global.msg[14] = "\WTHEN..^1. WHEN YOU&REACH THE CAPITAL^1,&CROSS \YTHE BARRIER\W./"
        global.msg[15] = "THAT'S THE MAGICAL&SEAL TRAPPING US&ALL UNDERGROUND./"
        global.msg[16] = "ANYTHING CAN ENTER&THROUGH IT^1, BUT&NOTHING CAN EXIT.../"
        global.msg[17] = "... EXCEPT SOMEONE&WITH A POWERFUL&SOUL./"
        global.msg[18] = "... LIKE YOU!!!/"
        global.msg[19] = "THAT'S WHY THE&KING WANTS TO&ACQUIRE A HUMAN./"
        global.msg[20] = "HE WANTS TO OPEN&THE BARRIER WITH&SOUL POWER./"
        global.msg[21] = "THEN US MONSTERS&CAN RETURN TO&THE SURFACE!/%%"
        break
    case 544:
        global.msg[0] = "YOU'RE BACK AGAIN?!?!/"
        global.msg[1] = "I FINALLY REALIZE&THE TRUE REASON WHY./"
        global.msg[2] = "YOU.../"
        global.msg[3] = "JUST MISS SEEING MY&FACE SO MUCH.../"
        global.msg[4] = "I'M NOT SURE I&CAN FIGHT SOMEONE&WHO FEELS THIS WAY./"
        global.msg[5] = "BUT MOSTLY..^1. I'M GETTING&REALLY TIRED OF&CAPTURING YOU!/"
        global.msg[6] = "SO..^1.&WHAT DO YOU SAY?/"
        global.msg[6] = "\TS \F0 \T0 %"
        global.msg[7] = "* (Fight Papyrus?)& &         Yes         No\C"
        global.msg[8] = " "
        break
    case 545:
        global.msg[0] = "\TP %"
        if (global.choice == 0)
        {
            global.msg[1] = "OKAY.../"
            global.msg[2] = "I GUESS./"
            global.msg[3] = "IF YOU WANT ME TO&CAPTURE YOU./"
            global.msg[4] = "I'LL TRY AGAIN!!!/%%"
            obj_papyrus8.conversation = 14
        }
        if (global.choice == 1)
        {
            global.msg[1] = "... OKAY.../"
            global.msg[2] = "I GUESS I'LL ACCEPT&MY FAILURE.../%%"
            obj_papyrus8.conversation = 17
            obj_fogmaker.s = 1
            global.flag[67] = 0
            global.flag[68] = 1
        }
        break
    case 547:
        global.msg[0] = "* Welcome to Snowed Inn^1!&* Snowdin's premier hotel!/"
        global.msg[1] = "* One night is 80G.& &         Stay        Leave\C"
        if (global.flag[72] == 2)
        {
            global.msg[0] = "* Back again^1?&* Well^1, stay as long as you&  like./"
            global.msg[1] = "* How about it?& &         Stay        Leave\C"
        }
        global.msg[2] = " "
        if (obj_townnpc_innlady.jtext == 1)
            global.msg[0] = "* What^1?&* No^1, you can't get a&  second key!"
        if (global.flag[7] == 1)
        {
            global.msg[0] = "* Hello^1!&* Sorry^1, no time for a&  nap.../"
            global.msg[1] = "* Snowed Inn is shutting&  down so we can all go&  to the surface./%%"
            if (global.flag[72] == 2)
            {
                global.msg[0] = "* Oh^1, there you are^1.&* I was worrying about&  you!/"
                global.msg[1] = "* Things are going to be OK^1,&  you hear?/"
                global.msg[2] = "* We're all going to the&  surface world soon.../"
                global.msg[3] = "* There's bound to be a&  place you can stay there!/%%"
            }
        }
        break
    case 548:
        if (global.choice == 0)
        {
            global.msg[0] = "Error./%%"
            if (global.flag[72] == 2)
            {
                global.msg[0] = "* Here's your room key^1.&* Make sure to bundle up!/%%"
                obj_townnpc_innlady.conversation = 2
                with (obj_townnpc_innlady)
                    jtext = 1
            }
            if (global.gold < 80 && global.flag[72] == 0)
            {
                global.msg[0] = "* ... You don't even have 80G?/"
                global.msg[1] = "* Oh^1! You poor thing^1.&* I can only imagine what&  you've been through./"
                global.msg[2] = "* One of the rooms upstairs&  is empty^1./"
                global.msg[3] = "* You can sleep there for&  free^1, okay?/%%"
                obj_townnpc_innlady.conversation = 2
                with (obj_townnpc_innlady)
                    jtext = 1
                global.flag[72] = 2
            }
            if (global.gold >= 80)
            {
                if (global.flag[72] == 0 || global.flag[72] == 1)
                {
                    global.msg[0] = "* Here's your room key^1.&* Make sure to bundle up!/%%"
                    obj_townnpc_innlady.conversation = 2
                    with (obj_townnpc_innlady)
                        jtext = 1
                    global.flag[72] = 1
                }
            }
            if (global.gold < 80 && global.flag[72] == 1)
                global.msg[0] = "* You aren't carrying enough&  money./%%"
        }
        else
            global.msg[0] = "* Well^1, feel free to come&  back any time./%%"
        break
    case 549:
        global.msg[0] = "* (Look through the telescope?)& &         Yes         No\C"
        global.msg[1] = " "
        break
    case 550:
        global.msg[0] = " %%"
        global.msg[1] = " "
        if (global.choice == 0)
        {
            if (instance_exists(obj_starchecker) == 0)
                instance_create(view_xview[0], view_yview[0], obj_starchecker)
        }
        break
    case 551:
        if (obj_mainchara.dsprite != spr_maincharad_pranked)
        {
            global.msg[0] = "* i'm thinking about&  getting into the&  telescope business./"
            global.msg[1] = "* it's normally 50000G&  to use this premium&  telescope.../"
            global.msg[2] = "* but..^1.&*\E1 since i know you^1,&  you can use it for free./"
            global.msg[3] = "\E2* howzabout it?/"
            global.msg[4] = "\TS \F0 \T0 %"
            global.msg[5] = "* (Use the telescope?)& &         Yes         No\C"
        }
        else
        {
            global.msg[0] = "* huh^1?&* you aren't satisfied?/"
            global.msg[1] = "\E1* don't worry./"
            global.msg[2] = "\E2* i'll give you a&  full refund./%%"
        }
        global.msg[6] = " "
        break
    case 552:
        global.msg[0] = " %%"
        global.msg[1] = " "
        if (global.choice == 0)
        {
            if (instance_exists(obj_starchecker) == 0)
                instance_create(view_xview[0], view_yview[0], obj_starchecker)
            obj_mainchara.dsprite = spr_maincharad_pranked
            obj_mainchara.lsprite = spr_maincharal_pranked
        }
        else
        {
            scr_sansface(0, 0)
            global.msg[1] = "* well^1, come back&  whenever you want./%%"
        }
        break
    case 553:
        armor = "BEPIS."
        if (global.armor == 4)
            armor = "GROSS BANDAGE"
        if (global.armor == 12)
            armor = "FADED RIBBON"
        if (global.armor == 15)
            armor = "BANDANNA"
        if (global.armor == 24)
            armor = "DUSTY TUTU"
        global.flag[75] = global.armor
        global.msg[0] = "* Ring..\E0./"
        scr_papface(1, 0)
        global.msg[2] = "HELLO^1!&THIS IS PAPYRUS!!!/"
        global.msg[3] = "\E3HOW DID I GET THIS&NUMBER...?/"
        global.msg[4] = "\E0IT WAS EASY!!!/"
        global.msg[5] = "\E0I JUST DIALED EVERYNUMBER SEQUENTIALLYUNTIL I GOT YOURS!!!/"
        global.msg[6] = "NYEH HEH HEH HEH!!/"
        global.msg[7] = "\E2SO..^1.&WHAT ARE YOU&WEARING...?/"
        global.msg[8] = "\E3I'M..^1.&ASKING FOR A&FRIEND./"
        global.msg[9] = (("\E0SHE THOUGHT SHE&SAW YOU WEARING A&" + armor) + "./")
        global.msg[10] = (("\E3IS THAT TRUE^1?&ARE YOU WEARING A&" + armor) + "?/")
        global.msg[11] = "\TS \F0 \T0 %"
        global.msg[12] = "* (What will you say?)& &         Yes         No\C"
        global.msg[13] = " "
        break
    case 554:
        armor = "BEPIS."
        if (global.armor == 4)
            armor = "GROSS BANDAGE"
        if (global.armor == 12)
            armor = "FADED RIBBON"
        if (global.armor == 15)
            armor = "BANDANNA"
        if (global.armor == 24)
            armor = "DUSTY TUTU"
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.flag[76] = 0
            global.msg[1] = (("SO YOU ARE WEARING&A " + armor) + ".../")
            global.msg[2] = "GOT IT!!^1!&WINK WINK!!!/"
            global.msg[3] = "HAVE A NICE DAY!/"
            global.msg[4] = "\TS \F0 \T0 %"
            global.msg[5] = "* Click.../%%"
        }
        else
        {
            global.flag[76] = 1
            global.msg[1] = (("SO YOU AREN'T&WEARING A&" + armor) + ".../")
            global.msg[2] = "GOT IT!/"
            global.msg[3] = "YOU'RE MY FRIEND^1,&SO I TRUST YOU&100-PERCENT./"
            global.msg[4] = "HAVE A NICE DAY!/"
            global.msg[5] = "\TS \F0 \T0 %"
            global.msg[6] = "* Click.../%%"
        }
        break
    case 556:
        global.msg[0] = "* (There's an empty pie tin&  inside the stove.)/%%"
        if instance_exists(obj_papyrusparent)
        {
            scr_papface(0, 0)
            global.msg[1] = "\E0MY BROTHER ALWAYS&GOES OUT TO EAT^1.&BUT.../"
            global.msg[2] = "\E3RECENTLY^1, HE TRIED&'BAKING' SOMETHING./"
            global.msg[3] = "IT WAS LIKE..^1.&A QUICHE./"
            global.msg[4] = "BUT FILLED WITH A&SUGARY^1, NON-EGG&SUBSTANCE./"
            global.msg[5] = "\E0HOW ABSURD!/%%"
        }
        break
    case 557:
        global.msg[0] = "* (It's a joke book.)/"
        global.msg[1] = "* (Take a look inside?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 558:
        if (global.choice == 0)
        {
            global.msg[0] = "* (Inside the joke book was&  a quantum physics book.)/"
            global.msg[1] = "* (You look inside...)/"
            global.msg[2] = "* (Inside the quantum physics&  book was another joke&  book.)/"
            global.msg[3] = "* (You look inside...)/"
            global.msg[4] = "* (There's another quantum&  physics book...)/"
            global.msg[5] = "* (You decide to stop.)/%%"
        }
        else
            global.msg[0] = " %%"
        break
    case 559:
        if instance_exists(obj_papyrusparent)
        {
            scr_papface(0, 0)
            global.msg[1] = "THAT'S MY ROOM^1!/"
            global.msg[2] = "IF YOU'VE&FINISHED LOOKING&AROUND.../"
            global.msg[3] = "WE COULD GO IN&AND.../"
            global.msg[4] = "" + chr(34) + "HANG-OUT" + chr(34) + " LIKE&A PAIR OF VERY&COOL FRIENDS?/"
            if (global.flag[66] == 1)
                global.msg[4] = "\E3DO WHATEVER&PEOPLE DO WHEN&THEY DATE???/"
            global.msg[5] = "\TS \F0 \T0 %"
            global.msg[6] = "* (Go inside?)& &         Yes         No\C"
            global.msg[7] = " %"
        }
        else
        {
            global.msg[0] = "* (It's the door to&  Papyrus's room.)/"
            global.msg[1] = "* (It's covered in many&  labels...)/"
            global.msg[2] = "\TP %"
            global.msg[3] = "* (NO GIRLS ALLOWED!)/"
            global.msg[4] = "* (NO BOYS ALLOWED!)/"
            global.msg[5] = "* (PAPYRUS ALLOWED.)/%%"
        }
        break
    case 560:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            with (obj_papdoor)
                event_user(2)
        }
        break
    case 561:
        if instance_exists(obj_papyrusparent)
        {
            scr_papface(0, 0)
            global.msg[1] = "THERE ARE NO&SKELETONS INSIDE&MY CLOSET!!!/"
            global.msg[2] = "\E3EXCEPT ME&SOMETIMES./"
            global.msg[3] = "\TS \F0 \T0 %"
            global.msg[4] = "* (Look in the closet?)& &         Yes         No\C"
            global.msg[5] = " "
        }
        else
        {
            global.msg[0] = "* (Look in the closet?)& &         Yes         No\C"
            global.msg[1] = " "
        }
        break
    case 562:
        if (global.choice == 0)
            global.msg[0] = "* (Clothes are hung up&  neatly inside.)/%%"
        else
        {
            global.msg[0] = " %%"
            if instance_exists(obj_papyrusparent)
            {
                scr_papface(0, 0)
                global.msg[1] = "IT'S OK TO BE&INTIMIDATED BY&MY FASHION SENSE./%%"
            }
        }
        break
    case 563:
        global.msg[0] = "\E3SO^1, UM.../"
        global.msg[1] = "\E3IF YOU'VE SEEN&EVERYTHING.../"
        global.msg[2] = "\E2DO YOU WANT TO&START HANGING OUT?/"
        global.msg[3] = "\TS \F0 \T0 %"
        global.msg[4] = "* (Begin the hangouts?)& &         Yes         No\C"
        global.msg[5] = " "
        if (global.flag[66] == 1)
        {
            global.msg[2] = "\E2DO YOU WANT TO&START THE DATE?/"
            global.msg[3] = "\TS \F0 \T0 %"
            global.msg[4] = "* (Begin the date?)& &         Yes         No\C"
            global.msg[5] = " "
        }
        break
    case 564:
        if (global.choice == 0)
        {
            obj_papyrus_hisroom.intro = 4
            scr_papface(0, 0)
            global.msg[1] = "\E0OKAY!!^1!&DATING START!!!/%%"
            if (global.flag[66] == 0)
                global.msg[1] = "\E0OKAY!!^1!&LET'S HANG TEN!!/%%"
        }
        else
        {
            scr_papface(0, 2)
            global.msg[1] = "\E2TAKE YOUR TIME..^1.&I'LL WAIT FOR&YOU./%%"
        }
        break
    case 565:
        global.msg[0] = "* (This mailbox is labelled&  " + chr(34) + "PAPYRUS" + chr(34) + ".)/"
        global.msg[1] = "* (Look inside?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 566:
        if (global.choice == 0)
            global.msg[0] = "* (It's empty.)/%%"
        else
            global.msg[0] = "* (You realize that would&  probably be illegal.)/%%"
        break
    case 567:
        global.msg[0] = "* what^1?&* haven't you seen a guy&  with two jobs before?/"
        global.msg[1] = "\E1* fortunately^1, two jobs&  means twice as many&  legally-required breaks./"
        global.msg[2] = "\E0* i'm going to grillby's.&* wanna come?&  Yeah        I'm busy \C"
        global.msg[3] = " "
        if (global.flag[67] == 1)
            global.msg[0] = "* .../%%"
        if (scr_murderlv() >= 7)
        {
            global.msg[0] = "\E2* hey^1, looks like you're&  really turning yourself&  around./"
            global.msg[1] = "\E0* how about i treat you&  to lunch at grillby's?/"
            global.msg[2] = "\E1* ... when everyone you&  scared away comes&  back^1, i mean./%%"
        }
        break
    case 568:
        if (global.choice == 0)
        {
            global.msg[0] = "* well^1, if you insist..^1.&* i'll pry myself away&  from my work.../%%"
            if instance_exists(obj_sans_sentry2)
                obj_sans_sentry2.con = 1
        }
        if (global.choice == 1)
            global.msg[0] = "* OK^1. have fun./%%"
        break
    case 570:
        global.msg[0] = "\E1* whoops^1, watch where&  you sit down./"
        global.msg[1] = "* sometimes weirdos put&  whoopee cushions on&  the seats./"
        global.msg[2] = "\E0* anyway^1, let's order./"
        global.msg[3] = "* whaddya want...?& &  Fries       Burger\C"
        global.msg[4] = " "
        break
    case 571:
        global.msg[0] = "* ok^1, coming right up./%%"
        if (global.choice == 0)
        {
            global.flag[391] = 1
            global.msg[0] = "* hey^1, that sounds&  pretty good./"
            global.msg[1] = "* grillby^1, we'll have&  a double order of&  fries./%%"
            obj_grillbynpc_sansdate.burg = 0
        }
        if (global.choice == 1)
        {
            global.flag[391] = 2
            global.msg[0] = "* hey^1, that sounds&  pretty good./"
            global.msg[1] = "* grillby^1, we'll have&  a double order of&  burg./%%"
            obj_grillbynpc_sansdate.burg = 1
        }
        break
    case 572:
        global.msg[0] = "* so^1, what do you&  think.../"
        global.msg[1] = "* of my brother?& &  Cool        Uncool\C"
        global.msg[2] = " "
        break
    case 573:
        if (global.choice == 0)
        {
            global.msg[0] = "* of course he's cool./"
            global.msg[1] = "\E1* you'd be cool too&  if you wore that&  outfit every day./"
            global.msg[2] = "\E0* he'd only take that&  thing off if he&  absolutely had to./"
            global.msg[3] = "* oh well^1.&* at least he washes&  it./"
            global.msg[4] = "\E1* and by that i mean&  he wears it in&  the shower./%%"
        }
        else
        {
            global.msg[0] = "* hey^1, pal./"
            global.msg[1] = "\E1* sarcasm isn't funny^1,&  okay?/"
            global.msg[2] = "\E0* my brother's a real&  star./"
            global.msg[3] = "* he's the person who&  pushed me to get&  this sentry job./"
            global.msg[4] = "* maybe it's a little&  strange^1, but&  sometimes.../"
            global.msg[5] = "* ... it's nice to have&  someone call you out&  on being lazy./"
            global.msg[6] = "\E1* even though nothing&  could be further&  from the truth./%%"
        }
        break
    case 574:
        global.msg[0] = "* here comes the grub./"
        global.msg[1] = "* want some ketchup^1?& &  Yes         No    \C"
        global.msg[2] = " "
        break
    case 575:
        if (global.choice == 0)
            global.msg[0] = "\E2* bone appetit./%%"
        else
        {
            global.msg[0] = "\E2* more for me./%%"
            if instance_exists(obj_grillbynpc_sansdate)
                obj_grillbynpc_sansdate.burg = 2
        }
        break
    case 576:
        global.msg[0] = "\W* have you ever heard&  of a \Ytalking flower\W?&  Yes         No    \C"
        global.msg[1] = " "
        break
    case 577:
        if (global.choice == 0)
            global.msg[0] = "\E1* so you know all&  about it./"
        if (global.choice == 1)
            global.msg[0] = "\E1* i'll tell you^1, then./"
        global.msg[1] = "\W*\E0 the \Becho flower\W./"
        global.msg[2] = "* they're all over the&  marsh./"
        global.msg[3] = "* say something to them^1,&  and they'll repeat it&  over and over.../"
        global.msg[4] = "* what about it?/"
        global.msg[5] = "* well^1, papyrus told&  me something interesting&  the other day./"
        global.msg[6] = "* sometimes^1, when no&  one else is around.../"
        global.msg[7] = "* a flower appears and&  whispers things to&  him./"
        global.msg[8] = "* flattery..^1.&* advice..^1.&* encouragement.../"
        global.msg[9] = "* ... predictions./"
        global.msg[10] = "* weird^1, huh?/"
        global.msg[11] = "* someone must be using&  an echo flower to&  play a trick on him./"
        global.msg[12] = "* keep an eye out^1, ok?/"
        global.msg[13] = "* thanks./%%"
        break
    case 578:
        global.msg[0] = "\E1* oh^1, by the way.../"
        global.msg[1] = "\E0* i'm flat broke^1.&* can you foot the&  bill?/"
        global.msg[2] = "* it's just 10000G.& &  Yes         No    \C"
        global.msg[3] = " "
        break
    case 579:
        global.msg[0] = " %%"
        break
    case 580:
        if (global.flag[84] == 4)
            global.msg[0] = "* My mind is running wild^1!&* I haven't felt like this&  in a long time.../%%"
        if (global.flag[84] == 5)
            global.msg[0] = "* Please leave./%%"
        if (global.flag[84] < 2)
        {
            global.msg[0] = "* You..^1.&* You came from outside^1,&  didn't you?/"
            global.msg[1] = "* People like you are so&  rare.../"
            global.msg[2] = "* Please^1!* Stranger!/"
            global.msg[3] = "* Tell me about outside...?& &         Yes         No\C"
            global.msg[4] = " "
        }
        if (global.flag[84] == 2)
            global.msg[0] = "* Well^1, what are you&  waiting for?/%%"
        if (global.flag[84] == 3)
        {
            global.msg[0] = "* Oh^1!&* You're back!/"
            global.msg[1] = "* How's the room?& &         Different   Same\C"
            global.msg[2] = " "
        }
        break
    case 581:
        if (global.flag[84] < 3)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* Huh^1?&* " + chr(34) + "SURFACE" + chr(34) + "^1?&* What do you mean?/"
                global.msg[1] = "* I just meant outside this&  room./"
                global.msg[2] = "* If you haven't noticed^1, my&  mycelium have bound me&  to the ground./"
                global.msg[3] = "* Please^1!&* Stranger!/"
                global.msg[4] = "* I'll make this simple./"
                global.msg[5] = "* I've spent my whole life&  in the same spot^1,&  in the same room./"
                global.msg[6] = "* But I've long wondered&  what lies inside the&  room to the right./"
                global.msg[7] = "* Long I've fantasized&  about entering^1, and&  changing my scenery.../"
                global.msg[8] = "* No..^1.&* Changing my LIFE!/"
                global.msg[9] = "* Please^1.&* Go and tell me what's&  inside./%%"
                if (doak == 0)
                {
                    if (global.flag[84] == 0)
                        global.flag[84] = 2
                    doak = 1
                }
            }
            if (global.choice == 1)
                global.msg[0] = "* Is everyone out there&  like you^1?&* How terrible./%%"
        }
        else
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* Oh^1, that's a relief^1!/"
                global.msg[1] = "* That's all I need to&  continue my fantasies^1.&* Thank you^1, stranger./%%"
                global.flag[84] = 4
            }
            if (global.choice == 1)
            {
                global.msg[0] = "* So it's the same./"
                global.msg[1] = "* The same.../"
                global.msg[2] = "* Same.../"
                global.msg[3] = "* .../"
                global.msg[4] = "* OK./%%"
                global.flag[84] = 5
            }
        }
        break
    case 583:
        if (global.flag[85] == 0)
        {
            global.msg[0] = "* (Take an umbrella?)& &         Take one    Do not\C"
            global.msg[1] = " "
        }
        else
        {
            global.msg[0] = "* (Return the umbrella?)& &         Put back    Do not\C"
            global.msg[1] = " "
        }
        break
    case 584:
        if (doak == 0)
        {
            if (global.flag[85] == 0)
            {
                if (global.choice == 0)
                {
                    global.msg[0] = "* (You took an umbrella.)/%%"
                    global.flag[85] = 1
                    if instance_exists(obj_umbrellabox)
                        obj_umbrellabox.image_index = 1
                    obj_mainchara.dsprite = spr_maincharad_umbrella
                    obj_mainchara.rsprite = spr_maincharar_umbrella
                    obj_mainchara.lsprite = spr_maincharal_umbrella
                    obj_mainchara.usprite = spr_maincharau_umbrella
                }
                else
                    global.msg[0] = " %%"
            }
            else if (global.choice == 0)
            {
                global.msg[0] = "* (You returned the umbrella.)/%%"
                global.flag[85] = 0
                if instance_exists(obj_umbrellabox)
                    obj_umbrellabox.image_index = 0
                obj_mainchara.dsprite = spr_maincharad
                obj_mainchara.rsprite = spr_maincharar
                obj_mainchara.usprite = spr_maincharau
                obj_mainchara.lsprite = spr_maincharal
            }
            else
                global.msg[0] = " %%"
            doak = 1
        }
        break
    case 585:
        global.msg[0] = "* (It's a statue^1.)&* (The structures at its&  feet seem dry.)/%%"
        if (global.flag[85] == 1 && global.flag[86] == 0)
        {
            global.msg[0] = "* (Put the umbrella on the&  statue?)&         Yes         Do not\C"
            global.msg[1] = " "
        }
        if (global.flag[86] == 1)
            global.msg[0] = "* (The music continues^1, and&  doesn't stop.)/%%"
        break
    case 586:
        if (doak == 0)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* (You place the umbrella&  atop the statue.)/"
                global.msg[1] = "* (Inside the statue^1, a music&  box begins to play...)/%%"
                global.flag[85] = 0
                global.flag[86] = 1
                if instance_exists(obj_musicstatue)
                {
                    obj_musicstatue.image_index = 1
                    obj_musicstatue.con = 1
                }
                obj_mainchara.dsprite = spr_maincharad
                obj_mainchara.rsprite = spr_maincharar
                obj_mainchara.usprite = spr_maincharau
                obj_mainchara.lsprite = spr_maincharal
            }
            else
                global.msg[0] = " %%"
            doak = 1
        }
        break
    case 587:
        global.msg[0] = "* (It's a rusty old&  fridge.)/"
        global.msg[1] = "* (Look inside?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 588:
        if (global.choice == 0)
        {
            global.msg[0] = "* (You open the fridge.^1)&* (The air fills with a&  rotten stench.)/"
            global.msg[1] = "* (All the food inside here&  spoiled long ago.)/%%"
        }
        else
            global.msg[0] = " %%"
        break
    case 589:
        doak = 0
        noroom = 0
        global.msg[0] = "* error/%%"
        if (global.flag[108] > 2)
            global.msg[0] = "* (The cooler is empty.)/%%"
        if (global.flag[108] == 1 || global.flag[108] == 2)
        {
            global.msg[0] = "* (Take a space food bar&  from the cooler?)&         Yes         No \C"
            global.msg[1] = " "
        }
        if (global.flag[108] == 0)
        {
            global.msg[0] = "* (It's a cooler^1.&* It has no brand^1, and&  shows no signs of wear...)/"
            global.msg[1] = "* (Inside are a couple&  of freeze-dried space&  food bars.)/"
            global.msg[2] = "* (Take one?)& &         Yes         No \C"
            global.msg[3] = " "
            global.flag[108] = 1
        }
        break
    case 590:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                noroom = 0
                doak = 1
                script_execute(scr_itemget, 35)
                if (noroom == 0)
                    global.flag[108] += 1
            }
            if (noroom == 0)
                global.msg[0] = "* (You got the Astronaut Food.)/%%"
            if (noroom == 1)
                global.msg[0] = "* (You're carrying too much.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = "* %%"
        break
    case 591:
        if (global.flag[355] == 0)
        {
            global.msg[0] = "* (Seems like a regular&  training dummy.)/"
            global.msg[1] = "* (Do you want to beat it&  up?)&         Yes         No \C"
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* (You've had enough of&  the dummy.)/%%"
        break
    case 592:
        if (global.choice == 0)
        {
            if (global.lv == 1)
            {
                global.msg[0] = "* (You tap the dummy with&  your fist.)/"
                global.msg[1] = "* (You feel bad.)/%%"
            }
            if (global.lv > 1)
            {
                global.msg[0] = "* (You hit the dummy&  lightly.)/"
                global.msg[1] = "* (You don't feel like&  you learned anything.)/%%"
            }
            if (global.lv > 4)
            {
                global.msg[0] = "* (You sock the dummy.)/"
                global.msg[1] = "* (Who cares?)/%%"
            }
            if (global.lv > 7)
            {
                global.msg[0] = "* (You punch the dummy at&  full force.)/"
                global.msg[1] = "* (Feels good.)/%%"
            }
            global.flag[355] = 1
        }
        if (global.choice == 1)
        {
            global.msg[0] = "* (You stare into each&  other's eyes for a&  moment...)/%%"
            global.flag[355] = 2
        }
        break
    case 593:
        global.msg[0] = "* (It's a horse stable.)/"
        global.msg[1] = "* (Do you want to go&  inside?)&         Yes         No \C"
        global.msg[2] = " "
        break
    case 594:
        if (global.choice == 0)
        {
            global.msg[0] = "* (You jostle the door.)&* (It's locked.)/"
            global.msg[1] = "* (Suddenly^1, from inside&  the [redacted], you hear a&/%%"
        }
        if (global.choice == 1)
            global.msg[0] = "%%"
        break
    case 595:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 596:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 597:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 598:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 599:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 600:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 601:
        global.msg[0] = "* x/%%"
        global.flag[92] = 2
        break
    case 606:
        global.msg[0] = "* (This CD is labelled&  " + chr(34) + "Spooktunes." + chr(34) + ")/"
        global.msg[1] = "* (Play it?)& &         Play it     No\C"
        global.msg[2] = " "
        if (global.flag[94] == 1)
        {
            global.msg[0] = "* (This CD is labelled&  " + chr(34) + "Spooktunes." + chr(34) + ")/"
            global.msg[1] = "* (This CD is playing.)&* (Turn it off?)&         Stop it     No\C"
            global.msg[2] = " "
        }
        break
    case 607:
        if (global.flag[94] != 1)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* (You play the CD.)/%%"
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 1
            }
            if (global.choice == 1)
                global.msg[0] = "* (Spooktunes are dead.)/%%"
        }
        if (global.flag[94] == 1)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* (The CD stops moving.)/%%"
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 2
            }
            if (global.choice == 1)
                global.msg[0] = "* %%"
        }
        break
    case 608:
        global.msg[0] = "* (This CD is labelled&  " + chr(34) + "Spookwave." + chr(34) + ")/"
        global.msg[1] = "* (Play it?)& &         Play it     No\C"
        global.msg[2] = " "
        if (global.flag[94] == 2)
        {
            global.msg[0] = "* (This CD is labelled&  " + chr(34) + "Spookwave." + chr(34) + ")/"
            global.msg[1] = "* (This CD is playing.)&* (Turn it off?)&         Stop it     No\C"
            global.msg[2] = " "
        }
        break
    case 609:
        if (global.flag[94] != 2)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* (You play the CD.)/%%"
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 3
            }
            if (global.choice == 1)
                global.msg[0] = "* %%"
        }
        if (global.flag[94] == 2)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* (The CD stops moving.)/%%"
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 2
            }
            if (global.choice == 1)
                global.msg[0] = "* %%"
        }
        break
    case 610:
        global.msg[0] = "* (This CD is labelled&  " + chr(34) + "Ghouliday Music." + chr(34) + ")/"
        global.msg[1] = "* (Play it?)& &         Play it     No\C"
        global.msg[2] = " "
        if (global.flag[94] == 3)
        {
            global.msg[0] = "* (This CD is labelled&  " + chr(34) + "Ghouliday Music." + chr(34) + ")/"
            global.msg[1] = "* (This CD is playing.)&* (Turn it off?)&         Stop it     No\C"
            global.msg[2] = " "
        }
        break
    case 611:
        if (global.flag[94] != 3)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* (You play the CD.)/%%"
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 4
            }
            if (global.choice == 1)
                global.msg[0] = "* %%"
        }
        if (global.flag[94] == 3)
        {
            if (global.choice == 0)
            {
                global.msg[0] = "* (The CD stops moving.)/%%"
                if instance_exists(obj_napstablookdate_music)
                    obj_napstablookdate_music.con = 2
            }
            if (global.choice == 1)
                global.msg[0] = "* %%"
        }
        break
    case 612:
        global.msg[0] = "* (Look inside the fridge?)& &         Open it     No\C"
        global.msg[1] = " "
        break
    case 613:
        if (global.choice == 0)
        {
            if (global.flag[93] < 2)
            {
                global.msg[0] = "* (There's a lonely sandwich&  inside.)/%%"
                if instance_exists(obj_napstablookdate)
                    obj_napstablookdate.con = 11
            }
            else
                global.msg[0] = "* (It's empty.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = "%%"
        break
    case 615:
        global.msg[0] = "* this is a ghost sandwich.../"
        global.msg[1] = "* do you want to try it...& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 616:
        if (global.choice == 0)
        {
            global.msg[0] = "* (You attempt to bite&  into the ghost sandwich.)/"
            global.msg[1] = "* (You phase right through&  it...)/"
            global.msg[2] = "* oh.../"
            global.msg[3] = "* nevermind.../%%"
        }
        if (global.choice == 1)
            global.msg[0] = "* oh.....................&  ....................&  ................./%%"
        break
    case 617:
        global.msg[0] = "* after a great meal i like&  to lie on the ground and&  feel like garbage.../"
        global.msg[1] = "* it's a family tradition.../"
        global.msg[2] = "* do you want..^1.&* ... to join me...&         Yes         No\C"
        global.msg[3] = " "
        break
    case 618:
        if (global.choice == 0)
            global.msg[0] = "* okay..^1.&* follow my lead.../%%"
        if (global.choice == 1)
        {
            global.flag[93] = 9
            global.msg[0] = "* oh.....................&  ....................&  ................./%%"
            obj_napstablookdate.con = 80
        }
        break
    case 619:
        global.msg[0] = "* do you want to play a game^1?&* it's called thundersnail./"
        global.msg[1] = "* the snails will race^1, and if&  the yellow snail wins^1, you&  win./"
        global.msg[2] = "* it's 10G to play.& &         Play        No\C"
        global.msg[3] = " "
        break
    case 620:
        if (global.choice == 0)
        {
            if (global.gold == 0)
            {
                global.msg[0] = "* um..^1.&* you don't have any money?/"
                global.msg[1] = "* n-no^1, you can still play^1,&  don't worry about it.../"
                global.msg[2] = "* okay..^1.&* press [Z] repeatedly to&  encourage your snail./"
                global.msg[3] = "* ready?/%%"
            }
            if (global.gold < 10 && global.gold > 0)
            {
                global.msg[0] = "* um..^1. that's less than 10G./"
                global.msg[1] = "* but since you're my only&  real customer^1, i guess i'll&  just take what you have.../"
                global.msg[2] = "* okay..^1.&* press [Z] repeatedly to&  encourage your snail./"
                global.msg[3] = "* ready?/%%"
                global.gold = 0
            }
            if (global.gold >= 10)
            {
                global.gold -= 10
                global.msg[0] = "* okay..^1.&* press [Z] repeatedly to&  encourage your snail./"
                global.msg[1] = "* ready?/%%"
            }
            obj_napstablook_farm2.con = 1
        }
        if (global.choice == 1)
            global.msg[0] = "* oh.........../%%"
        break
    case 621:
        global.msg[0] = "* .../"
        global.msg[1] = "* Seven./"
        global.msg[2] = "* Seven human souls./"
        global.msg[3] = "* With the power of seven&  human souls^1, our king.../"
        global.msg[4] = "\W* \YKing \RASGORE \YDreemurr\W.../"
        global.msg[5] = "* ... will become a god./"
        global.msg[6] = "\W* With that power^1, \RASGORE\W &  can finally shatter the&  barrier./"
        global.msg[7] = "* He will finally take the&  surface back from humanity.../"
        global.msg[8] = "* And give them back the&  suffering and pain that&  we have endured./"
        global.msg[9] = "* .../"
        global.msg[10] = "* Understand^1, human?/"
        global.msg[11] = "* This is your only chance&  at redemption./"
        global.msg[12] = "* Give up your soul.../"
        global.msg[13] = "* Or I'll tear it from&  your body./%%"
        break
    case 622:
        if (global.choice == 0)
        {
            global.msg[0] = "* That spark in your eyes.../"
            global.msg[1] = "* You're really eager to&  die^1, aren't you?/%%"
        }
        if (global.choice == 1)
            global.msg[0] = "* .../%%"
        break
    case 623:
        global.msg[0] = "* Yo^1, I know I'm not supposed&  to be here^1, but.../"
        global.msg[1] = "* I wanna ask you something./"
        global.msg[2] = "* .../"
        global.msg[3] = "* Man^1, I've never had to ask&  anyone this before.../"
        global.msg[4] = "* Umm.../"
        global.msg[5] = "* Yo..^1. You're human^1, right?&* Haha./"
        global.msg[6] = "* Man^1! I knew it!/"
        global.msg[7] = "* ... well^1, I know it now^1,&  I mean.../"
        global.msg[8] = "* Undyne told me^1, um^1, " + chr(34) + "stay&  away from that human." + chr(34) + "/"
        global.msg[9] = "* So^1, like^1, ummm.../"
        global.msg[10] = "* I guess that makes us enemies&  or something./"
        global.msg[11] = "* But I kinda stink at that^1,&  haha./"
        global.msg[12] = "* Yo^1, say something mean so&  I can hate you?/"
        global.msg[13] = "* Please? & &         Yes         No \C"
        global.msg[14] = " "
        break
    case 624:
        if (global.choice == 0)
        {
            global.msg[0] = "* Huh...?/"
            global.msg[1] = "* Yo^1, that's your idea of&  something mean?/"
            global.msg[2] = "* My sister says that to me&  ALL THE TIME!/"
            global.msg[3] = "* Guess I have to do it^1, haha./"
            global.msg[4] = "* Yo^1, I..^1. I hate your guts./"
            global.msg[5] = "* .../"
            global.msg[6] = "* Man^1, I..^1. I'm such a turd./"
            global.msg[7] = "* I'm..^1. I'm gonna go home&  now./%%"
        }
        if (global.choice == 1)
        {
            global.msg[0] = "* Yo^1, what^1?&* So I have to do it?/"
            global.msg[1] = "* Here goes nothing.../"
            global.msg[2] = "* Yo^1, I..^1. I hate your guts./"
            global.msg[3] = "* .../"
            global.msg[4] = "* Man^1, I..^1. I'm such a turd./"
            global.msg[5] = "* I'm..^1. I'm gonna go home&  now./%%"
        }
        break
    case 625:
        if (global.flag[353] <= 19)
        {
            global.msg[0] = "* (It's a water cooler.)&* (Take a cup of water?)&         Yes         No \C"
            global.msg[1] = " "
        }
        else
        {
            global.msg[0] = "* (There's no more water left&  in the cooler.)/%%"
            if instance_exists(obj_undynefall)
                global.msg[0] = "* (Sadistically^1, you've poured&  out all the water right in&  front of Undyne's eyes.)/%%"
        }
        break
    case 626:
        if (global.choice == 0)
        {
            global.flag[440] += 1
            global.msg[0] = "* (You take a cup of water.)/%%"
            with (obj_watercooler)
                event_user(1)
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 627:
        global.msg[0] = "* (Get rid of the water?)& &         Yes         No \C"
        global.msg[1] = " "
        break
    case 628:
        if (global.choice == 0)
        {
            global.msg[0] = "* (You pour the water on&  the ground next to the&  water cooler.)/%%"
            with (obj_watercooler)
            {
                if instance_exists(obj_undynefall)
                    global.flag[441] += 1
                global.flag[353] += 1
                event_user(1)
            }
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 629:
        global.msg[0] = "* A rousing error./%%"
        if instance_exists(obj_watercooler)
        {
            if (obj_watercooler.havewater == 1)
            {
                global.msg[0] = "* (Give Undyne the water?)& &         Yes         No \C"
                global.msg[1] = " "
            }
            else
                global.msg[0] = "* (She looks dry...)/%%"
        }
        break
    case 630:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            global.interact = 1
            with (obj_undynefall)
                event_user(1)
        }
        break
    case 632:
        armor1 = "BEPIS."
        armor2 = "BEPIS."
        if (global.flag[75] == 4)
            armor1 = "GROSS BANDAGE"
        if (global.flag[75] == 12)
            armor1 = "FADED RIBBON"
        if (global.flag[75] == 15)
            armor1 = "BANDANNA"
        if (global.flag[75] == 24)
            armor1 = "DUSTY TUTU"
        if (global.flag[77] == 4)
            armor2 = "GROSS BANDAGE"
        if (global.flag[77] == 12)
            armor2 = "FADED RIBBON"
        if (global.flag[77] == 15)
            armor2 = "BANDANNA"
        if (global.flag[77] == 24)
            armor2 = "DUSTY TUTU"
        global.msg[0] = "* Ring..\E0./"
        scr_papface(1, 0)
        global.msg[2] = "HELLO^1!&THIS IS PAPYRUS!!!/"
        global.msg[3] = "REMEMBER WHEN&I ASKED YOU&ABOUT CLOTHES?/"
        global.msg[4] = "\E3WELL^1, THE FRIEND&WHO WANTED TO&KNOW.../"
        global.msg[5] = "\E0HER OPINION OF&YOU IS VERY.../"
        global.msg[6] = "\E3MURDERY./"
        global.msg[7] = "\E0ERROR!!^1!&SEE YOU LATER!/%%"
        if (global.flag[75] == global.flag[77])
        {
            if (global.flag[76] == 0)
            {
                global.msg[7] = "\E0BUT I BET YOU&KNEW THAT&ALREADY!/"
                global.msg[8] = "\E3AND BECAUSE YOU&KNEW THAT.../"
                global.msg[9] = "\E0I TOLD HER WHAT&YOU TOLD ME&YOU WERE WEARING!/"
                global.msg[10] = (("A " + armor1) + "!/")
                global.msg[11] = "BECAUSE I KNEW^1,&OF COURSE.../"
                global.msg[12] = "\E3AFTER SUCH A&SUSPICIOUS&QUESTION.../"
                global.msg[13] = "\E0YOU WOULD&OBVIOUSLY CHANGE&YOUR CLOTHES!/"
                global.msg[14] = "YOU'RE SUCH A&SMART COOKIE!/"
                global.msg[15] = "THIS WAY YOU'RE&SAFE AND I&DIDN'T LIE!!!/"
                global.msg[16] = "NO BETRAYAL&ANYWHERE!!!/"
                global.msg[17] = "BEING FRIENDS&WITH EVERYONE&IS EASY!!!/"
                global.msg[18] = "\TS \F0 \T0 %"
                global.msg[19] = "* Click.../%%"
            }
        }
        if (global.flag[75] != global.flag[77])
        {
            if (global.flag[76] == 0)
            {
                global.msg[7] = "\E0WELL^1, WORRY&NOT DEAR HUMAN!/"
                global.msg[8] = "PAPYRUS WOULD&NEVER BETRAY YOU!/"
                global.msg[9] = "\E3I AM NOT A&CRUEL PERSON./"
                global.msg[10] = "\E0I STRIVE TO BE&COMFORTING AND&PLEASANT./"
                global.msg[11] = "PAPYRUS!&HE SMELLS LIKE&THE MOON./"
                global.msg[12] = "SO, BECAUSE OF&MY INHERENT&GOODNESS.../"
                global.msg[13] = (("I TOLD HER YOU&WERE NOT WEARING&A " + armor1) + "!/")
                global.msg[14] = "\E3EVEN THOUGH YOU&TOLD ME YOU&WERE!/"
                global.msg[15] = "INSTEAD^1, I MADE&SOMETHING UP!/"
                global.msg[16] = "I TOLD HER YOU&WERE WEARING.../"
                global.msg[17] = (("\E0A " + armor2) + "./")
                global.msg[18] = "\E3IT PAINED ME TO&TELL SUCH A&BOLDFACED LIE./"
                global.msg[19] = (("I KNOW YOU WOULD&NEVER EVER WEAR&A " + armor2) + "./")
                global.msg[20] = "\E0BUT YOUR SAFETY&IS MORE IMPORTANT&THAN FASHION./"
                global.msg[21] = "\E3DANG!/"
                global.msg[22] = "I JUST WANT TO&BE FRIENDS WITH&EVERYONE.../"
                global.msg[23] = "\TS \F0 \T0 %"
                global.msg[24] = "* Click.../%%"
            }
        }
        if (global.flag[75] == global.flag[77])
        {
            if (global.flag[76] == 1)
            {
                global.msg[7] = "\E0WELL^1, WORRY&NOT DEAR HUMAN!/"
                global.msg[8] = "\E3PAPYRUS WOULD&NEVER BETRAY YOU!/"
                global.msg[9] = (("\WY\E0OU SAID YOU WERE\Y &NOT WEARING A&" + armor1) + "\W./")
                global.msg[10] = "\E3SO OF COURSE&I ACTUALLY&TOLD HER.../"
                global.msg[11] = (("\E0YOU WERE&INDEED WEARING&A " + armor1) + "!/")
                global.msg[12] = "\E3IT PAINED ME TO&TELL SUCH A&BOLDFACED LIE./"
                global.msg[13] = (("BUT SINCE YOU&AREN'T WEARING&A " + armor1) + "./")
                global.msg[14] = "\E0SHE SURELY&WON'T ATTACK&YOU!/"
                global.msg[15] = "NOW YOU ARE&SAFE AND SOUND./"
                global.msg[16] = "\E2WOWIE..^1.&THIS IS HARD./"
                global.msg[17] = "I JUST WANT TO&BE EVERYBODY'S&FRIEND!/"
                global.msg[18] = "\TS \F0 \T0 %"
                global.msg[19] = "* Click.../%%"
            }
        }
        if (global.flag[75] != global.flag[77])
        {
            if (global.flag[76] == 1)
            {
                global.msg[7] = "\E0BUT I BET YOU&KNEW THAT&ALREADY!/"
                global.msg[8] = "\E3AND BECAUSE YOU&KNEW THAT.../"
                global.msg[9] = "\E0I KNEW WHEN&YOU SAID:/"
                global.msg[10] = (('\E3"I AM NOT&WEARING A&' + armor1) + '."/')
                global.msg[11] = "\E0IT WAS REALLY&A SECRET CODE!/"
                global.msg[12] = "\E3YOU REALLY&MEANT.../"
                global.msg[13] = (('\E0"I ACTUALLY AM&WEARING&A ' + armor1) + '!"/')
                global.msg[14] = "YOU WERE TRYING&TO PROTECT&YOURSELF.../"
                global.msg[15] = "WHILE MAKING IT&SO I DIDN'T&HAVE TO LIE!/"
                global.msg[16] = "I PICKED UP ON&THIS, AND FOLLOWED&YOUR PLAN./"
                global.msg[17] = (("I TOLD HER YOU&WERE NOT WEARING&A " + armor1) + "!/")
                global.msg[18] = "IN FACT I TOOK&IT ONE STEP&FURTHER!/"
                global.msg[19] = "\E3I TOLD HER YOU&WERE PROBABLY.../"
                global.msg[20] = (("\E0WEARING A&" + armor2) + "!/")
                global.msg[21] = "\E3OF COURSE, YOU&WOULD NEVER&WEAR THAT./"
                global.msg[22] = "\E0BUT THAT'S THE&POINT!/"
                global.msg[23] = "SHE WON'T&RECOGNIZE YOU&NOW!/"
                global.msg[24] = "AND I DIDN'T&HAVE TO BETRAY&EITHER OF YOU!/"
                global.msg[25] = "SINCE I JUST&TOLD HER WHAT&YOU SAID!/"
                global.msg[26] = "WOWIE^1!&YOU'RE SUCH A&SMART COOKIE!/"
                global.msg[27] = "I REALLY CAN&BE FRIENDS WITH&EVERYONE!!!/"
                global.msg[28] = "\TS \F0 \T0 %"
                global.msg[29] = "* Click.../%%"
            }
        }
        break
    case 633:
        global.msg[0] = "* Ring..\E0./"
        scr_papface(1, 0)
        global.msg[2] = "HEY^1!&WHAT'S UP!?/"
        global.msg[3] = "I WAS JUST&THINKING.../"
        global.msg[4] = "YOU^1, ME^1, AND&UNDYNE SHOULD ALL&HANG OUT SOMETIME!/"
        global.msg[5] = "I THINK YOU&WOULD MAKE&GREAT PALS!/"
        global.msg[6] = "LET'S MEET UP&AT HER HOUSE&LATER!/"
        global.msg[7] = "\TS \F0 \T0 %"
        global.msg[8] = "* Click.../%%"
        if (global.flag[88] < 3)
        {
            global.msg[5] = "AFTER YOU HANG&OUT WITH ME.../"
            global.msg[6] = "LET'S MEET UP&AT HER HOUSE!/"
            global.msg[7] = "I THINK YOU&WOULD MAKE&GREAT PALS!/"
            global.msg[8] = "\TS \F0 \T0 %"
            global.msg[9] = "* Click.../%%"
        }
        break
    case 635:
        global.msg[0] = "* (It's a book labelled&  Monster History Part 6.)&         Read it     Do not\C"
        global.msg[1] = " "
        break
    case 636:
        global.msg[0] = "* Unfortunately^1, monsters are&  not experienced with&  illness./"
        global.msg[1] = "* However^1, when monsters are&  about to expire of age^1,&  they lie down^1, immobile./"
        global.msg[2] = "* We call this state&  " + chr(34) + "Fallen Down." + chr(34) + "/"
        global.msg[3] = "* A person who has Fallen&  Down will soon perish./"
        global.msg[4] = "* In a way^1, this confusing&  situation was all too&  familiar./%%"
        if (global.choice == 1)
            global.msg[0] = "* (You decide not to read it.)/%%"
        break
    case 637:
        global.msg[0] = "* (It's a book labelled&  Monster History Part 6.)&         Read it     Do not\C"
        global.msg[1] = " "
        break
    case 638:
        global.msg[0] = "* Unfortunately^1, monsters are&  not experienced with&  illness./"
        global.msg[1] = "* However^1, when monsters are&  about to expire of age^1,&  they lie down^1, immobile./"
        global.msg[2] = "* We call this state&  " + chr(34) + "Fallen Down." + chr(34) + "/"
        global.msg[3] = "* A person who has Fallen&  Down will soon perish./"
        global.msg[4] = "* In a way^1, this confusing&  situation was all too&  familiar./%%"
        if (global.choice == 1)
            global.msg[0] = "* (You decide not to read it.)/%%"
        break
    case 639:
        global.msg[0] = "* (It's a book labelled&  Monster History Part 7.)&         Read it     Do not\C"
        global.msg[1] = " "
        break
    case 640:
        global.msg[0] = "* When a human dies^1, its&  soul remains stable&  outside the body./"
        global.msg[1] = "* Meanwhile^1, a monster's soul&  disappears near-instantly&  upon death./"
        global.msg[2] = "* This allows monsters to&  absorb the souls of&  humans.../"
        global.msg[3] = "* While it is extremely&  difficult for humans to&  absorb a monster's soul./"
        global.msg[4] = "* This is why they feared us./"
        global.msg[5] = "* Though monsters are weak^1,&  with enough human souls.../"
        global.msg[6] = "* They could easily destroy&  all of mankind./%%"
        if (global.choice == 1)
            global.msg[0] = "* (You decide not to read it.)/%%"
        break
    case 641:
        global.msg[0] = "* (It's a book labelled&  Monster History Part 8.)&         Read it     Do not\C"
        global.msg[1] = " "
        break
    case 642:
        global.msg[0] = "* There is one exception&  to the aforementioned&  rules:/"
        global.msg[1] = "* A certain type of monster^1,&  the " + chr(34) + "boss" + chr(34) + " monster./"
        global.msg[2] = "* Due to its life cycle^1, it&  possesses an incredibly&  strong soul for a monster./"
        global.msg[3] = "* This soul can remain&  stable after death^1, if&  only for a few moments./%%"
        if (global.choice == 1)
            global.msg[0] = "* (You decide not to read it.)/%%"
        break
    case 643:
        global.msg[0] = "* (All these books are labelled&  Human History.)&         Read one    Do not\C"
        global.msg[1] = " "
        break
    case 644:
        global.msg[0] = "* (You look inside a book.)/"
        global.msg[1] = "* (It's a comic of a giant&  robot fighting a beautiful&  alien princess.)/"
        global.msg[2] = "* (This doesn't strike you&  as very accurate...)/%%"
        if (global.choice == 1)
            global.msg[0] = "* (You decide not to.)/%%"
        break
    case 645:
        global.msg[0] = "* (All these books are labelled&  Human History.)&         Read one    Do not\C"
        global.msg[1] = " "
        break
    case 646:
        global.msg[0] = "* (You look inside a book.)&* (It's a comic book.)/"
        global.msg[1] = "* (Two scantily-clad chefs are&  flinging energy pancakes&  at each other.)/"
        global.msg[2] = "* (This doesn't strike you&  as very accurate...)/%%"
        if (global.choice == 1)
            global.msg[0] = "* (You decide not to.)/%%"
        break
    case 647:
        global.msg[0] = "* (All these books are labelled&  Human History.)&         Read one    Do not\C"
        global.msg[1] = " "
        break
    case 648:
        global.msg[0] = "* (You look inside a book.)&* (It's a comic book.)/"
        global.msg[1] = "* (A hideous android is running&  to school with toast in&  its mouth.)/"
        global.msg[2] = "* (Seems like it's late.)/"
        global.msg[3] = "* (This doesn't strike you&  as very accurate...)/%%"
        if (global.choice == 1)
            global.msg[0] = "* (You decide not to.)/%%"
        break
    case 660:
        global.msg[0] = "* (There's a piano here.^1)&* (Play it?)&         Yes         No\C"
        global.msg[1] = " "
        break
    case 661:
        global.msg[0] = " %%"
        global.msg[1] = " "
        if (global.choice == 0)
        {
            if (instance_exists(obj_paino) == 0)
                instance_create(2, 2, obj_paino)
        }
        break
    case 666:
        global.msg[0] = "See that heart^1? &That is your SOUL^1,&the very culmination&of your being!/"
        global.msg[1] = "Your SOUL starts off&weak^1, but can grow&strong if you gain&a lot of LV./"
        global.msg[2] = "What's LV stand for^1?&Why^1, LOVE^1, of course!/"
        global.msg[3] = "You want some&LOVE, don't you?/"
        global.msg[4] = "Don't worry,&I'll share some&with you!/%"
        break
    case 667:
        global.msg[0] = "Down here^1, LOVE is&shared through..^1./"
        global.msg[1] = "Little white..^2.\E1 &" + chr(34) + "friendliness&pellets." + chr(34) + "/"
        global.msg[2] = "\E2Are you ready\E0?/%"
        break
    case 668:
        global.msg[0] = "Move around^1!&Get as many as&you can^2!%%%"
        global.msg[1] = "%%%"
        break
    case 669:
        global.msg[0] = "You idiot./"
        global.msg[1] = "In this world^1, it's&kill or BE killed./"
        global.msg[2] = "Why would ANYONE pass&up an opportunity&like this!?/%"
        break
    case 670:
        global.msg[0] = "Die./%"
        break
    case 671:
        global.msg[0] = "Hey buddy^1,&you missed them./"
        global.msg[1] = "Let's try again^1,&okay?/%"
        break
    case 672:
        global.msg[0] = "Is this a joke^2?&Are you braindead^2?&RUN^2. INTO^2. THE^2.&BULLETS!!!"
        break
    case 673:
        global.msg[0] = "You know what's&going on here^1,&don't you?/"
        global.msg[1] = "You just wanted to&see me suffer./%"
        break
    case 674:
        global.msg[0] = "\E1What a terrible&creature^1, torturing&such a poor^1,&innocent youth.../"
        global.msg[1] = "\E2Ah, do not be&afraid^1, my child./"
        global.msg[2] = "\XI am \BTORIEL\X,&caretaker of&the \RRUINS\X./"
        global.msg[3] = "I pass through this&place every day to&see if anyone has&fallen down./"
        global.msg[4] = "You are the first&human to come here&in a long time./"
        global.msg[5] = "I will do my best&to ensure your&protection during&your time here./%%"
        global.msg[5] = "\E2Come^2!&I will guide you&through the&catacombs./%%"
        global.msg[6] = "%%%"
        break
    case 680:
        global.msg[0] = "* Three gold for the ferry.& &         Yes         No\C"
        global.msg[1] = " "
        break
    case 681:
        global.msg[0] = "* Later^1, then./%%"
        global.msg[1] = " "
        if (global.choice == 0)
        {
            if (instance_exists(obj_purpledude) == 1)
                obj_purpledude.con = 1
            global.msg[0] = "* Hop on!/%%"
        }
        break
    case 682:
        global.msg[0] = "* (It's a switch.)& &         Press it    Don't\C"
        global.msg[1] = " "
        break
    case 683:
        if (global.flag[371] == 0 && doak == 0)
        {
            global.msg[0] = " %%"
            if (global.choice == 0)
            {
                sc = instance_create(0, 0, obj_soundcombo)
                with (sc)
                {
                    sound1 = snd_switchpull_n
                    sound2 = snd_spearappear
                    alarm[1] = 8
                }
                doak = 1
                global.msg[0] = "* (The lasers were deactivated.)/%%"
                if instance_exists(obj_laserswitch1)
                {
                    with (obj_laserswitch1)
                        event_user(0)
                }
            }
        }
        if (global.flag[371] == 1 && doak == 0)
        {
            global.msg[0] = " %%"
            if (global.choice == 0)
            {
                sc = instance_create(0, 0, obj_soundcombo)
                with (sc)
                {
                    sound1 = snd_switchpull_n
                    sound2 = snd_spearappear
                    alarm[1] = 8
                }
                doak = 1
                global.msg[0] = "* (The lasers were reactivated.)/%%"
                if instance_exists(obj_laserswitch1)
                {
                    with (obj_laserswitch1)
                        event_user(1)
                }
            }
        }
        break
    case 684:
        global.msg[0] = "* Ring.../"
        global.msg[1] = "\TS \E3 \F6 \TA %"
        global.msg[2] = "\E1* Um.../"
        global.msg[3] = "\E0* I noticed you've been&  kind of quiet.../"
        global.msg[4] = "\W*\E8 Are you w-worried&  about meeting \RASGORE\W...?/"
        global.msg[5] = "\E2* .../"
        global.msg[6] = "\E0* W-well^1, don't worry^1,&  okay?/"
        global.msg[7] = "\E7* Th-the king is a&  really nice guy.../"
        global.msg[8] = "\E0* I'm sure you can&  talk to him^1, and.../"
        global.msg[9] = "* W-with your human&  soul^1, you can pass&  through the barrier!/"
        global.msg[10] = "* S-so no worrying^1, OK^1?&* J-just forget about it&  and smile./"
        global.msg[11] = "\TS \F0 \T0 %"
        global.msg[12] = "* Click.../%%"
        break
    case 685:
        doak = 0
        noroom = 0
        global.msg[0] = "* hey buddy^1, what's up^1?&* wanna buy a hot dog?/"
        global.msg[1] = "* it's only 30G.& &  Yes         No \C"
        global.msg[2] = " "
        if (global.flag[380] > 0 && global.item[7] != 0)
        {
            if (instance_number(obj_hotdog) < 30)
            {
                global.msg[0] = "* here^1.&* have fun./%%"
                if (global.flag[380] == 1)
                {
                    global.msg[0] = "* here's another hot&  dog./"
                    global.msg[1] = "* it's on the house^1.&* well^1, no^1.&* it's on you./%%"
                }
                if (doak == 0)
                {
                    with (obj_hotdoggen)
                        event_user(0)
                    global.flag[380] += 1
                }
            }
            else
            {
                global.msg[0] = "* sorry^1, thirty is&  the limit on&  head-dogs./%%"
                if (global.flag[381] == 0)
                {
                    global.msg[0] = "\TS*^1 \Tsi'll be 'frank' with&  you./"
                    global.msg[1] = "* as much as i like&  putting hot dogs&  on your head.../"
                    global.msg[2] = "* thirty is just&  an excessive number./"
                    global.msg[3] = "* twenty-nine^1, now&  that's fine^1, but&  thirty.../"
                    global.msg[4] = "* does it look like&  my arms can reach&  that high?/%%"
                }
                global.flag[381] = 1
            }
        }
        break
    case 686:
        script_execute(scr_cost, 30)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    if (global.flag[379] != 1)
                        script_execute(scr_itemget, 38)
                    if (global.flag[379] == 1)
                        script_execute(scr_itemget, 39)
                    if (noroom == 0)
                    {
                        global.gold -= 30
                        global.flag[378] += 30
                    }
                }
            }
            if (noroom == 0)
            {
                if (afford == 1)
                {
                    global.msg[0] = "* thanks, kid^1.&* here's your hot dog./%%"
                    if (global.flag[379] == 0)
                    {
                        global.msg[0] = "* thanks, kid^1.&* here's your 'dog./"
                        global.msg[1] = "* yeah^1. 'dog^1.&* apostrophe-dog^1.&* it's short for hot-dog./%%"
                    }
                    if (global.flag[379] == 1)
                    {
                        global.msg[0] = "* another h'dog^1?&* here you go.../"
                        global.msg[1] = "* whoops^1, i'm actually&  out of hot dogs./"
                        global.msg[2] = "* here^1, you can have&  a hot cat instead./%%"
                    }
                    if (global.flag[379] == 2)
                    {
                        global.msg[0] = "* another dog^1, coming&  right up.../"
                        global.msg[1] = "* ... you really like&  hot animals^1, don't&  you?/"
                        global.msg[2] = "* hey^1, i'm not judging./"
                        global.msg[3] = "* i'd be out of a job&  without folks like you./%%"
                    }
                    if (global.flag[379] == 3)
                    {
                        global.msg[0] = "* cool^1.&* here's that ''dog./"
                        global.msg[1] = "* apostrophe-apostrophe&  dog./"
                        global.msg[2] = "* it's short for&  apostrophe-dog./"
                        global.msg[3] = "* which is^1, in turn^1,&  short for.../%%"
                    }
                    if (global.flag[379] == 4)
                    {
                        global.msg[0] = "* another one^1?&* okay./"
                        global.msg[1] = "* careful^1.&* if you eat&  too many hot dogs.../"
                        global.msg[2] = "* you'll probably get&  huge like me./"
                        global.msg[3] = "* huge as in super-&  popular^1, i mean./"
                        global.msg[4] = "* i'm practically&  a hot-dog tycoon now./%%"
                    }
                    global.flag[379] += 1
                }
                if (afford == 0)
                {
                    global.msg[0] = "* whoops^1, you don't have&  enough cash./"
                    global.msg[1] = "* you should get a job^1.&* i've heard being a&  sentry pays well./%%"
                }
            }
            if (noroom == 1)
            {
                global.msg[0] = "* you're holding too much^1.&* ... guess i'll just put&  it on your head./%%"
                with (obj_hotdoggen)
                    event_user(0)
                global.flag[380] += 1
                noroom = 2
            }
        }
        if (global.choice == 1)
            global.msg[0] = "* yeah^1, you've gotta&  save your money for&  college and spiders./%%"
        break
    case 690:
        global.msg[0] = "* Ring.../"
        global.msg[1] = "\TS \E3 \F6 \TA %"
        global.msg[2] = "* L-looks like you&  beat him!/"
        global.msg[3] = "\E0* Y-you did a really&  great job out there./"
        global.msg[4] = " &  All thanks&  to you      ... \C"
        global.msg[5] = " "
        break
    case 691:
        if (global.choice == 0)
        {
            global.msg[0] = "\E3* What^1?&* Oh no^1, I mean.../"
            global.msg[1] = "\E4* You were the one&  doing everything cool!/"
            global.msg[2] = "\E0* I just wrote some&  silly programs for&  your phone./"
        }
        if (global.choice == 1)
        {
            global.msg[0] = "\E1* .../"
            global.msg[1] = "\E2* .../"
            global.msg[2] = "\E1* .../"
        }
        global.msg[3] = "\E2* .../"
        global.msg[4] = "\E4* ... umm^1, h-hey^1, this&  might sound strange^1,&  but.../"
        global.msg[5] = "\E6* ... c-can I tell&  you something?/"
        global.msg[6] = "\E9* .../"
        global.msg[7] = "\E4* B-before I met you^1,&  I d-didn't really.../"
        global.msg[8] = "\E9* I didn't really&  like myself very&  much./"
        global.msg[9] = "* For a long time^1,&  I f-felt like a&   total screw-up./"
        global.msg[10] = "\E9* L-like I couldn't&  do a-anything&  w-without.../"
        global.msg[11] = "\E9* W-without ending up&  letting everyone&  down./"
        global.msg[12] = "\E3* B-but...!/"
        global.msg[13] = "\E4* Guiding you has&  made me feel.../"
        global.msg[14] = "\E9* A lot better about&  myself./"
        global.msg[15] = "\E0* So... thanks for&  letting me help&  you./"
        global.msg[16] = "\E9* .../"
        global.msg[17] = "\E4* Uhhh^1, anyway^1, we're&  almost to the CORE./"
        global.msg[18] = "\E0* It's just past&  MTT Resort./"
        global.msg[19] = "\E6* Come on^1!&* Let's finish this!/%%"
        global.msg[20] = "\TS \F0 \T0 %"
        global.msg[21] = "* Click.../%%"
        break
    case 692:
        global.msg[0] = "\E0EUREKA!!!/"
        global.msg[1] = "I'VE FIGURED OUT&THE PUZZLE!!!/"
        global.msg[2] = "\E3YOU SEEM LIKE&YOU'RE HAVING&FUN^1, THOUGH.../"
        global.msg[3] = "\E0DO YOU ABSOLUTELY^1,&DAPSOLUTELY WANT&THE SOLUTION???/"
        global.msg[4] = "\TS \F0 \T0 %"
        global.msg[5] = "* (Do you absolutely^1,&  dapsolutely want the answer?)&         Yes         No\C"
        global.msg[6] = " "
        break
    case 693:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.msg[1] = "THE^1!&SOLUTION^1!&IS!/"
            global.msg[2] = "(PLEASE IMAGINE&A DRUMROLL IN&YOUR HEAD)/"
            global.msg[3] = "... THAT TREE&OVER THERE HAS&A SWITCH ON IT!/"
            global.msg[4] = "CHECK IT&OUTIE!!!/%%"
        }
        if (global.choice == 1)
        {
            global.msg[1] = "WOW..^1.&YOU'RE TRULY A&PUZZLE PASSIONEER!/"
            global.msg[2] = "I'M SO ENTHUSED&BY YOUR&ENTHUSIASM!!!/"
            global.msg[3] = "YOU CAN DO IT^1,&HUMAN!!!/%%"
        }
        break
    case 694:
        global.msg[0] = "I'VE FIGURED OUT&THE PUZZLE!!!/"
        global.msg[1] = "\E0DO YOU ABSOLUTELY^1,&DAPSOLUTELY WANT&THE SOLUTION???/"
        global.msg[2] = "\TS \F0 \T0 %"
        global.msg[3] = "* (Do you absolutely^1,&  dapsolutely want the answer?)&         Yes         No\C"
        global.msg[4] = " "
        break
    case 695:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            global.msg[1] = "THE^1!&SOLUTION^1!&IS!/"
            global.msg[2] = "(PLEASE IMAGINE&A DRUMROLL IN&YOUR HEAD)/"
            global.msg[3] = "... THAT TREE&OVER THERE HAS&A SWITCH ON IT!/"
            global.msg[4] = "CHECK IT&OUTIE!!!/%%"
        }
        if (global.choice == 1)
        {
            global.msg[1] = "WOW..^1.&YOU'RE TRULY A&PUZZLE PASSIONEER!/"
            global.msg[2] = "I'M SO ENTHUSED&BY YOUR&ENTHUSIASM!!!/"
            global.msg[3] = "YOU CAN DO IT^1,&HUMAN!!!/%%"
        }
        break
    case 696:
        global.msg[0] = "* (There's a switch on the&  trunk of this tree.)/"
        global.msg[1] = "* (Press it?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 697:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            if (obj_xoxocontroller1.fvic != 1)
            {
                obj_xoxocontroller1.fvic = 1
                snd_play(snd_switchpull_n)
            }
        }
        break
    case 698:
        global.msg[0] = "OHO^1!&THE HUMAN ARRIVES!/"
        global.msg[1] = "ARE YOU READY TO&HANG OUT WITH&UNDYNE?/"
        global.msg[2] = "I HAVE A PLAN&TO MAKE YOU TWO&GREAT FRIENDS!/"
        global.msg[3] = "\TS \F0 \T0 %"
        global.msg[4] = "* (Will you hang out?)& &         Yes         No\C"
        global.msg[5] = " "
        break
    case 699:
        scr_papface(0, 3)
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = "OKAY^1!&STAND BEHIND ME!/%%"
            if instance_exists(obj_undynedate_outside)
                obj_undynedate_outside.con = 5
        }
        if (global.choice == 1)
        {
            global.msg[1] = "HMMM..^1.&STILL GETTING&READY?/"
            global.msg[2] = "\E0TAKE YOUR TIME!/%%"
        }
        break
    case 700:
        global.msg[0] = "OKAY^1!&ALL READIED-UP&TO HANG OUT!?/"
        global.msg[1] = "\TS \F0 \T0 %"
        global.msg[2] = "* (Will you hang out?)& &         Yes         No\C"
        global.msg[3] = " "
        break
    case 701:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = "OKAY^1!&STAND BEHIND ME!/%%"
            if instance_exists(obj_undynedate_outside)
                obj_undynedate_outside.con = 5
        }
        if (global.choice == 1)
            global.msg[1] = "TAKE YOUR TIME!/%%"
        break
    case 702:
        scr_papface(0, 0)
        if (global.choice == 0)
        {
            scr_papface(0, 0)
            global.msg[1] = "OKAY^1!&STAND BEHIND ME!/%%"
            if instance_exists(obj_undynedate_outside)
                obj_undynedate_outside.con = 5
        }
        if (global.choice == 1)
            global.msg[1] = "TAKE YOUR TIME!/%%"
        break
    case 703:
        global.msg[0] = "\E4* .../"
        global.msg[1] = "\E5* So why are YOU&  here?/"
        global.msg[2] = "\E4* To rub your victory&  in my face?/"
        global.msg[3] = "\E4* To humiliate me&  even further?/"
        global.msg[4] = "\E1* IS THAT IT? & &  Yes         No\C"
        global.msg[5] = " "
        break
    case 704:
        if (global.choice == 0)
        {
            global.msg[0] = "\E2* Oh-ho-ho-ho./"
            global.msg[1] = "\E1* Well^1, I've got news&  for you^1, BRAT./"
            global.msg[2] = "\E2* You're on MY&  battlefield now./"
            global.msg[3] = "\E3* And you AREN'T&  going to&  humiliate me./"
            global.msg[4] = "\E3* I'll TELL you&  what's going to&  happen./"
            global.msg[5] = "\E0* We're going to&  hang out./"
            global.msg[6] = "\E2* We're going to&  have a good&  time./"
            global.msg[7] = "\M1* We're going to&  become " + chr(34) + "friends." + chr(34) + "/"
            global.msg[8] = "\E3* You'll become so&  enamored with me.../"
            global.msg[9] = "\E1* YOU'LL be the one&  feeling humiliated&  for your actions!/"
            global.msg[10] = "\E6* Fuhuhuhuhu!!/"
            global.msg[11] = "\M2* It's the perfect&  revenge!!!/"
            global.msg[12] = "\E1* Err.../"
            global.msg[13] = "\E9* Why don't you&  have a seat?/%%"
            if instance_exists(obj_undynedate_inside)
                obj_undynedate_inside.con = 50
        }
        if (global.choice == 1)
        {
            global.msg[0] = "\E4* Then why are you&  here?/"
            global.msg[1] = "\E1* ...!/"
            global.msg[2] = "\E2* Wait^1, I get it./"
            global.msg[3] = "\E3* You think that I'm&  gonna be friends&  with you^1, huh?/"
            global.msg[4] = "* Right???&              NEVER &  Yes         with you\C"
            global.msg[5] = " "
        }
        break
    case 705:
        if (global.choice == 0)
        {
            global.msg[0] = "\E6* Really^1?&* How delightful!^1!&* I accept!/"
            global.msg[1] = "* Let's all frolick&  in the fields&  of friendship!/"
            global.msg[2] = "\E2* ...NOT!/"
            global.msg[3] = "\E2* Why would I EVER&  be friends with&  YOU!?/"
            global.msg[4] = "\E3* If you weren't my&  houseguest^1, I'd beat&  you up right now!/"
            global.msg[5] = "\E0* You're the enemy&  of everyone's hopes&  and dreams!/"
            global.msg[6] = "\E1* I WILL NEVER&  BE YOUR FRIEND./"
            global.msg[7] = "\E3* Now get out of&  my house!/%%"
            if instance_exists(obj_undynedate_inside)
                obj_undynedate_inside.con = 40
        }
        if (global.choice == 1)
        {
            global.msg[0] = "\E1* WHAT?/"
            global.msg[1] = "\E4* First you parade&  into my house^1,&  then you INSULT me?/"
            global.msg[2] = "\E2* You little BRAT^1!&* I have half a&  mind to.../"
            global.msg[3] = "\E1* .../"
            global.msg[4] = "\E3* Wait./"
            global.msg[5] = "\E2* I'll prove you&  WRONG./"
            global.msg[6] = "\E3* We ARE going to&  be friends./"
            global.msg[7] = "\E1* In fact.../"
            global.msg[8] = "\E3* We./"
            global.msg[9] = "\M1* Are going to be&  BESTIES./"
            global.msg[10] = "* I'll make you like&  me so much.../"
            global.msg[11] = "\E1* Your WHOLE LIFE&  will revolve around&  me!!/"
            global.msg[12] = "\M2* It's the perfect&  revenge!!!/"
            global.msg[13] = "\E6* FUHUHUHUHU!!!/"
            global.msg[14] = "\E1* Err.../"
            global.msg[15] = "\E9* Now^1, why don't&  you have a seat?/%%"
            if instance_exists(obj_undynedate_inside)
                obj_undynedate_inside.con = 50
        }
        break
    case 706:
        global.msg[0] = "* (Sit down and progress?)& &         Yes         No\C"
        global.msg[1] = " "
        break
    case 707:
        global.msg[0] = " %%"
        if (global.choice == 0)
            obj_undynedate_inside.con = 60
        if (global.choice == 1)
        {
            global.msg[0] = " %%"
            global.msg[1] = " %%"
        }
        break
    case 708:
        global.msg[0] = "* That sugar's for&  the tea./"
        global.msg[1] = "\E2* I'm not gonna give&  you a cup of&  sugar!/"
        global.msg[2] = "\E6* What do I look&  like^1, the ice-cream&  woman?/"
        global.msg[3] = "\E2* Do human ice-cream&  women TERRORIZE HUMANITY&  with ENERGY SPEARS?/"
        global.msg[4] = "\E3* Are their ice-cream&  songs a PRELUDE TO&  DESTRUCTION?/"
        global.msg[5] = "\E1* IS THAT IT? & &  Yes         No\C"
        global.msg[6] = " "
        break
    case 709:
        if (global.choice == 0)
        {
            global.msg[0] = "\E1* ... what^1?&* REALLY?/"
            global.msg[1] = "\E6* That rules!!!/%%"
        }
        if (global.choice == 1)
            global.msg[0] = "\E3* That's what I&  thought./%%"
        break
    case 710:
        global.msg[0] = "* Envision these&  vegetables as your&  greatest enemy!/"
        global.msg[1] = "\E2* Now!^1!&* Pound them to dust&  with your fists!!/"
        global.msg[2] = "\TS \F0 \T0 %"
        global.msg[3] = "* (How will you pound?)& &         Strong      Wimpy\C"
        global.msg[4] = " "
        break
    case 711:
        if (global.choice == 0)
        {
            obj_undynedate_inside.con = 140
            global.msg[0] = "* (You punch the vegetables&  at full force^1.&* You knock over a tomato.)/"
            scr_undface(1, 6)
            global.msg[2] = "* YEAH^1!&* YEAH!/"
            global.msg[3] = "\E1* Our hearts are&  uniting against these&  healthy ingredients!/"
            global.msg[4] = "\M2* NOW IT'S MY TURN!/"
            global.msg[5] = "* NGAHHH!/%%"
        }
        if (global.choice == 1)
        {
            obj_undynedate_inside.con = 141
            global.msg[0] = "* (You pet the vegetables&  in an affectionate&  manner.)/"
            scr_undface(1, 1)
            global.msg[2] = "* OH MY GOD!!^1!&* STOP PETTING THE&  ENEMY!!!/"
            global.msg[3] = "\M2* I'll show you&  how it's done!/"
            global.msg[4] = "* NGAHHH!/%%"
        }
        break
    case 712:
        global.msg[0] = "* ... we add the&  noodles!/"
        global.msg[1] = "\E0* Homemade noodles&  are the best!/"
        global.msg[2] = "\E6* BUT I JUST BUY&  STORE-BRAND!/"
        global.msg[3] = "\M2* THEY'RE THE&  CHEAPEST!!!/"
        global.msg[4] = "\E1* NGAHHHHHHHHH&  HHHHHHHHHH!!!/"
        global.msg[5] = "\E9* Uhh^1, just put them&  in the pot./"
        global.msg[6] = "\TS \F0 \T0 %"
        global.msg[7] = "\M0* (How will you put them in?)& &         Fiercely    Careful\C"
        global.msg[8] = " "
        break
    case 713:
        if (global.choice == 0)
        {
            obj_undynedate_inside.con = 199
            global.msg[0] = "* (You throw everything into&  the pot as hard as you can^1,&  including the box.)/"
            global.msg[1] = "* (It clanks against the&  empty bottom.)/"
            scr_undface(2, 6)
            global.msg[3] = "\M2* YEAH!!^1!&* I'M INTO IT!!!/%%"
        }
        if (global.choice == 1)
        {
            obj_undynedate_inside.con = 200
            global.msg[0] = "* (You place the noodles&  in one at a time.)/"
            global.msg[1] = "* (They clank against the&  empty bottom.)/"
            scr_undface(2, 9)
            global.msg[3] = "* Nice???/%%"
        }
        break
    case 714:
        global.msg[0] = "\E0* Humans suck^1, but&  their history..^1.&* Kinda rules./"
        global.msg[1] = "\E2* Case in point^1:&* This giant sword!/"
        global.msg[2] = "\E0* Historically^1, humans&  wielded swords up&  to 10x their size./"
        global.msg[3] = "\E1* RIGHT?& &  True        False\C"
        global.msg[4] = " "
        break
    case 715:
        if (global.choice == 0)
        {
            global.msg[0] = "\E6* Heh^1, I knew it!/"
            global.msg[1] = "\E2* When I first heard&  that^1, I immediately&  wanted one!/"
            global.msg[2] = "\E0* So me and Alphys&  built a giant&  sword together./"
            global.msg[3] = "\E0* She figured out all&  the specs herself.../"
            global.msg[4] = "\E6* She's smart^1, huh!?/%%"
        }
        if (global.choice == 1)
        {
            global.msg[0] = "\E2* Pfft^1!&* You liar!/"
            global.msg[1] = "\E3* I've READ Alphys's&  human history book&  collection!/"
            global.msg[2] = "\E3* I know all about&  your giant swords.../"
            global.msg[3] = "\E3* Your colossal^1,&  alien-fighting&  robots.../"
            global.msg[4] = "* Your supernatural&  princesses.../"
            global.msg[5] = "\E6* Heh^1! There's no&  way you're gonna&  fool me!!!/%%"
        }
        break
    case 716:
        global.msg[0] = "* (Look inside the bone drawer?)& &         Yes         No\C"
        global.msg[1] = " "
        break
    case 717:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            obj_bonedrawer_check.con = 5
        }
        break
    case 720:
        global.msg[0] = "\M5* WHAT A SENSATIONAL OPPORTUNITY&  FOR A STORY!/"
        global.msg[1] = "\M3* I CAN SEE THE HEADLINE NOW:/"
        global.msg[2] = "\M4* " + chr(34) + "A DOG EXISTS SOMEWHERE." + chr(34) + "/"
        global.msg[3] = "\M2* FRANKLY^1, I'M BLOWN AWAY./"
        global.msg[4] = "* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[5] = " "
        break
    case 721:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 1
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 722:
        global.msg[0] = "\M5* THIS DOG..^1.&* STILL EXISTS!/"
        global.msg[1] = "* THIS STORY..^1.&* JUST KEEPS GETTING&  BETTER AND BETTER!/"
        global.msg[2] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[3] = " "
        break
    case 723:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 1
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 724:
        global.msg[0] = "\M5* OH MY!!!!/"
        global.msg[1] = "\M2* ... IT'S A COMPLETELY&  NONDESCRIPT GLASS OF WATER./"
        global.msg[2] = "\M4* BUT ANYTHING CAN MAKE&  A GREAT STORY WITH ENOUGH&  SPIN!/"
        global.msg[3] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[4] = " "
        break
    case 725:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 2
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 726:
        global.msg[0] = "\M3* I'M HONORED TO BE IN THE&  PRESENCE OF SUCH A HUGE&  LUKEWARM WATER FAN^1, FOLKS!/"
        global.msg[1] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[2] = " "
        break
    case 727:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 2
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 728:
        global.msg[0] = "\M5* OH NO!!^1!&* THAT MOVIE SCRIPT!!^1!&* HOW'D??^1? THAT GET THERE???/"
        global.msg[1] = "\M4* IT'S A SUPER-JUICY SNEAK&  PREVIEW OF MY LATEST&  GUARANTEED-NOT-TO-BOMB FILM:/"
        global.msg[2] = "\M6* METTATON THE MOVIE XXVIII..^1.&  STARRING METTATON!/"
        global.msg[3] = "\M1* I'VE HEARD THAT LIKE THE&  OTHER FILMS.../"
        global.msg[4] = "\M1* IT CONSISTS MOSTLY OF A SINGLE&  FOUR-HOUR SHOT OF ROSE PETALS&  SHOWERING ON MY RECLINING BODY./"
        global.msg[5] = "\M5* OOH!!^1!&* BUT THAT'S!!^1!&* NOT CONFIRMED!!/"
        global.msg[6] = "\M5* YOU WOULDN'T (COUGH) SPOIL MY&  MOVIE FOR EVERYONE WITH A&  PROMOTIONAL STORY^1, WOULD YOU?/"
        global.msg[7] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[8] = " "
        break
    case 729:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 3
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
        {
            global.msg[0] = "\M5* PHEW!!^1! THAT WAS CLOSE!^1!&* YOU ALMOST GAVE ME A BUNCH&  OF FREE ADVERTISEMENT!!/%%"
            global.msg[1] = "\M2 %%"
        }
        break
    case 730:
        global.msg[0] = "\M3* OH^1!&* YOU'RE BACK!/"
        global.msg[1] = "\M6* THAT'S RIGHT^1, FOLKS^1!&* IT SEEMS NO ONE CAN RESIST&  THE ALLURE OF MY NEW FILM!/"
        global.msg[2] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[3] = " "
        break
    case 731:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 3
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 732:
        global.msg[0] = "\M4* BASKETBALL'S A BLAST^1, ISN'T IT^1,&  DARLING?/"
        global.msg[1] = "\M1* TOO BAD YOU CAN'T PLAY WITH&  THESE BALLS./"
        global.msg[2] = "\M4* THEY'RE MTT-BRAND FASHION&  BASKETBALLS^1.&* FOR WEARING^1, NOT PLAYING./"
        global.msg[3] = "\M6* YOU CAN'T GET RICH AND FAMOUS&  LIKE MOI WITHOUT BEAUTIFYING&  A FEW ORBS./"
        global.msg[4] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[5] = " "
        break
    case 733:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 4
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 734:
        global.msg[0] = "* IT SEEMS OUR REPORTER IS DRAWN&  TO SPORTS LIKE MOTHS TO A&  FLAMING BASKETBALL HOOP./"
        global.msg[1] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[2] = " "
        break
    case 735:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 4
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 736:
        global.msg[0] = "\M5* OH MY^1! IT'S A PRESENT^1!&* AND IT'S ADDRESSED TO YOU^1,&  DARLING!/"
        global.msg[1] = "\M6* AREN'T YOU JUST BURSTING&  WITH EXCITEMENT?/"
        global.msg[2] = "\M5* WHAT COULD BE INSIDE^1?&* WELL^1, NO TIME LIKE THE&  " + chr(34) + "PRESENT" + chr(34) + " TO FIND OUT!/"
        global.msg[3] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[4] = " "
        break
    case 737:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 5
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 738:
        global.msg[0] = "\M4* READY FOR YOUR..^1.&* PRESENTATION?/"
        global.msg[1] = "\M4* (... LET'S CUT THAT ONE IN&  POST.)/"
        global.msg[2] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[3] = " "
        break
    case 739:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 5
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 740:
        global.msg[0] = "\M5* OOH LA LA^1!&* THIS VIDEO GAME YOU FOUND..^1.&* IS DYNAMITE!!!/"
        global.msg[1] = "\M4* THOUGH I DON'T MAKE AN&  APPEARANCE IN IT UNTIL&  THREE-FOURTHS IN./"
        global.msg[2] = "\M3* BUT I LIKE THAT./"
        global.msg[3] = "\M6* APPEARING FROM THE HEAVENS LIKE&  MANNA^1, SLAKING THE AUDIENCE'S&  HUNGER FOR GORGEOUS ROBOTS.../"
        global.msg[4] = "\M5* OOH^1!&* THAT'S METTATON!/"
        global.msg[5] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[6] = " "
        break
    case 741:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 6
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 742:
        global.msg[0] = "* AH^1, YOU UNDERSTAND./"
        global.msg[1] = "* THIS IS A GAME WHERE YOU&  SHOULD CHECK EVERYTHING&  TWICE./"
        global.msg[2] = "\M2* (REPORT THIS ONE?)& &         Report      Look More\C"
        global.msg[3] = " "
        break
    case 743:
        if (global.choice == 0)
        {
            global.msg[0] = " %%"
            obj_mettnewsevent.eventchoice = 6
            obj_mettnewsevent.con = 50
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 744:
        doak = 0
        noroom = 0
        global.msg[0] = "* Buy a Spider Cider for 9999G?& &         Yes         No \C"
        global.msg[1] = " "
        break
    case 745:
        script_execute(scr_cost, 9999)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 10)
                    if (noroom == 0)
                    {
                        global.gold -= 9999
                        global.flag[59] += 9999
                        global.flag[403] = 1
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = "* Some spiders crawled down&  and gave you a jug./%%"
            if (afford == 0)
                global.msg[0] = "* You didn't have enough&  gold./%%"
        }
        if (noroom == 1)
            global.msg[0] = "* You are carrying too&  many items./%%"
        if (global.choice == 1)
            global.msg[0] = "*%%"
        break
    case 746:
        doak = 0
        noroom = 0
        global.msg[0] = "* Buy a Spider Donut for 9999G?& &         Yes         No \C"
        global.msg[1] = " "
        break
    case 747:
        script_execute(scr_cost, 9999)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 7)
                    if (noroom == 0)
                    {
                        global.gold -= 9999
                        global.flag[59] += 9999
                        global.flag[403] = 1
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = "* Some spiders crawled down&  and gave you a donut./%%"
            if (afford == 0)
                global.msg[0] = "* You didn't have enough&  gold./%%"
        }
        if (noroom == 1)
            global.msg[0] = "* You are carrying too&  many items./%%"
        if (global.choice == 1)
            global.msg[0] = "*%%"
        break
    case 748:
        global.msg[0] = "* Ribbit^1, ribbit.&* (I have heard you are quite&  merciful^1, for a human...)/"
        global.msg[1] = "\W* (Surely you know by now a&  monster wears a \YYELLOW\W name&  when you can \YSPARE\W it.)/"
        global.msg[2] = "* (What do you think of that?)&         Very        It's&         Helpful     Bad\C"
        global.msg[3] = " "
        break
    case 749:
        if (global.choice == 0)
        {
            global.msg[0] = "* (It is rather helpful.^1)&* (Remember^1, sparing is just&  saying you won't fight.)/"
            global.msg[1] = "* (Maybe one day^1, you'll&  have to do it even if&  their name isn't yellow.)/%%"
        }
        if (global.choice == 1)
        {
            global.msg[0] = "* (Really^1? Then^1, I'll tell all&  of my friends to tell&  their friends' friends...)/"
            global.msg[1] = "* (Never use yellow names.)&* (How about that?)/"
            global.msg[2] = "         Keep        No more&         Yellow      Yellow&         Names       Names\C"
            global.msg[3] = " "
        }
        break
    case 750:
        if (global.choice == 0)
            global.msg[0] = "* (OK^1, they will still&  use yellow names.)/%%"
        if (global.choice == 1)
        {
            global.msg[0] = "* (OK^1, I will let them&  know not to use yellow&  names.)/%%"
            global.flag[22] = 1
        }
        break
    case 751:
        global.msg[0] = "* Ribbit^1, ribbit^1.&* (How are you doing without&  yellow names?)/"
        global.msg[1] = "                     Bring &         It's        Them&         great       Back\C"
        global.msg[2] = " "
        break
    case 752:
        if (global.choice == 0)
        {
            global.msg[0] = "* (Glad to hear it.^1)&* (Though^1, I do not know why&  you dislike yellow.)/"
            global.msg[1] = "* (You had better hope you do&  not encounter a banana-themed&  monster.)/%%"
        }
        if (global.choice == 1)
        {
            global.msg[0] = "* (Huh^1? It's rather inconvenient&  that you changed your mind&  like this.)/"
            global.msg[1] = "* (Since I told everyone&  not to use yellow names^1,&  everyone threw theirs out.)/"
            global.msg[2] = "* (This is really troubling...^1)&* (Hmmm...)/"
            global.msg[2] = "\W* (Well^1, last year it was&  fashionable to have \ppink\W &  names.)/"
            global.msg[3] = "* (I think everyone still&  has those in their closets&  somewhere...)/"
            global.msg[4] = "* (I'll ask everyone to look.^1)&* (But this is the last time!)/%%"
            global.flag[22] = 2
        }
        break
    case 753:
        global.msg[0] = "* Ribbit^1, ribbit...&* (I hope you're satisfied.)/%%"
        break
    case 754:
        global.msg[0] = "* \YNAPSTABLOOK22 has sent you&  a friend request.\W /"
        global.msg[1] = "* Accept the request?& &         Accept      Reject\C"
        global.msg[2] = " "
        break
    case 755:
        global.flag[409] = 1
        global.msg[0] = "* (It seems to have already&  rejected itself...)/%%"
        break
    case 756:
        global.msg[0] = "* \YMETTATON has sent you a&  Mortal Enemy request.\W /"
        global.msg[1] = "* Accept the request?& &         Accept      Reject\C"
        global.msg[2] = " "
        break
    case 757:
        if (global.choice == 0)
        {
            global.msg[0] = "* Congratulations^1!&* You are now Mortal Enemies&  with Mettaton./"
            global.msg[1] = "* \YCOOLSKELETON95\W has posted&  a comment on this change./"
            global.msg[2] = "* CONGRATULATIONS^1, YOU TWO^1!&* WISH YOU A LONG AND&  HORRIBLE RIVALRY./%%"
        }
        if (global.choice == 1)
            global.msg[0] = "* You rejected the request./%%"
        break
    case 758:
        global.msg[0] = "* \YMETTATON has sent you an&  invitation to " + chr(34) + "Die." + chr(34) + "\W /"
        global.msg[1] = "* RSVP?& &         Respond     Ignore\C"
        global.msg[2] = " "
        break
    case 759:
        if (global.choice == 0)
            global.msg[0] = "* Bepis valley Granola Bars/%%"
        if (global.choice == 1)
            global.msg[0] = "* Bepis valley Granola Bars/%%"
        break
    case 760:
        global.msg[0] = "* hey^1.&* i heard you're going&  to the core./"
        global.msg[1] = "\E0* how about grabbing some&  dinner with me first?&  Yeah        I'm busy \C"
        global.msg[2] = " "
        if (global.flag[67] == 1)
            global.msg[0] = "* .../%%"
        break
    case 761:
        if (global.choice == 0)
        {
            global.msg[0] = "* great^1, thanks for&  treating me./%%"
            if instance_exists(obj_sans_prefinaldate)
                obj_sans_prefinaldate.con = 1
        }
        if (global.choice == 1)
            global.msg[0] = "* well^1, have fun in&  there./%%"
        break
    case 762:
        global.msg[0] = "* This is the barrier./"
        global.msg[1] = "* This is what keeps&  us all trapped&  underground./"
        global.msg[2] = "* .../"
        global.msg[3] = "* If.../"
        global.msg[4] = "* If by chance you&  have any unfinished&  business.../"
        global.msg[5] = "* Please do what you&  must./"
        global.msg[6] = " & &         Continue    Go Back\C"
        global.msg[7] = " "
        break
    case 763:
        if (global.choice == 0)
        {
            obj_barrierevent.con = 10
            global.msg[0] = "* ..^2.&* ... I see.../"
            global.msg[1] = "* This is it^1, then./%%"
        }
        if (global.choice == 1)
        {
            global.flag[457] = 1
            obj_barrierevent.con = 40
            global.msg[0] = "* I see./"
            global.msg[1] = "* Anything you want to&  do is important&  enough./"
            global.msg[2] = "* Even something as small&  as reading a book^1,&  or taking a walk.../"
            global.msg[3] = "* Please take your time./%%"
        }
        break
    case 764:
        global.msg[0] = "* Oh..^1.&* Back so soon?/"
        global.msg[1] = "* How are you feeling?/"
        global.msg[2] = " & &         Ready       Go Back\C"
        global.msg[3] = " "
        break
    case 765:
        if (global.choice == 0)
        {
            obj_barrierevent.con = 10
            global.msg[0] = "* ..^2.&* ... I see.../"
            global.msg[1] = "* This is it^1, then./%%"
        }
        if (global.choice == 1)
        {
            obj_barrierevent.con = 40
            global.msg[0] = "* I see./"
            global.msg[1] = "* Do what you have to./%%"
        }
        break
    case 770:
        global.msg[0] = "* Tra la la^1.&* I am the riverman./"
        global.msg[1] = "* Or am I the riverwoman...^1?&* It doesn't really matter./"
        global.msg[2] = "* I love to ride in my boat^1.&* Would you care to join me?/"
        global.msg[3] = "* (Ride in the boat?)& &         Yes         No\C"
        global.msg[4] = " "
        if (global.flag[460] > 0)
        {
            global.msg[0] = "* Tra la la^1.&* Care for a ride?/"
            global.msg[1] = "* (Ride in the boat?)& &         Yes         No\C"
            global.msg[2] = " "
        }
        break
    case 771:
        if (global.choice == 0)
        {
            global.msg[0] = "* Where will we go today?& &         Error       Error\C"
            if (room == room_fire_dock)
                global.msg[0] = "* Where will we go today?& &         Snowdin     Waterfall\C"
            if (room == room_water_dock)
                global.msg[0] = "* Where will we go today?& &         Snowdin     Hotland\C"
            if (room == room_tundra_dock)
                global.msg[0] = "* Where will we go today?& &         Waterfall   Hotland\C"
            global.msg[1] = " "
        }
        if (global.choice == 1)
            global.msg[0] = "* Then perhaps another time^1.&* Or perhaps not^1.&* It doesn't really matter./%%"
        break
    case 772:
        if (global.choice == 0)
        {
            if (room == room_fire_dock || room == room_water_dock)
                global.flag[459] = 1
            if (room == room_tundra_dock)
                global.flag[459] = 2
        }
        if (global.choice == 1)
        {
            if (room == room_tundra_dock || room == room_water_dock)
                global.flag[459] = 3
            if (room == room_fire_dock)
                global.flag[459] = 2
        }
        if instance_exists(obj_dogboat_thing)
            obj_dogboat_thing.con = 0.1
        global.msg[0] = "* Then we're off.../%%"
        break
    case 780:
        if (global.flag[490] == 0)
        {
            global.msg[0] = "* (There's a switch on the&  wall.)/"
            global.msg[1] = "* (Press it?)& &         Yes         No\C"
            global.msg[2] = " "
        }
        else
            global.msg[0] = "* (The switch doesn't do&  anything.)/%%"
        break
    case 781:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            global.flag[490] = 1
            if instance_exists(obj_amalgam_dogevent)
                obj_amalgam_dogevent.con = 50
        }
        break
    case 782:
        global.msg[0] = "* (Seems like a comfortable&  bed.)/"
        global.msg[1] = "* (Lie on it?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 783:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            if instance_exists(obj_bed_watcher)
                obj_bed_watcher.con = 5
        }
        break
    case 784:
        if (global.flag[484] > 1)
            global.msg[0] = "* (It's just a regular suspicious&  bed now.)/%%"
        if (global.flag[484] == 1)
        {
            global.msg[0] = "* (It's a yellow key.^1)&* (You put it on your&  keychain.)/%%"
            global.flag[484] = 2
        }
        if (global.flag[484] == 0)
        {
            global.msg[0] = "* (There's something under&  the sheets.)/"
            global.msg[1] = "* (Check it out?)& &         Yes         No\C"
            global.msg[2] = " "
        }
        break
    case 785:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            if (global.flag[484] == 0)
            {
                snd_play(snd_noise)
                global.flag[484] = 1
            }
        }
        break
    case 786:
        global.msg[0] = "* (The power has been turned&  on.)/%%"
        if (global.flag[491] == 0)
        {
            global.msg[0] = "* (It seems like this controls&  the elevator's power.)/"
            global.msg[1] = "* (Turn it on?)& &         Yes         No\C"
            global.msg[2] = " "
        }
        break
    case 787:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            with (obj_mainchara)
                uncan = 1
            if instance_exists(obj_lab_powerswitch)
            {
                global.flag[491] = 1
                obj_lab_powerswitch.con = 5
            }
        }
        break
    case 800:
        global.msg[0] = (("\E7* I always was a crybaby^1,&  wasn't I^1, " + global.charname) + "?/")
        global.msg[1] = "\E1* .../"
        global.msg[2] = "\E2* ... I know./"
        global.msg[3] = (("\E0* You're not actually&  " + global.charname) + ", are you?/")
        global.msg[4] = (("\E7* " + global.charname) + "'s been gone for&  a long time./")
        global.msg[5] = "* .../"
        global.msg[6] = "\E9* Um..^1. what.../"
        global.msg[7] = "\E0* What IS your name?/"
        global.msg[8] = "\E2* .../"
        global.msg[9] = "\E5* " + chr(34) + "Frisk?" + chr(34) + "/"
        global.msg[10] = "\E7* That's.../"
        global.msg[11] = "\E5* A nice name./"
        global.msg[12] = "* .../"
        global.msg[13] = "\E7* Frisk.../"
        global.msg[14] = "\E0* I haven't felt like&  this for a long time./"
        global.msg[15] = "\E2* As a flower^1, I was&  soulless./"
        global.msg[16] = "\E1* I lacked the power to&  love other people./"
        global.msg[17] = "\E2* However^1, with everyone's&  souls inside me.../"
        global.msg[18] = "\E7* I not only have my own&  compassion back.../"
        global.msg[19] = "\E5* But I can feel every&  other monster's as&  well./"
        global.msg[20] = "\E7* They all care about&  each other so much./"
        global.msg[21] = "\E0* And..^1. they care about&  you too^1, Frisk./"
        global.msg[22] = "* .../"
        global.msg[23] = "\E7* I wish I could tell&  you how everyone&  feels about you./"
        global.msg[24] = "* Papyrus..^1. Sans..^1.&* Undyne..^1. Alphys.../"
        global.msg[25] = "\E0* ... Toriel./"
        global.msg[26] = "\E7* Monsters are weird./"
        global.msg[27] = "\E5* Even though they barely&  know you.../"
        global.msg[28] = "\E6* It feels like they&  all really love&  you./"
        global.msg[29] = "\E8* Haha./"
        global.msg[30] = "* .../"
        global.msg[31] = "\E1* Frisk..^1. I..^1.&* I understand if you&  can't forgive me./"
        global.msg[32] = "* I understand if you&  hate me./"
        global.msg[33] = "* I acted so strange and&  horrible./"
        global.msg[34] = "\E3* I hurt you./"
        global.msg[35] = "* I hurt so many people./"
        global.msg[36] = "\E1* Friends^1, family^1,&  bystanders.../"
        global.msg[37] = "\E3* There's no excuse for&  what I've done./"
        global.msg[38] = " & &  Forgive     Do not\C"
        global.msg[39] = " "
        break
    case 801:
        if (global.choice == 0)
        {
            global.msg[0] = "\E3* Wh..^1. what?/"
            global.msg[1] = "\E7* ... Frisk^1, come on./"
            global.msg[2] = "\E0* You're..^1.&* You're gonna make me&  cry again./"
            global.msg[3] = "\E7* ... besides^1, even if&  you do forgive me.../"
        }
        if (global.choice == 1)
        {
            global.msg[0] = "\E2* ... right^1./"
            global.msg[1] = "* I understand./"
            global.msg[2] = "\E1* I just hope that.../"
            global.msg[3] = "* I can make up for&  it a little right&  now./"
        }
        global.msg[4] = "\E1* I can't keep these&  souls inside of me./"
        global.msg[5] = "\E0* The least I can do&  is return them./"
        global.msg[6] = "\E2* But first.../"
        global.msg[7] = "\E4* There's something I&  have to do./"
        global.msg[8] = "* Right now^1, I can feel&  everyone's hearts&  beating as one./"
        global.msg[9] = "* They're all burning&  with the same&  desire./"
        global.msg[10] = "* With everyone's power..^1.&* With everyone's&  determination.../"
        global.msg[11] = "* It's time for&  monsters.../"
        global.msg[12] = "* To finally go free./%%"
        break
    case 803:
        global.msg[0] = "\E7* Frisk.../"
        global.msg[1] = "\E0* I have to go now./"
        global.msg[2] = "\E7* Without the power of&  everyone's souls.../"
        global.msg[3] = "\E1* I can't keep&  maintaining this&  form./"
        global.msg[4] = "* In a little while.../"
        global.msg[5] = "* I'll turn back into&  a flower./"
        global.msg[6] = "\E3* I'll stop being&  " + chr(34) + "myself." + chr(34) + "/"
        global.msg[7] = "* I'll stop being able&  to feel love again./"
        global.msg[8] = "\E1* So..^1. Frisk./"
        global.msg[9] = "\E7* It's best if you&  just forget about&  me^1, OK?/"
        global.msg[10] = "\E0* Just go be with&  the people who&  love you./"
        global.msg[11] = "\TS \F0 \T0 %"
        global.msg[12] = " &         Comfort&         him         Do not\C"
        global.msg[13] = " "
        break
    case 804:
        global.msg[0] = " %%"
        if instance_exists(obj_asriel_overworldanim)
        {
            if (global.choice == 0)
                obj_asriel_overworldanim.con = 28
            if (global.choice == 1)
                obj_asriel_overworldanim.con = 50
        }
        break
    case 806:
        global.msg[0] = "\E0* So^1, Alphys.../"
        global.msg[1] = "\E9* What do you want&  to do now that&  we're all free?/"
        global.msg[2] = "\E0* We have the whole&  world to explore&  now./"
        scr_alface(3, 3)
        global.msg[4] = "\E3* W-well^1, of course&  I'm going to go&  out and.../"
        global.msg[5] = "\E4* Um.../"
        global.msg[6] = "\E3* No^1, I should be&  honest!!/"
        global.msg[7] = "\E1* I'm gonna stay inside&  and watch anime like&  a total loser!/"
        scr_papface(8, 0)
        global.msg[9] = "\E0THAT'S THE SPIRIT!/"
        global.msg[10] = "EVERYONE!!^1!&A CELEBRATION!!!&TO BEING LOSERS!!/"
        scr_undface(11, 9)
        global.msg[12] = "\E9* Heh^1.&* Papyrus has the&  right idea./"
        global.msg[13] = "\E0* Losing to Frisk is&  the best thing to&  ever happen to me./"
        global.msg[14] = "\E0* So I'm glad that&  we.../"
        global.msg[15] = "\E9* Huh^1?&* What is it^1, Asgore?/"
        scr_asgface(16, 2)
        global.msg[17] = "\E2* Um..^1. what's an.../"
        global.msg[18] = "\E0* ... anime?/"
        scr_alface(19, 1)
        global.msg[20] = "\E1* (Oh My God?)/"
        global.msg[21] = "\E3* (Frisk^1. Please.)/"
        global.msg[22] = "\E2* (Help me explain what&  anime is to Asgore.)/"
        global.msg[23] = "\E0* Y-you see^1, it's&  like a cartoon^1,&  but.../"
        global.msg[24] = "\TS \F0 \T0 %"
        global.msg[25] = " &         With        With&         Sword's     Gun's\C"
        global.msg[26] = " "
        break
    case 807:
        scr_asgface(0, 2)
        global.msg[1] = "\E2* So it's like a&  cartoon..^1.&* But with swords?/"
        if (global.choice == 1)
            global.msg[1] = "\E2* So it's like a&  cartoon..^1.&* But with guns?/"
        global.msg[2] = "\E0* Golly^1!&* That sounds neato!/"
        global.msg[3] = "\E3* Where is this^1?&* Where can I see the&  Anime./"
        scr_alface(4, 3)
        global.msg[5] = "\E3* H-hold on^1, uh..^1.&* I think I have&  some on my phone./"
        global.msg[6] = "\E0* Here^1, l-look at&  this!/"
        global.msg[7] = "\E0* .../"
        global.msg[8] = "\E3*...Oh^1, uh.../"
        global.msg[9] = "\E4* Um..^1. that's the..^1.&* That's the wrong.../"
        global.msg[10] = "\E5* Uh^1, nevermind./"
        scr_asgface(11, 1)
        global.msg[12] = "* Golly^1.&* Were those two robots.../"
        scr_undface(13, 9)
        global.msg[14] = "\E9* ... kissing?/"
        scr_asgface(15, 0)
        global.msg[16] = "\E0* Boy^1!&* Technology sure is&  something^1, isn't it?/"
        scr_alface(17, 5)
        global.msg[18] = "\E5* Eheheh..^1. yeah^1!&* It sure is!/%%"
        break
    case 808:
        global.msg[0] = "\E0* Psst..^1.&* F-Frisk./"
        global.msg[1] = "\E3* Um^1, you've gotta&  tell me./"
        global.msg[2] = "\E6* D..^1. do you think&  Asgore and Toriel&  are...?/"
        global.msg[3] = "\E3* Uh^1, ever gonna get&  back together?/"
        global.msg[4] = "\TS \F0 \T0 %"
        global.msg[5] = "& &         Yeah        Nope\C"
        global.msg[6] = " "
        break
    case 809:
        global.msg[0] = "* Error.../%%"
        if (global.choice == 0)
        {
            scr_alface(0, 7)
            global.msg[1] = "\E7* Y-yeah!!^1!&* Yeah^1, that's what&  I hope^1, too./"
            global.msg[2] = "\E7* Just think about how&  cute they must have&  been together./"
            global.msg[3] = "\E0* It's quickly becoming&  my number one ship&  of all time./"
            global.msg[4] = "\E7* Tori and Gorey.../"
            global.msg[5] = "\E5* My..^1.&* My old boss and&  his ex-wife./"
            global.msg[6] = "\E8* ... uh^1, that sounds&  a lot less cool&  all of a sudden./%%"
        }
        if (global.choice == 1)
        {
            scr_alface(0, 9)
            global.msg[1] = "\E8* ... yeah^1, that's what&  I thought./"
            global.msg[2] = "\E7* A woman can dream&  though^1, right?/"
            global.msg[3] = "\E2* And write fanfiction./"
            global.msg[4] = "\E1* A LOT of fanfiction./%%"
        }
        break
    case 810:
        global.msg[0] = "\E0* Frisk^1!&* I just realized!/"
        global.msg[1] = "\E3* Now that we aren't&  fighting each&  other.../"
        global.msg[2] = "\E2* I can finally ask&  you.../"
        global.msg[3] = "\E0* " + chr(34) + "Would you like a&  cup of tea?" + chr(34) + "/"
        global.msg[4] = "\E2* .../"
        global.msg[5] = "\E0* Would you like a&  cup of tea?/"
        global.msg[6] = "\TS \F0 \T0 %"
        global.msg[7] = "& &         Yes         No\C"
        global.msg[8] = " "
        break
    case 811:
        global.msg[0] = "* Error.../%%"
        if (global.choice == 0)
        {
            scr_asgface(0, 0)
            global.msg[1] = "* Oh^1!&* Well!/"
            global.msg[2] = "\E3* Actually^1, the cup I&  had is cold now./"
            global.msg[3] = "* So you shouldn't&  have it./"
            global.msg[4] = "\E0* But^1, I am so&  happy you said&  yes./"
            global.msg[5] = "\E0* As soon as I can^1,&  I will make some&  more for you./"
            global.msg[6] = "\E0* Then we can be&  great pals./%%"
        }
        if (global.choice == 1)
        {
            scr_asgface(0, 3)
            global.msg[1] = "\E3* Oh.../"
            global.msg[2] = "\E3* Okay./"
            scr_undface(3, 2)
            global.msg[4] = "\E2* Frisk^1! Stop^1!&* You're breaking his&  big burly heart!/"
            scr_asgface(5, 2)
            global.msg[6] = "\E2* Um^1, it's OK^1, Undyne./"
            global.msg[7] = "\E0* My heart's already&  broken./"
            scr_undface(8, 6)
            global.msg[9] = "\E6* ASGORE^1! STOP^1!&* YOU'RE BREAKING MY&  BIG BURLY HEART!/"
            scr_alface(10, 9)
            global.msg[11] = "\E9* Y-yeah^1, Asgore^1.&* Don't break Undyne's&  heart./"
            global.msg[12] = "\E2* That's my job./"
            scr_undface(13, 2)
            global.msg[14] = "\E2* OH MY GOD!&* YOU'RE GOING BACK&  IN THE TRASH!!!/"
            scr_papface(15, 0)
            global.msg[16] = "\E0CAN I GO IN THE&TRASH TOO?/"
            scr_undface(17, 9)
            global.msg[18] = "\E9* Sure^1, Papyrus./"
            scr_sansface(19, 1)
            global.msg[20] = "\E1* guess i have to&  go in the trash&  too./"
            scr_torface(21, 0)
            global.msg[22] = "\E0* Oh^1, may I enter&  the trash as well?/"
            scr_undface(23, 1)
            global.msg[24] = "\E1* Uh^1, okay?/"
            scr_asgface(25, 0)
            global.msg[26] = "\E0* Am I invited to&  the trash?/"
            scr_undface(27, 6)
            global.msg[28] = "\E6* SURE!!!&* WHY NOT!!!/"
            scr_torface(29, 1)
            global.msg[30] = "\E1* On second thought^1,&  do not put me&  in the trash./"
            scr_asgface(31, 5)
            global.msg[32] = "\E5* Oh.../"
            scr_undface(33, 1)
            global.msg[34] = "\E1* OH MY GOD!!!/%%"
        }
        break
    case 812:
        global.msg[0] = "* (If you leave here^1, your&  adventure will really&  be over.)/"
        global.msg[1] = "* (Your friends will follow&  you out of the underground.)/"
        global.msg[2] = "\TS \F0 \T0 %"
        global.msg[3] = " &         Don't       I'm&         leave       ready\C"
        global.msg[4] = " "
        break
    case 813:
        global.msg[0] = " %%"
        if (global.choice == 0)
            obj_underground_exit.con = 2
        if (global.choice == 1)
            obj_underground_exit.con = 10
        break
    case 814:
        global.msg[0] = "\E0* Oh my.../"
        scr_asgface(1, 0)
        global.msg[2] = "\E0* Isn't it beautiful^1,&  everyone?/"
        scr_alface(3, 3)
        global.msg[4] = "\E3* Wow..^1. it's e-even&  better than on TV./"
        global.msg[5] = "\E7* WAY better^1!&* Better than I ever&  imagined!/"
        scr_undface(6, 1)
        global.msg[7] = "\E1* Frisk^1, you LIVE with&  this!?/"
        global.msg[8] = "\E9* The sunlight is so&  nice..^1. and the air&  is so fresh!/"
        global.msg[9] = "* I really feel alive!/"
        scr_papface(10, 0)
        global.msg[11] = "\E0HEY SANS.../"
        global.msg[12] = "\E3WHAT'S THAT GIANT&BALL?/"
        scr_sansface(13, 1)
        global.msg[14] = "\E1* we call that&  " + chr(34) + "the sun^1," + chr(34) + " my friend./"
        scr_papface(15, 0)
        global.msg[16] = "\E0THAT'S THE SUN!^1?&WOWIE!!!/"
        global.msg[17] = "I CAN'T BELIEVE&I'M FINALLY MEETING&THE SUN!!!/"
        scr_asgface(18, 0)
        global.msg[19] = "\E0* I could stand here&  and watch this for&  hours.../"
        scr_torface(20, 0)
        global.msg[21] = "\E0* Yes^1, it is beautiful^1,&  is it not?/"
        global.msg[22] = "\E1* But we should really&  think about what comes&  next./"
        scr_asgface(23, 3)
        global.msg[24] = "\E3* Oh^1, right./"
        global.msg[25] = "\E0* Everyone.../"
        global.msg[26] = "* This is the beginning&  of a bright new&  future./"
        global.msg[27] = "* An era of peace between&  humans and monsters./"
        global.msg[28] = "\E2* Frisk.../"
        global.msg[29] = "* I have something to&  ask of you./"
        global.msg[30] = "\E0* Will you act as our&  ambassador to the&  humans?/"
        global.msg[31] = "\TS \F0 \T0 %"
        global.msg[32] = "* (Be the ambassador?)& &         Yes         No\C"
        global.msg[33] = " "
        break
    case 815:
        scr_papface(0, 0)
        global.msg[1] = "WOWi, Nice error./%%"
        if (global.choice == 0)
        {
            global.msg[1] = "\E0YEAH^1!&FRISK WILL BE THE&BEST AMBASSADOR!/"
            global.msg[2] = "AND I^1, THE GREAT&PAPYRUS.../"
            global.msg[3] = "WILL BE THE BEST&MASCOT!/"
            global.msg[4] = "I'LL GO MAKE A&GOOD FIRST&IMPRESSION!/%%"
        }
        if (global.choice == 1)
        {
            global.msg[1] = "\E0IT'S OK FRISK^1!&I'VE GOT YOU&COVERED!/"
            global.msg[2] = "IF YOU DON'T WANT&TO BE THE&AMBASSADOR.../"
            global.msg[3] = "I CAN DO IT FOR&YOU!!!/"
            global.msg[4] = "I'LL GO MAKE A&GOOD FIRST&IMPRESSION!/%%"
        }
        break
    case 820:
        global.msg[0] = "* Frisk.../"
        global.msg[1] = "\E2* You came from this&  world^1, right...?/"
        global.msg[2] = "\E1* So you must have&  a place to return to^1,&  do you not?/"
        global.msg[3] = "\E2* What will you do&  now?/"
        global.msg[4] = "\TS \F0 \T0 %"
        global.msg[5] = "         I want      I have&         to stay     places&         with you    to go\C"
        global.msg[6] = " "
        break
    case 821:
        global.msg[0] = " %%"
        if (global.choice == 0)
        {
            if instance_exists(obj_outsideworld_event)
                obj_outsideworld_event.con = 100
        }
        if (global.choice == 1)
        {
            if instance_exists(obj_outsideworld_event)
                obj_outsideworld_event.con = 200
        }
        break
    case 825:
        global.msg[0] = "* (The door has no mail slot.)/"
        global.msg[1] = "* (Slide the letter under?)& &         Slide       NO!!!! \C"
        global.msg[2] = " "
        break
    case 826:
        if (global.choice == 0)
        {
            if (doak == 0)
            {
                snd_play(snd_knock)
                doak = 1
            }
            global.msg[0] = "* (You slide the letter under&  the door and give it a&  knock.)/%%"
            if instance_exists(obj_alabdoor_l)
            {
                obj_alabdoor_l.myinteract = 5
                obj_alabdoor_l.con = 2
            }
        }
        if (global.choice == 1)
            global.msg[0] = "* (You'll keep the letter&  warm for a little longer.)/%%"
        break
    case 827:
        global.msg[0] = "* (It's a note from Alphys.)/"
        global.msg[1] = "* (Read it...?)& &         Read        Do not \C"
        global.msg[2] = " "
        break
    case 828:
        if (global.choice == 0)
        {
            global.msg[0] = "* (It's hard to read because&  of the handwriting^1, but&  you try your best...)/"
            global.msg[1] = "* Hey./"
            global.msg[2] = "* Thanks for your help back&  there./"
            global.msg[3] = "* You guys..^1.&* Your support really means a&  lot to me./"
            global.msg[4] = "* But..^1.&* As difficult as it is&  to say this.../"
            global.msg[5] = "* You guys alone can't&  magically make my own&  problems go away./"
            global.msg[6] = "* I want to be a better&  person./"
            global.msg[7] = "* I don't want to be&  afraid anymore./"
            global.msg[8] = "* And for that to happen^1,&  I have to be able to&  face my own mistakes./"
            global.msg[9] = "* I'm going to start&  doing that now./"
            global.msg[10] = "* I want to be clear./"
            global.msg[11] = "* This isn't anyone else's&  problem but mine./"
            global.msg[12] = "* But if you don't ever&  hear from me again.../"
            global.msg[13] = "* If you want to know&  " + chr(34) + "the truth." + chr(34) + "/"
            global.msg[14] = "* Enter the door to the&  north of this note./"
            global.msg[15] = "* You all at least deserve&  to know what I did./"
            global.msg[16] = "* (That's all she wrote.)/%%"
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 829:
        doak = 0
        noroom = 0
        if (global.flag[495] < 8)
        {
            global.msg[0] = "* (Buy chips for 25G?)& &         Buy         No \C"
            global.msg[1] = " "
        }
        else
        {
            global.msg[0] = "* (There were no chips left&  in the machine.)/%%"
            global.msg[1] = " "
        }
        break
    case 830:
        script_execute(scr_cost, 25)
        if (global.choice == 0)
        {
            if (afford == 1)
            {
                if (doak == 0)
                {
                    doak = 1
                    script_execute(scr_itemget, 58)
                    if (noroom == 0)
                    {
                        global.gold -= 25
                        global.flag[495] += 1
                    }
                }
            }
        }
        if (noroom == 0)
        {
            global.msg[0] = "* (The vending machine&  dispensed some chisps.)/%%"
            if (afford == 0)
                global.msg[0] = "* (You didn't have enough&  gold.)/%%"
        }
        if (noroom == 1)
            global.msg[0] = "* (You are carrying too&  many items.)/%%"
        if (global.choice == 1)
            global.msg[0] = "*%%"
        break
    case 831:
        global.msg[0] = "* Ring..\E0.\TT /"
        global.msg[1] = "\F1 %"
        ini_open("undertale.ini")
        bs = ini_read_real("Toriel", "Bscotch", 0)
        ini_close()
        global.msg[2] = "* Hello?&* This is TORIEL./"
        global.msg[3] = "* For no reason in&  particular...&* Which do you prefer?/"
        global.msg[4] = "* Cinnamon or&  butterscotch?/"
        global.msg[5] = "\E1* ... wait^1.&* Do not tell me./"
        if (bs == 0)
            global.msg[6] = "\E0* It is ERROR MESSAGE!& &  Yes         No      \C"
        if (bs == 1)
            global.msg[6] = "\E0* Is it Butterscotch?& &  Yes         No      \C"
        if (bs == 2)
            global.msg[6] = "\E0* Is it Cinnamon?& &  Yes         No      \C"
        global.msg[7] = " "
        break
    case 832:
        ini_open("undertale.ini")
        bs = ini_read_real("Toriel", "Bscotch", 0)
        ini_close()
        if (global.choice == 0)
        {
            if (bs == 1)
                global.flag[46] = 1
            if (bs == 2)
                global.flag[46] = 0
            global.msg[0] = "\E0* Hee hee hee^1.&* I had a feeling./"
            global.msg[1] = "\E1* When humans fall down&  here^1, strangely..^1.&* I.../"
            global.msg[2] = "\E1* I often feel like&  I already know them./"
            global.msg[3] = "\E0* Truthfully^1, when I first&  saw you^1, I felt.../"
            global.msg[4] = "\E1* ... like I was seeing&  an old friend for&  the first time./"
            global.msg[5] = "\E0* Strange^1, is it not?/"
            global.msg[6] = "* Well^1, thank you for&  your selection./"
            global.msg[7] = "\TS \F0 \T0 %"
            global.msg[8] = "* Click.../%%"
        }
        if (global.choice == 1)
        {
            if (bs == 1)
                global.flag[46] = 0
            if (bs == 2)
                global.flag[46] = 1
            global.msg[0] = "\E1* Oh..^1. I see./"
            global.msg[1] = "\E0* Well^1, thank you^1.&* Goodbye for now./"
            global.msg[2] = "\TS \F0 \T0 %"
            global.msg[3] = "* Click.../%%"
            ini_open("undertale.ini")
            bs = ini_read_real("Toriel", "Bscotch", 0)
            if (bs == 1)
                ini_write_real("Toriel", "Bscotch", 2)
            else
                ini_write_real("Toriel", "Bscotch", 1)
            ini_close()
        }
        break
    case 833:
        global.msg[0] = "* (Seems like you could skip&  Mettaton's monologue by&  turning him around now.)/"
        global.msg[1] = "* (What will you do?)& &         Skip        Hear again\C"
        global.msg[2] = " "
        break
    case 834:
        if (global.choice == 0)
        {
            global.msg[0] = "* (You told Mettaton there&  was something cool&  behind him.)/%%"
            if instance_exists(obj_mettboss_event)
                obj_mettboss_event.con = 4.5
        }
        if (global.choice == 1)
        {
            global.msg[0] = " %%"
            obj_mettboss_event.con = 6
        }
        break
    case 835:
        global.msg[0] = "* though.../"
        global.msg[1] = "* one thing about you&  always struck me&  as kinda odd./"
        global.msg[2] = "* now^1, i understand&  acting in self-defense./"
        global.msg[3] = "* you were thrown into&  those situations&  against your will./"
        global.msg[4] = "* but.../"
        global.msg[5] = "* sometimes.../"
        global.msg[6] = "* you act like you&  know what's gonna&  happen./"
        global.msg[7] = "* like you've already&  experienced it all&  before./"
        global.msg[8] = "* this is an odd thing&  to say^1, but.../"
        global.msg[9] = "\W* if you have some sort&  of \Yspecial power\W.../"
        global.msg[10] = "* isn't it your&  responsibility to do&  the right thing?/"
        global.msg[11] = "\TS \F0 \T0 %"
        global.msg[12] = " & &         Yes         No\C"
        global.msg[13] = " "
        break
    case 836:
        if (global.choice == 0)
        {
            scr_sansface(0, 1)
            global.msg[1] = "\E1* ah./"
            global.msg[2] = "\E0* i see./"
            global.msg[3] = "\E3* .../%%"
            if instance_exists(obj_lastsans_trigger)
                obj_lastsans_trigger.con = 20
        }
        if (global.choice == 1)
        {
            scr_sansface(0, 1)
            global.msg[1] = "\E1* heh./"
            global.msg[2] = "\E0* well^1, that's your&  viewpoint./"
            global.msg[3] = "\E2* i won't judge you&  for it./"
            global.msg[4] = "\E3* .../%%"
            if instance_exists(obj_lastsans_trigger)
                obj_lastsans_trigger.con = 21
        }
        break
    case 837:
        global.msg[0] = "* (Ring...)/"
        scr_alface(1, 0)
        global.msg[2] = "\E0* Hey!/"
        global.msg[3] = "\E3* This um^1, doesn't have&  anything to do with&  guiding you..^1. but.../"
        global.msg[4] = "\E2* .../"
        global.msg[5] = "\E6* Uhh^1, hey^1, would you want&  to watch a human TV&  show together???/"
        global.msg[6] = "* Sometime???/"
        global.msg[7] = "\E4* It's called^1, um^1,&  M..^1.Mew Mew Kissy&  Cutie.../"
        global.msg[8] = "\TS \F0 \T0 %"
        global.msg[9] = " & &         Sure!       ...no...\C"
        global.msg[10] = " "
        break
    case 838:
        scr_alface(0, 5)
        if (global.choice == 0)
        {
            global.msg[1] = "* R-really!?/"
            global.msg[2] = "\E3* It's so good^1!&* It's um^1, my favorite&  show!/"
            global.msg[3] = "* It's all about this&  human girl named Mew Mew&  who has cat ears!%"
            global.msg[4] = "\E3* Which humans don't have!&* S-so she's all&  sensitive about them!%"
            global.msg[5] = "\E6* But like...&* Eventually!%"
            global.msg[6] = "* She realizes that her&  ears don't matter!%"
            global.msg[7] = "* That her friends like&  her despite the ears!%"
            global.msg[8] = "\E7* It's really moving!%"
            global.msg[9] = "\E5* Whoops, spoilers%"
            global.msg[10] = "\E6* Also, this sounds&  weird, but she has&  the power!%"
            global.msg[11] = "\E5* To control the minds&  of anyone she kisses!%"
            global.msg[12] = "\E3* She kisses people and&  controls them to fix&  her problems!!%"
            global.msg[13] = "\E5* They don't remember&  anything after the&  kiss I mean!!%"
            global.msg[14] = "\E3* BUT IF SHE MISSES&  THE KISS!!!&* THEN!!%"
            global.msg[15] = "\E4* Then^1, uh^1, and^1, uh^1,&  also I mean^1, of course%"
            global.msg[16] = "\E5* Eventually^1, she&  realizes that&  controlling people%"
            global.msg[17] = "\E3* OKAY WELL I almost&  spoiled the whole&  show^1, but%"
            global.msg[18] = "\E5* Uhhh^1, I think you'd&  really like it!!!/"
            global.msg[19] = "\E0* We should watch it^1!&* After you get through&  all this!/"
            global.msg[20] = "\TS \F0 \T0 %"
            global.msg[21] = "* (Click...)/%%"
        }
        if (global.choice == 1)
        {
            global.msg[1] = "\E5* Um^1! Well^1!&* That's okay!/"
            global.msg[2] = "* Just thought I'd!^1!&* Ask!!!/"
            global.msg[3] = "\E6* B-but I think you'd&  really like it!!/"
            global.msg[4] = "* If you gave it a&  chance!!/"
            global.msg[5] = "\TS \F0 \T0 %"
            global.msg[6] = "* (Click...)/%%"
        }
        break
    case 839:
        global.msg[0] = "\M1* Did y'hear!^1?&* You're back!/"
        global.msg[1] = "\M0* I'll tell you a big secret./"
        global.msg[2] = "\M1* I'm starting a band^1, y'hear?/"
        global.msg[3] = "\M1* It's called the Red Hot&  Chibi Peppers./"
        global.msg[4] = "\M0* All I've thought of is&  the name./"
        global.msg[5] = "\M3* And I don't^1, play...&* Instruments^1, or sing./"
        global.msg[6] = "\M1* Well^1!&* Do you think we'll be&  popular!!!/"
        global.msg[7] = " & &         Yeah        No\C"
        global.msg[8] = " "
        break
    case 840:
        if (global.choice == 0)
        {
            global.flag[496] = 6
            global.msg[0] = "\M0* Yeah^1, me too.../%%"
        }
        if (global.choice == 1)
        {
            global.flag[496] = -1
            with (obj_onionsan_event)
                con = 25
            global.msg[0] = " %%"
        }
        break
    case 845:
        global.msg[0] = "* (It's a lamp.)/"
        global.msg[1] = "* (Turn it on?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 846:
        if (global.choice == 0)
        {
            global.msg[0] = "* (There's no lightbulb.^1)&* (A flashlight is stuck in&  the bulb socket.)/"
            global.msg[1] = "* (Turn it on?)& &         Yes         No\C"
            global.msg[2] = " "
        }
        else
            global.msg[0] = " %%"
        break
    case 847:
        if (global.choice == 0)
            global.msg[0] = "* (The flashlight is out of&  batteries.)/%%"
        else
            global.msg[0] = " %%"
        break
    case 850:
        with (obj_heatsflamesman)
            sprite_index = spr_heatsf_remember
        global.msg[0] = "* Hey^1, hey^1!&* Did you remember my name?/"
        global.msg[1] = "* (Did you?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 851:
        with (obj_heatsflamesman)
            sprite_index = spr_heatsf_shock
        if (global.choice == 0)
        {
            global.flag[434] = 1
            global.msg[0] = "* Wh-WHAT!^1?&* You REMEMBER!?/"
            global.msg[1] = "* How could I be so easily&  defeateeeeeeeeed!?/%%"
        }
        if (global.choice == 1)
        {
            global.flag[434] = 2
            global.msg[0] = "* Wh-WHAT!^1?&* You DON'T REMEMBER!?/"
            global.msg[1] = "* How could I be so easily&  defeateeeeeeeeed!?/%%"
        }
        break
    case 853:
        global.msg[0] = "* (It's a small white dog.^1)&* (It's fast asleep...)/"
        global.msg[1] = "* (Fight the dog?)& &         Yes         No\C"
        global.msg[2] = " "
        break
    case 854:
        if (global.choice == 0)
        {
            global.msg[0] = "* (Can't fight the dog.)/"
            global.msg[1] = "* (Seems like the fabric it's&  sleeping on has too many&  holes in it.)/"
            global.msg[2] = "* (Seems like the dog needs&  to " + chr(34) + "patch" + chr(34) + " the fabric.)/"
            global.msg[3] = "* (Then you can fight the dog.^1)&* (... maybe.)/"
            global.msg[4] = "* (Upon closer examination^1,&  the holes in the fabric&  seem to be growing.)/"
            global.msg[5] = "* (Might take a while for the&  dog to fix all of them.)/"
            global.msg[6] = "* (Dogs aren't usually very&  good at knitting.)/"
            global.msg[7] = "* (A crocheting dog is out of&  the question.)/%%"
        }
        else
            global.msg[0] = "* (Let sleeping dogs lie^1, instead&  of fighting them.^1)&* (That's how the saying goes.)/%%"
        break
    case 860:
        global.msg[0] = "* (Knock knock)./"
        if (global.flag[262] == 0)
        {
            global.msg[0] = "* Oooooaaah ^1!&* Room service !/"
            global.msg[1] = "* Got my " + chr(34) + "Sea Tea" + chr(34) + " ?& &         Yes         No\C"
            global.msg[2] = " "
            if (global.flag[7] == 1)
                global.msg[0] = "* Room service never came ^1.&* (Sigh ...)/%%"
        }
        else
        {
            global.msg[0] = "* Thanks a million ./%%"
            if (global.flag[7] == 1)
                global.msg[0] = "* (No response.)/%%"
        }
        break
    case 861:
        if (global.choice == 0)
        {
            scr_itemremove(41)
            if (removed == 1)
            {
                global.gold += 99
                global.flag[262] = 1
                global.msg[0] = "* (You pour the Sea Tea under&  the door.)/"
                global.msg[1] = "* HUH !?!?!?!^2?&* That's just the way I want ^1!&* Here's a tip ./"
                global.msg[2] = "* (You got 99G.)/%%"
            }
            else
                global.msg[0] = "* ..^1.&* No you don't ./%%"
        }
        else
            global.msg[0] = "* Then ...!?/%%"
        break
    case 862:
        global.msg[0] = "* (Knock knock)./"
        if (global.flag[263] == 0)
        {
            global.msg[0] = "* Oooooaaah ^1!&* Room service !/"
            global.msg[1] = "* Got my " + chr(34) + "Cinnamon Bun" + chr(34) + " ?& &         Yes         No\C"
            global.msg[2] = " "
            if (global.flag[7] == 1)
                global.msg[0] = "* Room service never came ^1.&* (Sigh ...)/%%"
        }
        else
        {
            global.msg[0] = "* Thanks a trillion ./%%"
            if (global.flag[7] == 1)
                global.msg[0] = "* (No response.)/%%"
        }
        break
    case 863:
        if (global.choice == 0)
        {
            scr_itemremove(21)
            if (removed == 1)
            {
                global.gold += 99
                global.flag[263] = 1
                global.msg[0] = "* (You flatten the Cinnamon Bun&  until it's paper thin.)&* (You slide it under the door.)/"
                global.msg[1] = "* HUH !?!?!?!^2?&* That's just the way I want ^1!&* Here's a tip ./"
                global.msg[2] = "* (You got 99G.)/%%"
            }
            else
                global.msg[0] = "* ..^1.&* No you don't ./%%"
        }
        else
            global.msg[0] = "* Then ...!?/%%"
        break
    case 864:
        global.msg[0] = "* (You hear shuffling.)/"
        global.msg[1] = "* (Seems like you could put&  something under the door.)/"
        global.msg[2] = " & &         Put         No Put\C"
        global.msg[3] = " "
        if (global.flag[7] == 1)
            global.msg[0] = "* (No response.)/%%"
        break
    case 865:
        if (global.choice == 0)
        {
            global.msg[0] = "* (...)/"
            global.msg[1] = "* (But you didn't have anything&  appealing.)/%%"
            type = 0
            scr_itemcheck(38)
            if (haveit == 1 && global.flag[264] == 0)
                type = 1
            scr_itemcheck(39)
            if (haveit == 1)
                type = 2
            scr_itemcheck(28)
            if (haveit == 1)
                type = 3
            scr_itemcheck(29)
            if (haveit == 1)
                type = 4
            scr_itemcheck(30)
            if (haveit == 1)
                type = 4
            scr_itemcheck(31)
            if (haveit == 1)
                type = 4
            scr_itemcheck(32)
            if (haveit == 1)
                type = 4
            scr_itemcheck(33)
            if (haveit == 1)
                type = 4
            scr_itemcheck(34)
            if (haveit == 1)
                type = 4
            if (type == 1 && global.flag[264] == 0)
            {
                scr_itemremove(38)
                scr_itemget(62)
                global.msg[0] = "* (You put a Hot Dog in front&  of the door.)/"
                global.msg[1] = "* (A white paw shoots out from&  under the door.)/"
                global.msg[2] = "* (It tries to pull the Hot Dog&  into its room...)/"
                global.msg[3] = "* (But it keeps pressing down too&  hard^1, and the hot dog keeps&  spinning away.)/"
                global.msg[4] = "* (...)/"
                global.msg[5] = "* (It finally succeeds.)/"
                global.msg[6] = "* (...)/"
                global.msg[7] = "* (You hear the grinding of&  stone.)/"
                global.msg[8] = "* (A single hushpuppy slides&  out from under the door.)/"
                global.msg[9] = "* (You got Hush Puppy.)/%%"
                global.flag[264] = 1
            }
            if (type == 2)
            {
                global.msg[0] = "* (You put a Hot Cat in front&  of the door.)/"
                global.msg[1] = "* (You hear growling...)/%%"
            }
            if (type == 3)
            {
                scr_itemremove(28)
                global.msg[0] = "* (You put a Dog Salad in front&  of the door.^1)&* (It slides underneath.)/"
                global.msg[1] = "* (...)/"
                global.msg[2] = "* (The Dog Salad was absorbed&  by the darkness.)/%%"
            }
            if (type == 4)
            {
                rr = choose(29, 30, 31, 32, 33, 34)
                scr_itemget(rr)
                global.msg[0] = "* (You put a Dog Residue in&  front of the door.)/"
                global.msg[1] = "* (It slides underneath the door^1,&  as if pulled by a magnet.)/"
                global.msg[2] = "* (...)/"
                if (noroom == 1)
                    global.msg[3] = "* (ZOMMM!!^1!)&* (It shoots back out at a&  high speed!)/%%"
                else
                    global.msg[3] = "* (Two Dog Residues slowly slide&  back out from underneath&  the door.)/%%"
            }
        }
        if (global.choice == 1)
            global.msg[0] = " %%"
        break
    case 866:
        global.msg[0] = "* Yes^1, we know^1.&* The elevator to the city&  is NOT working./"
        global.msg[1] = "* Because of this incident^1, rooms&  are running at a special rate!/"
        global.msg[2] = "* 200G a room^1. Interested^1?& &         Stay        Do not\C"
        global.msg[3] = " "
        if (global.flag[267] == 2)
        {
            global.msg[0] = "* 200G a room^1. Interested^1?& &         Stay        Do not\C"
            global.msg[1] = " "
        }
        if (global.flag[267] == 1)
        {
            global.flag[267] = 2
            global.msg[0] = "* Did you enjoy your stay?/"
            global.msg[1] = "* What^1?&* Room..^1.&* Key?/"
            global.msg[2] = "* No^1, we don't do that./"
            global.msg[3] = "* If you leave your room^1,&  you'll have to pay again./"
            global.msg[4] = "* 200G a room^1. Interested^1?& &         Stay        Do not\C"
            global.msg[5] = " "
        }
        break
    case 867:
        if (global.choice == 0)
        {
            if (global.gold >= 200)
            {
                global.gold -= 200
                with (obj_mainchara)
                    uncan = 1
                global.msg[0] = "* Fabulous^1!&* We'll escort you to your&  room!/%%"
                if (global.flag[267] < 1)
                    global.flag[267] = 1
                obj_hotelreceptionist.con = 1
            }
            else
                global.msg[0] = "* ... that's not enough money./%%"
        }
        else
            global.msg[0] = "* Do let us know if you&  change your mind^1.&* Have a sparkular day!/%%"
        break
    case 870:
        global.msg[0] = "* ... MY ONE TRUE LOVE?/"
        global.msg[1] = "* .../"
        global.msg[2] = "* (YOU LOOK BORED^1, DARLING.)/"
        global.msg[3] = "* (I WANT THIS TO BE A STELLAR&  PERFORMANCE^1, SO IF YOU&  WON'T GIVE IT YOUR ALL...)/"
        global.msg[4] = "* (THEN I'LL SKIP AHEAD FOR&  THE AUDIENCE'S SAKE.)/"
        global.msg[5] = "* (Perform?)& &         Yeah        Skip this\C"
        global.msg[6] = " "
        break
    case 871:
        if (global.choice == 0)
            global.msg[0] = "* (UNDERSTOOD.^1)&* (LET'S KNOCK 'EM DEAD!)/%%"
        else
        {
            global.msg[0] = "* (KA-SIGH...^1)&* (THE SHOW MUST GO ON!)/%%"
            with (obj_playmovement)
                con = 240
        }
        break
    case 888:
        global.msg[0] = "Interesting./"
        global.msg[1] = "You want to go back./"
        global.msg[2] = "You want to go bac^1k&to the worl^2d&you destroyed./"
        global.msg[3] = "It was you who pushed&everythin^1g to its edge./"
        global.msg[4] = "It was you who led the worl^1d&to its destruction./"
        global.msg[5] = "But you cannot accept it./"
        global.msg[6] = "You think you are above&consequences.&         Yes         No\C"
        global.msg[7] = ""
        break
    case 889:
        if (global.choice == 0)
            global.msg[0] = "Exactly./%%"
        else
            global.msg[0] = "Then what are you looking for?/%%"
        break
    case 890:
        global.msg[0] = "Perhaps./"
        global.msg[1] = "We can reach a compromise./"
        global.msg[2] = "You still have somethin^1g&I want./"
        global.msg[3] = "Give it to me./"
        global.msg[4] = "And I will bring this&world back./"
        global.msg[5] = " & &         Yes         No\C"
        global.msg[6] = ""
        break
    case 891:
        if (global.choice == 0)
        {
            global.msg[0] = "Then it is agreed./"
            global.msg[1] = "You will give me your SOUL.& &         Yes         No\C"
            global.msg[2] = " "
        }
        else
        {
            global.msg[0] = "Then stay here for&all eternity./"
            global.msg[1] = " "
        }
        break
    case 892:
        if (global.choice == 0)
        {
            global.msg[0] = ".../"
            global.msg[1] = "Then^1, it is done./%%"
        }
        else
        {
            global.msg[0] = "Then stay here for&all eternity./"
            global.msg[1] = " "
        }
        break
    case 900:
        global.msg[0] = "* hey./"
        global.msg[1] = "* is your refrigerator&  running?/"
        global.msg[2] = " & &         yes         no\C"
        global.msg[3] = " "
        break
    case 901:
        if (global.choice == 0)
            global.msg[0] = "* nice^1.&* i'll be over to deposit&  the brewskis./%%"
        else
        {
            global.msg[0] = "* ok^1, i'll send someone&  over to fix it./"
            global.msg[1] = "* thanks for letting me&  know./"
            global.msg[2] = "* good communication is&  important./%%"
        }
        break
    case 1001:
        global.msg[0] = "   * Check         * Compliment&   * Threat"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1002:
        global.msg[0] = "   * Check         * Talk"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1003:
        global.msg[0] = "   * Check         * Compliment&   * Threat"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1004:
        global.msg[0] = "   * Check         * Compliment&   * Threat"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1005:
        global.msg[0] = "   * Check         * Console&   * Terrorize"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1006:
        global.msg[0] = "   * Check         * Imitate&   * Flirt"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1007:
        global.msg[0] = "   * Check         * Talk"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1008:
        global.msg[0] = "   * Check         * Talk&   * Devour        * Dinner"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1009:
        global.msg[0] = "   * Check         * Pick On&   * Don't Pick On"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1010:
        global.msg[0] = "   * Check         * Talk"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1011:
        global.msg[0] = "   * Check         * Flirt&   * Threat        * Cheer"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1012:
        global.msg[0] = "   * Check         * Imitate&   * Flirt"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1013:
        global.msg[0] = "   * Check         * Pet"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1014:
        global.msg[0] = "   * Check         * Pet&   * Pet           * Pet  &   * Pet           * Pet  "
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 1
        break
    case 1015:
        global.msg[0] = "   * Check         * Pet&   * Re-sniff      * Roll Around"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1016:
        global.msg[0] = "   * Check         * Pet&   * Re-sniff      * Roll Around"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1017:
        global.msg[0] = "   * Check         * Pet&   * Beckon        * Play &   * Ignore"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1018:
        if (global.flag[57] == 2)
        {
            global.msg[0] = "   * Check         * Agree&   * Clash         * Joke"
            global.choices[0] = 1
            global.choices[1] = 1
            global.choices[2] = 0
            global.choices[3] = 1
            global.choices[4] = 1
            global.choices[5] = 0
        }
        else
        {
            global.msg[0] = "   * Check         * Laugh&   * Heckle        * Joke"
            global.choices[0] = 1
            global.choices[1] = 1
            global.choices[2] = 0
            global.choices[3] = 1
            global.choices[4] = 1
            global.choices[5] = 0
        }
        break
    case 1019:
        global.msg[0] = "   * Check         * Compliment&   * Ignore        * Steal"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1020:
        global.msg[0] = "   * Check         * Compliment"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1021:
        global.msg[0] = "   * Check         * Ditch"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1022:
        global.msg[0] = "   * Check         * Decorate&   * Undecorate    * Gift"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1023:
        global.msg[0] = "   * Check         * Flex&   * Shoo"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1024:
        global.msg[0] = "   * Check         * Flex&   * Feed Temmie   * Talk&     Flakes  "
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1025:
        global.msg[0] = "   * Check         * Flirt&   * Insult"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        if (scr_murderlv() >= 7)
        {
            global.msg[0] = "   * Check"
            global.choices[0] = 1
            global.choices[1] = 0
            global.choices[2] = 0
            global.choices[3] = 0
            global.choices[4] = 0
            global.choices[5] = 0
        }
        break
    case 1026:
        if instance_exists(obj_moldsmalx)
        {
            if (obj_moldsmalx.stage == 0)
            {
                global.msg[0] = "   * Check         * Imitate&   * Flirt"
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 0
                global.choices[5] = 0
            }
            else if (global.flag[74] == 0)
            {
                global.msg[0] = "   * Check         * Lie Down&   * Hug           * Unhug"
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 1
                global.choices[5] = 0
            }
            else
            {
                global.msg[0] = "   * Check         * Lie Down&   * Hug           * Unhug"
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 1
                global.choices[5] = 0
            }
        }
        break
    case 1028:
        global.msg[0] = "   * Check         * Clean&   * Touch         * Joke"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1029:
        global.msg[0] = "   * Check         * Smile&   * Hum           * Conduct"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1031:
        global.msg[0] = "   * Check         * Talk"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1032:
        global.msg[0] = "   * Check         * Plead&   * Challenge"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        if instance_exists(obj_undyneboss)
        {
            if (obj_undyneboss.con >= 50)
            {
                global.msg[0] = "   * Check"
                global.choices[0] = 1
                global.choices[1] = 0
                global.choices[2] = 0
                global.choices[3] = 0
                global.choices[4] = 0
                global.choices[5] = 0
            }
        }
        break
    case 1033:
        global.msg[0] = "   * Check         * Cry"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1034:
        global.msg[0] = "   * Check         * Whisper&   * Clean Armor"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1035:
        global.msg[0] = "   * Check         * Whisper&   * Clean Armor"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1036:
        global.msg[0] = "   * Check         * Flirt&   * Approach"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1037:
        global.msg[0] = "   * Check         * Criticize&   * Encourage     * Hug"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1038:
        global.msg[0] = "   * Check         * Cool Down&   * Heat Up       * Invite"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1039:
        cashfactor = "NaN"
        global.msg[0] = (((("\W   * Check         * Struggle&   * Pay " + string(global.flag[382])) + "G&         \YYour Money: ") + string(global.gold)) + "G \W ")
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1040:
        global.msg[0] = "   * Check         * Yell"
        if (global.flag[385] > 0)
            global.msg[0] = "\W   * Check         \Y* Yellow \W"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1041:
        global.msg[0] = "   * Check         * Fake Attack"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1042:
        global.msg[0] = "   * Check         * Talk     &   * Stare         * Clear Mind"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1043:
        global.msg[0] = "   * Check         * Talk     &   * Sing"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1044:
        global.msg[0] = "   * Check         * Compliment&   * Threaten      * Mystify"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1045:
        global.msg[0] = "   * Check         * Pick On&   * Don't Pick    * Challenge&     On"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1046:
        global.msg[0] = "   * Check         * Console&   * Terrorize     * Pray"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1047:
        global.msg[0] = "   * Check         * Defuse Bomb"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1048:
        global.msg[0] = "   * Check         * Whisper&   * Touch Glove"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1049:
        global.msg[0] = "   * Check         * Whisper&   * Touch Glove"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1050:
        global.msg[0] = "   * Check         * Burn"
        if (global.flag[424] > 0)
            global.msg[0] = "\W   * Check         \Y* Turn \W"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1051:
        global.msg[0] = "   * Check         * Boast&   * Pose          * Heel Turn"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1052:
        global.msg[0] = "   * Check         * Talk"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1053:
        global.msg[0] = "   * Call          * Hum&   * Scream        * Flex&   * Unhug         * Cry"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 1
        break
    case 1054:
        global.msg[0] = "   * Check         * Pick On&   * Mystify       * Clean&   * Hum           * Pray"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 1
        break
    case 1055:
        global.msg[0] = "   * Check         * Laugh&   * Heckle        * Joke"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1056:
        global.msg[0] = "   * Check         * ITEM &   * STAT          * CELL"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        if instance_exists(obj_memoryhead)
        {
            if (obj_memoryhead.coherent == 1)
            {
                global.msg[0] = "   * Check         * Join&   * Refuse"
                global.choices[0] = 1
                global.choices[1] = 1
                global.choices[2] = 0
                global.choices[3] = 1
                global.choices[4] = 0
                global.choices[5] = 0
            }
        }
        break
    case 1057:
        global.msg[0] = "   * Check         * Pet&   * Beckon        * Play &   * Ignore"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 1
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1058:
        global.msg[0] = "   * Fake Hit      * Recipe&   * Smile         * Clash"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1059:
        global.msg[0] = "   * Encourage     * Call  &   * Nerd Out      * Quiz"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1060:
        global.msg[0] = "   * Joke          * Puzzle &   * Recipe        * Insult"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1061:
        global.msg[0] = "   * Take break    * Joke   &   * Judgment      * Crossword"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1062:
        global.msg[0] = "   * Talk          * Mercy  &   * Hug           * Preference"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1063:
        global.msg[0] = "   * Talk          * Mercy  &   * Stare         * Hug"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1064:
        global.msg[0] = "   * Check"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1065:
        global.msg[0] = "   * Check"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1066:
        global.msg[0] = "   * Check"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1067:
        global.msg[0] = "   * Check"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1068:
        global.msg[0] = "   * Check"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1070:
        global.msg[0] = "   * Check         * Compliment&   * Threaten      * Mystify"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1071:
        global.msg[0] = "   * Check         * Pick On&   * Don't Pick    * Challenge&     On"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1072:
        global.msg[0] = "   * Check         * Console&   * Terrorize     * Pray"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1073:
        global.msg[0] = "   * Check         * Talk"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1074:
        global.msg[0] = "   * Check         * Switch&   * Fix           * Lie Down"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1075:
        global.msg[0] = "   * Check         * Hiss&   * Devour        * Snack"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1076:
        global.msg[0] = "   * Check         * Applaud&   * Boo           * Nothing"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 1
        global.choices[5] = 0
        break
    case 1080:
        global.msg[0] = "   * Check         * Something"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1081:
        global.msg[0] = "   * Check         * Draw"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1099:
        global.msg[0] = "   * Check         * Hope  &   * Dream"
        global.choices[0] = 1
        global.choices[1] = 1
        global.choices[2] = 0
        global.choices[3] = 1
        global.choices[4] = 0
        global.choices[5] = 0
        break
    case 1100:
        global.msg[0] = "   * Error"
        global.choices[0] = 1
        global.choices[1] = 0
        global.choices[2] = 0
        global.choices[3] = 0
        global.choices[4] = 0
        global.choices[5] = 0
        if (global.flag[501] == 0)
        {
            global.msg[0] = "   * Struggle"
            global.choices[0] = 1
            global.choices[1] = 0
            global.choices[2] = 0
            global.choices[3] = 0
            global.choices[4] = 0
            global.choices[5] = 0
        }
        if (global.flag[501] == 1)
        {
            if (global.flag[505] == 0)
                global.msg[0] = "\W   * Undyne        "
            else
                global.msg[0] = "\W   \Y* (Saved)      \W "
            if (global.flag[506] == 0)
                global.msg[0] += "* Alphys \W &"
            else
                global.msg[0] += "\Y* (Saved)\W &"
            if (global.flag[507] == 0)
                global.msg[0] += "\W   * Papyrus       * Sans \W &"
            else
                global.msg[0] += "\Y   * (Saved)       * (Saved)\W &"
            if (global.flag[508] == 0)
                global.msg[0] += "\W   * Toriel        * Asgore \W "
            else
                global.msg[0] += "\Y   * (Saved)       * (Saved)\W "
            global.choices[0] = 1
            global.choices[1] = 1
            global.choices[2] = 1
            global.choices[3] = 1
            global.choices[4] = 1
            global.choices[5] = 1
        }
        if (global.flag[501] > 1)
        {
            if (global.flag[501] == 2)
                global.msg[0] = "   * Someone else"
            if (global.flag[501] == 3)
                global.msg[0] = "   * Asriel Dreemurr"
            global.choices[0] = 1
            global.choices[1] = 0
            global.choices[2] = 0
            global.choices[3] = 0
            global.choices[4] = 0
            global.choices[5] = 0
        }
        break
    case 1501:
        global.msg[0] = "* Dialing..\E0.\TT /"
        if (doak == 0)
        {
            doak = 1
            global.flag[40] += 1
        }
        if (global.flag[40] == 1)
        {
            global.msg[1] = "\F1 %"
            global.msg[2] = "\E0* This is TORIEL./"
            global.msg[3] = "* You only wanted to&  say hello...^2?&* Well then./"
            global.msg[4] = "\E0* 'Hello!'/"
            global.msg[5] = "* I hope that suffices^1.&* Hee hee./"
            global.msg[6] = "\TS \F0 \T0 %"
            global.msg[7] = "* Click.../%%"
        }
        if (global.flag[40] == 2)
        {
            global.msg[1] = "\F1 %"
            global.msg[2] = "\E0* This is TORIEL./"
            global.msg[3] = "* You want to say&  hello again?/"
            global.msg[4] = "* 'Salutations!'/"
            global.msg[5] = "* Is that enough?/"
            global.msg[6] = "\TS \F0 \T0 %"
            global.msg[7] = "* Click.../%%"
        }
        if (global.flag[40] == 3)
        {
            global.msg[1] = "\F1 \TT %"
            global.msg[2] = "\E0* This is TORIEL./"
            global.msg[3] = "* Are you bored^1?&* I should have given&  a book to you./"
            global.msg[4] = "* My apologies./"
            global.msg[5] = "* Why not use your&  imagination to&  divert yourself?/"
            global.msg[6] = "* Pretend you are..^1.&* A monarch!/"
            global.msg[7] = "* Rule over the leaf pile&  with a fist of iron./"
            global.msg[8] = "* Can you do that for me?/"
            global.msg[9] = "\TS \F0 \T0 %"
            global.msg[10] = "* Click.../%%"
        }
        if (global.flag[40] > 3)
        {
            global.msg[1] = "\F1 \TT %"
            global.msg[2] = "\E0* This is TORIEL./"
            global.msg[3] = "* Hello^1, my child./"
            global.msg[4] = "\E1* Sorry^1, I do not have&  much to say./"
            global.msg[5] = "\E0* It was nice to hear&  your voice^1, though^1./"
            global.msg[6] = "\TS \F0 \T0 %"
            global.msg[7] = "* Click.../%%"
        }
        break
    case 1502:
        global.msg[0] = "* Dialing..\E0.\TT /"
        global.msg[1] = "\F1 %"
        global.msg[2] = "* This is TORIEL./"
        global.msg[3] = "\E1* Help with a puzzle^1.^1.^1.?/"
        global.msg[4] = "* Um^1, you have not&  left the room^1, have you?/"
        global.msg[5] = "\E0* Wait patiently for&  me and we can solve&  it together!/"
        global.msg[6] = "\TS \F0 \T0 %"
        global.msg[7] = "* Click.../%%"
        break
    case 1503:
        global.msg[0] = "* Dialing..\E0.\TT /"
        global.msg[1] = "\F1 %"
        global.msg[2] = "* This is TORIEL./"
        global.msg[3] = "\E1* You want to know&  more about me?/"
        global.msg[4] = "* Well^1, I am afraid there&  is not much to say./"
        global.msg[5] = "\E0* I am just a silly little&  lady who worries too&  much!/"
        global.msg[6] = "\TS \F0 \T0 %"
        global.msg[7] = "* Click.../%%"
        break
    case 1504:
        global.flag[42] = 1
        global.msg[0] = "* Dialing..\E0.\TT /"
        global.msg[1] = "\F1 %"
        global.msg[2] = "* This is TORIEL./"
        global.msg[3] = "\E8* Huh^2?&* Did you just call&  me... " + chr(34) + "Mom" + chr(34) + "?/"
        global.msg[4] = "\E1* Well...&* I suppose.../"
        global.msg[5] = "* Would that make you&  happy?/"
        global.msg[6] = "* To call me..^2.&* " + chr(34) + "Mother?" + chr(34) + "/"
        global.msg[7] = "\E0* Well then^1, call me&  whatever you like!/!"
        global.msg[8] = "\TS \F0 \T0 %"
        global.msg[9] = "* Click.../%%"
        break
    case 1505:
        if (doak == 0)
        {
            doak = 1
            global.flag[41] += 1
        }
        if (global.flag[41] == 1)
        {
            global.msg[0] = "* Dialing..\E0.\TT /"
            global.msg[1] = "\F1 %"
            global.msg[2] = "\E8* ...^2 huh???/"
            global.msg[3] = "\E1* Oh,^1 heh..^1 heh...^1 \E0 &* Ha ha ha!/"
            global.msg[4] = "* How adorable...^1 I&  could pinch your cheek!/"
            global.msg[5] = "* You can certainly find&  better than an old woman&  like me./"
            global.msg[6] = "\TS \F0 \T0 %"
            global.msg[7] = "* Click.../%%"
        }
        if (global.flag[41] == 2)
        {
            global.msg[0] = "* Dialing..\E0.\TT /"
            global.msg[1] = "\F1 %"
            global.msg[2] = "\E1* Oh dear,^1 are you&  serious...?/"
            global.msg[3] = "\E1* I do not know if this is&  pathetic,^1 or endearing./"
            global.msg[4] = "\TS \F0 \T0 %"
            global.msg[5] = "* Click.../%%"
            if (global.flag[42] == 1)
            {
                global.msg[3] = "\E8* And after you said you&  want to call&  me " + chr(34) + "mother..." + chr(34) + "/"
                global.msg[4] = "\E0* You are an...^2 &  \E1... " + chr(34) + "interesting" + chr(34) + "&  child./"
                global.msg[5] = "\TS \F0 \T0 %"
                global.msg[6] = "* Click.../%%"
            }
        }
        break
    case 1506:
        if (global.flag[45] == 4)
        {
            global.msg[0] = "* Dialing... /"
            global.msg[1] = "* ... /"
            global.msg[2] = "* But nobody came./%%"
        }
        else
        {
            global.msg[0] = "* Dialing... /"
            global.msg[1] = "* ... /"
            global.msg[2] = "* Nobody picked up./%%"
            scr_itemcheck(27)
            if (haveit == 1)
            {
                global.msg[0] = "* Dialing... /"
                global.msg[1] = "* ... /"
                global.msg[2] = "* The ringing is coming from&  inside your inventory./%%"
            }
        }
        break
    case 1507:
        global.faceemotion = 99
        global.msg[0] = "* Dialing...\TT /"
        global.msg[1] = "\F1 %"
        global.msg[2] = "* Hey^1, you silly&  child./"
        global.msg[3] = "* If you want to&  talk to me^1, I am&  right here./"
        global.msg[4] = "\TS \F0 \T0 %"
        global.msg[5] = "* Click.../%%"
        break
    case 1508:
        global.msg[0] = "* Dialing... /"
        global.msg[1] = "* ... /"
        global.msg[2] = "* (Somewhere^1, signals deflected&  by a dog.)/%%"
        break
    case 1510:
        if (global.flag[7] == 0)
            scr_papcall()
        else
            global.msg[0] = "* (No response.^1)&* (Their phone might be out&  of batteries.)/%%"
        break
    case 1515:
        scr_torcall()
        break
    case 1520:
        global.msg[0] = "* (The box is aclog with the&  the hair of a dog.)/%%"
        break
    case 2001:
        global.msg[0] = "\E2* Welcome to your new&  home,^1 innocent one./"
        global.msg[1] = "* Allow me to educate you&  in the operation of&  the RUINS./%"
        break
    case 2002:
        global.faceplate = 1
        global.msg[0] = "\E8* ...^2 huh???/"
        global.msg[1] = "\E1* Oh,^1 heh..^1 heh...^1 \E0 &* Ha ha ha!/"
        global.msg[2] = "* How adorable...^1 I&  could pinch your cheek!/"
        global.msg[3] = "* You can certainly find&  better than an old woman&  like me./%"
        break
    case 3002:
        global.msg[0] = "* You encountered the Dummy."
        global.msg[1] = "%%%"
        break
    case 3003:
        global.msg[0] = "* Froggit attacks you!"
        global.msg[1] = "%%%"
        break
    case 3004:
        global.msg[0] = "* Froggit hopped close!"
        global.msg[1] = "%%%"
        break
    case 3005:
        global.msg[0] = "* Whimsun approached meekly!"
        global.msg[1] = "%%%"
        break
    case 3006:
        global.msg[0] = "* Froggit and Whimsun drew near!"
        global.msg[1] = "%%%"
        break
    case 3007:
        global.msg[0] = "* Moldsmal blocked the way!"
        global.msg[1] = "%%%"
        break
    case 9999:
        i = 0
        fileid = file_text_open_read("testlines.txt")
        while (file_text_eof(fileid) == 0)
        {
            global.msg[i] = file_text_read_string(fileid)
            file_text_readln(fileid)
            i += 1
        }
        file_text_close(fileid)
        break
}

