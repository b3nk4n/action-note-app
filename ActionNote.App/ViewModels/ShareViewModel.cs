using ActionNote.Common;
using ActionNote.Common.Helpers;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.Devices;
using UWPCore.Framework.Graphics;
using UWPCore.Framework.Input;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Storage;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.ShareTarget;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class ShareViewModel : ViewModelBase
    {
        private IDataService _dataService;
        private IStorageService _localStorageService;
        private IGraphicsService _graphicsService;
        private IDeviceInfoService _deviceInfoService;
        private IStatusBarService _statusBarService;
        private IActionCenterService _actionCenterService;
        private IKeyboardService _keyboardService;

        private Localizer _localizer = new Localizer();
        private Localizer _commonLocalizer = new Localizer("ActionNote.Common");

        private Random _random = new Random();

        private ShareOperation ShareOperation { get; set; }

        public ShareViewModel()
        {
            _dataService = Injector.Get<IDataService>();
            _localStorageService = Injector.Get<ILocalStorageService>();
            _graphicsService = Injector.Get<IGraphicsService>();
            _deviceInfoService = Injector.Get<IDeviceInfoService>();
            _statusBarService = Injector.Get<IStatusBarService>();
            _actionCenterService = Injector.Get<IActionCenterService>();
            _keyboardService = Injector.Get<IKeyboardService>();

            SaveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (!noteItem.IsEmtpy)
                {
                    await SaveNoteAsync(noteItem);
                    ShareOperation.ReportCompleted();
                }
                else
                {
                    ShareOperation.ReportError(_localizer.Get("Message.CanNotSaveEmpty"));
                }
            });

            DiscardCommand = new DelegateCommand(() =>
            {
                ShareOperation.ReportCompleted();
            });

            ColorSelectedCommand = new DelegateCommand<string>((colorString) =>
            {
                var category = ColorCategoryConverter.FromAnyString(colorString);

                SelectedNote.Color = category;
            },
            (colorString) =>
            {
                return SelectedNote != null;
            });
        }

        public async override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            var note = new NoteItem();

            ShareOperation = parameter as ShareOperation;
            if (ShareOperation != null)
            {
                if (ShareOperation.Data.Contains(StandardDataFormats.Text))
                {
                    note.Title = ShareOperation.Data.Properties.Title;
                    note.Content = await ShareOperation.Data.GetTextAsync();
                }
                else if (ShareOperation.Data.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await ShareOperation.Data.GetWebLinkAsync();
                    note.Title = ShareOperation.Data.Properties.Title;
                    note.Content = uri.AbsolutePath;
                }
            }

            SelectedNote = note;
        }

        private async Task RemoveAttachement(NoteItem noteItem)
        {
            noteItem.AttachementFile = null; // remember: file is physically deleted on app-suspend
            await _dataService.RemoveUnsyncedEntry(noteItem);
        }

        private async Task SaveNoteAsync(NoteItem noteItem)
        {
            // do nothing, when note is unchanged
            if (!noteItem.HasContentChanged &&
                !noteItem.HasAttachementChanged)
                return;

            await StartProgressAsync(_localizer.Get("Progress.Saving"));

            if (string.IsNullOrWhiteSpace(noteItem.Title))
            {
                var quickNotesDefaultTitle = AppSettings.QuickNotesDefaultTitle.Value;
                if (string.IsNullOrEmpty(quickNotesDefaultTitle))
                {
                    quickNotesDefaultTitle = _commonLocalizer.Get("QuickNotes");
                }

                noteItem.Title = quickNotesDefaultTitle;
            }

            var updateDeleted = false;
            if (await _dataService.ContainsNote(noteItem.Id))
            {
                if (await _dataService.UpdateNoteAsync(noteItem) == UpdateResult.Deleted)
                {
                    updateDeleted = true;
                }
            }
            else
            {
                await _dataService.AddNoteAsync(noteItem);
            }

            if (!updateDeleted &&
                noteItem.HasAttachement &&
                noteItem.HasAttachementChanged &&
                _dataService.IsSynchronizationActive)
            {
                await StartProgressAsync(_localizer.Get("Progress.UploadingFile"));
                await _dataService.UploadAttachement(noteItem);
            }

            await StopProgressAsync();
        }

        private async Task StartProgressAsync(string message)
        {
            if (!_dataService.IsSynchronizationActive)
                return;

            if (_deviceInfoService.IsWindows)
            {
                ShowProgress = true;
            }
            else
            {
                await _statusBarService.StartProgressAsync(message, true);
            }
        }

        private async Task StopProgressAsync()
        {
            await _statusBarService.StopProgressAsync();
            ShowProgress = false;
        }

        /// <summary>
        /// Gets or sets the selected note.
        /// </summary>
        public NoteItem SelectedNote
        {
            get { return _selectedNote; }
            set
            {
                Set(ref _selectedNote, value);
                RaisePropertyChanged("SelectedAttachementImageOrReload");
            }
        }
        private NoteItem _selectedNote;

        public ImageSource SelectedAttachementImageOrReload
        {
            get
            {
                if (_selectedNote == null)
                    return null;

                return _selectedNote.AttachementImage;
            }
        }

        /// <summary>
        /// Gets or sets whether to show the progress bar (used on non-mobile only)
        /// </summary>
        public bool ShowProgress
        {
            get { return _showProgress; }
            private set
            {
                Set(ref _showProgress, value);
            }
        }
        private bool _showProgress;

        public ICommand SaveCommand { get; private set; }

        public ICommand DiscardCommand { get; private set; }

        public ICommand ColorSelectedCommand { get; private set; }
    }
}
