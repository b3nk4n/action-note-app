using System;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml.Navigation;
using ActionNote.App.Views;
using UWPCore.Framework.Share;
using UWPCore.Framework.Storage;
using ActionNote.Common;
using UWPCore.Framework.Common;
using ActionNote.Common.Helpers;
using UWPCore.Framework.UI;
using Windows.UI.Popups;
using System.Threading.Tasks;
using UWPCore.Framework.Devices;

namespace ActionNote.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IDataService _dataService;
        private IShareContentService _shareContentService;
        private IStorageService _localStorageSerivce;
        private ITilePinService _tilePinService;
        private IDialogService _dialogService;
        private IStatusBarService _statusBarService;
        private IDeviceInfoService _deviceInfoService;

        private Localizer _localizer = new Localizer();

        public ObservableCollection<NoteItem> NoteItems
        {
            get;
            private set;
        } = new ObservableCollection<NoteItem>();

        public MainViewModel()
        {
            _dataService = Injector.Get<IDataService>();
            _shareContentService = Injector.Get<IShareContentService>();
            _localStorageSerivce = Injector.Get<ILocalStorageService>();
            _tilePinService = Injector.Get<ITilePinService>();
            _dialogService = Injector.Get<IDialogService>();
            _statusBarService = Injector.Get<IStatusBarService>();
            _deviceInfoService = Injector.Get<IDeviceInfoService>();

            ClearCommand = new DelegateCommand(async () =>
            {
                var result = await _dialogService.ShowAsync(
                    _localizer.Get("Message.ReallyDeleteAll"),
                    _localizer.Get("Message.Title.Warning"),
                    0, 1,
                    new UICommand(_localizer.Get("Message.Option.Yes")) { Id = "y" },
                    new UICommand(_localizer.Get("Message.Option.No")) { Id = "n" });
                if (result.Id.ToString().Equals("n"))
                    return;

                // unpin all tiles
                foreach (var noteItem in await _dataService.GetAllNotes())
                {
                    await _tilePinService.UnpinAsync(noteItem.Id);

                    await _dataService.MoveToArchivAsync(noteItem); // TODO: moveAllToArchive method? --> only 1 REST API call
                }
                NoteItems.Clear();

                SelectedNote = null;

                RaisePropertyChanged("HasNoNotes");
            },
            () =>
            {
                return _dataService.NotesCount > 0;
            });

            AddCommand = new DelegateCommand(() =>
            {
                NavigationService.Navigate(typeof(EditPage));
            });

            EditCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                NavigationService.Navigate(typeof(EditPage), AppConstants.PARAM_ID + noteItem.Id);
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            RemoveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (await _dataService.MoveToArchivAsync(noteItem))
                {
                    SelectedNote = null;
                    NoteItems.Remove(noteItem);

                    await _tilePinService.UnpinAsync(noteItem.Id);
                }

                RaisePropertyChanged("HasNoNotes");
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            PinCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                await _tilePinService.PinOrUpdateAsync(noteItem);
                RaisePropertyChanged("IsSelectedNotePinned");
            },
            (noteItem) =>
            {
                return noteItem != null && !noteItem.IsEmtpy;
            });

            UnpinCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                await _tilePinService.UnpinAsync(noteItem.Id);
                RaisePropertyChanged("IsSelectedNotePinned");
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            ShareCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var description = _localizer.Get("ShareContentDescription");
                if (noteItem.HasAttachement)
                {
                    var file = await _localStorageSerivce.GetFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile);
                    if (file != null)
                        _shareContentService.ShareImage(noteItem.Title, file, null, noteItem.Content, description);
                }
                else
                {
                    _shareContentService.ShareText(noteItem.Title, noteItem.Content, description);
                }
            },
            (noteItem) =>
            {
                return noteItem != null && !noteItem.IsEmtpy;
            });

            SortCommand = new DelegateCommand<string>((sortType) =>
            {
                AppSettings.SortNoteBy.Value = sortType;

                var sorted = NoteUtils.Sort(NoteItems, sortType);
                NoteItems = new ObservableCollection<NoteItem>(sorted);
                RaisePropertyChanged("NoteItems");
            },
            (sortType) =>
            {
                return NoteItems.Count > 0;
            });

            SyncCommand = new DelegateCommand(async () =>
            {
                await ExecuteSync();
            },
            () =>
            {
                return _dataService.IsSynchronizationActive;
            });
        }

        private async Task ExecuteSync()
        {
            if (await CheckUserLogin())
            {
                await StartProgressAsync(_localizer.Get("Progress.Syncing"));

                // sync notes
                var syncResult = await _dataService.SyncNotesAsync();
                if (syncResult == SyncResult.Success || syncResult == SyncResult.Unchanged)
                {
                    await _dataService.UploadMissingAttachements();
                    await _dataService.DownloadMissingAttachements();

                    await ReloadDataAsync();
                }
                else if (syncResult == SyncResult.Failed)
                {
                    await _dialogService.ShowAsync(_localizer.Get("Message.SyncFailed"),
                        _localizer.Get("Message.Title.Warning"));
                }

                await StopProgressAsync();
            }
        }

        public override async void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            await ReloadDataAsync();

            await CheckUserLogin();
        }

        private async Task<bool> CheckUserLogin()
        {
            if (_dataService.IsUserLoginPending)
            {
                var dialogResult = await _dialogService.ShowAsync(
                    _localizer.Get("Message.LoginPending"),
                    _localizer.Get("Message.Title.Information"),
                    0, 1, 
                    new UICommand(_localizer.Get("Message.Option.Yes")) { Id = "y" },
                    new UICommand(_localizer.Get("Message.Option.No")) { Id = "n" });

                if (dialogResult.Id.ToString().Equals("n"))
                    return false;

                if (!await _dataService.CheckUserAndLogin())
                {
                    await _dialogService.ShowAsync(
                        _localizer.Get("Message.LoginFailedInfo"),
                        _localizer.Get("Message.Title.Information"));
                }

                return true;
            }
            return true;
        }

        private async Task ReloadDataAsync()
        {
            // ensure the repository has been loaded (which is required after suspend-shutdown)
            await _dataService.LoadNotesAsync();

            NoteItems.Clear();
            var data = await _dataService.GetAllNotes();

            if (data != null)
            {
                var sorted = NoteUtils.Sort(data, AppSettings.SortNoteBy.Value);
                NoteItems = new ObservableCollection<NoteItem>(sorted);
                RaisePropertyChanged("NoteItems");
                RaisePropertyChanged("HasNoNotes");
            }
        }

        private async Task StartProgressAsync(string message)
        {
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
                RaisePropertyChanged("IsSelectedNotePinned");
            }
        }
        private NoteItem _selectedNote;

        public bool IsSelectedNotePinned
        {
            get
            {
                if (SelectedNote == null)
                    return false;

                return _tilePinService.Contains(SelectedNote.Id);
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

        public bool HasNoNotes
        {
            get
            {
                return NoteItems.Count == 0;
            }
        }

        public ICommand ClearCommand { get; private set; }

        public ICommand AddCommand { get; private set; }

        public ICommand EditCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public ICommand PinCommand { get; private set; }

        public ICommand UnpinCommand { get; private set; }

        public ICommand ShareCommand { get; private set; }

        public ICommand SortCommand { get; private set; }

        public ICommand SyncCommand { get; private set; }
    }
}
