using System;
using System.Runtime.Serialization;

namespace ActionNote.Common.Services.Communication
{
    [DataContract]
    public class SyncDataRequestItem
    {
        [DataMember(Name = "_id")]
        public string Id { get; set; }

        [DataMember(Name = "date")]
        public DateTimeOffset ChangedDate { get; set; }

        public SyncDataRequestItem() { }

        public SyncDataRequestItem(string id, DateTimeOffset date)
        {
            Id = id;
            ChangedDate = date;
        }
    }
}
