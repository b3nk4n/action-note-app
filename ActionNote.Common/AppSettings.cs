using UWPCore.Framework.Storage;

namespace ActionNote.Common
{
    public class AppSettings
    {
        public static StoredObjectBase<bool> SyncWithActionCenter = new LocalObject<bool>("syncActionCenter", true);

        public static StoredObjectBase<bool> AllowRemoveNotes = new LocalObject<bool>("allowRemoveNotes", true);

        public static StoredObjectBase<bool> SaveNoteOnBack = new LocalObject<bool>("saveNoteOnBack", true);

        public static StoredObjectBase<bool> QuickNotesEnabled = new LocalObject<bool>("quickNote", true);

        public static StoredObjectBase<string> SortNoteBy = new LocalObject<string>("sortNotes", AppConstants.SORT_DATE);
    }
}
