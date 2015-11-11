using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;
using ActionNote.App.Views;
using UWPCore.Framework.Share;
using UWPCore.Framework.Storage;
using ActionNote.Common;

namespace ActionNote.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;
        private IShareContentService _shareContentService;
        private IStorageService _localStorageSerivce;
        private ITilePinService _tilePinService;

        public ObservableCollection<NoteItem> NoteItems {
            get;
            private set;
        } = new ObservableCollection<NoteItem>();

        public MainViewModel()
        {
            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();
            _shareContentService = Injector.Get<IShareContentService>();
            _localStorageSerivce = Injector.Get<ILocalStorageService>();
            _tilePinService = Injector.Get<ITilePinService>();

            ClearCommand = new DelegateCommand(() =>
            {
                _notesRepository.Clear();
                NoteItems.Clear();

                _toastUpdateService.Refresh(_notesRepository);

                SelectedNote = null;
            },
            () =>
            {
                return _notesRepository.Count > 0;
            });

            AddCommand = new DelegateCommand(() =>
            {
                NavigationService.Navigate(typeof(EditPage));
            });

            EditCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                NavigationService.Navigate(typeof(EditPage), AppConstants.PARAM_ID + noteItem.Id);
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            RemoveCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                _notesRepository.Remove(noteItem);
                NoteItems.Remove(noteItem);

                _toastUpdateService.Refresh(_notesRepository);

                SelectedNote = null;
            },
            (noteItem) =>
            {
                return noteItem != null;
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

            ShareCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (noteItem.HasAttachement)
                {
                    var file = await _localStorageSerivce.GetFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile);
                    if (file != null)
                        _shareContentService.ShareImage(noteItem.Title, file, null, noteItem.Content, "Bla bla..."); // TODO translate description
                }
                else
                {
                    _shareContentService.ShareText(noteItem.Title, noteItem.Content, "Bla bla bla..."); // TODO translate description
                }
            },
            (noteItem) =>
            {
                return noteItem != null && !noteItem.IsEmtpy;
            });
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            ReloadData();
        }

        public override async Task OnNavigatedFromAsync(IDictionary<string, object> state, bool suspending)
        {
            await base.OnNavigatedFromAsync(state, suspending);
        }

        private void ReloadData()
        {
            // ensure the repository has been loaded
            //await _notesRepository.Load(); data is loaded in app.xaml

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
            set
            {
                Set(ref _selectedNote, value);
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

        public ICommand ClearCommand { get; private set; }

        public ICommand AddCommand { get; private set; }

        public ICommand EditCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public ICommand PinCommand { get; private set; }

        public ICommand UnpinCommand { get; private set; }

        public ICommand ShareCommand { get; private set; }
    }
}
