using System.Collections.Generic;

namespace ActionNote.Common.Models
{
    public interface IRepository<TEntity, in TKey> where TEntity : class // TODO: move to framework?
    {
        void Add(TEntity entity);
        TEntity Get(TKey id);
        IList<TEntity> GetAll();
        void Update(TEntity prototype);
        void Remove(TKey id);
        void Remove(TEntity entity);

        bool Save();
        bool Load();
        bool HasLoaded { get; }

        bool IsEmpty { get; }
        int Count { get; }
    }
}
