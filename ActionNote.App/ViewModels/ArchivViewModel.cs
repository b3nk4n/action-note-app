using ActionNote.App.Views;
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
using Windows.UI.Xaml.Controls;
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

            ReadOnlyCommand = new DelegateCommand<NoteItem>((noteItem) =>
            {
                NavigationService.Navigate(typeof(ReadOnlyPage), AppConstants.PARAM_ID + noteItem.Id);
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            ClearCommand = new DelegateCommand(async () =>
            {
                var result = await _dialogService.ShowAsync(
                    _localizer.Get("Message.ReallyDeleteAll"),
                    _localizer.Get("Message.Title.Attention"),
                    0, 1,
                    new UICommand(_localizer.Get("Message.Option.Yes")) { Id = "y" },
                    new UICommand(_localizer.Get("Message.Option.No")) { Id = "n" });
                if (result.Id.ToString().Equals("n"))
                    return;

                if (await _dataService.RemoveAllFromArchiveAsync())
                {
                    NoteItems.Clear();

                    SelectedNote = null;
                }
                else
                {
                    await _dialogService.ShowAsync(
                        _localizer.Get("Message.CouldNotDeleteArchive"),
                        _localizer.Get("Message.Title.Warning"));
                }
            },
            () =>
            {
                return _dataService.ArchivesCount > 0;
            });

            RemoveCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (await _dataService.RemoveFromArchiveAsync(noteItem))
                {
                    NoteItems.Remove(noteItem);

                    SelectedNote = null;
                }
                else
                { // Currently we never get here, because the method returns TRUE even when we have no internet connection.
                    await _dialogService.ShowAsync(
                        _localizer.Get("Message.CouldNotDeleteArchive"),
                        _localizer.Get("Message.Title.Warning"));
                }
            },
            (noteItem) =>
            {
                return noteItem != null;
            });

            RestoreCommand = new DelegateCommand<NoteItem>(async (noteItem) =>
            {
                if (await _dataService.RestoreFromArchiveAsync(noteItem))
                {
                    NoteItems.Remove(noteItem);
                    SelectedNote = null;
                }
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

            ToggleNoteItemViewState = new DelegateCommand(() =>
            {
                var newValue = !AppSettings.IsNoteItemMaximized.Value;
                AppSettings.IsNoteItemMaximized.Value = newValue;

                UpdateNoteItemViewState(newValue);
            });
        }

        private void UpdateNoteItemViewState(bool isMaximized)
        {
            RaisePropertyChanged("NoteItemMaxHeight");
            RaisePropertyChanged("NotesExpandStateIcon");
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
            var data = await _dataService.GetAllArchives();

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

        public double NoteItemMaxHeight
        {
            get
            {
                if (AppSettings.IsNoteItemMaximized.Value)
                    return double.MaxValue;
                else
                    return AppConstants.MINIMIZED_NOTE_ITEM_HEIGHT;
            }
        }

        public IconElement NotesExpandStateIcon
        {
            get
            {
                string glymph;
                if (AppSettings.IsNoteItemMaximized.Value)
                    glymph = "\uE73F"; // BackToWindow
                else
                    glymph = "\uE740"; // Fullscreen

                return new FontIcon()
                {
                    Glyph = glymph
                };
            }
        }

        public ICommand ReadOnlyCommand { get; private set; }

        public ICommand ClearCommand { get; private set; }

        public ICommand RemoveCommand { get; private set; }

        public ICommand RestoreCommand { get; private set; }

        public ICommand SortCommand { get; private set; }

        public ICommand ToggleNoteItemViewState { get; private set; }
    }
}
