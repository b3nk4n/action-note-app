using System;
using UWPCore.Framework.Common;
using Windows.UI.Xaml.Data;

namespace ActionNote.Common.Converters
{
    public class FlagUnflagConverter : IValueConverter
    {
        private static readonly Localizer _localizer = new Localizer("ActionNote.Common");

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? _localizer.Get("Swipe.MarkUnimportant") : _localizer.Get("Swipe.MarkImportant");
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
