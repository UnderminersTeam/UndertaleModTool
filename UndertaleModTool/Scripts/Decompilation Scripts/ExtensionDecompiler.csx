// Made with the help of QuantumV and The United Modders Of Pizza Tower Team
using System.Text;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Text.RegularExpressions;

EnsureDataLoaded();

string extFolder = GetFolder(FilePath) + "Decompiled" + Path.DirectorySeparatorChar + "extensions" + Path.DirectorySeparatorChar;

if (Directory.Exists(extFolder))
{
    Directory.Delete(extFolder, true);
}

Directory.CreateDirectory(extFolder);

public class AssetReference
{
    public string name { get; set; }
    public string path { get; set; }
}

string GetFolder(string path)
{
    return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar;
}

public class GMExtensionFunction {
    public string resourceType = "GMExtensionFunction";
    public string resourceVersion = "1.0";
    public string name;
    public int argCount = 0;
    public List<UndertaleExtensionVarType> args = new List<UndertaleExtensionVarType>{};
    public string documentation = "";
    public string externalName;
    public string help = "";
    public bool hidden = false;
    public uint kind = 1;
    public UndertaleExtensionVarType returnType = UndertaleExtensionVarType.String;
}

public class GMExtensionFile {
    public string resourceType = "GMExtensionFile";
    public string resourceVersion = "1.0";
    public string name = "";
    public string filename;
    public int copyToTargets = -1; 
    public string final = "";
    public string init = "";
    public List<string> order = new List<string>{};
    public string origname = "";
    public List<string> ProxyFiles = new List<string>{};
    public bool uncompress = false;
    public bool usesRunnerInterface = false;
    public UndertaleExtensionKind kind = UndertaleExtensionKind.Dll;
    public List<GMExtensionFunction> functions = new List<GMExtensionFunction>{};
    // idk
    public List<string> constants = new List<string>{};
}

public class GMExtensionOption {
    public string resourceType = "GMExtensionOption";
    public string resourceVersion = "1.2";
    public string name;
    public string defaultValue = "0";
    public string description = "";
    public string displayName = "";
    public bool exportToINI = true;
    public string extensionId;
    public string guid;
    public bool hidden = false;
    public List<string> listItems = new List<string>{};
    public uint optType = 1;
}

public class GMExtension {
    public string resourceType = "GMExtension";
    public string resourceVersion = "1.2";
    public string name;
    public string androidactivityinject = "";
    public string androidclassname = "";
    public string androidcodeinjection = "";
    public string androidinject = "";
    public string androidmanifestinject = "";
    public List<string> androidPermissions = new List<string>{};
    public bool androidProps = false;
    public string androidsourcedir = "";
    public string author = "";
    public string classname = "";
    public int copyToTargets = -1;
    public string date = DateTime.Now.ToString(
        "yyyy-MM-ddTHH:mm:sszzz", System.Globalization.CultureInfo.InvariantCulture
    );
    public string description = "";
    public bool exportToGame = true;
    public string extensionVersion = "1.0.0";
    public List<GMExtensionFile> files = new List<GMExtensionFile>{};
    public string gradleinject = "";
    public bool hasConvertedCodeInjection = true;
    public string helpfile = "";
    public string HTML5CodeInjection = "";
    public bool html5Props = false;
    public List<string> IncludedResources = new List<string>{};
    public string installdir = "";
    public string iosCocoaPodDependencies = "";
    public string iosCocoaPods = "";
    public string ioscodeinjection = "";
    public string iosdelegatename = "";
    public string iosplistinject = "";
    public bool iosProps = false;
    public List<string> iosSystemFrameworkEntries = new List<string>{};
    public List<string> iosThirdPartyFrameworkEntries = new List<string>{};
    public string license = "";
    public string maccompilerflags = "";
    public string maclinkerflags = "";
    public string macsourcedir = "";
    public List<GMExtensionOption> options = new List<GMExtensionOption>{};
    public string optionsFile = "options.json";
    public string packageId = "";
    public string productId = "";
    public string sourcedir = "";
    public int supportedTargets = -1;
    public string tvosclassname = null;
    public string tvosCocoaPodDependencies = "";
    public string tvosCocoaPods = "";
    public string tvoscodeinjection = "";
    public string tvosdelegatename = null;
    public string tvosmaccompilerflags = "";
    public string tvosmaclinkerflags = "";
    public string tvosplistinject = "";
    public bool tvosProps = false;
    public List<string> tvosSystemFrameworkEntries = new List<string>{};
    public List<string> tvosThirdPartyFrameworkEntries = new List<string>{};
    public AssetReference parent;
}

foreach (UndertaleExtension ext in Data.Extensions) {
    string folderName = Path.Combine(extFolder, ext.Name.Content);
    Directory.CreateDirectory(folderName);
    string yyPath = Path.Combine(folderName, ext.Name.Content + ".yy");

    var files = new List<GMExtensionFile>{};
    foreach (UndertaleExtensionFile file in ext.Files) {
        var functions = new List<GMExtensionFunction>{};
        foreach (UndertaleExtensionFunction func in file.Functions) {
            var args = new List<UndertaleExtensionVarType>{};
            foreach (UndertaleExtensionFunctionArg arg in func.Arguments)
            {
                args.Add(arg.Type);
            }
            
            functions.Add(new GMExtensionFunction{
                name = func.Name.Content,
                externalName = func.ExtName.Content,
                kind = func.Kind,
                returnType = func.RetType,
                args = args,
                argCount = args.Count
            });
        }

        files.Add(new GMExtensionFile{
            filename = file.Filename.Content,
            init = file.InitScript.Content,
            final = file.CleanupScript.Content,
            kind = file.Kind,
            functions = functions
        });

        string source = Path.Combine(GetFolder(FilePath), file.Filename.Content);
        string dest = Path.Combine(folderName, file.Filename.Content);
        if (File.Exists(source)) {
            if (File.Exists(dest)) {
                File.Delete(dest);
            }
            System.IO.File.Copy(source, dest, false);
        }
    }

    // fuck it
    // TODO?: options
    var options = new List<GMExtensionOption>{};

    var extData = new GMExtension{
        name = ext.Name.Content,
        classname = ext.ClassName.Content,
        extensionVersion = ext?.Version?.Content ?? "1.0.0",
        files = files,
        options = options,

        parent = new AssetReference{
            name = "Extensions",
            path = "folders/Extensions.yy",
        },
    };

    string exportedyy = JsonConvert.SerializeObject(extData, Formatting.Indented);
    File.WriteAllText(yyPath, exportedyy);
}