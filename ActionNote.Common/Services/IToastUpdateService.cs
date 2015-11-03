using ActionNote.Common.Models;

namespace ActionNote.Common.Services
{
    public interface IToastUpdateService
    {
        void Refresh(INotesRepository notesRepository);
        void DeleteNotesThatAreMissingInActionCenter(INotesRepository notesRepository);
    }
}
