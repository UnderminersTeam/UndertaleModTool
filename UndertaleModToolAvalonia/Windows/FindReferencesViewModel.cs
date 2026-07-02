using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using PropertyChanged.SourceGenerator;
using UndertaleModLib;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class FindReferencesViewModel
{
    public IView? View;

    public MainViewModel MainVM { get; }

    [Notify]
    private bool _IsEnabled = true;

    [Notify]
    private UndertaleResource? _Resource;
    [Notify]
    private ObservableCollection<FindReferencesResult> _Results = [];

    ILoaderWindow? loaderWindow;

    public FindReferencesViewModel(IServiceProvider serviceProvider, UndertaleResource? resource = null)
    {
        MainVM = serviceProvider.GetRequiredService<MainViewModel>();
        Resource = resource;
    }

    public async void FindReferences()
    {
        Results.Clear();

        if (MainVM.Data is null)
            return;
        if (Resource is null)
            return;

        // Set up loader window
        loaderWindow = View!.LoaderOpen();
        loaderWindow.SetText("Finding references...");

        IsEnabled = false;
        MainVM.IsEnabled = false;

        Results = await Task.Run(() =>
        {
            UndertaleResource? resource = Resource;

            UndertaleData data = MainVM.Data;
            Assembly currentAssembly = typeof(UndertaleData).Assembly;

            ObservableCollection<FindReferencesResult> results = [];
            Stack propStack = new();

            // TODO: This is really bad, remove
            void RecurseProperties(object obj, UndertaleObject? topObject = null, string propChain = "")
            {
                Type objType = obj.GetType();

                if (propStack.Contains(obj))
                    return;

                propStack.Push(obj);

                if (obj is UndertaleObject objResource && objResource is UndertaleResource or UndertaleGeneralInfo or UndertaleOptions or UndertaleLanguage or UndertaleGlobalInit)
                {
                    // Update top resource
                    if (topObject is null)
                    {
                        topObject = objResource;
                        propChain = "";
                    }
                }

                // Check if value is the correct one
                if (obj == resource)
                {
                    if (topObject is not null)
                        results.Add(new(topObject, propChain));
                }

                // If it's a resource, and not the current top object, stop right there,
                // there's no point in looping anymore since those should already be dealt with in the first pass
                if (obj is UndertaleResource && obj != topObject)
                {
                    propStack.Pop();
                    return;
                }

                // If it's a list, loop its values
                if (obj is IList objList)
                {
                    foreach (object? item in objList)
                    {
                        if (item is not null)
                        {
                            var t = item.GetType();
                            if (t.IsPrimitive || t == typeof(string) || t.IsEnum)
                                break;

                            // Add []
                            RecurseProperties(item, topObject, $"{propChain}[{item}]");
                        }
                    }
                }
                // If not, then loop its properties
                // Must be in the same assembly, to avoid complications
                else if (objType.Assembly == currentAssembly)
                {
                    PropertyInfo[] props = objType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
                    foreach (PropertyInfo prop in props)
                    {
                        // Ignore properties that require indexes
                        if (prop.GetIndexParameters().Length != 0)
                            continue;

                        object? propValue = prop.GetValue(obj);
                        if (propValue is null)
                            continue;

                        var t = prop.PropertyType;
                        if (t.IsPrimitive || t == typeof(string) || t.IsEnum)
                            continue;

                        // Add . if not empty
                        RecurseProperties(propValue, topObject, $"{(propChain != "" ? propChain + "." : "")}{prop.Name}");
                    }
                }

                propStack.Pop();
            }

            RecurseProperties(data);

            return results;
        });

        // Close loader window
        loaderWindow.Close();

        IsEnabled = true;
        MainVM.IsEnabled = true;
    }

    public void OpenResult(FindReferencesResult result, bool inNewTab = false)
    {
        MainVM.TabOpen(result.Resource, inNewTab);
    }

    public class FindReferencesResult
    {
        public string AssetType { get; }
        public string Name { get; }
        public string Property { get; }

        public UndertaleObject Resource;

        public FindReferencesResult(UndertaleObject resource, string property)
        {
            AssetType = resource.GetType().Name;
            if (resource is UndertaleNamedResource namedResource)
            {
                Name = namedResource.Name?.Content ?? "<unknown>";
            }
            else
            {
                Name = resource.ToString() ?? "<unknown>";
            }
            Property = property;
            Resource = resource;
        }
    }
}
