using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System;
using Windows.Storage;
using Windows.Storage.Pickers;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Storage;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using UWPCore.Framework.Navigation;
using UWPCore.Framework.Speech;
using ActionNote.Common.Helpers;
using ActionNote.App.Views;
using UWPCore.Framework.Graphics;
using UWPCore.Framework.Common;
using UWPCore.Framework.UI;
using Windows.UI.Xaml.Media;
using UWPCore.Framework.Devices;
using UWPCore.Framework.Share;
using UWPCore.Framework.Input;
using Windows.System;

namespace ActionNote.App.ViewModels
{
    public interface EditViewModelCallbacks
    {
        void SelectTitle();
        void UnfocusTextBoxes();
    }

    public class EditViewModel : ViewModelBase
    {
        private EditViewModelCallbacks _callbacks;

        private IDataService _dataService;
        private IStorageService _localStorageService;
        private ISpeechService _speechService;
        private ISerializationService _serializationService;
        private ITilePinService _tilePinService;
        private IGraphicsService _graphicsService;
        private IDialogService _dialogService;
        private IStatusBarService _statusBarService;
        private IDeviceInfoService _deviceInfoService;
        private IActionCenterService _actionCenterService;
        private IShareContentService _shareContentService;
        private IKeyboardService _keyboardService;

        private Localizer _localizer = new Localizer();
        private Localizer _commonLocalizer = new Localizer("ActionNote.Common");

        private Random _random = new Random();

        // For sample data only
        public EditViewModel()
            : this(null)
        { }

        public EditViewModel(EditViewModelCallbacks callbacks)
        {
            _callbacks = callbacks;

            _dataService = Injector.Get<IDataService>();
            _localStorageService = Injector.Get<ILocalStorageService>();
            _speechService = Injector.Get<ISpeechService>();
            _serializationService = Injector.Get<ISerializationService>();
            _tilePinService = Injector.Get<ITilePinService>();
            _graphicsService = Injector.Get<IGraphicsService>();
            _dialogService = Injector.Get<IDialogService>();
            _statusBarService = Injector.Get<IStatusBarService>();
            _deviceInfoService = Injector.Get<IDeviceInfoService>();
            _actionCenterService = Injector.Get<IActionCenterService>();
            _shareContentService = Injector.Get<IShareContentService>();
            _keyboardService = Injector.Get<IKeyboardService>();
            RegisterForKeyboard();

            SaveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (!noteItem.IsEmtpy)
                {
                    await SaveNoteAsync(noteItem);
                    GoBackToMainPageWithoutBackEvent();
                }
                else
                {
                    await _dialogService.ShowAsync(
                        _localizer.Get("Message.CanNotSaveEmpty"),
                        _localizer.Get("Message.Title.Info"));
                }
            });

            DiscardCommand = new DelegateCommand(() =>
            {
                GoBackToMainPageWithoutBackEvent();
            });

            RemoveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                await StartProgressAsync(_localizer.Get("Progress.Deleting"));

                if (await _dataService.MoveToArchiveAsync(noteItem))
                {
                    await _tilePinService.UnpinAsync(noteItem.Id);

                    GoBackToMainPageWithoutBackEvent();
                }

                await StopProgressAsync();
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            SelectAttachementCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var picker = new FileOpenPicker();
                picker.ViewMode = PickerViewMode.Thumbnail;
                picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                picker.FileTypeFilter.Add(".jpg");
                picker.FileTypeFilter.Add(".jpeg");
                picker.FileTypeFilter.Add(".png");
                _actionCenterService.StartTemporaryRefreshBlocking(5);
                StorageFile file = await picker.PickSingleFileAsync();

                if (file != null)
                {
                    //var noteItem = await _dataService.GetNote(note.Id);
                    var canonicalPrefix = noteItem.Id + '-' + string.Format("{0:00000}", _random.Next(100000)) + '-';
                    var fileName = canonicalPrefix + file.Name;

                    var destinationFile = await _localStorageService.CreateOrReplaceFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + fileName);
                    if (await _graphicsService.ResizeImageAsync(file, destinationFile, 1024, 1024))
                    {
                        if (noteItem.AttachementFile != null)
                        {
                            await RemoveAttachement(noteItem);
                        }

                        noteItem.AttachementFile = fileName;

                        RaisePropertyChanged("SelectedAttachementImageOrReload");
                    }
                }

                RemoveAttachementCommand.RaiseCanExecuteChanged();
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            RemoveAttachementCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                await RemoveAttachement(noteItem);

                RemoveAttachementCommand.RaiseCanExecuteChanged();
            },
            (noteItem) =>
            {
                return noteItem != null && noteItem.HasAttachement;
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

            ColorSelectedCommand = new DelegateCommand<string> ((colorString) =>
            {
                var category = ColorCategoryConverter.FromAnyString(colorString);

                SelectedNote.Color = category;
            },
            (colorString) =>
            {
                return SelectedNote != null;
            });

            // TODO: redundand with MainPageViewModel
            ShareCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var description = _localizer.Get("ShareContentDescription");
                if (noteItem.HasAttachement)
                {
                    var file = await _localStorageService.GetFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile);
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
        }

        private async Task RemoveAttachement(NoteItem noteItem)
        {
            noteItem.AttachementFile = null; // remember: file is physically deleted on app-suspend
            await _dataService.RemoveUnsyncedEntry(noteItem);
        }

        /// <summary>
        /// Go back and prevent duplicated save.
        /// </summary>
        private void GoBackToMainPageWithoutBackEvent()
        {
            if (NavigationService.CanGoBack)
                NavigationService.GoBack();
            else
                NavigationService.Navigate(typeof(MainPage));
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
                noteItem.Title = _commonLocalizer.Get("QuickNote");
            }

            var updateDeleted = false;
            if (await _dataService.ContainsNote(noteItem.Id))
            {
                if (await _dataService.UpdateNoteAsync(noteItem) == UpdateResult.Deleted)
                {
                    updateDeleted = true;
                    await _dialogService.ShowAsync(_localizer.Get("Message.DeletedOutside"), _localizer.Get("Message.Title.Warning"));
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

            // update tile
            await _tilePinService.UpdateAsync(noteItem);
        }

        public override void OnResume()
        {
            base.OnResume();

            RegisterForKeyboard();
        }

        public async override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            NavigationService.FrameFacade.BackRequested += BackRequested;

            NoteItem noteToEdit = null;
            if (parameter != null)
            {
                var stringParam = (string)parameter;

                if (stringParam.StartsWith(AppConstants.PARAM_ID))
                {
                    string noteId = stringParam.Remove(0, AppConstants.PARAM_ID.Length);

                    var note = await _dataService.GetNote(noteId);

                    if (note != null)
                    {
                        noteToEdit = note.Clone();
                        IsEditMode = true;
                    }
                }
                else
                {
                    noteToEdit = _serializationService.DeserializeJson<NoteItem>(stringParam);
                    IsEditMode = false;
                }
            }

            if (noteToEdit == null)
            {
                noteToEdit = new NoteItem();
                IsEditMode = false;
            }

            if (state.Count > 0)
            {
                noteToEdit.Id = state["id"] as string;
                noteToEdit.Title = state["title"] as string;
                noteToEdit.Content = state["content"] as string;
                noteToEdit.AttachementFile = state["attachementFile"] as string;
                noteToEdit.IsImportant = (bool)state["isImportant"];
                noteToEdit.Color = (ColorCategory)Enum.Parse(typeof(ColorCategory), state["color"] as string);
                noteToEdit.ChangedDate = (DateTimeOffset)state["changedDate"];
            }
            else
            {
                // only select title when app is not resumed
                if (!IsEditMode)
                    _callbacks.SelectTitle();
            }

            SelectedNote = noteToEdit;

            await CheckAndDownloadAttachement();
        }

        public override async Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            _keyboardService.UnregisterForKeyDown();

            await base.OnNavigatedFromAsync(state, suspending);

            if (!suspending)
                NavigationService.FrameFacade.BackRequested -= BackRequested;

            if (suspending)
            {
                state["id"] = SelectedNote.Id;
                state["title"] = SelectedNote.Title;
                state["content"] = SelectedNote.Content;
                state["attachementFile"] = SelectedNote.AttachementFile;
                state["isImportant"] = SelectedNote.IsImportant;
                state["color"] = SelectedNote.Color.ToString();
                state["changedDate"] = SelectedNote.ChangedDate;
            }
        }

        private void RegisterForKeyboard()
        {
            _keyboardService.RegisterForKeyDown(async (e) =>
            {
                if (e.ControlKey && e.VirtualKey == VirtualKey.S)
                {
                    _callbacks.UnfocusTextBoxes();
                    await Task.Delay(50);
                    SaveCommand.Execute(SelectedNote);
                }
                else if (e.ControlKey && e.VirtualKey == VirtualKey.X)
                {
                    DiscardCommand.Execute(null);
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.S)
                {
                    _callbacks.UnfocusTextBoxes();
                    await Task.Delay(50);
                    ShareCommand.Execute(SelectedNote);
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.R)
                {
                    _callbacks.UnfocusTextBoxes();
                    await Task.Delay(50);
                    ReadNoteCommand.Execute(SelectedNote);
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.P)
                {
                    _callbacks.UnfocusTextBoxes();
                    await Task.Delay(50);
                    if (IsSelectedNotePinned)
                    {
                        UnpinCommand.Execute(SelectedNote);
                    }
                    else
                    {
                        PinCommand.Execute(SelectedNote);
                    }
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.I ||
                         e.AltKey && e.VirtualKey == VirtualKey.F ||
                         e.AltKey && e.VirtualKey == VirtualKey.M)
                {
                    SelectedNote.IsImportant = !SelectedNote.IsImportant;
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.Number1 ||
                         e.AltKey && e.VirtualKey == VirtualKey.NumberPad1)
                {
                    SelectedNote.Color = ColorCategory.Neutral;
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.Number2 ||
                         e.AltKey && e.VirtualKey == VirtualKey.NumberPad2)
                {
                    SelectedNote.Color = ColorCategory.Red;
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.Number3 ||
                         e.AltKey && e.VirtualKey == VirtualKey.NumberPad3)
                {
                    SelectedNote.Color = ColorCategory.Blue;
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.Number4 ||
                         e.AltKey && e.VirtualKey == VirtualKey.NumberPad4)
                {
                    SelectedNote.Color = ColorCategory.Green;
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.Number5 ||
                         e.AltKey && e.VirtualKey == VirtualKey.NumberPad5)
                {
                    SelectedNote.Color = ColorCategory.Violett;
                }
                else if (e.AltKey && e.VirtualKey == VirtualKey.Number6 ||
                         e.AltKey && e.VirtualKey == VirtualKey.NumberPad6)
                {
                    SelectedNote.Color = ColorCategory.Orange;
                }
            });
        }

        private async void BackRequested(object sender, HandledEventArgs e)
        {
            if (AppSettings.SaveNoteOnBack.Value)
            {
                if (SelectedNote != null && !SelectedNote.IsEmtpy)
                {
                    e.Handled = true;

                    // WAIT! To ensure all text-input fields lose their focus and the bindings are fired!
                    _callbacks.UnfocusTextBoxes();
                    await Task.Delay(50);

                    await SaveNoteAsync(SelectedNote);
                    GoBackToMainPageWithoutBackEvent();
                }
            }
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
            set {
                Set(ref _selectedNote, value);
                RaisePropertyChanged("SelectedAttachementImageOrReload");
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
        /// Gets or sets whether the edit page is in EDIT mode or ADD mode. 
        /// </summary>
        public bool IsEditMode
        {
            get { return _isEditMode; }
            private set
            {
                Set(ref _isEditMode, value);
                RaisePropertyChanged("PageTitle");
            }
        }
        private bool _isEditMode;

        public string PageTitle
        {
            get
            {
                return IsEditMode ? _localizer.Get("EditTitle.Text") : _localizer.Get("NewTitle.Text");
            }
        }

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

        public ICommand RemoveCommand { get; private set; }

        public ICommand DiscardCommand { get; private set; }

        public ICommand SelectAttachementCommand { get; private set; }

        public ICommand RemoveAttachementCommand { get; private set; }

        public ICommand VoiceToTextCommand { get; private set; }

        public ICommand ReadNoteCommand { get; private set; }

        public ICommand ColorSelectedCommand { get; private set; }

        public ICommand PinCommand { get; private set; }

        public ICommand UnpinCommand { get; private set; }

        public ICommand ShareCommand { get; private set; }
    }
}
