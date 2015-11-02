﻿using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.Data;
using UWPCore.Framework.Storage;

namespace ActionNote.Common.Models
{
    public class NotesRepository : RepositoryBase<NoteItem, string>, INotesRepository
    {
        public const string DATA_FILE = "data.json";

        private IStorageService _localStorageService;
        private ISerializationService _serializationService;

        public NotesRepository()
        {
            _localStorageService = UniversalApp.Injector.Get<ILocalStorageService>();
            _serializationService = UniversalApp.Injector.Get<ISerializationService>();
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

                if (prototype.Color != null)
                {
                    entity.Color = prototype.Color;
                }
            }
        }

        public override async Task<bool> Save()
        {
            var data = _serializationService.SerializeJson(GetAll());
            await _localStorageService.WriteFile(DATA_FILE, data);
            return true;
        }

        public override async Task<bool> Reload()
        {
            Clear();

            var data = await _localStorageService.ReadFile(DATA_FILE);

            if (!string.IsNullOrEmpty(data))
            {
                var modelData = _serializationService.DeserializeJson<List<NoteItem>>(data);
                foreach (var item in modelData)
                {
                    Add(item);
                }
            }

            HasLoaded = true;
            return true;
        }
    }
}
