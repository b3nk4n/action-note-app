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

namespace ActionNote.App.ViewModels
{
    public class EditViewModel : ViewModelBase
    {
        private INotesRepository _notesRepository;
        private IToastUpdateService _toastUpdateService;
        private IStorageService _localStorageService;

        public EnumSource<ColorCategory> ColorEnumSource { get; private set; } = new EnumSource<ColorCategory>();

        public EditViewModel()
        {
            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();
            _localStorageService = Injector.Get<ILocalStorageService>();

            SaveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
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

                _toastUpdateService.Refresh(_notesRepository);

                NavigationService.GoBack();
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            DiscardCommand = new DelegateCommand(() =>
            {
                NavigationService.GoBack();
            });

            RemoveCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                _notesRepository.Remove(noteItem.Id);

                _toastUpdateService.Refresh(_notesRepository);

                NavigationService.GoBack();
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
        }

        public override Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            return base.OnNavigatedFromAsync(state, suspending);
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
