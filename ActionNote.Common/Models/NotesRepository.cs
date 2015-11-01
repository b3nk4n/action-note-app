using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.Data;

namespace ActionNote.Common.Models
{
    public class NotesRepository : RepositoryBase<NoteItem, string>, INotesRepository
    {
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

        public override bool Save()
        {
            // TODO: save data to disk
            return false;
        }

        public override bool Reload()
        {
            // TODO: load data from disk
            Add(new NoteItem("Title 1", "Content 1") { Id = "1" });
            Add(new NoteItem("Title 2", "Content 2") { Id = "2" });
            Add(new NoteItem("Title 3", "Content 3") { Id = "3" });
            Add(new NoteItem("Title 4", "Content 4") { Id = "4" });
            Add(new NoteItem("Title 5", "Content 5") { Id = "5" });

            HasLoaded = true;
            return true;
        }
    }

    //public class NotesRepository : INotesRepository
    //{
    //    private IList<NoteItem> _data;

    //    public bool HasLoaded { get; private set; }

    //    public NotesRepository()
    //    {
    //        _data = new List<NoteItem>();
    //    }

    //    public void Add(NoteItem entity)
    //    {
    //        _data.Add(entity);
    //    }

    //    public NoteItem Get(string id)
    //    {
    //        foreach (var entity in _data)
    //        {
    //            if (entity.Id == id)
    //                return entity;
    //        }

    //        return null;
    //    }

    //    public IList<NoteItem> GetAll()
    //    {
    //        return _data;
    //    }

    //    public bool Contains(string id)
    //    {
    //        return Get(id) != null;
    //    }

    //    public void Remove(NoteItem entity)
    //    {
    //        Remove(entity.Id);
    //    }

    //    public void Remove(string id)
    //    {
    //        int indexToRemove = -1;
    //        int index = 0;
    //        foreach (var entity in _data)
    //        {
    //            if (entity.Id == id)
    //            {
    //                indexToRemove = index;
    //                break;
    //            }
    //            ++index;
    //        }

    //        if (indexToRemove != -1)
    //            _data.RemoveAt(indexToRemove);
    //    }

    //    public void Clear()
    //    {
    //        _data.Clear();
    //    }

    //    public void Update(NoteItem prototype)
    //    {
    //        var entity = Get(prototype.Id);

    //        if (entity != null)
    //        {
    //            if (prototype.Title != null)
    //            {
    //                entity.Title = prototype.Title;
    //            }

    //            if (prototype.Content != null)
    //            {
    //                entity.Content = prototype.Content;
    //            }
    //        }
    //    }

    //    public bool Save()
    //    {
    //        // TODO: save data to disk
    //        return false;
    //    }

    //    public bool Load()
    //    {
    //        if (!HasLoaded)
    //        {
    //            return Reload();
    //        }

    //        return false;
    //    }

    //    public bool Reload()
    //    {
    //        // TODO: load data from disk
    //        Add(new NoteItem("Title 1", "Content 1") { Id = "1" });
    //        Add(new NoteItem("Title 2", "Content 2") { Id = "2" });
    //        Add(new NoteItem("Title 3", "Content 3") { Id = "3" });
    //        Add(new NoteItem("Title 4", "Content 4") { Id = "4" });
    //        Add(new NoteItem("Title 5", "Content 5") { Id = "5" });

    //        HasLoaded = true;
    //        return true;
    //    }

    //    public int Count
    //    {
    //        get
    //        {
    //            return _data.Count;
    //        }
    //    }

    //    public bool IsEmpty
    //    {
    //        get
    //        {
    //            return _data.Count > 0;
    //        }
    //    }
    //}
}
