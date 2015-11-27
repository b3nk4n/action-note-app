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

        // local
        public const string SERVER_BASE_PATH = "http://localhost:64302/";

        // productive
        //public const string SERVER_BASE_PATH = "http://bsautermeister.de/actionnote-service/";

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
        public readonly static Color COLOR_GREEN = Color.FromArgb(255, 97, 255, 73);
        public readonly static Color COLOR_BLUE = Color.FromArgb(255, 73, 191, 255);
        public readonly static Color COLOR_ORANGE = Color.FromArgb(255, 255, 164, 73);
        public readonly static Color COLOR_VIOLETT = Color.FromArgb(255, 200, 73, 255);

        public const string SORT_DATE = "date";
        public const string SORT_CATEGORY = "category";

        public const string QUICK_NOTES_CONTENT = "content";
        public const string QUICK_NOTES_TITLE_AND_CONTENT = "titlecontent";
    }
}
