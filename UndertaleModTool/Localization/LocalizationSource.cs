using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace UndertaleModTool.Localization
{
    public class LocalizationSource : INotifyPropertyChanged
    {
        private static readonly LocalizationSource _instance = new();
        public static LocalizationSource Instance => _instance;

        private readonly ResourceManager _manager;

        public event PropertyChangedEventHandler PropertyChanged;

        private CultureInfo _currentCulture = CultureInfo.CurrentUICulture;

        public CultureInfo CurrentCulture
        {
            get => _currentCulture;
            set
            {
                if (_currentCulture.Name != value.Name)
                {
                    _currentCulture = value;
                    CultureInfo.DefaultThreadCurrentUICulture = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                }
            }
        }

        public string this[string key] => _manager.GetString(key, _currentCulture) ?? key;

        public static string GetString(string key) => Instance[key];

        public static string FallbackNoGameLoaded => Instance["Main_FallbackNoGameLoaded"];

        public LocalizationSource()
        {
            _manager = new ResourceManager("UndertaleModTool.Localization.Strings", typeof(LocalizationSource).Assembly);
        }
    }
}
