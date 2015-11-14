using ActionNote.Common.Models;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI;
using Windows.UI.Xaml.Media;
using ActionNote.Common.Helpers;

namespace ActionNote.Common.Converters
{
    public class ColorCategoryToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            var colorCategory = (ColorCategory)value;

            Color color = ColorCategoryConverter.ToColor(colorCategory);

            return new SolidColorBrush(color);
        }

        /// <summary>
        /// Not supported.
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
