using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;

        public ObservableCollection<NoteItem> NoteItems { get; private set; } = new ObservableCollection<NoteItem>();

        public MainViewModel()
        {
            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();

            ClearCommand = new DelegateCommand(() =>
            {
                _notesRepository.Clear();
                NoteItems.Clear();

                _toastUpdateService.Refresh();

                SelectedNote = null;
            },
            () =>
            {
                return _notesRepository.Count > 0;
            });

            AddCommand = new DelegateCommand(() =>
            {
                // TODO: show add new note view
            });

            RemoveCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                _notesRepository.Remove(noteItem);
                NoteItems.Remove(noteItem);

                _toastUpdateService.Refresh();

                SelectedNote = null;
            },
            (noteItem) =>
            {
                return noteItem != null;
            });
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            _notesRepository.Load();
            foreach (var note in _notesRepository.GetAll())
            {
                NoteItems.Add(note);
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

        public ICommand ClearCommand { get; private set; }

        public ICommand AddCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }
    }
}
