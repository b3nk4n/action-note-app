using System.Runtime.Serialization;
using UWPCore.Framework.Data;

namespace ActionNote.Common.Models
{
    public enum UnsyncedType
    {
        FileUpload,
        FileDownload
    }

    [DataContract]
    public class UnsyncedItem : IRepositoryItem<string>
    {
        /// <summary>
        /// Gets or sets the resource ID, which can be the note ID or a unique file name.
        /// </summary>
        [DataMember(Name = "resourceId")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the synchronization type, that still has to be performed.
        /// </summary>
        [DataMember(Name = "type")]
        public UnsyncedType Type { get; set; }

        public UnsyncedItem() { }

        public UnsyncedItem(string resourceId, UnsyncedType type)
        {
            Id = resourceId;
            Type = type;
        }
    }
}
