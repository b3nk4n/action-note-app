
namespace ActionNote.Common.Models
{
    public interface IRepositoryItem<TKey> // TODO: move to framework
    {
        /// <summary>
        /// Gets the Id.
        /// </summary>
        TKey Id { get; }
    }
}
