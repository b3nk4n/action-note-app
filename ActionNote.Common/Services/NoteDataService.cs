using ActionNote.Common.Models;
using ActionNote.Common.Services.Communication;
using Ninject;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Data;
using UWPCore.Framework.Logging;
using UWPCore.Framework.Networking;
using UWPCore.Framework.Storage;

using Windows.Web.Http;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Note data service class, to build a facade around notes repository and archive repository.
    /// </summary>
    public class NoteDataService : INoteDataService
    {
        private const int DEFAULT_TIMEOUT = 5000;

        private IStorageService _localStorageService;

        public INotesRepository Notes { get; private set; }
        public INotesRepository Archiv { get; private set; }
        private IUnsyncedRepository Unsynced { get; set; }

        private IHttpService _httpService;
        private ISerializationService _serializationService;
        private INetworkInfoService _networkInfoService;

        private StoredObjectBase<bool> _notesChangedInBackgroundFlag = new LocalObject<bool>("_notesChangedInBackground_", false);
        private StoredObjectBase<bool> _archiveChangedInBackgroundFlag = new LocalObject<bool>("_archiveChangedInBackground_", false);

        [Inject]
        public NoteDataService(INotesRepository notesRepository, INotesRepository archivRepository, IUnsyncedRepository unsyncedRepository, ILocalStorageService localStorageService,
            IHttpService httpService, ISerializationService serializationService, INetworkInfoService networkInfoService)
        {
            Notes = notesRepository;
            Notes.BaseFolder = "data/";
            Archiv = archivRepository;
            Archiv.BaseFolder = "archiv/";
            Unsynced = unsyncedRepository; // TODO: make sure to reload after app resume? (like notes and archive)
            Unsynced.BaseFolder = "unsynced/";

            _localStorageService = localStorageService;
            _httpService = httpService;
            _serializationService = serializationService;
            _networkInfoService = networkInfoService;
        }

        public async Task<IList<NoteItem>> GetAllNotes()
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            return Notes.GetAll();
        }

        public async Task<IList<string>> GetAllNoteIds()
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            return Notes.GetAllIds();
        }

        public async Task<int> NotesCount()
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            return Notes.Count;
        }

        public async Task<NoteItem> GetNote(string id)
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            return Notes.Get(id);
        }

        public async Task<bool> ContainsNote(string id)
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            return Notes.Contains(id);
        }

        public async Task<bool> AddNoteAsync(NoteItem item)
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            // update the timestamp
            item.ChangedDate = DateTimeOffset.Now;

            if (await Notes.Save(item))
            {
                Notes.Add(item);

                if (_networkInfoService.HasInternet)
                {
                    var res = await _httpService.PostJsonAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/add/XXXXXXXXXX"), item, DEFAULT_TIMEOUT);

                    if (res != null &&
                        res.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }

                // sync error handling
                var unsyncedItem = new UnsyncedItem(item.Id, UnsyncedType.Added);
                Unsynced.Add(unsyncedItem);
                await Unsynced.Save(unsyncedItem);
            }
            
            return false;
        }

        public async Task<bool> UpdateNoteAsync(NoteItem item)
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            // update the timestamp
            item.ChangedDate = DateTimeOffset.Now;

            if (await Notes.Save(item))
            {
                Notes.Update(item);

                if (_networkInfoService.HasInternet)
                {
                    var res = await _httpService.PutAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/update/XXXXXXXXXX"), item, DEFAULT_TIMEOUT);

                    if (res != null &&
                        res.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }

                // sync error handling
                await Unsynced.Load(); // ensure loaded
                var unsyncedItem = new UnsyncedItem(item.Id, UnsyncedType.Updated);
                Unsynced.Add(unsyncedItem);
                await Unsynced.Save(unsyncedItem);
            }
            
            return false;
        }

        public async Task<bool> SyncNotesAsync()
        {
            if (!_networkInfoService.HasInternet)
            {
                // no error handling needed here
                return false;
            }

            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            var syncDataRequest = new SyncDataRequest();
            foreach (var noteItem in Notes.GetAll())
            {
                syncDataRequest.Data.Add(new SyncDataRequestItem(noteItem.Id, noteItem.ChangedDate));
            }

            var res = await _httpService.PostJsonAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/sync/XXXXXXXXXX"), syncDataRequest, DEFAULT_TIMEOUT);

            if (res != null &&
                res.IsSuccessStatusCode)
            {
                var jsonResponse = await res.Content.ReadAsStringAsync();
                var syncDataResult = _serializationService.DeserializeJson<SyncDataResponse>(jsonResponse);

                if (syncDataResult != null)
                {
                    foreach (var note in syncDataResult.Changed)
                    {
                        if (Notes.Contains(note.Id))
                        {
                            Notes.Update(note);
                        }
                        await Notes.Save(note);
                    }

                    // ensure archiv data has loaded
                    if (!Archiv.HasLoaded)
                        await Archiv.Load();

                    foreach (var note in syncDataResult.Added)
                    {
                        if (!Notes.Contains(note.Id) &&
                            !Archiv.Contains(note.Id))
                        {
                            Notes.Add(note);
                        }
                        await Notes.Save(note);
                    }

                    foreach (var note in syncDataResult.Deleted)
                    {
                        if (Notes.Contains(note.Id))
                        {
                            await MoveToArchivInternalAsync(note);
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> UploadAttachement(NoteItem item)
        {
            if (!item.HasAttachement)
                return true;

            if (_networkInfoService.HasInternet)
            {
                string filePath = AppConstants.ATTACHEMENT_BASE_PATH + item.AttachementFile;
                var file = await _localStorageService.GetFileAsync(filePath);
                using (var stream = await file.OpenReadAsync())
                {
                    var content = new HttpMultipartFormDataContent();
                    content.Add(new HttpStreamContent(stream), "file", item.AttachementFile);

                    var res = await _httpService.PostAsync(new Uri(AppConstants.SERVER_BASE_PATH + "attachements/file/XXXXXXXXXX/" + item.AttachementFile), content, DEFAULT_TIMEOUT);

                    if (res != null &&
                        res.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
            }

            // sync error handling
            await Unsynced.Load(); // ensure loaded
            var unsyncedItem = new UnsyncedItem(item.Id, UnsyncedType.FileUpload);
            Unsynced.Add(unsyncedItem);
            await Unsynced.Save(unsyncedItem);
            return false;
        }

        public async Task<bool> DownloadAttachement(NoteItem item)
        {
            if (!item.HasAttachement)
                return true;

            if (_networkInfoService.HasInternet)
            {
                var res = await _httpService.GetAsync(new Uri(AppConstants.SERVER_BASE_PATH + "attachements/file/XXXXXXXXXX/" + item.AttachementFile), 5000);

                if (res != null &&
                    res.IsSuccessStatusCode)
                {
                    string filePath = AppConstants.ATTACHEMENT_BASE_PATH + item.AttachementFile;
                    var buffer = await res.Content.ReadAsBufferAsync();
                    if (await _localStorageService.WriteFile(filePath, buffer))
                    {
                        return true;
                    }
                }
            }

            // sync error handling
            // TODO: no sync error handling needed, in case we would load as needed? think about what is best
            await Unsynced.Load(); // ensure loaded
            var unsyncedItem = new UnsyncedItem(item.Id, UnsyncedType.FileUpload);
            Unsynced.Add(unsyncedItem);
            await Unsynced.Save(unsyncedItem);
            return false;
        }

        public async Task<bool> MoveToArchivAsync(NoteItem item)
        {
            if (await MoveToArchivInternalAsync(item))
            {
                var res = await _httpService.DeleteAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/markdelete/XXXXXXXXXX/" + item.Id), DEFAULT_TIMEOUT);

                if (res != null &&
                    res.IsSuccessStatusCode)
                {
                    return true;
                }
                else
                {
                    // sync error handling
                    await Unsynced.Load(); // ensure loaded
                    var unsyncedItem = new UnsyncedItem(item.Id, UnsyncedType.Deleted);
                    Unsynced.Add(unsyncedItem);
                    await Unsynced.Save(unsyncedItem);
                    return false;
                }
            }

            // here we did not even moved the file locally
            return false;
        }

        private async Task<bool> MoveToArchivInternalAsync(NoteItem item)
        {
            // ensure notes data has loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            // ensure archiv data has loaded
            if (!Archiv.HasLoaded)
                await Archiv.Load();

            if (await Archiv.Save(item))
            {
                Notes.Remove(item);
                Archiv.Add(item);
                return true;
            }
            
            return false;
        }

        public async Task CleanUpAttachementFilesAsync()
        {
            // ensure notes data has loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            var referencedAttachements = new List<string>();

            foreach (var note in Notes.GetAll())
            {
                if (note.HasAttachement)
                    referencedAttachements.Add(note.AttachementFile);
            }

            // ensure archiv data has loaded
            if (!Archiv.HasLoaded)
                await Archiv.Load(); 

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
