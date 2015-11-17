using ActionNote.Common.Models;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Service interface to manage the action center.
    /// </summary>
    public interface IToastUpdateService
    {
        /// <summary>
        /// Refreshes the whole action center.
        /// </summary>
        /// <param name="notesRepository">The notes repository to get the data.</param>
        void Refresh(INotesRepository notesRepository);

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
    }
}
