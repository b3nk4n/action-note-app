using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Input;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace ActionNote.App.ViewModels
{
    public interface MainViewModelCallbacks
    {
        void ShowEditView(NoteItem noteItem);
        void HideEditView();
    }

    public class MainViewModel : ViewModelBase
    {
        private MainViewModelCallbacks _callbacks;

        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;

        public ObservableCollection<NoteItem> NoteItems {
            get;
            private set;
        } = new ObservableCollection<NoteItem>();

        public MainViewModel(MainViewModelCallbacks callbacks)
        {
            _callbacks = callbacks;

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
                _callbacks.ShowEditView(null);
            });

            EditCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                _callbacks.ShowEditView(noteItem);
            },
            (noteItem) =>
            {
                return noteItem != null;
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

            ReloadData();

            // TODO: why do we have to manually raise the property? it's an observable collection!
            RaisePropertyChanged("NoteItems");
        }

        public override async Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await _notesRepository.Save();

            await base.OnNavigatedFromAsync(state, suspending);
        }

        private void ReloadData()
        {
            NoteItems.Clear();
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

        public ICommand EditCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }
    }
}
