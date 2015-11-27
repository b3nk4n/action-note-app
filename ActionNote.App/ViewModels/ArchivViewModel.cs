using ActionNote.Common;
using ActionNote.Common.Helpers;
using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.UI;
using Windows.UI.Popups;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class ArchivViewModel : ViewModelBase
    {
        private IDataService _dataService;
        private IDialogService _dialogService;

        private Localizer _localizer = new Localizer();

        public ObservableCollection<NoteItem> NoteItems
        {
            get;
            private set;
        } = new ObservableCollection<NoteItem>();

        public ArchivViewModel()
        {
            _dataService = Injector.Get<IDataService>();
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

                // TODO: ensure server knows about deleted items (else: sync problem!)

                _dataService.Archiv.Clear();
                NoteItems.Clear();

                SelectedNote = null;
            },
            () =>
            {
                return _dataService.Archiv.Count > 0;
            });

            RemoveCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                // TODO: ensure server knows about deleted items  (else: sync problem!)

                _dataService.Archiv.Remove(noteItem);
                NoteItems.Remove(noteItem);

                SelectedNote = null;
            },
            (noteItem) =>
            {
                return noteItem != null;
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

        public async override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            await ReloadData();
        }

        private async Task ReloadData()
        {
            // ensure the repository has been loaded
            await _dataService.LoadArchiveAsync();

            NoteItems.Clear();
            var data = _dataService.Archiv.GetAll();

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

        public ICommand ClearCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public ICommand SortCommand { get; private set; }
    }
}
