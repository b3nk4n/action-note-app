using System.Runtime.Serialization;

namespace ActionNote.Common.Services.Communication
{
    [DataContract]
    public class ServerResponse
    {
        /// <summary>
        /// The result was ok without any errors.
        /// </summary>
        public const string OK = "OK";

        /// <summary>
        /// The data item was already deleted by another device and is deprecated.
        /// </summary>
        public const string DELETED = "DELETED";

        [DataMember(Name = "msg")]
        public string Message { get; set; }
    }
}
