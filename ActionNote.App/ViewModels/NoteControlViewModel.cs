using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Windows.Input;
using UWPCore.Framework.Data;
using UWPCore.Framework.Mvvm;
using System.Collections.Generic;
using Windows.UI.Xaml.Navigation;
using System.Threading.Tasks;

namespace ActionNote.App.ViewModels
{
    public class NoteControlViewModel : ViewModelBase
    {
        private INotesRepository _notesRepository;
        private IToastUpdateService _toastUpdateService;

        public EnumSource<ColorCategory> ColorEnumSource { get; private set; } = new EnumSource<ColorCategory>();

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

        public void NotifyControlShown() // TODO: remove when refactored?
        {
            ColorEnumSource.SelectedValue = SelectedNote.Color.ToString(); // TODO: this is never called: (1) nested view model (2) no navigation is performed! --> merge noteControl inside of main page?
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
