using ActionNote.Common.Models;
using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Notifications.Models;
using Windows.UI.Xaml.Navigation;

namespace ActionNote.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;

        public ICommand TestPostNotification { get; private set; }

        public MainViewModel()
        {
            _toastUpdateService = Injector.Get<IToastUpdateService>();
            _notesRepository = Injector.Get<INotesRepository>();

            TestPostNotification = new DelegateCommand(() =>
            {
                _toastUpdateService.Refresh();
            });
        }

        public override void OnNavigatedTo(object parameter, NavigationMode mode, IDictionary<string, object> state)
        {
            base.OnNavigatedTo(parameter, mode, state);

            _notesRepository.Load();
        }
    }
}
