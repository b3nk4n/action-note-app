using System.Threading.Tasks;
using UWPCore.Framework.Data;

namespace ActionNote.Common.Models
{
    /// <summary>
    /// Marker interface to get rid of the generics.
    /// </summary>
    public interface INotesRepository : IRepository<NoteItem, string>
    {
        /// <summary>
        /// Sets the base folder.
        /// </summary>
        string BaseFolder { set; }

        /// <summary>
        /// Saves the repository data to disk.
        /// </summary>
        /// <returns>Returns True for success, else False.</returns>
        Task<bool> Save(NoteItem item);
    }
}
