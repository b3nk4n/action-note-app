using System.Threading.Tasks;
using UWPCore.Framework.Data;

namespace ActionNote.Common.Models
{
    public interface IUnsyncedRepository : IRepository<UnsyncedItem, string>
    {
        /// <summary>
        /// Sets the base folder.
        /// </summary>
        string BaseFolder { set; }

        /// <summary>
        /// Saves the repository data to disk.
        /// </summary>
        /// <returns>Returns True for success, else False.</returns>
        Task<bool> Save(UnsyncedItem item);
    }
}
