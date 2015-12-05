using System;
using Windows.UI.Xaml.Data;

namespace ActionNote.Common.Converters
{
    public class FlagUnflagConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return (bool)value ? "Mark as unimportant" : "Mark as important"; // TODO: translate
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
