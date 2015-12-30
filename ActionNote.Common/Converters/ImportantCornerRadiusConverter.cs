using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace ActionNote.Common.Converters
{
    public class ImportantCornerRadiusConverter : IValueConverter
    {
        private readonly CornerRadius IMPORTANT_RADIUS = new CornerRadius(6);
        private readonly CornerRadius UNIMPORTANT_RADIUS = new CornerRadius(4);

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool)
            {
                if ((bool)value)
                    return IMPORTANT_RADIUS;
            }

            return UNIMPORTANT_RADIUS;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
