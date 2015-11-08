using ActionNote.Common.Models;
using System.Threading.Tasks;

namespace ActionNote.Common.Services
{
    public interface ITilePinService
    {
        Task PinOrUpdateAsync(NoteItem noteItem);
        void Update(NoteItem noteItem);
        bool Contains(string noteId);
        Task UnpinAsync(string noteId);
    }
}
