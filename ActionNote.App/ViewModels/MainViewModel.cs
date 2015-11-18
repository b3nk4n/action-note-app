using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml.Navigation;
using ActionNote.App.Views;
using UWPCore.Framework.Share;
using UWPCore.Framework.Storage;
using ActionNote.Common;
using UWPCore.Framework.Common;
using ActionNote.Common.Helpers;
using UWPCore.Framework.UI;
using Windows.UI.Popups;

namespace ActionNote.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private INoteDataService _dataService;
        private IShareContentService _shareContentService;
        private IStorageService _localStorageSerivce;
        private ITilePinService _tilePinService;
        private IDialogService _dialogService;

        private Localizer _localizer = new Localizer();

        public ObservableCollection<NoteItem> NoteItems {
            get;
            private set;
        } = new ObservableCollection<NoteItem>();

        public MainViewModel()
        {
            _dataService = Injector.Get<INoteDataService>();
            _shareContentService = Injector.Get<IShareContentService>();
            _localStorageSerivce = Injector.Get<ILocalStorageService>();
            _tilePinService = Injector.Get<ITilePinService>();
            _dialogService = Injector.Get<IDialogService>();

            ClearCommand = new DelegateCommand(async () =>
            {
                var result = await _dialogService.ShowAsync(
                    _localizer.Get("Message.ReallyDeleteAll"),
                    _localizer.Get("Message.Title.Warning"),
                    0, 1,
                    new UICommand(_localizer.Get("Message.Option.Yes")) { Id = "y" },
                    new UICommand(_localizer.Get("Message.Option.No")) { Id = "n" });
                if (result.Id.ToString().Equals("n"))
                    return;

                // unpin all tiles
                foreach (var noteItem in _dataService.Notes.GetAll())
                {
                    await _tilePinService.UnpinAsync(noteItem.Id);

                    _dataService.MoveToArchiv(noteItem);
                }
                NoteItems.Clear();

                SelectedNote = null;
            },
            () =>
            {
                return _dataService.Notes.Count > 0;
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
                _tilePinService.UnpinAsync(noteItem.Id);

                _dataService.MoveToArchiv(noteItem);
                NoteItems.Remove(noteItem);

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
                var description = _localizer.Get("ShareContentDescription");
                if (noteItem.HasAttachement)
                {
                    var file = await _localStorageSerivce.GetFileAsync(AppConstants.ATTACHEMENT_BASE_PATH + noteItem.AttachementFile);
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

            SortCommand = new DelegateCommand<string>((sortType) =>
            {
                AppSettings.SortNoteBy.Value = sortType;

                var sorted = NoteUtils.Sort(NoteItems, sortType);
                NoteItems = new ObservableCollection<NoteItem>(sorted);
                RaisePropertyChanged("NoteItems");
            },
            (sortType) =>
            {
                return NoteItems.Count > 0;
            });
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            ReloadData();
        }

        private void ReloadData()
        {
            // ensure the repository has been loaded
            //await _notesRepository.Load(); data is loaded in app.xaml

            NoteItems.Clear();
            var data = _dataService.Notes.GetAll(); // TODO: reload all from disk?

            if (data != null)
            {
                var sorted = NoteUtils.Sort(data, AppSettings.SortNoteBy.Value);
                NoteItems = new ObservableCollection<NoteItem>(sorted);
                RaisePropertyChanged("NoteItems");
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

        public ICommand SortCommand { get; private set;}
    }
}
