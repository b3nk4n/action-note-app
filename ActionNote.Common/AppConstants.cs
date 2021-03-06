using Windows.UI;

namespace ActionNote.Common
{
    /// <summary>
    /// Common constants of the app.
    /// </summary>
    public static class AppConstants
    {
        /// <summary>
        /// The base path of all attachement files.
        /// </summary>
        public const string ATTACHEMENT_BASE_PATH = "attachements/";

#if DEBUG
        // local
        //public const string SERVER_BASE_PATH = "http://localhost:64302/";

        // productive
        public const string SERVER_BASE_PATH = "http://bsautermeister.de/actionnote-service/";
#else
        // productive on RELEASE
        public const string SERVER_BASE_PATH = "http://bsautermeister.de/actionnote-service/";
#endif
        /// <summary>
        /// The pro version key.
        /// </summary>
        public const string IAP_PRO_VERSION = "actionnote_pro";

        /// <summary>
        /// The ID navigation parameter.
        /// </summary>
        public const string PARAM_ID = "id=";

        public readonly static Color COLOR_ACCENT = Color.FromArgb(255, 0, 99, 177);
        public readonly static Color COLOR_WHITE = Colors.White;
        public readonly static Color COLOR_RED = Color.FromArgb(255, 255, 73, 73);
        public readonly static Color COLOR_GREEN = Color.FromArgb(255, 23, 235, 119);
        public readonly static Color COLOR_BLUE = Color.FromArgb(255, 73, 191, 255);
        public readonly static Color COLOR_ORANGE = Color.FromArgb(255, 255, 164, 73);
        public readonly static Color COLOR_VIOLETT = Color.FromArgb(255, 200, 73, 255);

        public const string SORT_DATE = "date";
        public const string SORT_CATEGORY = "category";
        public const string SORT_ALPHABETICAL = "alphabetical";

        public const string QUICK_NOTES_CONTENT = "content";
        public const string QUICK_NOTES_TITLE_AND_CONTENT = "titlecontent";

        public const string SYNC_INTERVAL_30 = "30";
        public const string SYNC_INTERVAL_45 = "45";
        public const string SYNC_INTERVAL_60 = "60";
        public const string SYNC_INTERVAL_120 = "120";
        public const string SYNC_INTERVAL_240 = "240";
        public const string SYNC_INTERVAL_MANUAL = "MANUAL";

        public static double MINIMIZED_NOTE_ITEM_HEIGHT = 32;

        public const string LIVE_TILE_FLIP_NOTES = "flip";
        public const string LIVE_TILE_TITLE_LIST = "titleList";
    }
}
