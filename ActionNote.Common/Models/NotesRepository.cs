using System.Threading.Tasks;
using UWPCore.Framework.Data;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Storage;

namespace ActionNote.Common.Models
{
    public class NotesRepository : RepositoryBase<NoteItem, string>, INotesRepository
    {
        private IStorageService _localStorageService;
        private ISerializationService _serializationService;

        public string BaseFolder { private get; set; }

        public NotesRepository()
        {
            IInjector injector = Injector.Instance;
            _localStorageService = injector.Get<ILocalStorageService>();
            _serializationService = injector.Get<ISerializationService>();
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
            // clear memory-data only, but not the files
            base.Clear();

            var dataFiles = await _localStorageService.GetFilesAsync(BaseFolder);

            if (dataFiles != null)
            {
                foreach (var dataFile in dataFiles)
                {
                    var jsonData = await _localStorageService.ReadFile(BaseFolder + dataFile.Name);
                    var note = _serializationService.DeserializeJson<NoteItem>(jsonData);

                    if (note != null)
                        Add(note);
                }
            }

            HasLoaded = true;
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
