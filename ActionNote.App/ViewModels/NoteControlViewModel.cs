using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Windows.Input;
using UWPCore.Framework.Mvvm;

namespace ActionNote.App.ViewModels
{
    public class NoteControlViewModel : ViewModelBase
    {
        private INotesRepository _notesRepository;
        private IToastUpdateService _toastUpdateService;

        public NoteControlViewModel()
            : this(null)
        {
        }

        public NoteControlViewModel(NoteItem editItem)
        {
            SelectedNote = (editItem == null) ? new NoteItem() : editItem.Clone();

            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();

            SaveCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                if (_notesRepository.Contains(noteItem.Id))
                {
                    _notesRepository.Update(noteItem);
                }
                else
                {
                    _notesRepository.Add(noteItem);
                }
                _toastUpdateService.Refresh();

                // TODO: hide itself
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            DiscardCommand = new DelegateCommand(() =>
            {
                NavigationService.GoBack();
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
