using ActionNote.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Data;
using UWPCore.Framework.Common;

namespace ActionNote.Common.Converters
{
    public class ColorCategoryToImageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null)
                return "/Assets/Images/neutral.png"; // error

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
