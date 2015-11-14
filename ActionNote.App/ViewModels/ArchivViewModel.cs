using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using UWPCore.Framework.Mvvm;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class ArchivViewModel : ViewModelBase
    {
        private INoteDataService _dataService;

        public ObservableCollection<NoteItem> NoteItems
        {
            get;
            private set;
        } = new ObservableCollection<NoteItem>();

        public ArchivViewModel()
        {
            _dataService = Injector.Get<INoteDataService>();
        }

        public async override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            await ReloadData();
        }

        private async Task ReloadData()
        {
            // ensure the repository has been loaded
            await _dataService.Archiv.Load();

            NoteItems.Clear();
            foreach (var note in _dataService.Archiv.GetAll())
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
    }
}
