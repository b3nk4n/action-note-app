using ActionNote.Common.Models;
using System;
using UWPCore.Framework.Common;
using Windows.UI;
using Windows.UI.Xaml;

namespace ActionNote.Common.Helpers
{
    public static class ColorCategoryConverter
    {
        /// <summary>
        /// Gets a color category from any string in any language.
        /// </summary>
        /// <param name="value">The color string in any language.</param>
        /// <returns>Returns the color category of the string or NEUTRAL.</returns>
        public static ColorCategory FromAnyString(string value)
        {
            ColorCategory? color = null;
            try
            {
                color = (ColorCategory)Enum.Parse(typeof(ColorCategory), value);
            }
            catch (Exception) { }
            
            if (!color.HasValue)
            {
                switch (value.ToLower())
                {
                    case "rot":
                        color = ColorCategory.Red;
                        break;

                    case "grün":
                        color = ColorCategory.Green;
                        break;

                    case "blau":
                        color = ColorCategory.Blue;
                        break;

                    case "gelb":
                        color = ColorCategory.Yellow;
                        break;

                    case "violett":
                        color = ColorCategory.Violett;
                        break;

                    default:
                        color = ColorCategory.Neutral;
                        break;
                }
            }

            return color.Value;
        }

        public static Color ToColor(ColorCategory category, bool useTheming = true)
        {
            Color color;
            switch (category)
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
                default:
                    if (useTheming)
                    {
                        var theme = UniversalApp.Current.ApplicationTheme;
                        if (theme == ApplicationTheme.Light)
                            color = Colors.Black;
                        else
                            color = Colors.White;
                    }
                    else
                    {
                        color = Colors.White;
                    }       
                    break;
            }

            return color;
        }
    }
}
