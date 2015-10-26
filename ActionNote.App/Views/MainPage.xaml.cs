using System;
using ActionNote.App.ViewModels;
using UWPCore.Framework.Controls;
using UWPCore.Framework.Tasks;
using Windows.ApplicationModel.Background;

namespace ActionNote.App.Views
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : UniversalPage
    {
        private static string BG_TASK_ACTIONCENTER = "ActionNote.ActionBarChangedBackgroundTask";

        IBackgroundTaskService _backgroundTaskService;

        public MainPage()
        {
            InitializeComponent();

            _backgroundTaskService = Injector.Get<IBackgroundTaskService>();

            Loaded += (s, e) =>
            {
                RegisterBackgroundTask();
            };
        }

        /// <summary>
        /// (Re)registers the background task.
        /// </summary>
        private async void RegisterBackgroundTask()
        {
            // unregister previous one, to ensure the latest version is running
            if (_backgroundTaskService.RegistrationExists(BG_TASK_ACTIONCENTER))
                _backgroundTaskService.Unregister(BG_TASK_ACTIONCENTER);

            if (await _backgroundTaskService.RequestAccessAsync())
            {
                _backgroundTaskService.Register(BG_TASK_ACTIONCENTER, "ActionNote.Tasks.ActionBarChangedBackgroundTask", new ToastNotificationHistoryChangedTrigger());
            }
        }
    }
}
