using ActionNote.Common.Models;
using System.Threading.Tasks;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Note data service interface, to build a facade around notes repository and archive repository.
    /// </summary>
    public interface INoteDataService
    {
        /// <summary>
        /// Gets the notes.
        /// </summary>
        INotesRepository Notes { get; }

        /// <summary>
        /// Gets the notes archiv.
        /// </summary>
        INotesRepository Archiv { get; }

        /// <summary>
        /// Cleans up the unreferences attachement files.
        /// </summary>
        Task CleanUpAttachementFilesAsync();
    }
}
