using UWPCore.Framework.Storage;

namespace ActionNote.Common
{
    public class AppSettings
    {
        public static StoredObjectBase<bool> ShowNotesInActionCenter = new LocalObject<bool>("showInActionCenter", true);

        public static StoredObjectBase<bool> AllowRemoveNotes = new LocalObject<bool>("allowRemoveNotes", true);

        public static StoredObjectBase<bool> SaveNoteOnBack = new LocalObject<bool>("saveNoteOnBack", true);

        public static StoredObjectBase<bool> QuickNotesEnabled = new LocalObject<bool>("quickNote", true);

        public static StoredObjectBase<string> QuickNotesContentType = new LocalObject<string>("quickNotesContentType", AppConstants.QUICK_NOTES_CONTENT);

        public static StoredObjectBase<string> SortNoteBy = new LocalObject<string>("sortNotes", AppConstants.SORT_DATE);

        public static StoredObjectBase<string> SortNoteInActionCenterBy = new LocalObject<string>("sortNotesInActionCenter", AppConstants.SORT_DATE);

        public static StoredObjectBase<string> QuickNotesDefaultTitle = new LocalObject<string>("qickNotesDefaultTitle", string.Empty); // Emtpy means to use "QuickNotes"

        // *** Pro Version only: ***

        public static StoredObjectBase<string> UserId = new LocalObject<string>("userId", null);

        public static StoredObjectBase<bool> SyncEnabled = new LocalObject<bool>("proSyncNotes", false);

        public static StoredObjectBase<bool> SyncOnStart = new LocalObject<bool>("proSyncOnStart", true);

        public static StoredObjectBase<bool> SyncInBackground = new LocalObject<bool>("proSyncInBackground", true);

        public static StoredObjectBase<string> BackgroundTaskSyncInterval = new LocalObject<string>("proSyncBackInverval", AppConstants.SYNC_INTERVAL_60);
    }
}
