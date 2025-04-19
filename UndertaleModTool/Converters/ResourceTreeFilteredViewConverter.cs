using System;
using System.ComponentModel;
using System.Windows.Data;

namespace UndertaleModTool
{
    [ValueConversion(typeof(object), typeof(ICollectionView))]
    public class ResourceTreeFilteredViewConverter : FilteredViewConverter
    {
        protected override Predicate<object> CreateFilter()
        {
            Predicate<object> baseFilter = base.CreateFilter();
            return (obj) =>
            {
                if (!Settings.Instance.ShowNullEntriesInResourceTree && obj is null)
                    return false;
                return baseFilter(obj);
            };
        }
    }
}