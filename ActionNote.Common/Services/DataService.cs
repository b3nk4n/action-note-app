using ActionNote.Common.Models;
using ActionNote.Common.Services.Communication;
using Ninject;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Accounts;
using UWPCore.Framework.Data;
using UWPCore.Framework.Logging;
using UWPCore.Framework.Networking;
using UWPCore.Framework.Storage;
using UWPCore.Framework.Store;
using Windows.Web.Http;

namespace ActionNote.Common.Services
{
    /// <summary>
    /// Note data service class, to build a facade around notes repository and archive repository.
    /// </summary>
    public class DataService : IDataService
    {
        private const int DEFAULT_TIMEOUT = 5000;

        private IStorageService _localStorageService;

        public INotesRepository Notes { get; private set; }
        public INotesRepository Archiv { get; private set; }
        private IUnsyncedRepository Unsynced { get; set; }

        private IHttpService _httpService;
        private ISerializationService _serializationService;
        private INetworkInfoService _networkInfoService;
        private ILicenseService _licenseService;
        private IOnlineIdService _onlineIdService;

        private StoredObjectBase<bool> _notesChangedInBackgroundFlag = new LocalObject<bool>("_notesChangedInBackground_", false);
        private StoredObjectBase<bool> _archiveChangedInBackgroundFlag = new LocalObject<bool>("_archiveChangedInBackground_", false);

        [Inject]
        public DataService(INotesRepository notesRepository, INotesRepository archivRepository, IUnsyncedRepository unsyncedRepository, ILocalStorageService localStorageService,
            IHttpService httpService, ISerializationService serializationService, INetworkInfoService networkInfoService, ILicenseService licenseService,
            IOnlineIdService onlineIdService)
        {
            Notes = notesRepository;
            Notes.BaseFolder = "data/";
            Archiv = archivRepository;
            Archiv.BaseFolder = "archiv/";
            Unsynced = unsyncedRepository;
            Unsynced.BaseFolder = "unsynced/";

            _localStorageService = localStorageService;
            _httpService = httpService;
            _serializationService = serializationService;
            _networkInfoService = networkInfoService;
            _licenseService = licenseService;
            _onlineIdService = onlineIdService;
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

        /// <summary>
        /// Attention: It is not ensured, that the data has loaded!!!
        /// </summary>
        public int NotesCount
        {
            get
            {
                return Notes.Count;
            }
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

                if (IsSynchronizationActive &&
                    _networkInfoService.HasInternet)
                {
                    var res = await _httpService.PostJsonAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/add/" + UserId), item, DEFAULT_TIMEOUT);

                    if (res != null &&
                        res.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }
            }
            
            return false;
        }

        public async Task<UpdateResult> UpdateNoteAsync(NoteItem item)
        {
            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            // update the timestamp
            item.ChangedDate = DateTimeOffset.Now;

            if (await Notes.Save(item))
            {
                Notes.Update(item);

                if (IsSynchronizationActive)
                {
                    if (_networkInfoService.HasInternet)
                    {
                        var res = await _httpService.PutAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/update/" + UserId), item, DEFAULT_TIMEOUT);

                        if (res != null &&
                            res.IsSuccessStatusCode)
                        {
                            var jsonResponse = await res.Content.ReadAsStringAsync();
                            var serverResponse = _serializationService.DeserializeJson<ServerResponse>(jsonResponse);

                            if (serverResponse != null)
                            {
                                if (serverResponse.Message == ServerResponse.DELETED)
                                {
                                    await MoveToArchivInternalAsync(item);
                                    return UpdateResult.Deleted;
                                }
                            }

                            return UpdateResult.Success;
                        }
                        else
                        {
                            return UpdateResult.Failed;
                        }
                    }
                    else
                    {
                        return UpdateResult.Failed;
                    }
                }
            }
            
            return UpdateResult.Nop;
        }

        public async Task<SyncResult> SyncNotesAsync()
        {
            if (!IsSynchronizationActive)
                return SyncResult.Nop;

            HasSyncedInThisSession = true;

            if (!_networkInfoService.HasInternet)
            {
                // no error handling needed here
                return SyncResult.Failed;
            }

            // ensure loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            var syncDataRequest = new SyncDataRequest();
            foreach (var noteItem in Notes.GetAll())
            {
                syncDataRequest.Data.Add(new SyncDataRequestItem(noteItem.Id, noteItem.ChangedDate));
            }

            var res = await _httpService.PostJsonAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/sync/" + UserId), syncDataRequest, DEFAULT_TIMEOUT);

            if (res != null &&
                res.IsSuccessStatusCode)
            {
                var jsonResponse = await res.Content.ReadAsStringAsync();
                var syncDataResult = _serializationService.DeserializeJson<SyncDataResponse>(jsonResponse);

                var unchanged = true;

                if (syncDataResult != null)
                {
                    foreach (var note in syncDataResult.Changed)
                    {
                        if (Notes.Contains(note.Id))
                        {
                            Notes.Update(note);
                            await Notes.Save(note);
                            unchanged = false;
                        }
                    }

                    // ensure archiv data has loaded
                    if (!Archiv.HasLoaded)
                        await Archiv.Load();

                    var unsyncedNotesToDelete = new List<NoteItem>();
                    foreach (var note in syncDataResult.Added)
                    {
                        if (!Notes.Contains(note.Id))
                        {
                            if (!Archiv.Contains(note.Id))
                            {
                                // add when we are sure we did not delete it locally
                                Notes.Add(note);
                                await Notes.Save(note);
                                unchanged = false;
                            }
                            else
                            {
                                unsyncedNotesToDelete.Add(note);
                            }
                        }
                    }

                    foreach (var note in syncDataResult.Deleted)
                    {
                        if (Notes.Contains(note.Id))
                        {
                            await MoveToArchivInternalAsync(note);
                            unchanged = false;
                        }
                    }

                    if (syncDataResult.MissingIds.Count > 0)
                    {
                        var unsyncedAddNotes = new List<NoteItem>();

                        foreach (var noteId in syncDataResult.MissingIds)
                        {
                            var note = Notes.Get(noteId);

                            if (note != null)
                                unsyncedAddNotes.Add(note);
                        }

                        var addResult = await _httpService.PostJsonAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/addrange/" + UserId), unsyncedAddNotes, DEFAULT_TIMEOUT);

                        if (addResult != null &&
                            addResult.IsSuccessStatusCode)
                        {
                            // do noting
                        }
                        else
                        {
                            // ignore error and retry the next time
                        }
                    }

                    if (unsyncedNotesToDelete.Count > 0)
                    {
                        await MoveRangeToArchivAsync(unsyncedNotesToDelete);
                    }
                }

                if (unchanged)
                    return SyncResult.Unchanged;
                else
                    return SyncResult.Success;
            }
            else
            {
                return SyncResult.Failed;
            }
        }

        public async Task<bool> UploadAttachement(NoteItem item)
        {
            if (!item.HasAttachement)
                return true;

            if (IsSynchronizationActive &&
                _networkInfoService.HasInternet)
            {
                string filePath = AppConstants.ATTACHEMENT_BASE_PATH + item.AttachementFile;
                var file = await _localStorageService.GetFileAsync(filePath);
                using (var stream = await file.OpenReadAsync())
                {
                    var content = new HttpMultipartFormDataContent();
                    content.Add(new HttpStreamContent(stream), "file", item.AttachementFile);

                    var res = await _httpService.PostAsync(new Uri(AppConstants.SERVER_BASE_PATH + "attachements/file/" + UserId + "/" + item.AttachementFile), content, DEFAULT_TIMEOUT);

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
            if (Unsynced.Contains(item.Id))
                Unsynced.Update(unsyncedItem);
            else
                Unsynced.Add(unsyncedItem);
            await Unsynced.Save(unsyncedItem);
            return false;
        }

        public async Task UploadMissingAttachements()
        {
            if (!IsSynchronizationActive ||
                !_networkInfoService.HasInternet)
                return;

            await Notes.Load(); // ensure loaded
            await Unsynced.Load(); // ensure loaded

            var unsyncedFiles = Unsynced.GetAll();

            foreach (var unsyncedFile in unsyncedFiles)
            {
                if (unsyncedFile.Type == UnsyncedType.FileUpload)
                {
                    var note = Notes.Get(unsyncedFile.Id);

                    if (note != null &&
                        note.HasAttachement)
                    {
                        if (await UploadAttachement(note))
                        {
                            // we can remove, because we are iterating a copy
                            Unsynced.Remove(unsyncedFile);
                        }
                    }
                }
            }
        }

        public async Task RemoveUnsyncedEntry(NoteItem item)
        {
            await Unsynced.Load(); // ensure loaded

            Unsynced.Remove(item.Id);
        }

        public async Task<bool> DownloadAttachement(NoteItem item)
        {
            if (!IsSynchronizationActive || // do not create a snyc log here, because it can even be downloaded later
                !item.HasAttachement)
                return false;

            if ( _networkInfoService.HasInternet)
            {
                var res = await _httpService.GetAsync(new Uri(AppConstants.SERVER_BASE_PATH + "attachements/file/" + UserId + "/" + item.AttachementFile), 5000);

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

            return false;
        }

        public async Task<bool> DownloadMissingAttachements()
        {
            if (!IsSynchronizationActive ||
                !_networkInfoService.HasInternet)
                return false;

            var downloadedAnything = false;
            foreach (var noteItem in Notes.GetAll())
            {
                if (noteItem.HasAttachement)
                {
                    string filePath = AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile;

                    if (!await _localStorageService.ContainsFile(filePath))
                    {
                        if (await DownloadAttachement(noteItem))
                        {
                            downloadedAnything = true;
                        }
                    }
                }
            }

            return downloadedAnything;
        }

        public async Task<bool> MoveToArchivAsync(NoteItem item)
        {
            if (await MoveToArchivInternalAsync(item))
            {
                if (IsSynchronizationActive &&
                    _networkInfoService.HasInternet)
                {
                    // mark notes as deleted
                    var res = await _httpService.DeleteAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/delete/" + UserId + "/" + item.Id), DEFAULT_TIMEOUT);

                    if (res != null &&
                        res.IsSuccessStatusCode)
                    {
                        return true;
                    }
                }

                return true;
            }

            // here we did not even moved the file locally
            return false;
        }

        public async Task<bool> MoveRangeToArchivAsync(IList<NoteItem> items)
        {
            var idsToDeleted = new List<string>();
            foreach (var note in items)
            {
                if (!await MoveToArchivInternalAsync(note))
                {
                    // stop, as soon as one item could not be deleted
                    return false;
                }
                idsToDeleted.Add(note.Id);
            }

            if (idsToDeleted.Count > 0)
            {
                if (IsSynchronizationActive &&
                _networkInfoService.HasInternet)
                {
                    // mark notes as deleted
                    var deleteResult = await _httpService.PostJsonAsync(new Uri(AppConstants.SERVER_BASE_PATH + "notes/delete/" + UserId), idsToDeleted, DEFAULT_TIMEOUT);

                    if (deleteResult != null &&
                        deleteResult.IsSuccessStatusCode)
                    {
                        // do noting
                        return true;
                    }
                    else
                    {
                        // ignore error and retry the next time
                        return false;
                    }
                }
            }

            return true;
        }

        private async Task<bool> MoveToArchivInternalAsync(NoteItem item)
        {
            // ensure notes data has loaded
            if (!Notes.HasLoaded)
                await Notes.Load();

            // ensure archiv data has loaded
            if (!Archiv.HasLoaded)
                await Archiv.Load();

            // update the timestamp
            item.ChangedDate = DateTimeOffset.Now;

            if (await Archiv.Save(item))
            {
                Notes.Remove(item);

                if (Archiv.Contains(item.Id))
                    Archiv.Update(item);
                else
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

            // ensure archiv data has loaded
            if (!Unsynced.HasLoaded)
                await Archiv.Load();

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

                            // remove unsynced file
                            if (Unsynced.Contains(attachementFile.Name))
                                Unsynced.Remove(attachementFile.Name);
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

        public async Task<bool> CheckUserAndLogin()
        {
            if (IsUserLoginPending &&
                AppSettings.SyncEnabled.Value == true)
            {
                if (await _onlineIdService.AuthenticateAsync(OnlineIdService.SERVICE_SIGNIN))
                {
                    AppSettings.UserId.Value = _onlineIdService.UserIdentity.SafeCustomerId;
                }
                else
                {
                    // auto disable online sync
                    AppSettings.SyncEnabled.Value = true;

                    return false;
                }
            }
            return true;
        }

        private string UserId
        {
            get
            {
#if FAKE_USER && DEBUG
                return "XXXXXXXXXX";
#else
                return AppSettings.UserId.Value;
#endif
            }
        }

        private bool IsUserIdValid
        {
            get
            {
                return !string.IsNullOrEmpty(UserId);
            }
        }

        public bool IsUserLoginPending
        {
            get
            {
                return IsProVersion &&
                    !IsUserIdValid;
            }
        }

        public bool IsSynchronizationActive
        {
            get
            {
                return AppSettings.SyncEnabled.Value &&
                    !IsUserLoginPending;
            }
        }

        public bool IsProVersion
        {
            get
            {
#if FAKE_PRO && DEBUG
                return true;
#else
                return _licenseService.IsProductActive(AppConstants.IAP_PRO_VERSION);
#endif
            }
        }

        public bool HasSyncedInThisSession { get; private set; } = false;
    }
}
