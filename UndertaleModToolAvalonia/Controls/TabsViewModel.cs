using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using UndertaleModLib.Models;

namespace UndertaleModToolAvalonia;

public partial class TabsViewModel : ObservableObject
{
    public MainViewModel MainVM;

    public ObservableCollection<TabItemViewModel> Tabs { get; set; } = [];

    [ObservableProperty]
    public partial TabItemViewModel? TabSelected { get; set; }

    [ObservableProperty]
    public partial int TabSelectedIndex { get; set; }

    public TabsViewModel(MainViewModel mainVM)
    {
        MainVM = mainVM;
    }

    public async Task<TabItemViewModel?> TabOpen(object? item, bool inNewTab = false)
    {
        ITabContent? content = item switch
        {
            DescriptionViewModel vm => vm,
            _ => null,
        };

        if (content is null)
        {
            if (MainVM.Data is null)
                return null;

            content = item switch
            {
                "GeneralInfo" => new GeneralInfoViewModel(MainVM.Data),
                "GlobalInitScripts" => new GlobalInitScriptsViewModel(MainVM.Data.FORM.GLOB.List),
                "GameEndScripts" => new GameEndScriptsViewModel(MainVM.Data.FORM.GMEN.List),
                UndertaleAudioGroup r => new UndertaleAudioGroupViewModel(r),
                UndertaleSound r => new UndertaleSoundViewModel(r, MainVM.ServiceProvider),
                UndertaleSprite r => new UndertaleSpriteViewModel(r, MainVM.ServiceProvider),
                UndertaleBackground r => new UndertaleBackgroundViewModel(r),
                UndertalePath r => new UndertalePathViewModel(r),
                UndertaleScript r => new UndertaleScriptViewModel(r),
                UndertaleShader r => new UndertaleShaderViewModel(r, MainVM.ServiceProvider),
                UndertaleFont r => new UndertaleFontViewModel(r),
                UndertaleTimeline r => new UndertaleTimelineViewModel(r),
                UndertaleGameObject r => new UndertaleGameObjectViewModel(r, MainVM.ServiceProvider),
                UndertaleRoom r => new UndertaleRoomViewModel(r, MainVM.ServiceProvider),
                "Extensions" => new UndertaleExtensionChunkViewModel(MainVM.Data.FORM.EXTN),
                UndertaleExtension r => new UndertaleExtensionViewModel(r, MainVM.ServiceProvider),
                UndertaleTexturePageItem r => new UndertaleTexturePageItemViewModel(r, MainVM.ServiceProvider),
                UndertaleCode r => new UndertaleCodeViewModel(r, MainVM.ServiceProvider),
                UndertaleVariable r => new UndertaleVariableViewModel(r),
                UndertaleFunction r => new UndertaleFunctionViewModel(r),
                UndertaleCodeLocals r => new UndertaleCodeLocalsViewModel(r),
                UndertaleString r => new UndertaleStringViewModel(r),
                UndertaleEmbeddedTexture r => new UndertaleEmbeddedTextureViewModel(r, MainVM.ServiceProvider),
                UndertaleEmbeddedAudio r => new UndertaleEmbeddedAudioViewModel(r, MainVM.ServiceProvider),
                UndertaleTextureGroupInfo r => new UndertaleTextureGroupInfoViewModel(r),
                UndertaleEmbeddedImage r => new UndertaleEmbeddedImageViewModel(r),
                UndertaleAnimationCurve r => new UndertaleAnimationCurveViewModel(r),
                UndertaleParticleSystem r => new UndertaleParticleSystemViewModel(r),
                UndertaleParticleSystemEmitter r => new UndertaleParticleSystemEmitterViewModel(r),
                _ => null,
            };
        }

        if (content is not null)
        {
            if (!inNewTab && TabSelected is not null)
            {
                if (!await TabGoTo(content))
                    return null;
                return TabSelected;
            }
            else
            {
                TabItemViewModel tab = new(content);
                Tabs.Add(tab);
                TabSelected = tab;
                tab.OnOpen();
                return tab;
            }
        }

        return null;
    }

    [RelayCommand]
    public async Task TabClose(TabItemViewModel tab)
    {
        if (!await tab.Save())
            return;

        tab.OnClose();

        TabItemViewModel? selected = TabSelected;
        int index = TabSelectedIndex;

        Tabs.Remove(tab);

        if (TabSelected != selected)
        {
            if (index >= Tabs.Count)
                index = Tabs.Count - 1;

            TabSelectedIndex = index;
        }
    }

    public async Task TabCloseAll()
    {
        foreach (TabItemViewModel tab in Tabs.ToList())
        {
            await TabClose(tab);
        }
    }

    public async void TabCloseSelected()
    {
        if (TabSelected is not null)
            _ = TabClose(TabSelected);
    }

    public void TabCloseAllWithoutSaving()
    {
        foreach (TabItemViewModel tab in Tabs.ToList())
        {
            tab.OnClose();
        }
        Tabs.Clear();
    }

    public async Task<bool> TabSaveAll()
    {
        bool savedAll = true;

        foreach (TabItemViewModel tab in Tabs)
        {
            if (!await tab.Save())
                savedAll = false;
        }

        return savedAll;
    }

    public async Task<bool> TabGoTo(ITabContent content)
    {
        if (TabSelected is not null)
            if (!await TabSelected.GoTo(content))
                return false;

        MainVM.UpdateSelectedTabProperties();
        return true;
    }

    public void TabSetToPrevious()
    {
        if (TabSelectedIndex > 0)
            TabSelectedIndex--;
        else
            TabSelectedIndex = Tabs.Count - 1;
    }

    public void TabSetToNext()
    {
        if (TabSelectedIndex < Tabs.Count - 1)
            TabSelectedIndex++;
        else
            TabSelectedIndex = 0;
    }

    public async void TabGoBack()
    {
        if (TabSelected is not null)
            if (!await TabSelected.GoBack())
                return;

        MainVM.UpdateSelectedTabProperties();
    }

    public async void TabGoForward()
    {
        if (TabSelected is not null)
            if (!await TabSelected.GoForward())
                return;

        MainVM.UpdateSelectedTabProperties();
    }

    partial void OnTabSelectedChanged(TabItemViewModel? value)
    {
        MainVM.UpdateSelectedTabProperties();
    }
}
