using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        /// <summary>
        /// The ID navigation parameter.
        /// </summary>
        public const string PARAM_ID = "id=";

        public readonly static Color COLOR_ACCENT = Color.FromArgb(255, 0, 99, 177);
        public readonly static Color COLOR_WHITE = Colors.White;
        public readonly static Color COLOR_RED = Color.FromArgb(255, 255, 73, 73);
        public readonly static Color COLOR_GREEN = Color.FromArgb(255, 97, 255, 73);
        public readonly static Color COLOR_BLUE = Color.FromArgb(255, 73, 191, 255);
        public readonly static Color COLOR_YELLOW = Color.FromArgb(255, 246, 255, 73);
        public readonly static Color COLOR_VIOLETT = Color.FromArgb(255, 200, 73, 255);
    }
}
