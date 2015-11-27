using Ninject;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Data;
using UWPCore.Framework.Storage;

namespace ActionNote.Common.Models
{
    public class NotesRepository : RepositoryBase<NoteItem, string>, INotesRepository
    {
        private IStorageService _localStorageService;
        private ISerializationService _serializationService;

        private object _sync = new object();

        public string BaseFolder { private get; set; }

        [Inject]
        public NotesRepository(ILocalStorageService storageService, ISerializationService serializationService)
        {
            _localStorageService = storageService;
            _serializationService = serializationService;
        }

        public override void Update(NoteItem prototype)
        {
            var entity = Get(prototype.Id);

            if (entity != null)
            {
                if (prototype.Title != null)
                {
                    entity.Title = prototype.Title;
                }

                if (prototype.Content != null)
                {
                    entity.Content = prototype.Content;
                }

                entity.Color = prototype.Color;
                entity.AttachementFile = prototype.AttachementFile;
                entity.IsImportant = prototype.IsImportant;
                entity.ChangedDate = prototype.ChangedDate;
            }
        }

        public override async Task<bool> Save()
        {
            foreach (var note in GetAll())
            {
                await Save(note);
            }

            return true;
        }

        public async Task<bool> Save(NoteItem item)
        {
            var jsonData = _serializationService.SerializeJson(item);
            var filePath = BaseFolder + item.Id;
            await _localStorageService.WriteFile(filePath, jsonData);

            return true;
        }

        public override async Task<bool> Reload()
        {
            HasLoaded = true;

            var dataFiles = await _localStorageService.GetFilesAsync(BaseFolder);
            var noteList = new List<NoteItem>();
            if (dataFiles != null)
            {
                foreach (var dataFile in dataFiles)
                {
                    var jsonData = await _localStorageService.ReadFile(BaseFolder + dataFile.Name);
                    var note = _serializationService.DeserializeJson<NoteItem>(jsonData);

                    if (note != null)
                        noteList.Add(note);
                }
            }

            // lock, to make sure that items are not added multiple times when there are multiple calls to this method
            lock (_sync)
            {
                // clear memory-data only, but not the files
                base.Clear();

                foreach (var note in noteList)
                {
                    Add(note);
                }
            }

            return true;
        }

        public override void Remove(string id)
        {
            base.Remove(id);

            _localStorageService.DeleteFileAsync(BaseFolder + id);
        }

        public override void Clear()
        {
            base.Clear();

            _localStorageService.DeleteFolderAsync(BaseFolder);
        }
    }
}
