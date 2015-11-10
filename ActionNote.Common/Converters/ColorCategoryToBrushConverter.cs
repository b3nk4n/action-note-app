using ActionNote.Common.Models;
using System;
using Windows.UI.Xaml.Data;
using Windows.UI;
using Windows.UI.Xaml.Media;

namespace ActionNote.Common.Converters
{
    public class ColorCategoryToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            Color color;

            if (value == null)
                color = Colors.White;

            var colorCategory = (ColorCategory)value;

            switch (colorCategory)
            {
                case ColorCategory.Red:
                    color = AppConstants.COLOR_RED;
                    break;
                case ColorCategory.Blue:
                    color = AppConstants.COLOR_BLUE;
                    break;
                case ColorCategory.Green:
                    color = AppConstants.COLOR_GREEN;
                    break;
                case ColorCategory.Yellow:
                    color = AppConstants.COLOR_YELLOW;
                    break;
                case ColorCategory.Violett:
                    color = AppConstants.COLOR_VIOLETT;
                    break;
                case ColorCategory.Orange:
                    color = AppConstants.COLOR_ORANGE;
                    break;
                default:
                    color = AppConstants.COLOR_WHITE;
                    break;
            }

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
