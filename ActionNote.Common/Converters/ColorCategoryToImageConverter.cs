using ActionNote.Common.Models;
using System;
using Windows.UI.Xaml.Data;
using UWPCore.Framework.Common;

namespace ActionNote.Common.Converters
{
    public class ColorCategoryToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null || value is NoteItem) // TODO: why was it an NOTE ITEM !?!?
                return "/Assets/Images/neutral.png"; // error                               ==> combo-box ... scrollrad hoch ... kein icon statt neutral!!! 

            var color = (ColorCategory)value;
            return string.Format("/Assets/Images/{0}.png", color.ToString().FirstLetterToLower());
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
