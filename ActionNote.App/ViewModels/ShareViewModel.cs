using ActionNote.Common;
using ActionNote.Common.Helpers;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System;
using System.Linq;
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
using Windows.Storage;
using UWPCore.Framework.Notifications;

namespace ActionNote.App.ViewModels
{
    public class ShareViewModel : ViewModelBase
    {
        private IDataService _dataService;
        private IStorageService _localStorageService;
        private IGraphicsService _graphicsService;
        private IActionCenterService _actionCenterService;
        private IDeviceInfoService _deviceInfoService;
        private IKeyboardService _keyboardService;
        private IBadgeService _badgeService;

        private Localizer _localizer = new Localizer();
        private Localizer _commonLocalizer = new Localizer("ActionNote.Common");

        private Random _random = new Random();

        private ShareOperation ShareOperation { get; set; }

        public ShareViewModel()
        {
            _dataService = Injector.Get<IDataService>();
            _localStorageService = Injector.Get<ILocalStorageService>();
            _graphicsService = Injector.Get<IGraphicsService>();
            _actionCenterService = Injector.Get<IActionCenterService>();
            _deviceInfoService = Injector.Get<IDeviceInfoService>();
            _keyboardService = Injector.Get<IKeyboardService>();
            _badgeService = Injector.Get<IBadgeService>();

            SaveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (!noteItem.IsEmtpy)
                {
                    await SaveNoteAsync(noteItem);

                    if (WindowWrapper.ActiveWrappers.Count > 1) // more than 1 wrapper means the app is active as well
                    {
                        // notify the app to refresh (depatch on the first wrapper, because this is our app instance)
                        await WindowWrapper.ActiveWrappers.FirstOrDefault()?.Dispatcher.DispatchAsync(async () =>
                        {
                            var main = MainViewModel.StaticInstance;
                            if (main != null)
                            {
                                await main.ReloadDataAsync();
                            }
                        });
                    }

                    await WindowWrapper.ActiveWrappers.FirstOrDefault()?.Dispatcher.DispatchAsync(async () =>
                    {
                        if (WindowWrapper.ActiveWrappers.Count == 1 || // having exactly 1 wrapper means the app is not active
                            WindowWrapper.ActiveWrappers.Count > 1 && _deviceInfoService.IsPhone) 
                        {
                            var notes = await _dataService.GetAllNotes();
                            _actionCenterService.Refresh(notes);
                            var badge = _badgeService.Factory.CreateBadgeNumber(notes.Count);
                            _badgeService.GetBadgeUpdaterForApplication().Update(badge);
                        }
                    });

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

            var noteItem = new NoteItem();

            ShareOperation = parameter as ShareOperation;
            if (ShareOperation != null)
            {
                noteItem.Title = ShareOperation.Data.Properties.Title;

                if (ShareOperation.Data.Contains(StandardDataFormats.Text))
                {
                    noteItem.Content = await ShareOperation.Data.GetTextAsync();
                }
                else if (ShareOperation.Data.Contains(StandardDataFormats.WebLink))
                {
                    var uri = await ShareOperation.Data.GetWebLinkAsync();
                    noteItem.Content = uri.AbsoluteUri;
                }

                if (ShareOperation.Data.Contains(StandardDataFormats.StorageItems))
                {
                    var files = await ShareOperation.Data.GetStorageItemsAsync();
                    var file = files.FirstOrDefault() as StorageFile;

                    if (file != null)
                    {
                        //var noteItem = await _dataService.GetNote(note.Id);
                        var canonicalPrefix = noteItem.Id + '-' + string.Format("{0:00000}", _random.Next(100000)) + '-';
                        var fileName = canonicalPrefix + file.Name;

                        var destinationFile = await _localStorageService.CreateOrReplaceFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + fileName);
                        if (destinationFile != null &&
                            await _graphicsService.ResizeImageAsync(file, destinationFile, 1024, 1024))
                        {
                            if (noteItem.AttachementFile != null)
                            {
                                await RemoveAttachement(noteItem);
                            }

                            noteItem.AttachementFile = fileName;

                            RaisePropertyChanged("SelectedAttachementImageOrReload");
                        }
                    }
                }
            }

            SelectedNote = noteItem;
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

            StartProgress();

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
                await _dataService.UploadAttachement(noteItem);
            }

            StopProgress();
        }

        private void StartProgress()
        {
            if (!_dataService.IsSynchronizationActive)
                return;

            ShowProgress = true;
        }

        private void StopProgress()
        {
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
