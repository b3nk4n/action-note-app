using ActionNote.Common.Models;
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
        /// <param name="notesRepository">The notes repository to get the data.</param>
        Task RefreshAsync(INotesRepository notesRepository);

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
        /// <param name="notesRepository">The notes repository to get and remove the data.</param>
        void DeleteNotesThatAreMissingInActionCenter(INotesRepository notesRepository);

        /// <summary>
        /// Starts the remove blicking timer via a global setting.
        /// </summary>
        void StartTemporaryRemoveBlocking();

        /// <summary>
        /// Check whether the action center is remove-blocked.
        /// </summary>
        /// <returns>Returns True when it is remove blocked, else False.</returns>
        bool IsRemoveBlocked();
    }
}
