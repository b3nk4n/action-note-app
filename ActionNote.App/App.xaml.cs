using ActionNote.App.Views;
using ActionNote.Common;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using System;
using System.Threading.Tasks;
using UWPCore.Framework.Common;
using UWPCore.Framework.Controls;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Tasks;
using Windows.ApplicationModel.Activation;
using Windows.ApplicationModel.Background;
using Windows.UI.Xaml;

namespace ActionNote.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : UniversalApp
    {
        private static string BG_TASK_ACTIONCENTER = "ActionNote.ActionBarChangedBackgroundTask";

        private IBackgroundTaskService _backgroundTaskService;
        private IToastUpdateService _toastUpdateService;

        private DispatcherTimer _refreshActionCentertimer = new DispatcherTimer();

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
            : base(typeof(MainPage), AppBackButtonBehaviour.KeepAlive, new DefaultModule(), new AppModule())
        {
            InitializeComponent();

            _backgroundTaskService = Injector.Get<IBackgroundTaskService>();
            _toastUpdateService = Injector.Get<IToastUpdateService>();

            _refreshActionCentertimer.Interval = TimeSpan.FromSeconds(3);
            _refreshActionCentertimer.Tick += (s, e) =>
            {
                _toastUpdateService.Refresh();
                _refreshActionCentertimer.Stop();
            };

            // initialize Microsoft Application Insights
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
        }

        public async override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            // only add the app shell when the app was not already running
            if (args.PreviousExecutionState != ApplicationExecutionState.Running &&
                args.PreviousExecutionState != ApplicationExecutionState.Suspended)
            {
                Window.Current.Content = new AppShell(
                    RootFrame,
                    GetNavigationMenuItems(),
                    GetBottomDockedNavigationMenuItems());
            }

            // unregister previous one, to ensure the latest version is running
            if (_backgroundTaskService.RegistrationExists(BG_TASK_ACTIONCENTER))
                _backgroundTaskService.Unregister(BG_TASK_ACTIONCENTER);

            if (args.Kind == ActivationKind.ToastNotification) 
            {
                _toastUpdateService.Refresh();
            }
            else if (args.Kind == ActivationKind.Launch) // when launched from Action Center title
            {
                _toastUpdateService.Refresh();
                _refreshActionCentertimer.Start();
            }

            // (re)register background task
            if (await _backgroundTaskService.RequestAccessAsync())
            {
                _backgroundTaskService.Register(BG_TASK_ACTIONCENTER, "ActionNote.Tasks.ActionBarChangedBackgroundTask", new ToastNotificationHistoryChangedTrigger());
            }

            // start the user experience
            NavigationService.Navigate(DefaultPage);
        }

        /// <summary>
        /// Gets the navigation menu items.
        /// </summary>
        /// <returns>The navigation menu items.</returns>
        private static NavMenuItem[] GetNavigationMenuItems()
        {
            return new[]
            {
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.List,
                    Label = "Notes",
                    DestinationPage = typeof(MainPage)
                },
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.ListBlock,
                    Label = "Archiv",
                    DestinationPage = typeof(ArchivPage)
                }
            };
        }

        /// <summary>
        /// Gets the navigation menu items that are docked at the bottom.
        /// </summary>
        /// <returns>The navigation menu items.</returns>
        private static NavMenuItem[] GetBottomDockedNavigationMenuItems()
        {
            return new[]
            {
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.Info,
                    Label = "About",
                    DestinationPage = typeof(AboutPage)
                },
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.Setting,
                    Label = "Settings",
                    DestinationPage = typeof(SettingsPage)
                }
            };
        }
    }
}
