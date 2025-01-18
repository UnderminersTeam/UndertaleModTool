using System;

namespace UndertaleModTool
{
    public partial class DataHierarchyFilteredViewConverter : FilteredViewConverter
    {
        internal override Predicate<object> CreateFilter()
        {
            Predicate<object> baseFilter = base.CreateFilter();
            return (obj) =>
            {
                if (!Settings.Instance.ShowNullEntriesInDataHierarchy && obj is null)
                    return false;
                return baseFilter(obj);
            };
        }
    }
}