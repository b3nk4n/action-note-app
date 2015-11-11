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

namespace ActionNote.App.ViewModels
{
    public class EditViewModel : ViewModelBase
    {
        private INotesRepository _notesRepository;
        private IToastUpdateService _toastUpdateService;
        private IStorageService _localStorageService;
        private ISpeechService _speechService;
        private ISerializationService _serializationService;
        private ITilePinService _tilePinService;

        /// <summary>
        /// Flag that indicates that no save operation is needed on BACK event.
        /// This is to prevent double saving.
        /// </summary>
        private bool _blockBackEvent;

        public EditViewModel()
        {
            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();
            _localStorageService = Injector.Get<ILocalStorageService>();
            _speechService = Injector.Get<ISpeechService>();
            _serializationService = Injector.Get<ISerializationService>();
            _tilePinService = Injector.Get<ITilePinService>();

            SaveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                await SaveNoteAsync(noteItem);
                GoBackWithoutBackEvent();
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            DiscardCommand = new DelegateCommand(() =>
            {
                GoBackWithoutBackEvent();
            });

            RemoveCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                _notesRepository.Remove(noteItem.Id);
                _toastUpdateService.Refresh(_notesRepository);

                GoBackWithoutBackEvent();
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

                StorageFile file = await picker.PickSingleFileAsync();
                if (file != null)
                {
                    var canonicalPrefix = noteItem.Id + '-';
                    var fileName = canonicalPrefix + file.Name;

                    if (await _localStorageService.WriteFile(AppConstants.ATTACHEMENT_BASE_PATH + fileName, file))
                    {
                        noteItem.AttachementFile = fileName;
                    }
                }

                _toastUpdateService.Refresh(_notesRepository);

                RemoveAttachementCommand.RaiseCanExecuteChanged();
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            RemoveAttachementCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var filename = noteItem.AttachementFile;

                noteItem.AttachementFile = null;

                await _localStorageService.DeleteFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + filename); // TODO: fixme: raises exception !?!
                

                _toastUpdateService.Refresh(_notesRepository);

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

            ReadNoteCommand = new DelegateCommand<NoteItem>(async (noteItem) => // TODO: enabled state is not updated when note is changed. Create a NoteItemViewModel which bundles the functionality of a note?
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
        }

        /// <summary>
        /// Go back and prevent duplicated save.
        /// </summary>
        private void GoBackWithoutBackEvent()
        {
            // prevent duplicated save
            _blockBackEvent = true;

            NavigationService.GoBack();
        }

        private async Task SaveNoteAsync(NoteItem noteItem)
        {
            if (_notesRepository.Contains(noteItem.Id))
            {
                _notesRepository.Update(noteItem);
            }
            else
            {
                _notesRepository.Add(noteItem);
            }

            await _notesRepository.Save();

            // update action center
            _toastUpdateService.Refresh(_notesRepository);

            // update tile in case it was pinned
            _tilePinService.Update(noteItem);
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            NoteItem noteToEdit = null;
            if (parameter != null)
            {
                var stringParam = (string)parameter;

                if (stringParam.StartsWith(AppConstants.PARAM_ID))
                {
                    string noteId = stringParam.Remove(0, AppConstants.PARAM_ID.Length);
                    noteToEdit = _notesRepository.Get(noteId).Clone();
                    IsEditMode = true;
                }
                else
                {
                    noteToEdit = _serializationService.DeserializeJson<NoteItem>(stringParam);
                    IsEditMode = false;
                }
            }
            else
            {
                noteToEdit = new NoteItem();
                IsEditMode = false;
            }

            SelectedNote = noteToEdit;
        }

        public override async void OnNavigatingFrom(NavigatingEventArgs args)
        {
            base.OnNavigatingFrom(args);

            if (!_blockBackEvent)
            {
                if (args.NavigationMode == NavigationMode.Back ||
                    args.NavigationMode == NavigationMode.New) // when switching the tab in the hamburger menu
                {
                    if (AppSettings.SaveNoteOnBack.Value)
                    {
                        if (SelectedNote != null && !SelectedNote.IsEmtpy)
                            await SaveNoteAsync(SelectedNote);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected note.
        /// </summary>
        public NoteItem SelectedNote
        {
            get { return _selectedNote; }
            set { Set(ref _selectedNote, value); }
        }
        private NoteItem _selectedNote;

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
                return IsEditMode ? "EDIT" : "NEW NOTE"; // TODO: translate
            }
        }

        public ICommand SaveCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public ICommand DiscardCommand { get; private set; }

        public ICommand SelectAttachementCommand { get; private set; }

        public ICommand RemoveAttachementCommand { get; private set; }

        public ICommand VoiceToTextCommand { get; private set; }

        public ICommand ReadNoteCommand { get; private set; }

        public ICommand ColorSelectedCommand { get; private set; }
    }
}
