using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UWPCore.Framework.Storage;

namespace ActionNote.Common
{
    public class AppSettings
    {
        public static StoredObjectBase<bool> AllowRemoveNotes = new LocalObject<bool>("allowRemoveNotes", true);

        public static StoredObjectBase<bool> AllowClearNotes = new LocalObject<bool>("allowClearNotes", false);
    }
}
