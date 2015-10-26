using ActionNote.Common;
using System;
using UWPCore.Framework.Mvvm;

namespace ActionNote.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        public bool AllowClearNotes
        {
            get
            {
                return AppSettings.AllowClearNotes.Value;
            }
            set
            {
                AppSettings.AllowClearNotes.Value = value;
            }
        }

        public bool AllowRemoveNotes
        {
            get
            {
                return AppSettings.AllowRemoveNotes.Value;
            }
            set
            {
                AppSettings.AllowRemoveNotes.Value = value;
            }
        }
    }
}
