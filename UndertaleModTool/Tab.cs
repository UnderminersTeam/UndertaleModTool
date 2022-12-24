using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using UndertaleModLib.Models;
using UndertaleModLib;
using System.Windows;

namespace UndertaleModTool
{
    public class Tab : INotifyPropertyChanged
    {
        public static readonly BitmapImage ClosedIcon = new(new Uri(@"/Resources/X.png", UriKind.RelativeOrAbsolute));
        public static readonly BitmapImage ClosedHoverIcon = new(new Uri(@"/Resources/X_Down.png", UriKind.RelativeOrAbsolute));

        private static readonly MainWindow mainWindow = Application.Current.MainWindow as MainWindow;

        public event PropertyChangedEventHandler PropertyChanged;

        private object _currentObject;

        [PropertyChanged.DoNotNotify] // Prevents "PropertyChanged.Invoke()" injection on compile
        public object CurrentObject
        {
            get => _currentObject;
            set
            {
                object prevObj = _currentObject;
                _currentObject = value;

                SetTabTitleBinding(value, prevObj);

                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentObject)));
                mainWindow.RaiseOnSelectedChanged();
            }
        }
        public string TabTitle { get; set; } = "Untitled";
        public bool IsCustomTitle { get; set; }
        public int TabIndex { get; set; }
        public bool AutoClose { get; set; } = false;

        public ObservableCollection<object> History { get; } = new();
        public int HistoryPosition { get; set; }

        public Tab(object obj, int tabIndex, string tabTitle = null)
        {
            CurrentObject = obj;
            TabIndex = tabIndex;
            AutoClose = obj is DescriptionView;

            IsCustomTitle = tabTitle is not null;
            if (IsCustomTitle)
            {
                if (tabTitle.Length > 64)
                    TabTitle = tabTitle[..64] + "...";
                else
                    TabTitle = tabTitle;
            }
        }

        public static string GetTitleForObject(object obj)
        {
            if (obj is null)
                return null;

            string title = null;

            if (obj is DescriptionView view)
            {
                if (view.Heading.Contains("Welcome"))
                {
                    title = "Welcome!";
                }
                else
                {
                    title = view.Heading;
                }
            }
            else if (obj is UndertaleNamedResource namedRes)
            {
                string content = namedRes.Name?.Content;

                string header = obj switch
                {
                    UndertaleAudioGroup => "Audio Group",
                    UndertaleSound => "Sound",
                    UndertaleSprite => "Sprite",
                    UndertaleBackground => "Background",
                    UndertalePath => "Path",
                    UndertaleScript => "Script",
                    UndertaleShader => "Shader",
                    UndertaleFont => "Font",
                    UndertaleTimeline => "Timeline",
                    UndertaleGameObject => "Game Object",
                    UndertaleRoom => "Room",
                    UndertaleExtension => "Extension",
                    UndertaleTexturePageItem => "Texture Page Item",
                    UndertaleCode => "Code",
                    UndertaleVariable => "Variable",
                    UndertaleFunction => "Function",
                    UndertaleCodeLocals => "Code Locals",
                    UndertaleEmbeddedTexture => "Embedded Texture",
                    UndertaleEmbeddedAudio => "Embedded Audio",
                    UndertaleTextureGroupInfo => "Texture Group Info",
                    UndertaleEmbeddedImage => "Embedded Image",
                    UndertaleSequence => "Sequence",
                    UndertaleAnimationCurve => "Animation Curve",
                    _ => null
                };

                if (header is not null)
                    title = header + " - " + content;
                else
                    Debug.WriteLine($"Could not handle type {obj.GetType()}");
            }
            else if (obj is UndertaleString str)
            {
                string stringFirstLine = str.Content;
                if (stringFirstLine is not null)
                {
                    if (stringFirstLine.Length == 0)
                        stringFirstLine = "(empty string)";
                    else
                    {
                        int stringLength = StringTitleConverter.NewLineRegex.Match(stringFirstLine).Index;
                        if (stringLength != 0)
                            stringFirstLine = stringFirstLine[..stringLength] + " ...";
                    }
                }

                title = "String - " + stringFirstLine;
            }
            else if (obj is UndertaleChunkVARI)
            {
                title = "Variables Overview";
            }
            else if (obj is GeneralInfoEditor)
            {
                title = "General Info";
            }
            else if (obj is GlobalInitEditor)
            {
                title = "Global Init";
            }
            else if (obj is GameEndEditor)
            {
                title = "Game End";
            }
            else
            {
                Debug.WriteLine($"Could not handle type {obj.GetType()}");
            }

            if (title is not null)
            {
                // "\t" is displayed as 8 spaces.
                // So, replace all "\t" with spaces,
                // in order to properly shorten the title.
                title = title.Replace("\t", "        ");

                if (title.Length > 64)
                    title = title[..64] + "...";
            }

            return title;
        }

        public static void SetTabTitleBinding(object obj, object prevObj, TextBlock textBlock = null)
        {
            if (textBlock is null)
            {
                var cont = mainWindow.TabController.ItemContainerGenerator.ContainerFromIndex(mainWindow.CurrentTabIndex);
                textBlock = MainWindow.FindVisualChild<TextBlock>(cont);
            }
            else
                obj = (textBlock.DataContext as Tab)?.CurrentObject;

            if (obj is null || textBlock is null)
                return;

            bool objNamed = obj is UndertaleNamedResource;
            bool objString = obj is UndertaleString;

            if (prevObj is not null)
            {
                bool pObjNamed = prevObj is UndertaleNamedResource;
                bool pObjString = prevObj is UndertaleString;

                // If both objects have the same type (one of above)
                // or both objects are not "UndertaleNamedResource",
                // then there's no need to change the binding
                if (pObjNamed && objNamed || pObjString && objString || !(pObjNamed || objNamed))
                    return;
            }

            MultiBinding binding = new()
            {
                Converter = TabTitleConverter.Instance,
                Mode = BindingMode.OneWay
            };
            binding.Bindings.Add(new Binding() { Mode = BindingMode.OneTime });

            // These bindings are only for notification
            binding.Bindings.Add(new Binding("CurrentObject") { Mode = BindingMode.OneWay });
            if (objNamed)
                binding.Bindings.Add(new Binding("CurrentObject.Name.Content") { Mode = BindingMode.OneWay });
            else if (objString)
                binding.Bindings.Add(new Binding("CurrentObject.Content") { Mode = BindingMode.OneWay });

            textBlock.SetBinding(TextBlock.TextProperty, binding);
        }

        public override string ToString()
        {
            // for ease of debugging
            return GetType().FullName + " - {" + CurrentObject?.ToString() + '}';
        }
    }
    public class TabTitleConverter : IMultiValueConverter
    {
        public static TabTitleConverter Instance { get; } = new();

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values[0] is not Tab tab)
                return null;

            if (!tab.IsCustomTitle)
                tab.TabTitle = Tab.GetTitleForObject(tab.CurrentObject);

            return tab.TabTitle;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
