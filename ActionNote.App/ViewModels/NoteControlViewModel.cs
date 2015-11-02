using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Windows.Input;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using Windows.UI.Xaml.Controls;

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

            SaveCommand = new DelegateCommand<NoteItem>((noteItem) =>
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
                _toastUpdateService.Refresh();
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
    }
}
