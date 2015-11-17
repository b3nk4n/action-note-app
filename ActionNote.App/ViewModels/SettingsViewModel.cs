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
        private INotesRepository _notesRepository;

        public EnumSource<ElementTheme> ThemeEnumSource { get; private set; } = new EnumSource<ElementTheme>();

        public StringComboBoxSource SortInActionCenterStringSource { get; private set; }

        private Localizer _localizer = new Localizer();

        public SettingsViewModel()
        {
            _notesRepository = Injector.Get<INotesRepository>();

            // localize string source
            SortInActionCenterStringSource = new StringComboBoxSource(new List<SourceComboBoxItem>(){
                new SourceComboBoxItem(AppConstants.SORT_DATE, _localizer.Get("SortByDate.Text")),
                new SourceComboBoxItem(AppConstants.SORT_CATEGORY, _localizer.Get("SortByCategory.Text"))
            });
        }

        public bool SyncWithActionCenter
        {
            get
            {
                return AppSettings.SyncWithActionCenter.Value;
            }
            set
            {
                AppSettings.SyncWithActionCenter.Value = value;
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
            }
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            ThemeEnumSource.SelectedValue = UniversalPage.PageTheme.Value;
            SortInActionCenterStringSource.SelectedValue = AppSettings.SortNoteInActionCenterBy.Value;
        }

        public async override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await base.OnNavigatedFromAsync(state, suspending);

            UniversalPage.PageTheme.Value = ThemeEnumSource.SelectedValue;
            UniversalApp.Current.UpdateTheme();

            AppSettings.SortNoteInActionCenterBy.Value = SortInActionCenterStringSource.SelectedValue;
        }
    }
}
