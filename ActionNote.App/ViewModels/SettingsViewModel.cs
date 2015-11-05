using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using UWPCore.Framework.Mvvm;

namespace ActionNote.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;

        public SettingsViewModel()
        {
            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();
        }

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

        public bool SaveNoteOnBack
        {
            get
            {
                return AppSettings.SaveNoteOnBack.Value;
            }
            set
            {
                AppSettings.SaveNoteOnBack.Value = value;
            }
        }

        public bool QuickNotesEnabled
        {
            get
            {
                return AppSettings.QuickNotesEnabled.Value;
            }
            set
            {
                AppSettings.QuickNotesEnabled.Value = value;

                // update action center
                _toastUpdateService.Refresh(_notesRepository);
                // TODO: add function that only adds/removes the QuickNotes toast? Instead of a full refresh
            }
        }
    }
}
