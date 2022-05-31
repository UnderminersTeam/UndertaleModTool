EnsureDataLoaded();

string x = "String[] order = {\"";
foreach (string key in Data.FORM.Chunks.Keys)  
{
    if (key != "AUDO")
        x += (key + "\", \"");
    else
        x += (key + "\"};");
}
return x;
