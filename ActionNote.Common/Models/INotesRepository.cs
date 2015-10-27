using UWPCore.Framework.Data;

namespace ActionNote.Common.Models
{
    /// <summary>
    /// Marker interface to get rid of the generics.
    /// </summary>
    public interface INotesRepository : IRepository<NoteItem, string>
    {
    }
}
