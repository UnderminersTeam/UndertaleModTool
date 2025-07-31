using System.Linq;
using System.IO;

EnsureDataLoaded();

static Random rng;

ScriptMessage("Thank you to redspah for creating HATE, krzys-h for creating DeltaHATE, and anyone else involved with them for being a massive help.");

string seedInput = "0";
if (ScriptQuestion("Randomize seed, or input manually?"))
{
    seedInput = new Random().Next(int.MinValue, int.MaxValue).ToString();
    ScriptMessage("Generated seed: " + seedInput);
}
else
{
    seedInput = SimpleTextInput("Seed", "Enter random seed (number from -2147483648 to 2147483647)", "0", false);
}

rng = new Random(int.Parse(seedInput));

float randomPower = float.Parse(SimpleTextInput("Power", "Higher = more chaos (0.6 recommended). Below 1.0 keeps party sprites safe.", "0.6", false));

bool shuffleSprites = ScriptQuestion("Shuffle sprites?");
bool shuffleBigSprites = ScriptQuestion("Shuffle large sprites?");
bool shuffleAnimatedSprites = ScriptQuestion("Shuffle animated sprites?");
bool shuffleSounds = ScriptQuestion("Shuffle sounds?");
bool shuffleMusic = ScriptQuestion("Shuffle music?");
bool shuffleBackgrounds = ScriptQuestion("Shuffle backgrounds?");
bool shuffleText = ScriptQuestion("Shuffle text?");
bool shuffleFonts = ScriptQuestion("Shuffle fonts?");

if (ScriptQuestion("Go back to change settings?"))
{
    ScriptMessage("Script cancelled. Run again to change settings.");
    return;
}

static void Shuffle<T>(this IList<T> list)
{
    int n = list.Count;
    while (n > 1)
    {
        n--;
        int k = rng.Next(n + 1);
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
    }
}

static void ShuffleOnlySelected<T>(this IList<T> list, IList<int> selected, Action<int, int> swapFunc)
{
    int n = selected.Count;
    while (n > 1)
    {
        n--;
        int k = rng.Next(n + 1);
        swapFunc(selected[n], selected[k]);
        int idx = selected[k];
        selected[k] = selected[n];
        selected[n] = idx;
    }
}

static void ShuffleOnlySelected<T>(this IList<T> list, IList<int> selected)
{
    list.ShuffleOnlySelected(selected, (n, k) => 
    {
        T value = list[k];
        list[k] = list[n];
        list[n] = value;
    });
}

static void SelectSome(this IList<int> list, float amountToKeep)
{
    int toRemove = (int)(list.Count * (1 - amountToKeep));
    for (int i = 0; i < toRemove; i++)
        list.RemoveAt(rng.Next(list.Count));
}

string GameName = Data.GeneralInfo.DisplayName.Content.ToLower();
string dataWinDir = Path.GetDirectoryName(FilePath);
string parentDir = Path.GetDirectoryName(dataWinDir);
bool isFullGame = false;
bool isUndertale = false;

if (GameName.Contains("undertale"))
{
    isUndertale = true;
    ScriptMessage("Detected: UNDERTALE");
    if (ScriptQuestion("Undertale is currently highly unstable, and tends to have softlocks. Continue?"))
    {
        ScriptMessage("Script cancelled.");
        return;
    }
}
else if (GameName.Contains("survey_program"))
{
    ScriptMessage("Detected: DELTARUNE Survey Program Demo");
}
else if (GameName.Contains("chapter 1&2"))
{
    ScriptMessage("Detected: DELTARUNE Chapters 1&2 Demo");
}
else if ((GameName.Contains("chapter 1") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter1")) && !GameName.Contains("demo"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE Chapter 1 (Full Release)");
}
else if ((GameName.Contains("chapter 2") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter2")) && !GameName.Contains("demo"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE Chapter 2 (Full Release)");
}
else if ((GameName.Contains("chapter 3") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter3")) && !GameName.Contains("demo"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE Chapter 3 (Full Release)");
}
else if ((GameName.Contains("chapter 4") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter4")) && !GameName.Contains("demo"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE Chapter 4 (Full Release)");
}
else if ((GameName.Contains("chapter 5") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter5")) && !GameName.Contains("demo"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE Chapter 5 (Full Release)");
}
else if ((GameName.Contains("chapter 6") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter6")) && !GameName.Contains("demo"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE Chapter 6 (Full Release)");
}
else if ((GameName.Contains("chapter 7") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter7")) && !GameName.Contains("demo"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE Chapter 7 (Full Release)");
}
else if (GameName.Contains("chapter") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter"))
{
    isFullGame = true;
    ScriptMessage("Detected: DELTARUNE (Full Release)");
}
else if (GameName == "deltarune")
{
    ScriptMessage("Detected: DELTARUNE Main Menu (Full Release)");
}
else
{
    isUndertale = ScriptQuestion("Is this UNDERTALE?");
    if (!isUndertale)
    {
        isFullGame = ScriptQuestion("Is this the full release of DELTARUNE? (Choose 'No' for demos)");
        if (isFullGame)
            ScriptMessage("Mode: DELTARUNE (Full Release)");
        else
            ScriptMessage("Mode: DELTARUNE Demo");
    }
    else
    {
        ScriptMessage("Mode: UNDERTALE");
    }
}

List<int> tiny_static = new List<int>();
List<int> small_static = new List<int>();
List<int> characterlike_static = new List<int>();
List<int> big_static = new List<int>();
List<int> tiny_animated = new List<int>();
List<int> small_animated = new List<int>();
List<int> characterlike_animated = new List<int>();
List<int> big_animated = new List<int>();
List<int> faces = new List<int>();
List<int> player_sprites = new List<int>();
List<int> heart_sprites = new List<int>();

List<string> excludedSprites = new List<string>
{
    "spr_doorA", "spr_doorB", "spr_doorC", "spr_doorD", "spr_doorX"
};

if (isUndertale)
{
    excludedSprites.AddRange(new string[]
    {
        "spr_maincharal", "spr_maincharau", "spr_maincharar", "spr_maincharad",
        "spr_maincharau_stark", "spr_maincharar_stark", "spr_maincharal_stark",
        "spr_maincharad_pranked", "spr_maincharal_pranked",
        "spr_maincharad_umbrellafall", "spr_maincharau_umbrellafall", "spr_maincharar_umbrellafall", "spr_maincharal_umbrellafall",
        "spr_maincharad_umbrella", "spr_maincharau_umbrella", "spr_maincharar_umbrella", "spr_maincharal_umbrella",
        "spr_charad", "spr_charad_fall", "spr_charar", "spr_charar_fall", "spr_charal", "spr_charal_fall", "spr_charau", "spr_charau_fall",
        "spr_maincharar_shadow", "spr_maincharal_shadow", "spr_maincharau_shadow", "spr_maincharad_shadow",
        "spr_maincharal_tomato", "spr_maincharal_burnt",
        "spr_maincharal_water", "spr_maincharar_water", "spr_maincharau_water", "spr_maincharad_water", "spr_mainchara_pourwater",
        "spr_maincharad_b", "spr_maincharau_b", "spr_maincharar_b", "spr_maincharal_b", "spr_dumbtarget", "spr_target", "spr_battlebg", 
    });
}

if (shuffleFonts)
{
    List<int> en_fonts = new List<int>();
    List<int> ja_fonts = new List<int>();
    for (int i = 0; i < Data.Fonts.Count; i++)
    {
        if (Data.Fonts[i].Name.Content.StartsWith("fnt_ja_"))
            ja_fonts.Add(i);
        else
            en_fonts.Add(i);
    }
    Data.Fonts.ShuffleOnlySelected(en_fonts);
    Data.Fonts.ShuffleOnlySelected(ja_fonts);
}

if (shuffleSprites)
{
    for (int i = 0; i < Data.Sprites.Count; i++)
    {
        var sprite = Data.Sprites[i];
        if (sprite.Name.Content.StartsWith("bg_"))
            continue;
            
        if (sprite.Name.Content.EndsWith("_ja"))
            continue;
        
        if (sprite.Name.Content.EndsWith("bt"))
            continue;

        if (sprite.Name.Content.EndsWith("bt_center"))
            continue;

        if (sprite.Name.Content.EndsWith("bt_hollow"))
            continue;
            
        if (excludedSprites.Contains(sprite.Name.Content))
            continue;
            
        if (sprite.Name.Content.StartsWith("spr_blcon"))
            continue;
            
        bool isAnimated = sprite.Textures.Count > 1;
        
        if (sprite.Name.Content.StartsWith("spr_heart"))
        {
            heart_sprites.Add(i);
        }
        else if (sprite.Name.Content.StartsWith("spr_kris") || sprite.Name.Content.StartsWith("spr_mainchara") ||
            sprite.Name.Content.StartsWith("spr_susie_") || sprite.Name.Content.StartsWith("spr_noelle_") ||
            sprite.Name.Content.StartsWith("spr_ralsei_"))
        {
            player_sprites.Add(i);
        }
        else if (sprite.Name.Content.StartsWith("spr_face_"))
        {
            faces.Add(i);
        }
        else
        {
            if (sprite.Width < 50 && sprite.Height < 50)
            {
                if (isAnimated && shuffleAnimatedSprites)
                    tiny_animated.Add(i);
                else if (!isAnimated)
                    tiny_static.Add(i);
            }
            else if (sprite.Width < 50 && sprite.Height < 100)
            {
                if (isAnimated && shuffleAnimatedSprites)
                    characterlike_animated.Add(i);
                else if (!isAnimated)
                    characterlike_static.Add(i);
            }
            else if (sprite.Width < 100 && sprite.Height < 100)
            {
                if (isAnimated && shuffleAnimatedSprites)
                    small_animated.Add(i);
                else if (!isAnimated)
                    small_static.Add(i);
            }
            else
            {
                if (isAnimated && shuffleAnimatedSprites && shuffleBigSprites)
                    big_animated.Add(i);
                else if (!isAnimated && shuffleBigSprites)
                    big_static.Add(i);
            }
        }
    }
    
    if (shuffleAnimatedSprites)
    {
        tiny_animated.SelectSome(randomPower);
        small_animated.SelectSome(randomPower);
        characterlike_animated.SelectSome(randomPower);
        Data.Sprites.ShuffleOnlySelected(tiny_animated);
        Data.Sprites.ShuffleOnlySelected(small_animated);
        Data.Sprites.ShuffleOnlySelected(characterlike_animated);
    }
    
    if (shuffleBigSprites)
    {
        big_static.SelectSome(randomPower);
        Data.Sprites.ShuffleOnlySelected(big_static);
        
        if (shuffleAnimatedSprites)
        {
            big_animated.SelectSome(randomPower);
            Data.Sprites.ShuffleOnlySelected(big_animated);
        }
    }
    
    tiny_static.SelectSome(randomPower);
    small_static.SelectSome(randomPower);
    characterlike_static.SelectSome(randomPower);
    faces.SelectSome(randomPower);
    heart_sprites.SelectSome(randomPower);
    
    if (randomPower >= 1.0f)
    {
        player_sprites.SelectSome(randomPower);
        Data.Sprites.ShuffleOnlySelected(player_sprites);
    }
    
    Data.Sprites.ShuffleOnlySelected(tiny_static);
    Data.Sprites.ShuffleOnlySelected(small_static);
    Data.Sprites.ShuffleOnlySelected(characterlike_static);
    Data.Sprites.ShuffleOnlySelected(faces);
    Data.Sprites.ShuffleOnlySelected(heart_sprites);
    
    foreach (var obj in Data.GameObjects)
    {
        if (obj is null)
            continue;
        if (!obj.Visible)
            continue;

        if (obj._sprite.CachedId >= 0 && obj.Sprite != null)
        {
            string spriteName = obj.Sprite.Name.Content;
            if (spriteName.EndsWith("bt_ja"))
                continue;

            if (spriteName.EndsWith("bt_center_ja"))
                continue;

            if (spriteName.EndsWith("bt_hollow_ja"))
                continue;
            if (spriteName.EndsWith("_ja"))
            {
                string baseName = spriteName.Substring(0, spriteName.Length - 3);
                var baseSprite = Data.Sprites.FirstOrDefault(s => s.Name.Content == baseName);
                if (baseSprite != null)
                {
                    var jaVariant = Data.Sprites.FirstOrDefault(s => s.Name.Content == baseSprite.Name.Content + "_ja");
                    if (jaVariant != null)
                    {
                        obj.Sprite = jaVariant;
                    }
                    else
                    {
                        obj.Sprite = baseSprite;
                    }
                }
            }
        }
        if (obj._textureMaskId.CachedId >= 0 && obj.TextureMaskId != null)
        {
            string maskName = obj.TextureMaskId.Name.Content;
            if (maskName.EndsWith("_ja"))
            {
                string baseName = maskName.Substring(0, maskName.Length - 3);
                var baseMask = Data.Sprites.FirstOrDefault(s => s.Name.Content == baseName);
                if (baseMask != null)
                {
                    var jaVariant = Data.Sprites.FirstOrDefault(s => s.Name.Content == baseMask.Name.Content + "_ja");
                    if (jaVariant != null)
                    {
                        obj.TextureMaskId = jaVariant;
                    }
                    else
                    {
                        obj.TextureMaskId = baseMask;
                    }
                }
            }
        }
    }
}

if (shuffleBackgrounds)
{
    List<int> backgrounds = new List<int>();
    for (int i = 0; i < Data.Sprites.Count; i++)
    {
        var sprite = Data.Sprites[i];
        if (sprite.Name.Content.StartsWith("bg_"))
        {
            if (sprite.Name.Content.EndsWith("_ja"))
                continue;
                
            backgrounds.Add(i);
        }
    }
    backgrounds.SelectSome(randomPower);
    Data.Sprites.ShuffleOnlySelected(backgrounds);
}

if (shuffleSounds)
{
    if (isUndertale)
    {
        List<int> soundEffects = new List<int>();
        for (int i = 0; i < Data.Sounds.Count; i++)
        {
            var sound = Data.Sounds[i];
            if (sound.File != null && sound.Name.Content.StartsWith("snd_"))
            {
                if (sound.Name.Content.EndsWith("_ja"))
                    continue;
                    
                soundEffects.Add(i);
            }
        }
        Data.Sounds.ShuffleOnlySelected(soundEffects);
    }
    else
    {
        List<int> internalSounds = new List<int>();
        for (int i = 0; i < Data.Sounds.Count; i++)
        {
            var sound = Data.Sounds[i];
            if (sound.File != null)
            {
                if (sound.Name.Content.EndsWith("_ja"))
                    continue;
                    
                internalSounds.Add(i);
            }
        }
        Data.Sounds.ShuffleOnlySelected(internalSounds);
    }
}

if (shuffleMusic)
{
    if (isUndertale)
    {
        try
        {
            string gameRootDir = Path.GetDirectoryName(FilePath);
            var musicFiles = Directory.GetFiles(gameRootDir, "mus_*.ogg").ToList();
            if (musicFiles.Count > 0)
            {
                var musicData = new List<byte[]>();
                foreach (string file in musicFiles)
                {
                    musicData.Add(File.ReadAllBytes(file));
                }
                
                musicData.Shuffle();
                
                for (int i = 0; i < musicFiles.Count; i++)
                {
                    File.WriteAllBytes(musicFiles[i], musicData[i]);
                }
            }
            
            List<int> musicInternal = new List<int>();
            for (int i = 0; i < Data.Sounds.Count; i++)
            {
                var sound = Data.Sounds[i];
                if (sound.File != null && sound.Name.Content.StartsWith("mus_"))
                {
                    musicInternal.Add(i);
                }
            }
            Data.Sounds.ShuffleOnlySelected(musicInternal);
        }
        catch
        {
        }
    }
    else
    {
        try
        {
            string dataWinDir = Path.GetDirectoryName(FilePath);
            string gameRootDir = Path.GetDirectoryName(dataWinDir);
            string musDir = Path.Combine(gameRootDir, "mus");
            
            if (Directory.Exists(musDir))
            {
                var musicFiles = Directory.GetFiles(musDir, "*.ogg").ToList();
                if (musicFiles.Count > 0)
                {
                    var musicData = new List<byte[]>();
                    foreach (string file in musicFiles)
                    {
                        musicData.Add(File.ReadAllBytes(file));
                    }
                    
                    musicData.Shuffle();
                    
                    for (int i = 0; i < musicFiles.Count; i++)
                    {
                        File.WriteAllBytes(musicFiles[i], musicData[i]);
                    }
                }
            }
        }
        catch
        {
        }
    }
}

void StringSwap(int n, int k)
{
    string value = Data.Strings[k].Content;
    Data.Strings[k].Content = Data.Strings[n].Content;
    Data.Strings[n].Content = value;
}

List<(int, int)> swapHistory = new List<(int, int)>();

void RecordingStringSwap(int n, int k)
{
    string value = Data.Strings[k].Content;
    Data.Strings[k].Content = Data.Strings[n].Content;
    Data.Strings[n].Content = value;
    swapHistory.Add((n, k));
}

bool ShouldExcludeString(string str)
{
    if (string.IsNullOrEmpty(str))
        return true;
        
    if (!str.Contains(" ") && !str.Contains("&"))
        return true;
        
    if (str.StartsWith("scr_") || str.StartsWith("gml_") || str.StartsWith("mus_") || 
        str.StartsWith("audio_") || str.StartsWith("sound_"))
        return true;
        
    if (str.EndsWith("_0"))
        return true;
        
    if (str.Contains("_"))
        return true;
        
    if (str.Contains("/"))
    {
        if (!str.EndsWith("/%") && !str.EndsWith("/%%") && !str.EndsWith("/%%%") && !str.EndsWith("/"))
            return true;
    }
    
    if (str.Contains("}") && str.Count(c => c == '}') > str.Length / 4)
        return true;
    
    int weirdCharCount = str.Count(c => c < 32 || c > 126 && c < 0x3000);
    if (weirdCharCount > str.Length / 2)
        return true;
    
    return false;
}

bool IsJapaneseString(string str)
{
    foreach (char c in str)
    {
        if ((c >= 0x3040 && c <= 0x309F) ||
            (c >= 0x30A0 && c <= 0x30FF) ||
            (c >= 0x4E00 && c <= 0x9FAF))
        {
            return true;
        }
    }
    return false;
}

bool IsChoiceDialogue(string str)
{
    return str.StartsWith("#") || isUndertale && str.Contains("\\C");
}

bool HasSpecialFormatting(string str)
{
    return str.Contains("~");
}

if (shuffleText)
{
    if (isUndertale)
    {
        List<int> choice_dialogue_lines = new List<int>();
        List<int> special_format_lines = new List<int>();
        List<int> final_lines = new List<int>();
        List<int> continue_lines = new List<int>();
        List<int> waiting_lines = new List<int>();
        List<int> waiting_final_lines = new List<int>();
        List<int> waiting_continue_lines = new List<int>();
        List<int> backslash_lines = new List<int>();
        List<int> other_lines = new List<int>();
        
        List<int> jp_choice_dialogue_lines = new List<int>();
        List<int> jp_special_format_lines = new List<int>();
        List<int> jp_final_lines = new List<int>();
        List<int> jp_continue_lines = new List<int>();
        List<int> jp_waiting_lines = new List<int>();
        List<int> jp_waiting_final_lines = new List<int>();
        List<int> jp_waiting_continue_lines = new List<int>();
        List<int> jp_backslash_lines = new List<int>();
        List<int> jp_other_lines = new List<int>();
        
        for (int i = 0; i < Data.Strings.Count; i++)
        {
            var str = Data.Strings[i].Content;
            if (str.Length <= 3 || str.Any(x => x > 127) || ShouldExcludeString(str))
                continue;
            
            bool isJapanese = IsJapaneseString(str);
            
            if (IsChoiceDialogue(str))
            {
                if (isJapanese) jp_choice_dialogue_lines.Add(i);
                else choice_dialogue_lines.Add(i);
            }
            else if (HasSpecialFormatting(str))
            {
                if (isJapanese) jp_special_format_lines.Add(i);
                else special_format_lines.Add(i);
            }
            else if (str.StartsWith("\\"))
            {
                if (isJapanese) jp_backslash_lines.Add(i);
                else backslash_lines.Add(i);
            }
            else if (str.EndsWith("/%%"))
            {
                if (isJapanese) jp_waiting_final_lines.Add(i);
                else waiting_final_lines.Add(i);
            }
            else if (str.EndsWith("/%"))
            {
                if (isJapanese) jp_waiting_continue_lines.Add(i);
                else waiting_continue_lines.Add(i);
            }
            else if (str.EndsWith("/"))
            {
                if (isJapanese) jp_waiting_lines.Add(i);
                else waiting_lines.Add(i);
            }
            else if (str.EndsWith("%%"))
            {
                if (isJapanese) jp_final_lines.Add(i);
                else final_lines.Add(i);
            }
            else if (str.EndsWith("%"))
            {
                if (isJapanese) jp_continue_lines.Add(i);
                else continue_lines.Add(i);
            }
            else
            {
                if (isJapanese) jp_other_lines.Add(i);
                else other_lines.Add(i);
            }
        }
        
        Data.Strings.ShuffleOnlySelected(choice_dialogue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(special_format_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(backslash_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_final_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_continue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(final_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(continue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(other_lines, RecordingStringSwap);
        
        Data.Strings.ShuffleOnlySelected(jp_choice_dialogue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_special_format_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_backslash_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_waiting_final_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_waiting_continue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_waiting_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_final_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_continue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(jp_other_lines, RecordingStringSwap);
    }
    else if (isFullGame)
    {
        List<int> choicer_old_lines = new List<int>();
        List<int> choicer_neo_2_lines = new List<int>();
        List<int> choicer_neo_3_lines = new List<int>();
        List<int> choicer_neo_4_lines = new List<int>();
        List<int> choice_dialogue_lines = new List<int>();
        List<int> special_format_lines = new List<int>();
        List<int> final_lines = new List<int>();
        List<int> continue_lines = new List<int>();
        List<int> waiting_lines = new List<int>();
        List<int> waiting_final_lines = new List<int>();
        List<int> waiting_continue_lines = new List<int>();
        List<int> dash_whatever_that_is = new List<int>();
        List<int> other_lines = new List<int>();
        
        for (int i = 0; i < Data.Strings.Count; i++)
        {
            var str = Data.Strings[i].Content;
            if (str.Length <= 3 || str.Any(x => x > 127) || ShouldExcludeString(str))
                continue;
                
            if (IsChoiceDialogue(str))
                choice_dialogue_lines.Add(i);
            else if (HasSpecialFormatting(str))
                special_format_lines.Add(i);
            else if (str.Contains("\\\\C1"))
                choicer_old_lines.Add(i);
            else if (str.Contains("\\\\C2"))
                choicer_neo_2_lines.Add(i);
            else if (str.Contains("\\\\C3"))
                choicer_neo_3_lines.Add(i);
            else if (str.Contains("\\\\C4"))
                choicer_neo_4_lines.Add(i);
            else if (str.EndsWith("/%%"))
                waiting_final_lines.Add(i);
            else if (str.EndsWith("/%"))
                waiting_continue_lines.Add(i);
            else if (str.EndsWith("/"))
                waiting_lines.Add(i);
            else if (str.EndsWith("%%"))
                final_lines.Add(i);
            else if (str.EndsWith("%"))
                continue_lines.Add(i);
            else if (str.EndsWith("-"))
                dash_whatever_that_is.Add(i);
            else
                other_lines.Add(i);
        }
        
        Data.Strings.ShuffleOnlySelected(choice_dialogue_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(special_format_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_old_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_neo_2_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_neo_3_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_neo_4_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_final_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_continue_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(final_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(continue_lines, StringSwap);
        Data.Strings.ShuffleOnlySelected(dash_whatever_that_is, StringSwap);
        Data.Strings.ShuffleOnlySelected(other_lines, StringSwap);
    }
    else
    {
        Dictionary<string, string> translations = new Dictionary<string, string>();
        Dictionary<string, string> jaTranslations = new Dictionary<string, string>();
        string langDir = Path.Combine(Path.GetDirectoryName(FilePath), "lang");
        
        string[] possibleEnFiles = {
            Path.Combine(langDir, "lang_en.json"),
            Path.Combine(langDir, "lang_en_ch1.json")
        };
        
        string[] possibleJaFiles = {
            Path.Combine(langDir, "lang_ja.json"),
            Path.Combine(langDir, "lang_ja_ch1.json")
        };
        
        bool useJson = false;
        bool hasJapanese = false;
        
        foreach (string langPath in possibleEnFiles)
        {
            if (File.Exists(langPath))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(langPath))
                    {
                        string[] a = line.Split(new char[] { ':' }, 2);
                        if (a.Length != 2)
                            continue;
                        a[0] = a[0].Trim();
                        if (a[0].Length < 3)
                            continue;
                        a[0] = a[0].Substring(1, a[0].Length - 2);
                        a[1] = a[1].Trim();
                        if (a[1].Length < 3)
                            continue;
                        a[1] = a[1].Substring(1, a[1].Length - 3);
                        if (a[0] == "date")
                            continue;
                        if (a[1] == "||")
                            continue;
                        translations.Add(a[0], a[1]);
                    }
                    useJson = true;
                    break;
                }
                catch
                {
                }
            }
        }
        
        foreach (string jaPath in possibleJaFiles)
        {
            if (File.Exists(jaPath))
            {
                try
                {
                    foreach (string line in File.ReadAllLines(jaPath))
                    {
                        string[] a = line.Split(new char[] { ':' }, 2);
                        if (a.Length != 2)
                            continue;
                        a[0] = a[0].Trim();
                        if (a[0].Length < 3)
                            continue;
                        a[0] = a[0].Substring(1, a[0].Length - 2);
                        a[1] = a[1].Trim();
                        if (a[1].Length < 3)
                            continue;
                        a[1] = a[1].Substring(1, a[1].Length - 3);
                        if (a[0] == "date")
                            continue;
                        if (a[1] == "||")
                            continue;
                        jaTranslations.Add(a[0], a[1]);
                    }
                    hasJapanese = true;
                    break;
                }
                catch
                {
                }
            }
        }
        
        List<int> choicer_old_lines = new List<int>();
        List<int> choicer_neo_2_lines = new List<int>();
        List<int> choicer_neo_3_lines = new List<int>();
        List<int> choicer_neo_4_lines = new List<int>();
        List<int> choice_dialogue_lines = new List<int>();
        List<int> special_format_lines = new List<int>();
        List<int> final_lines = new List<int>();
        List<int> continue_lines = new List<int>();
        List<int> waiting_lines = new List<int>();
        List<int> waiting_final_lines = new List<int>();
        List<int> waiting_continue_lines = new List<int>();
        List<int> dash_whatever_that_is = new List<int>();
        List<int> other_lines = new List<int>();
        
        for (int i = 0; i < Data.Strings.Count; i++)
        {
            string str;
            if (useJson)
            {
                var id = Data.Strings[i].Content;
                if (!translations.ContainsKey(id))
                    continue;
                str = translations[id];
            }
            else
            {
                str = Data.Strings[i].Content;
            }
            
            if (str.Length <= 3 || str.Any(x => x > 127) || ShouldExcludeString(str))
                continue;
                
            if (IsChoiceDialogue(str))
                choice_dialogue_lines.Add(i);
            else if (HasSpecialFormatting(str))
                special_format_lines.Add(i);
            else if (str.Contains("\\\\C1"))
                choicer_old_lines.Add(i);
            else if (str.Contains("\\\\C2"))
                choicer_neo_2_lines.Add(i);
            else if (str.Contains("\\\\C3"))
                choicer_neo_3_lines.Add(i);
            else if (str.Contains("\\\\C4"))
                choicer_neo_4_lines.Add(i);
            else if (str.EndsWith("/%%"))
                waiting_final_lines.Add(i);
            else if (str.EndsWith("/%"))
                waiting_continue_lines.Add(i);
            else if (str.EndsWith("/"))
                waiting_lines.Add(i);
            else if (str.EndsWith("%%"))
                final_lines.Add(i);
            else if (str.EndsWith("%"))
                continue_lines.Add(i);
            else if (str.EndsWith("-"))
                dash_whatever_that_is.Add(i);
            else
                other_lines.Add(i);
        }
        
        Data.Strings.ShuffleOnlySelected(choice_dialogue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(special_format_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_old_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_neo_2_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_neo_3_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(choicer_neo_4_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_final_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_continue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(waiting_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(final_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(continue_lines, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(dash_whatever_that_is, RecordingStringSwap);
        Data.Strings.ShuffleOnlySelected(other_lines, RecordingStringSwap);
        
        if (hasJapanese)
        {
            try
            {
                if (useJson)
                {
                    foreach ((int n, int k) in swapHistory)
                    {
                        var idN = Data.Strings[n].Content;
                        var idK = Data.Strings[k].Content;
                        
                        if (jaTranslations.ContainsKey(idN) && jaTranslations.ContainsKey(idK))
                        {
                            string temp = jaTranslations[idN];
                            jaTranslations[idN] = jaTranslations[idK];
                            jaTranslations[idK] = temp;
                        }
                    }
                    
                    string[] jaFiles = possibleJaFiles.Where(File.Exists).ToArray();
                    if (jaFiles.Length > 0)
                    {
                        var jaLines = new List<string>();
                        jaLines.Add("{");
                        jaLines.Add("  \"date\": \"" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + "\",");
                        foreach (var kvp in jaTranslations)
                        {
                            string escapedKey = kvp.Key.Replace("\\", "\\\\").Replace("\"", "\\\"");
                            string escapedValue = kvp.Value.Replace("\\", "\\\\").Replace("\"", "\\\"");
                            jaLines.Add($"  \"{escapedKey}\": \"{escapedValue}\",");
                        }
                        if (jaLines.Count > 2)
                            jaLines[jaLines.Count - 1] = jaLines[jaLines.Count - 1].TrimEnd(',');
                        jaLines.Add("}");
                        File.WriteAllLines(jaFiles[0], jaLines);
                    }
                }
                else
                {
                    var jaStrings = new List<string>();
                    foreach (var kvp in jaTranslations)
                    {
                        jaStrings.Add(kvp.Value);
                    }
                    
                    foreach ((int n, int k) in swapHistory)
                    {
                        if (n < jaStrings.Count && k < jaStrings.Count)
                        {
                            string temp = jaStrings[n];
                            jaStrings[n] = jaStrings[k];
                            jaStrings[k] = temp;
                        }
                    }
                    
                    string[] jaFiles = possibleJaFiles.Where(File.Exists).ToArray();
                    if (jaFiles.Length > 0)
                    {
                        var jaLines = new List<string>();
                        jaLines.Add("{");
                        jaLines.Add("  \"date\": \"" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + "\",");
                        int index = 0;
                        foreach (var kvp in jaTranslations)
                        {
                            string escapedKey = kvp.Key.Replace("\\", "\\\\").Replace("\"", "\\\"");
                            string escapedValue = (index < jaStrings.Count ? jaStrings[index] : kvp.Value).Replace("\\", "\\\\").Replace("\"", "\\\"");
                            jaLines.Add($"  \"{escapedKey}\": \"{escapedValue}\",");
                            index++;
                        }
                        if (jaLines.Count > 2)
                            jaLines[jaLines.Count - 1] = jaLines[jaLines.Count - 1].TrimEnd(',');
                        jaLines.Add("}");
                        File.WriteAllLines(jaFiles[0], jaLines);
                    }
                }
            }
            catch
            {
            }
        }
    }
}

foreach (var obj in Data.GameObjects)
{
    if (obj is null)
        continue;
    if (!obj.Visible)
        continue;
    if (obj._sprite.CachedId >= 0)
        obj.Sprite = Data.Sprites[obj._sprite.CachedId];
    if (obj._textureMaskId.CachedId >= 0)
        obj.TextureMaskId = Data.Sprites[obj._textureMaskId.CachedId];
}

string completionMessage = "💧︎☜︎👍︎☼︎☜︎❄︎ 💣︎☜︎💧︎💧︎✌︎☝︎☜︎";

if (isUndertale)
{
    completionMessage = "💧︎☜︎👍︎☼︎☜︎❄︎ 💣︎☜︎💧︎💧︎✌︎☝︎☜︎";
}
else if (GameName.Contains("survey_program"))
{
    completionMessage = "Chaos, chaos!";
}
else if (GameName.Contains("chapter 1") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter1"))
{
    completionMessage = "Chaos, chaos!";
}
else if (GameName.Contains("chapter 2") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter2"))
{
    completionMessage = "[[HYPERLINK BLOCKED]]";
}
else if (GameName.Contains("chapter 1&2") || parentDir != null && Path.GetFileName(parentDir).StartsWith("deltarunedemo"))
{
    completionMessage = "Your choices mattered.";
}
else if (GameName.Contains("chapter 3") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter3"))
{
    completionMessage = "It's TV Time!";
}
else if (GameName.Contains("chapter 4") || parentDir != null && Path.GetFileName(parentDir).StartsWith("chapter4"))
{
    completionMessage = "Gyaa Ha ha!";
}
else if (GameName == "deltarune")
{
    completionMessage = "Your choices mattered.";
}

ScriptMessage(completionMessage + "\n\nRandomization complete!\nRemember to save.\nUsed seed: " + seedInput);