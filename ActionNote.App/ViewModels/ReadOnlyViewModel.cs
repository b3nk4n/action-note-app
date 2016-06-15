using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.Devices;
using UWPCore.Framework.Input;
using UWPCore.Framework.Launcher;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Share;
using UWPCore.Framework.Speech;
using UWPCore.Framework.Storage;
using UWPCore.Framework.UI;
using Windows.System;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class ReadOnlyViewModel : ViewModelBase
    {
        private IDataService _dataService;
        private IShareContentService _shareContentService;
        private IKeyboardService _keyboardService;
        private IDialogService _dialogService;
        private ISpeechService _speechService;
        private IStorageService _localStorageService;
        private IStatusBarService _statusBarService;
        private IDeviceInfoService _deviceInfoService;

        private Localizer _localizer = new Localizer();

        public ReadOnlyViewModel()
        {
            _dataService = Injector.Get<IDataService>();
            _localStorageService = Injector.Get<ILocalStorageService>();
            _speechService = Injector.Get<ISpeechService>();
            _dialogService = Injector.Get<IDialogService>();
            _shareContentService = Injector.Get<IShareContentService>();
            _statusBarService = Injector.Get<IStatusBarService>();
            _deviceInfoService = Injector.Get<IDeviceInfoService>();
            _keyboardService = Injector.Get<IKeyboardService>();
            RegisterForKeyboard();

            RemoveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (await _dataService.RemoveFromArchiveAsync(noteItem))
                {
                    SelectedNote = null;

                    GoBackToArchivePage();
                }
                else
                {
                    await _dialogService.ShowAsync(
                        _localizer.Get("Message.CouldNotDeleteArchive"),
                        _localizer.Get("Message.Title.Warning"));
                }
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            RestoreCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (await _dataService.RestoreFromArchiveAsync(noteItem))
                {
                    SelectedNote = null;

                    GoBackToArchivePage();
                }
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            VoiceToTextCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var result = await _speechService.RecoginizeUI();

                if (result != null)
                {
                    if (noteItem.Content == null)
                        noteItem.Content = result.Text;
                    else
                        noteItem.Content += "\r\n" + result.Text;
                }
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            ReadNoteCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var text = noteItem.Title + ". " + noteItem.Content;
                await _speechService.SpeakTextAsync(text);
            },
            (noteItem) =>
            {
                return noteItem != null && !string.IsNullOrWhiteSpace(noteItem.Content) && !string.IsNullOrWhiteSpace(noteItem.Content);
            });

            // TODO: redundand with MainPageViewModel
            ShareCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var content = string.Format("{0}\r{1}", noteItem.Title, noteItem.Content);
                var description = _localizer.Get("ShareContentDescription");
                if (noteItem.HasAttachement)
                {
                    var file = await _localStorageService.GetFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile);
                    if (file != null)
                        _shareContentService.ShareImage(noteItem.Title, file, null, content, description);
                }
                else
                {
                    _shareContentService.ShareText(noteItem.Title, content, description);
                }
            },
            (noteItem) =>
            {
                return noteItem != null && !noteItem.IsEmtpy;
            });

            OpenPicture = new DelegateCommand(async () =>
            {
                var file = await _localStorageService.GetFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + SelectedNote.AttachementFile);
                if (file != null)
                    await SystemLauncher.LaunchFileAsync(file);
            },
            () =>
            {
                return SelectedNote != null && SelectedNote.HasAttachement;
            });
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterForKeyboard();
        }

        public async override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            NoteItem noteToEdit = null;
            if (parameter != null)
            {
                var stringParam = (string)parameter;

                if (stringParam.StartsWith(AppConstants.PARAM_ID))
                {
                    string noteId = stringParam.Remove(0, AppConstants.PARAM_ID.Length);

                    var note = await _dataService.GetArchivedNote(noteId);

                    if (note != null)
                    {
                        noteToEdit = note.Clone();
                    }
                }
            }

            SelectedNote = noteToEdit;

            await CheckAndDownloadAttachement();
        }

        public override async Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            _keyboardService.UnregisterForKeyDown();

            await base.OnNavigatedFromAsync(state, suspending);
        }

        private void RegisterForKeyboard()
        {
            _keyboardService.RegisterForKeyDown((e) =>
            {
                if (e.AltKey && e.VirtualKey == VirtualKey.S)
                {
                    ShareCommand.Execute(SelectedNote);
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.R)
                {
                    ReadNoteCommand.Execute(SelectedNote);
                }
            });
        }

        /// <summary>
        /// Go back and prevent duplicated save.
        /// </summary>
        private void GoBackToArchivePage()
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(typeof(ArchivViewModel));
        }

        private async Task CheckAndDownloadAttachement()
        {
            if (SelectedNote == null ||
                !SelectedNote.HasAttachement)
                return;

            var attachementFile = AppConstants.ATTACHEMENT_BASE_PATH + SelectedNote.AttachementFile;
            if (!await _localStorageService.ContainsFile(attachementFile))
            {
                await StartProgressAsync(_localizer.Get("Progress.DownloadingFile"));
                if (await _dataService.DownloadAttachement(SelectedNote))
                {
                    RaisePropertyChanged("SelectedAttachementImageOrReload");
                }
                await StopProgressAsync();
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

        public ICommand RemoveCommand { get; private set; }

        public ICommand RestoreCommand { get; private set; }

        public ICommand VoiceToTextCommand { get; private set; }

        public ICommand ReadNoteCommand { get; private set; }

        public ICommand ShareCommand { get; private set; }

        public ICommand OpenPicture { get; set; }
    }
}
