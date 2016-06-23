using ActionNote.Common.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ActionNote.Common.Helpers
{
    public static class NoteUtils
    {
        private static readonly Random random = new Random();

        /// <summary>
        /// Sorts the data depending on the sort type.
        /// </summary>
        /// <param name="noteItems">The note items to sort.</param>
        /// <param name="sortType">The sort type.</param>
        /// <returns>Returns the sorted data.</returns>
        public static IEnumerable<NoteItem> Sort(IList<NoteItem> noteItems, string sortType)
        {
            if (sortType == AppConstants.SORT_DATE)
            {
                return noteItems.OrderByDescending(s => s.ChangedDate);
            }
            else
            {
                var intermediate = noteItems.OrderByDescending(s => s.ChangedDate);
                return intermediate.OrderBy(s => s.Color);
            }
        }

        /// <summary>
        /// Gets a random color category, excluding <see cref="ColorCategory.Random"/>.
        /// </summary>
        /// <returns>Returns a random color.</returns>
        public static ColorCategory GetRandomColor()
        {
            int rnd = random.Next(6);
            ColorCategory enumValue = (ColorCategory)Enum.ToObject(typeof(ColorCategory), rnd);
            return enumValue;
        }
    }
}
