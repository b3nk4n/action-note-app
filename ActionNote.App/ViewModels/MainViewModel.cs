﻿using System;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml.Navigation;
using ActionNote.App.Views;
using ActionNote.Common;
using UWPCore.Framework.Common;
using ActionNote.Common.Helpers;
using UWPCore.Framework.UI;
using Windows.UI.Popups;
using System.Threading.Tasks;
using UWPCore.Framework.Devices;
using UWPCore.Framework.Support;

namespace ActionNote.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IDataService _dataService;
        private ITilePinService _tilePinService;
        private IDialogService _dialogService;
        private IStatusBarService _statusBarService;
        private IDeviceInfoService _deviceInfoService;
        private IStartupActionService _startupActionsService;

        private Localizer _localizer = new Localizer();

        public ObservableCollection<NoteItem> NoteItems
        {
            get;
            private set;
        } = new ObservableCollection<NoteItem>();

        public MainViewModel()
        {
            _dataService = Injector.Get<IDataService>();
            _tilePinService = Injector.Get<ITilePinService>();
            _dialogService = Injector.Get<IDialogService>();
            _statusBarService = Injector.Get<IStatusBarService>();
            _deviceInfoService = Injector.Get<IDeviceInfoService>();
            _startupActionsService = Injector.Get<IStartupActionService>();

            _startupActionsService.Register(1, ActionExecutionRule.Equals, () =>
            {
                // if the app started the first time, and we detect the user has the pro-version: auto enable online-sync
                if (_dataService.IsProVersion)
                    AppSettings.SyncEnabled.Value = true;
            });

            ClearCommand = new DelegateCommand(async () =>
            {
                var result = await _dialogService.ShowAsync(
                    _localizer.Get("Message.ReallyDeleteAll"),
                    _localizer.Get("Message.Title.Attention"),
                    0, 1,
                    new UICommand(_localizer.Get("Message.Option.Yes")) { Id = "y" },
                    new UICommand(_localizer.Get("Message.Option.No")) { Id = "n" });
                if (result.Id.ToString().Equals("n"))
                    return;

                await StartProgressAsync(_localizer.Get("Progress.Deleting"));

                // unpin all tiles
                var allNotes = await _dataService.GetAllNotes();
                foreach (var noteItem in allNotes)
                {
                    await _tilePinService.UnpinAsync(noteItem.Id);
                }
                await _dataService.MoveRangeToArchiveAsync(allNotes);
                NoteItems.Clear();

                SelectedNote = null;

                RaisePropertyChanged("HasNoNotes");

                await StopProgressAsync();
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
                await StartProgressAsync(_localizer.Get("Progress.Deleting"));

                if (await _dataService.MoveToArchiveAsync(noteItem))
                {
                    SelectedNote = null;
                    NoteItems.Remove(noteItem);

                    await _tilePinService.UnpinAsync(noteItem.Id);
                }

                RaisePropertyChanged("HasNoNotes");

                await StopProgressAsync();
            },
            (noteItem) =>
            {
                return noteItem != null;
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

                    var allNoteIds = await _dataService.GetAllNoteIds();
                    await _tilePinService.UnpinUnreferencedTilesAsync(allNoteIds);
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
            _startupActionsService.OnNavigatedTo(mode);

            await ReloadDataAsync();

            await CheckUserLogin();

            await CheckAutoSync();
        }

        private async Task CheckAutoSync()
        {
            if (_dataService.IsSynchronizationActive &&
                            AppSettings.SyncOnStart.Value &&
                            !_dataService.HasSyncedInThisSession)
            {
                await ExecuteSync();
            }
        }

        private async Task<bool> CheckUserLogin()
        {
            if (_dataService.IsUserLoginPending &&
               !_dataService.HasDeniedToLoginInThisSession)
            {
                var dialogResult = await _dialogService.ShowAsync(
                    _localizer.Get("Message.LoginPending"),
                    _localizer.Get("Message.Title.Information"),
                    0, 1, 
                    new UICommand(_localizer.Get("Message.Option.Yes")) { Id = "y" },
                    new UICommand(_localizer.Get("Message.Option.No")) { Id = "n" });

                if (dialogResult.Id.ToString().Equals("n"))
                {
                    _dataService.HasDeniedToLoginInThisSession = true;
                    return false;
                } 

                if (!await _dataService.CheckUserAndLogin())
                {
                    await _dialogService.ShowAsync(
                        _localizer.Get("Message.LoginFailedInfo"),
                        _localizer.Get("Message.Title.Information"));
                    return false;
                }
                else
                {
                    SyncCommand.RaiseCanExecuteChanged();

                    if (!_dataService.HasSyncedInThisSession)
                        await ExecuteSync();
                }

                return true;
            }
            return true;
        }

        private async Task ReloadDataAsync()
        {
            // ensure the repository has been loaded (which is required after suspend-shutdown)
            await _dataService.LoadNotesAsync();

            var data = await _dataService.GetAllNotes();

            if (data != null)
            {
                var sorted = NoteUtils.Sort(data, AppSettings.SortNoteBy.Value);
                NoteItems.Clear();
                NoteItems = new ObservableCollection<NoteItem>(sorted);
                RaisePropertyChanged("NoteItems");
                RaisePropertyChanged("HasNoNotes");
            }
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
                RaisePropertyChanged("IsSelectedNotePinned");
            }
        }
        private NoteItem _selectedNote;

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
        
        public ICommand SortCommand { get; private set; }

        public ICommand SyncCommand { get; private set; }
    }
}
