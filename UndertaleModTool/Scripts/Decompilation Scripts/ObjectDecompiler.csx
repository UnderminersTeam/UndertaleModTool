// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UndertaleModLib.Util;
using System.Collections.Generic;

EnsureDataLoaded();

string objectFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "objects" + Path.DirectorySeparatorChar;

if (Directory.Exists(objectFolder))
{
    Directory.Delete(objectFolder, true);
}

public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }
}
public class GMEvent
{
    public string resourceType { get; set; } = "GMEvent";
    public string resourceVersion { get; set; }  = "1.0";
    public string name { get; set; } = "";
    public bool isDnD { get; set; }  = false;
    public uint eventNum { get; set; } = 0;
    public uint eventType { get; set; } = 0;
    public AssetReference collisionObjectId { get; set; } = null;
}
public class GMObjectProperty
{
    public string resourceType { get; set; } = "GMObjectProperty";

    public string resourceVersion { get; set; } = "1.0";

    public string name { get; set; }

    public int varType { get; set; } = 0;

    public string value { get; set; }
    public bool rangeEnabled { get; set; } = false;
    public double rangeMin { get; set; } = 0.0;
    public double rangeMax { get; set; } = 10.0;
    public List<string> listItems { get; set; } = new List<string>{};
    public bool multiselect { get; set; } = false;
    public List<string> filters { get; set; } = new List<string>{};
}

public class ObjectData
{
    public string resourceType { get; set; } = "GMObject";

    public string resourceVersion { get; set; } = "1.0";

    public string name { get; set; }

    public AssetReference spriteId { get; set; } = new AssetReference();
    public AssetReference spriteMaskId { get; set; } = new AssetReference();
    public bool visible { get; set; }

    public bool solid { get; set; }
    public bool persistent { get; set; }
    public bool managed { get; set; }
    public AssetReference parentObjectId { get; set; } = new AssetReference();
    public List<GMEvent> eventList { get; set; } = new List<GMEvent>();
    public List<GMObjectProperty> properties { get; set; } = new List<GMObjectProperty>();
    public AssetReference parent { get; set; } = new AssetReference();
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

ThreadLocal<GlobalDecompileContext> DECOMPILE_CONTEXT = new ThreadLocal<GlobalDecompileContext>(() => new GlobalDecompileContext(Data, false));

Regex assignmentRegex = new Regex(
    @"^(\w+) = (.+)$",
    RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.ECMAScript
);
// get variable definitions from a precreate event
List<GMObjectProperty> GetObjectProperties(UndertalePointerList<UndertaleGameObject.Event> evList)
{
    List<GMObjectProperty> list = new List<GMObjectProperty>{};
    if (evList == null) return list;
    foreach (UndertaleGameObject.Event ev in evList)
    {
        foreach (UndertaleGameObject.EventAction action in ev.Actions)
        {
            UndertaleCode code = action.CodeId;
            if (code == null) continue;
            string gml = "";
            try {
                gml = Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value);
            } catch (Exception e) {}
            foreach (Match match in assignmentRegex.Matches(gml)) {
                list.Add(new GMObjectProperty{
                    varType = 4, // expression
                    name = match.Groups[1].Captures[0].Value,
                    value = match.Groups[2].Captures[0].Value,
                });
            }
        }
    }
    return list;
}

SetProgressBar(null, "Objects", 0, Data.GameObjects.Count);
StartProgressBarUpdater();
await Task.Run(() => Parallel.ForEach(Data.GameObjects, (UndertaleGameObject gameObject) => {
    string objectDir = objectFolder + gameObject.Name.Content + Path.DirectorySeparatorChar;
    Directory.CreateDirectory(objectDir);
    ObjectData objectData = new ObjectData()
    {
        name = gameObject.Name.Content,
        spriteId = gameObject.Sprite != null ? new AssetReference()
        {
            name = gameObject.Sprite.Name.Content,
            path = $"sprites/{gameObject.Sprite.Name.Content}/{gameObject.Sprite.Name.Content}.yy"
        } : null,
        spriteMaskId = gameObject.TextureMaskId != null ? new AssetReference()
        {
            name = gameObject.TextureMaskId.Name.Content,
            path = $"sprites/{gameObject.TextureMaskId.Name.Content}/{gameObject.TextureMaskId.Name.Content}.yy"
        } : null,
        visible = gameObject.Visible,
        solid = gameObject.Solid,
        persistent = gameObject.Persistent,
        managed = gameObject.Managed,
        parentObjectId = gameObject.ParentId != null ? new AssetReference()
        {
            name = gameObject.ParentId.Name.Content,
            path = $"objects/{gameObject.ParentId.Name.Content}/{gameObject.ParentId.Name.Content}.yy"
        } : null,
        parent = new AssetReference()
        {
            name = "Objects",
            path = "folders/Objects.yy"
        },
    };
    for (var i = 0; i < gameObject.Events.Count; i++)
    {
        var evList = gameObject.Events[i];
        // PreCreate is used by variable definitions
        if ((EventType)i == EventType.PreCreate)
        {
            objectData.properties = GetObjectProperties(evList);
            continue;
        }
        foreach (var ev in evList)
        {
            AssetReference collObjRef = null;
            uint subtype = ev.EventSubtype;
            if ((EventType)i == EventType.Collision)
            {
                subtype = 0;
                var collObj = Data.GameObjects[(int)ev.EventSubtype];
                if (collObj != null)
                {
                    collObjRef = new AssetReference()
                    {
                        name = collObj.Name.Content,
                        path = $"objects/{collObj.Name.Content}/{collObj.Name.Content}.yy"
                    };
                }
            }
            objectData.eventList.Add(new GMEvent()
            {
                eventType = (uint)i,
                eventNum = subtype,
                collisionObjectId = collObjRef
            });
            
            if (ev.Actions.Count > 0)
            {
                var action = ev.Actions[0];
                var code = action.CodeId;
                var subtypeString = subtype.ToString();
                if ((EventType)i == EventType.Collision)
                {
                    subtypeString = Data.GameObjects[(int)ev.EventSubtype].Name.Content;
                }
                var gmlPath = $"{objectDir}{((EventType)i).ToString()}_{subtypeString}.gml";
                try
                {
                    File.WriteAllText(gmlPath, (code != null ? Decompiler.Decompile(code, DECOMPILE_CONTEXT.Value) : ""));
                }
                catch (Exception e)
                {
                    File.WriteAllText(gmlPath, "/*\nDECOMPILER FAILED!\n\n" + e.ToString() + "\n*/");
                }
            }
        }
    }
    string json = JsonConvert.SerializeObject(objectData, Formatting.Indented);
    File.WriteAllText(objectDir + gameObject.Name.Content + ".yy", json);
    IncrementProgressParallel();
}));

await StopProgressBarUpdater();
HideProgressBar();