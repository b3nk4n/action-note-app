using System.Collections.Generic;
using System.Runtime.Serialization;

namespace ActionNote.Common.Services.Communication
{
    [DataContract]
    public class SyncDataRequest
    {
        [DataMember(Name = "data")]
        public IList<SyncDataRequestItem> Data = new List<SyncDataRequestItem>();
    }
}
