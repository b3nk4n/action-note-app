using ActionNote.Common.Models;
using System.Collections.Generic;
using System.Linq;

namespace ActionNote.Common.Helpers
{
    public static class NoteUtils
    {
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
    }
}
