﻿using UWPCore.Framework.Storage;

namespace ActionNote.Common
{
    public class AppSettings
    {
        // TODO: make settings roaming?

        public static StoredObjectBase<bool> AllowRemoveNotes = new LocalObject<bool>("allowRemoveNotes", true);

        public static StoredObjectBase<bool> AllowClearNotes = new LocalObject<bool>("allowClearNotes", false);

        public static StoredObjectBase<bool> SaveNoteOnBack = new LocalObject<bool>("saveNoteOnBack", true);

        public static StoredObjectBase<bool> QuickNotesEnabled = new LocalObject<bool>("quickNote", true);
    }
}
