using System.Collections.Generic;
using System.Threading.Tasks;
using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using UWPCore.Framework.Controls;
using UWPCore.Framework.Common;

namespace ActionNote.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;

        public EnumSource<ElementTheme> ThemeEnumSource { get; private set; } = new EnumSource<ElementTheme>();

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

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            ThemeEnumSource.SelectedValue = UniversalPage.PageTheme.Value;
        }

        public async override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await base.OnNavigatedFromAsync(state, suspending);

            UniversalPage.PageTheme.Value = ThemeEnumSource.SelectedValue;

            UniversalApp.Current.UpdateTheme();
        }
    }
}
