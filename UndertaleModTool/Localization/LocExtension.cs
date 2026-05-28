using System;
using System.Windows.Data;
using System.Windows.Markup;

namespace UndertaleModTool.Localization
{
    public class LocExtension : MarkupExtension
    {
        public string Key { get; }

        public LocExtension(string key)
        {
            Key = key;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            var binding = new Binding($"[{Key}]")
            {
                Source = LocalizationSource.Instance,
                Mode = BindingMode.OneWay
            };
            return binding.ProvideValue(serviceProvider);
        }
    }
}
