using ActionNote.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Note data service interface, to build a facade around notes repository and archive repository.
    /// </summary>
    public interface IDataService
    {
        /// <summary>
        /// Gets the notes.
        /// Indirect access here, bacause the notes are synched.
        /// </summary>
        //INotesRepository Notes { get; }

        /// <summary>
        /// Gets the notes archiv.
        /// Direct access here, because the archive is not synced.
        /// </summary>
        INotesRepository Archiv { get; }

        Task<IList<string>> GetAllNoteIds();

        Task<IList<NoteItem>> GetAllNotes();

        int NotesCount { get; }

        Task<NoteItem> GetNote(string id);

        Task<bool> ContainsNote(string id);

        /// <summary>
        /// Moves the note to the archive.
        /// </summary>
        /// <param name="noteItem">The note item to delete.</param>
        Task<bool> MoveToArchivAsync(NoteItem noteItem);

        /// <summary>
        /// Cleans up the unreferences attachement files.
        /// </summary>
        Task CleanUpAttachementFilesAsync();

        Task<bool> LoadNotesAsync();

        Task<bool> LoadArchiveAsync();

        void FlagNotesHaveChangedInBackground();

        void FlagArchiveHasChangedInBackground();

        Task<bool> AddNoteAsync(NoteItem item);

        Task<bool> UpdateNoteAsync(NoteItem item);

        Task<bool> SyncNotesAsync();

        Task<bool> UploadAttachement(NoteItem noteItem);

        Task UploadMissingAttachements();

        Task RemoveUnsyncedEntry(NoteItem item);

        Task<bool> DownloadAttachement(NoteItem noteItem);

        Task DownloadMissingAttachements();
    }
}
