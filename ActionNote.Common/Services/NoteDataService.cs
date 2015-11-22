using ActionNote.Common.Models;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.Logging;
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

        private StoredObjectBase<bool> _notesChangedInBackgroundFlag = new LocalObject<bool>("_notesChangedInBackground_", false);
        private StoredObjectBase<bool> _archiveChangedInBackgroundFlag = new LocalObject<bool>("_archiveChangedInBackground_", false);

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

            await Archiv.Load(); // ensure archiv data has loaded
            foreach (var note in Archiv.GetAll())
            {
                if (note.HasAttachement)
                    referencedAttachements.Add(note.AttachementFile);
            }

            var attachementFiles = await _localStorageService.GetFilesAsync(AppConstants.ATTACHEMENT_BASE_PATH);

            if (attachementFiles != null)
            {
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

        public async Task<bool> LoadNotesAsync()
        {
            bool result;
            _notesChangedInBackgroundFlag.Invalidate();
            if (_notesChangedInBackgroundFlag.Value)
            {
                Logger.WriteLine("NOTES RELOAD");
                _notesChangedInBackgroundFlag.Value = false;
                result = await Notes.Reload();
            }
            else
            {
                Logger.WriteLine("NOTES LOAD");
                result = await Notes.Load();
            }
            return result;
        }

        public async Task<bool> LoadArchiveAsync()
        {
            bool result;
            _notesChangedInBackgroundFlag.Invalidate();
            if (_archiveChangedInBackgroundFlag.Value)
            {
                _archiveChangedInBackgroundFlag.Value = false;
                result = await Archiv.Reload();
            }
            else
            {
                result = await Archiv.Load();
            }
            return result;
        }

        public void FlagNotesHaveChangedInBackground()
        {
            _notesChangedInBackgroundFlag.Value = true;
        }

        public void FlagArchiveHasChangedInBackground()
        {
            _archiveChangedInBackgroundFlag.Value = true;
        }
    }
}
