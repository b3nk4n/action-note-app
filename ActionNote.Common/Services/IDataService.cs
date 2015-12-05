using ActionNote.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActionNote.Common.Services
{
    public enum SyncResult
    {
        Success,
        Unchanged,
        Nop,
        Failed
    }

    public enum UpdateResult
    {
        Success,
        Deleted,
        Nop,
        Failed
    }

    /// <summary>
    /// Note data service interface, to build a facade around notes repository and archive repository.
    /// </summary>
    public interface IDataService
    {
        Task<IList<string>> GetAllNoteIds();

        Task<IList<NoteItem>> GetAllNotes();

        int NotesCount { get; }

        Task<NoteItem> GetNote(string id);

        Task<bool> ContainsNote(string id);

        Task<IList<NoteItem>> GetAllArchives();

        int ArchivesCount { get; }

        /// <summary>
        /// Moves the note to the archive.
        /// </summary>
        /// <param name="noteItem">The note item to delete.</param>
        Task<bool> MoveToArchiveAsync(NoteItem noteItem);

        Task<bool> MoveRangeToArchiveAsync(IList<NoteItem> items);

        /// <summary>
        /// Removes the note permanently from archive.
        /// </summary>
        /// <param name="noteItem">The note item to delete.</param>
        /// <returns>Returns true when delete was successfule, else nothing was deleted.</returns>
        Task<bool> RemoveFromArchiveAsync(NoteItem noteItem);

        /// <summary>
        /// Removes all permanently from archive.
        /// </summary>
        /// <returns>Returns true when clear was successfule, else nothing was deleted.</returns>
        Task<bool> RemoveAllFromArchiveAsync();

        /// <summary>
        /// Cleans up the unreferences attachement files.
        /// </summary>
        Task CleanUpAttachementFilesAsync();

        Task<bool> LoadNotesAsync();

        Task<bool> LoadArchiveAsync();

        void FlagNotesNeedReload();

        void FlagArchiveNeedsReload();

        Task<bool> AddNoteAsync(NoteItem item);

        Task<UpdateResult> UpdateNoteAsync(NoteItem item);

        Task<SyncResult> SyncNotesAsync();

        Task<bool> UploadAttachement(NoteItem noteItem, bool createUnsyncItem = true);

        Task UploadMissingAttachements();

        Task RemoveUnsyncedEntry(NoteItem item);

        /// <summary>
        /// Downloads an attachmenent in case it is missing.
        /// </summary>
        /// <param name="noteItem">The note item to download its attachement.</param>
        /// <returns>Returns True when a file was downloaded, else False.</returns>
        Task<bool> DownloadAttachement(NoteItem noteItem);

        /// <summary>
        /// Downloads missing attachements.
        /// </summary>
        /// <returns>Returns True when at least one file was downloaded, else False.</returns>
        Task<bool> DownloadMissingAttachements();

        /// <summary>
        /// Checks whether the user is logged in, and in case he is not, the Live-Login pops up.
        /// </summary>
        /// <returns>Returns True when the user is logged in successfully (or already was), else False.</returns>
        Task<bool> CheckUserAndLogin();

        /// <summary>
        /// Indicates whether the service will perform sync operations.
        /// </summary>
        bool IsSynchronizationActive { get; }

        /// <summary>
        /// Gets whether the user login is still pending.
        /// </summary>
        bool IsUserLoginPending { get; }

        /// <summary>
        /// Gets whether the data service runs as the pro version.
        /// </summary>
        bool IsProVersion { get; }

        /// <summary>
        /// Gets whether a synchronization has been performed in this session. Used by auto sync
        /// to ensure not to auto-synced multiple times during one session.
        /// </summary>
        bool HasSyncedInThisSession { get; }

        /// <summary>
        /// Gets or sets whether the user already clicked NO to login in this session.
        /// </summary>
        bool HasDeniedToLoginInThisSession { get; set; } 
    }
}
