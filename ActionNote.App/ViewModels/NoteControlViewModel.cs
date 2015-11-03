using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System;
using System.Windows.Input;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Storage;
using System.Net.Http;
using Windows.Storage.Pickers;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Storage;

namespace ActionNote.App.ViewModels
{
    public interface INoteControlViewModelCallbacks
    {
        void NoteSaved(NoteItem noteItem);
        void NoteUpdated(NoteItem noteItem);
        void NoteDiscared();
    }

    public class NoteControlViewModel : ViewModelBase
    {
        private INotesRepository _notesRepository;
        private IToastUpdateService _toastUpdateService;
        private IStorageService _localStorageService;

        public EnumSource<ColorCategory> ColorEnumSource { get; private set; } = new EnumSource<ColorCategory>();

        private INoteControlViewModelCallbacks _callbacks;

        public NoteControlViewModel(INoteControlViewModelCallbacks callbacks)
            : this(callbacks, null)
        {
        }

        public NoteControlViewModel(INoteControlViewModelCallbacks callbacks, NoteItem editItem)
        {
            _callbacks = callbacks;
            SelectedNote = (editItem == null) ? new NoteItem() : editItem.Clone();

            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();
            _localStorageService = Injector.Get<ILocalStorageService>();

            SaveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (_notesRepository.Contains(noteItem.Id))
                {
                    _notesRepository.Update(noteItem);
                    _callbacks.NoteUpdated(noteItem);
                }
                else
                {
                    _notesRepository.Add(noteItem);
                    _callbacks.NoteSaved(noteItem);
                }

                await _notesRepository.Save();

                _toastUpdateService.Refresh(_notesRepository);
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            DiscardCommand = new DelegateCommand(() =>
            {
                //NavigationService.GoBack();
                _callbacks.NoteDiscared();
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
        /// Gets or sets the selected note.
        /// </summary>
        public NoteItem SelectedNote
        {
            get { return _selectedNote; }
            set { Set(ref _selectedNote, value); }
        }
        private NoteItem _selectedNote;

        public ICommand SaveCommand { get; private set; }

        public ICommand DiscardCommand { get; private set; }

        public ICommand SelectAttachementCommand { get; private set; }

        public ICommand RemoveAttachementCommand { get; private set; }
    }
}
