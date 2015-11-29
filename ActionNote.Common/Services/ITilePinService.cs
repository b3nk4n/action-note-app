using ActionNote.Common.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ActionNote.Common.Services
{
    public interface ITilePinService
    {
        Task PinOrUpdateAsync(NoteItem noteItem);

        Task UpdateAsync(NoteItem noteItem);

        /// <summary>
        /// Unpins all tiles that are not referenced in the list of note ids.
        /// </summary>
        /// <param name="noteIds">The list of note ids.</param>
        Task UnpinUnreferencedTilesAsync(IList<string> noteIds);

        bool Contains(string noteId);

        Task UnpinAsync(string noteId);
    }
}
