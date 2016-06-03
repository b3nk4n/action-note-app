using System.Collections.Generic;
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
using UWPCore.Framework.Devices;
using UWPCore.Framework.UI;
using System.Text;
using System;
using ActionNote.Common.Models;
using ActionNote.Common.Helpers;

namespace ActionNote.App.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private IDataService _dataService;
        private IDeviceInfoService _deviceInfoService;
        private IDialogService _dialogService;

        public EnumSource<ElementTheme> ThemeEnumSource { get; private set; } = new EnumSource<ElementTheme>();

        public StringComboBoxSource SortInActionCenterStringSource { get; private set; }

        public StringComboBoxSource QuickNoteContentTypeStringSource { get; private set; }

        public StringComboBoxSource BackgroundTaskSyncIntervalStringSource { get; private set; }

        public EnumSource<ColorCategory> DefaultNoteColorEnumSource { get; private set; } = new EnumSource<ColorCategory>();

        private Localizer _localizer = new Localizer();

        public SettingsViewModel()
        {
            _dataService = Injector.Get<IDataService>();
            _deviceInfoService = Injector.Get<IDeviceInfoService>();
            _dialogService = Injector.Get<IDialogService>();

            // localize string source
            SortInActionCenterStringSource = new StringComboBoxSource(new List<SourceComboBoxItem>(){
                new SourceComboBoxItem(AppConstants.SORT_DATE, _localizer.Get("SortByDate.Text")),
                new SourceComboBoxItem(AppConstants.SORT_CATEGORY, _localizer.Get("SortByCategory.Text"))
            });

            QuickNoteContentTypeStringSource = new StringComboBoxSource(new List<SourceComboBoxItem>(){
                new SourceComboBoxItem(AppConstants.QUICK_NOTES_CONTENT, _localizer.Get("QuickNotesTypeContent.Text")),
                new SourceComboBoxItem(AppConstants.QUICK_NOTES_TITLE_AND_CONTENT, _localizer.Get("QuickNotesTypeTitleAndContent.Text"))
            });

            BackgroundTaskSyncIntervalStringSource = new StringComboBoxSource(new List<SourceComboBoxItem>(){
                new SourceComboBoxItem(AppConstants.SYNC_INTERVAL_30, _localizer.Get("BackSyncInterval.30")),
                new SourceComboBoxItem(AppConstants.SYNC_INTERVAL_45, _localizer.Get("BackSyncInterval.45")),
                new SourceComboBoxItem(AppConstants.SYNC_INTERVAL_60, _localizer.Get("BackSyncInterval.60")),
                new SourceComboBoxItem(AppConstants.SYNC_INTERVAL_120, _localizer.Get("BackSyncInterval.120")),
                new SourceComboBoxItem(AppConstants.SYNC_INTERVAL_240, _localizer.Get("BackSyncInterval.240")),
                new SourceComboBoxItem(AppConstants.SYNC_INTERVAL_MANUAL, _localizer.Get("BackSyncInterval.Manual"))
            });
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            ThemeEnumSource.SelectedValue = UniversalPage.PageTheme.Value;

            SortInActionCenterStringSource.SelectedValue = AppSettings.SortNoteInActionCenterBy.Value;
            QuickNoteContentTypeStringSource.SelectedValue = AppSettings.QuickNotesContentType.Value;
            BackgroundTaskSyncIntervalStringSource.SelectedValue = AppSettings.BackgroundTaskSyncInterval.Value;
            DefaultNoteColorEnumSource.SelectedValue = AppSettings.DefaultNoteColor.Value;
        }

        public async override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await base.OnNavigatedFromAsync(state, suspending);

            UniversalPage.PageTheme.Value = ThemeEnumSource.SelectedValue;
            UniversalApp.Current.UpdateTheme();

            AppSettings.SortNoteInActionCenterBy.Value = SortInActionCenterStringSource.SelectedValue;
            AppSettings.QuickNotesContentType.Value = QuickNoteContentTypeStringSource.SelectedValue;
            AppSettings.BackgroundTaskSyncInterval.Value = BackgroundTaskSyncIntervalStringSource.SelectedValue;
            AppSettings.DefaultNoteColor.Value = DefaultNoteColorEnumSource.SelectedValue;

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

        public bool AllowClearNotes
        {
            get
            {
                return AppSettings.AllowClearNotes.Value;
            }
            set
            {
                AppSettings.AllowClearNotes.Value = value;

                if (!value)
                {
                    QuickNotesEnabled = true;
                    RaisePropertyChanged("QuickNotesEnabled");
                }
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

        public string QuickNotesDefaultTitle
        {
            get
            {
                return AppSettings.QuickNotesDefaultTitle.Value;
            }
            set
            {
                AppSettings.QuickNotesDefaultTitle.Value = value;
            }
        }

        public ColorCategory DefaultNoteColor
        {
            get
            {
                return ColorCategoryConverter.FromAnyString(AppSettings.DefaultNoteColor.Value);
            }
            set
            {
                AppSettings.DefaultNoteColor.Value = value.ToString();
            }
        }

        public bool IsPhone
        {
            get
            {
                return _deviceInfoService.IsPhone;
            }
        }
    }
}
