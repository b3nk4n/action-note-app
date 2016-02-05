using ActionNote.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Service interface to manage the action center.
    /// </summary>
    public interface IActionCenterService
    {
        /// <summary>
        /// Adds a new notification at the top, even when there is the quick notes already.
        /// </summary>
        /// <param name="noteItem">The note item to add.</param>
        void AddToTop(NoteItem noteItem);

        /// <summary>
        /// Clears the action center history.
        /// </summary>
        void Clear();

        /// <summary>
        /// Refreshes the whole action center.
        /// </summary>
        /// <param name="noteItems">The notes items that are in the repo and have to be in the action center.</param>
        void RefreshAsync(IList<NoteItem> noteItems);

        /// <summary>
        /// Gets whether the quick notes are in the action center or not.
        /// </summary>
        /// <returns>True if it is in the action center, else false.</returns>
        bool ContainsQuickNotes();

        /// <summary>
        /// Adds the QuickNotes to the action center.
        /// </summary>
        void AddQuickNotes();

        /// <summary>
        /// Removes the QuickNotes from the action center.
        /// </summary>
        void RemoveQuickNotes();

        /// <summary>
        /// Deletes all notes from the repository that are not referenced in the action center.
        /// </summary>
        /// <param name="noteItems">The notes items that are in the repository.</param>
        /// <returns>Returns the list of IDs that have to be moved to trash.</returns>
        IList<NoteItem> DiffWithNotesInActionCenter(IList<NoteItem> noteItems);

        /// <summary>
        /// Starts the remove blicking timer via a global setting.
        /// </summary>
        /// <param name="seconds">The block duration.</param>
        void StartTemporaryRemoveBlocking(int seconds);

        /// <summary>
        /// Check whether the action center is remove-blocked.
        /// </summary>
        /// <returns>Returns True when it is remove blocked, else False.</returns>
        bool IsRemoveBlocked();

        void StartTemporaryRefreshBlocking(int seconds);

        bool IsRefreshBlocked();

        /// <summary>
        /// Gets the number of notes (without the quicknotes notification).
        /// </summary>
        int NotesCount { get; }
    }
}
