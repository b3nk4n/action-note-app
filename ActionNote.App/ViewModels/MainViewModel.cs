using ActionNote.Common.Services;
using System.Collections.Generic;
using System.Text;
using System.Windows.Input;
using UWPCore.Framework.Mvvm;
using UWPCore.Framework.Notifications.Models;

namespace ActionNote.App.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private IToastUpdateService _toastUpdateService;

        public ICommand TestPostNotification { get; private set; }

        public MainViewModel()
        {
            _toastUpdateService = Injector.Get<IToastUpdateService>();

            TestPostNotification = new DelegateCommand(() =>
            {
                
            });
        }
    }
}
