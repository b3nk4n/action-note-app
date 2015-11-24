using ActionNote.Common.Models;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ActionNote.Common.Services.Communication
{
    [DataContract]
    public class SyncDataResponse
    {
        [DataMember(Name = "added")]
        public IList<NoteItem> Added { get; set; } = new List<NoteItem>();

        [DataMember(Name = "changed")]
        public IList<NoteItem> Changed { get; set; } = new List<NoteItem>();

        [DataMember(Name = "deleted")]
        public IList<NoteItem> Deleted { get; set; } = new List<NoteItem>();
    }
}
