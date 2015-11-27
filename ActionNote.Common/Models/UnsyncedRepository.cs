using Ninject;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Data;
using UWPCore.Framework.Storage;

namespace ActionNote.Common.Models
{
    /// <summary>
    /// Remember: We always store unsynced changes, even in the offline version. So that we know which items we have to upload
    ///           after upgrading to the pro version.
    /// </summary>
    public class UnsyncedRepository : RepositoryBase<UnsyncedItem, string>, IUnsyncedRepository
    {
        private IStorageService _localStorageService;
        private ISerializationService _serializationService;

        private object _sync = new object();

        public string BaseFolder { private get; set; }

        [Inject]
        public UnsyncedRepository(ILocalStorageService storageService, ISerializationService serializationService)
        {
            _localStorageService = storageService;
            _serializationService = serializationService;
        }

        public override async Task<bool> Reload()
        {
            HasLoaded = true;

            var dataFiles = await _localStorageService.GetFilesAsync(BaseFolder);
            var unsyncedList = new List<UnsyncedItem>();
            if (dataFiles != null)
            {
                foreach (var dataFile in dataFiles)
                {
                    var jsonData = await _localStorageService.ReadFile(BaseFolder + dataFile.Name);
                    var unsycedItem = _serializationService.DeserializeJson<UnsyncedItem>(jsonData);

                    if (unsycedItem != null)
                        unsyncedList.Add(unsycedItem);
                }
            }

            // lock, to make sure that items are not added multiple times when there are multiple calls to this method
            lock (_sync)
            {
                // clear memory-data only, but not the files
                base.Clear();

                foreach (var unsycedItem in unsyncedList)
                {
                    Add(unsycedItem);
                }
            }

            return true;
        }

        public override async Task<bool> Save()
        {
            foreach (var unsycedItem in GetAll())
            {
                await Save(unsycedItem);
            }

            return true;
        }

        public async Task<bool> Save(UnsyncedItem unsyncedItem)
        {
            var jsonData = _serializationService.SerializeJson(unsyncedItem);
            var filePath = BaseFolder + unsyncedItem.Id;
            await _localStorageService.WriteFile(filePath, jsonData);

            return true;
        }

        public override void Update(UnsyncedItem prototype)
        {
            var entity = Get(prototype.Id);

            if (entity != null)
            {
                entity.Type = prototype.Type;
            }
        }
    }
}
