using ActionNote.Common.Models;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.Storage;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Note data service class, to build a facade around notes repository and archive repository.
    /// </summary>
    public class NoteDataService : INoteDataService
    {
        private IStorageService _localStorageService;

        public INotesRepository Notes { get; private set; }
        public INotesRepository Archiv { get; private set; }

        [Inject]
        public NoteDataService(INotesRepository notesRepository, INotesRepository archivRepository, ILocalStorageService localStorageService)
        {
            Notes = notesRepository;
            Notes.BaseFolder = "data/";
            Archiv = archivRepository;
            Archiv.BaseFolder = "archiv/";

            _localStorageService = localStorageService;
        }

        public void MoveToArchiv(NoteItem noteItem)
        {
            Archiv.Add(noteItem);
            Archiv.Save(noteItem);

            Notes.Remove(noteItem);
        }

        public async Task CleanUpAttachementFilesAsync()
        {
            var referencedAttachements = new List<string>();

            foreach (var note in Notes.GetAll())
            {
                if (note.HasAttachement)
                    referencedAttachements.Add(note.AttachementFile);
            }

            foreach (var note in Archiv.GetAll())
            {
                if (note.HasAttachement)
                    referencedAttachements.Add(note.AttachementFile);
            }

            var attachementFiles = await _localStorageService.GetFilesAsync(AppConstants.ATTACHEMENT_BASE_PATH);
            foreach (var attachementFile in attachementFiles)
            {
                if (!referencedAttachements.Contains(attachementFile.Name))
                {
                    try
                    {
                        await attachementFile.DeleteAsync();
                    }
                    catch (Exception) { }
                }
            }
        }
    }
}
