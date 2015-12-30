using System;
using Windows.UI.Xaml.Data;

namespace ActionNote.Common.Converters
{
    public class ImportantWidthConverter : IValueConverter
    {
        private const int IMPORTANT_WIDTH = 12;
        private const int UNIMPORTANT_WIDTH = 8;

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                if ((bool)value)
                    return IMPORTANT_WIDTH;
            }

            return UNIMPORTANT_WIDTH;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
