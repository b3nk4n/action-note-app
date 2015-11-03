using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System;
using System.Windows.Input;
using Windows.Storage;
using Windows.Storage.Pickers;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Storage;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using UWPCore.Framework.Navigation;

namespace ActionNote.App.ViewModels
{
    public class EditViewModel : ViewModelBase
    {
        private INotesRepository _notesRepository;
        private IToastUpdateService _toastUpdateService;
        private IStorageService _localStorageService;

        public EnumSource<ColorCategory> ColorEnumSource { get; private set; } = new EnumSource<ColorCategory>();

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
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            RemoveAttachementCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                var filename = noteItem.AttachementFile;

                noteItem.AttachementFile = null;

                await _localStorageService.DeleteFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + filename);
                

                _toastUpdateService.Refresh(_notesRepository);
            },
            (noteItem) =>
            {
                return noteItem != null && noteItem.HasAttachement;
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
            // color value must be used from the enum source
            noteItem.Color = (ColorCategory)ColorEnumSource.SelectedItem;

            if (_notesRepository.Contains(noteItem.Id))
            {
                _notesRepository.Update(noteItem);
            }
            else
            {
                _notesRepository.Add(noteItem);
            }

            await _notesRepository.Save();

            _toastUpdateService.Refresh(_notesRepository);
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            NoteItem noteToEdit = null;
            if (parameter != null)
            {
                var noteId = (string)parameter;
                noteToEdit = _notesRepository.Get(noteId);
            }

            // add new note, when no ID was passed. Also create a copy to be able to discard all the changes
            if (noteToEdit == null)
            {
                SelectedNote = new NoteItem();
                IsEditMode = false;
            }
            else
            {
                SelectedNote = noteToEdit.Clone();
                IsEditMode = true;
            }

            ColorEnumSource.SelectedItem = SelectedNote.Color;
        }

        public override async void OnNavigatingFrom(NavigatingEventArgs args)
        {
            base.OnNavigatingFrom(args);

            if (!_blockBackEvent)
            {
                if (args.NavigationMode == NavigationMode.Back)
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
                return IsEditMode ? "EDIT" : "NEW NOTE";
            }
        }

        public ICommand SaveCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public ICommand DiscardCommand { get; private set; }

        public ICommand SelectAttachementCommand { get; private set; }

        public ICommand RemoveAttachementCommand { get; private set; }
    }
}
