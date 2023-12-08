using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using UndertaleModLib;

namespace UndertaleModTool
{
    [ValueConversion(typeof(object), typeof(ICollectionView))]
    public class FilteredViewConverter : DependencyObject, IValueConverter
    {
        public static DependencyProperty FilterProperty =
            DependencyProperty.Register("Filter", typeof(string),
                typeof(FilteredViewConverter),
                new FrameworkPropertyMetadata(null));

        public string Filter
        {
            get { return (string)GetValue(FilterProperty); }
            set { SetValue(FilterProperty, value); }
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return null;
            ICollectionView filteredView = CollectionViewSource.GetDefaultView(value);
            filteredView.Filter = (obj) =>
            {
                if (String.IsNullOrEmpty(Filter))
                    return true;
                if (obj is ISearchable)
                    return (obj as ISearchable)?.SearchMatches(Filter) ?? false;
                if (obj is UndertaleNamedResource)
                    return ((obj as UndertaleNamedResource)?.Name?.Content?.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0;
                if (obj is object[] links)
                    return links.Select(x => x is UndertaleNamedResource res ? res.Name?.Content : x.ToString())
                                .Any(x => (x?.IndexOf(Filter, StringComparison.OrdinalIgnoreCase) ?? -1) >= 0);
                return true;
            };
            return filteredView;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
