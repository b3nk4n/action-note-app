﻿using System.Collections.Generic;
using System.Threading.Tasks;
using ActionNote.Common;
using ActionNote.Common.Services;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using UWPCore.Framework.Controls;
using UWPCore.Framework.Common;
using ActionNote.App.Views;

namespace ActionNote.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private IDataService _dataService;

        public EnumSource<ElementTheme> ThemeEnumSource { get; private set; } = new EnumSource<ElementTheme>();

        public StringComboBoxSource SortInActionCenterStringSource { get; private set; }

        public StringComboBoxSource QuickNoteContentTypeStringSource { get; private set; }

        private Localizer _localizer = new Localizer();

        public SettingsViewModel()
        {
            _dataService = Injector.Get<IDataService>();

            // localize string source
            SortInActionCenterStringSource = new StringComboBoxSource(new List<SourceComboBoxItem>(){
                new SourceComboBoxItem(AppConstants.SORT_DATE, _localizer.Get("SortByDate.Text")),
                new SourceComboBoxItem(AppConstants.SORT_CATEGORY, _localizer.Get("SortByCategory.Text"))
            });
            QuickNoteContentTypeStringSource = new StringComboBoxSource(new List<SourceComboBoxItem>(){
                new SourceComboBoxItem(AppConstants.QUICK_NOTES_CONTENT, _localizer.Get("QuickNotesTypeContent.Text")),
                new SourceComboBoxItem(AppConstants.QUICK_NOTES_TITLE_AND_CONTENT, _localizer.Get("QuickNotesTypeTitleAndContent.Text"))
            });
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            ThemeEnumSource.SelectedValue = UniversalPage.PageTheme.Value;
            SortInActionCenterStringSource.SelectedValue = AppSettings.SortNoteInActionCenterBy.Value;
            QuickNoteContentTypeStringSource.SelectedValue = AppSettings.QuickNotesContentType.Value;
        }

        public async override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await base.OnNavigatedFromAsync(state, suspending);

            UniversalPage.PageTheme.Value = ThemeEnumSource.SelectedValue;
            UniversalApp.Current.UpdateTheme();

            AppSettings.SortNoteInActionCenterBy.Value = SortInActionCenterStringSource.SelectedValue;
            AppSettings.QuickNotesContentType.Value = QuickNoteContentTypeStringSource.SelectedValue;
        }

        public bool ShowNotesInActionCenter
        {
            get
            {
                return AppSettings.ShowNotesInActionCenter.Value;
            }
            set
            {
                AppSettings.ShowNotesInActionCenter.Value = value;
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

        public bool SyncEnabled
        {
            get
            {
                return AppSettings.SyncEnabled.Value;
            }
            set
            {
                if (!_dataService.IsProVersion && value)
                {
                    // if we want to activate it and we are not running the pro version
                    AppSettings.SyncEnabled.Value = false;
                    // deactivate sync, but we re-activate the setting when the purchase was successful.
                    NavigationService.Navigate(typeof(UpgradePage));
                }
                else
                {
                    AppSettings.SyncEnabled.Value = value;
                }
            }
        }

        public bool SyncOnStart
        {
            get
            {
                return AppSettings.SyncOnStart.Value;
            }
            set
            {
                AppSettings.SyncOnStart.Value = value;
            }
        }

        public bool SyncInBackground
        {
            get
            {
                return AppSettings.SyncInBackground.Value;
            }
            set
            {
                AppSettings.SyncInBackground.Value = value;
            }
        }
    }
}
