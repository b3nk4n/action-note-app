using ActionNote.Common.Models;
using System;

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

                    case "orange":
                        color = ColorCategory.Orange;
                        break;

                    default:
                        color = ColorCategory.Neutral;
                        break;
                }
            }

            return color.Value;
        }
    }
}
