﻿using ActionNote.App.Views;
using ActionNote.Common.Models;
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
using Windows.ApplicationModel;

namespace ActionNote.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : UniversalApp
    {
        private static string BG_TASK_ACTIONCENTER = "ActionNote.ActionBarChangedBackgroundTask";
        private static string BG_TASK_TOAST_TRIGGERED = "ActionNote.ActionTriggeredBackgroundTask";

        private IBackgroundTaskService _backgroundTaskService;
        private IToastUpdateService _toastUpdateService;
        private INotesRepository _notesRepository;

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
            _notesRepository = Injector.Get<INotesRepository>();

            _refreshActionCentertimer.Interval = TimeSpan.FromSeconds(3);
            _refreshActionCentertimer.Tick += (s, e) =>
            {
                _toastUpdateService.Refresh(_notesRepository);
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

            // load data
            await _notesRepository.Load();

            // unregister previous one, to ensure the latest version is running
            if (_backgroundTaskService.RegistrationExists(BG_TASK_ACTIONCENTER))
                _backgroundTaskService.Unregister(BG_TASK_ACTIONCENTER);
            if (_backgroundTaskService.RegistrationExists(BG_TASK_TOAST_TRIGGERED))
                _backgroundTaskService.Unregister(BG_TASK_TOAST_TRIGGERED);

            var pageType = DefaultPage;
            string parameter = null;
            if (args.Kind == ActivationKind.ToastNotification) 
            {
                _toastUpdateService.Refresh(_notesRepository);
                var toastArgs = args as ToastNotificationActivatedEventArgs;

                var splitted = toastArgs.Argument.Split('-');

                // toast button ([0] == command, [1] == id)
                if (splitted.Length == 2)
                {
                    if (splitted[0] == "edit")
                    {
                        pageType = typeof(EditPage);
                    }
                    else if (splitted[0] == "delete") // TODO: is this one launched in background? or is there a way without these buttons?
                    {
                        // TODO foreground deleted launch
                    }

                    parameter = splitted[1];
                }
                // toast launch
                else if (splitted.Length == 1)
                {
                    pageType = typeof(EditPage);
                    parameter = splitted[0];
                }
            }
            else if (args.Kind == ActivationKind.Launch) // when launched from Action Center title
            {
                _toastUpdateService.Refresh(_notesRepository);
                _refreshActionCentertimer.Start();
            }

            // (re)register background tasks
            if (await _backgroundTaskService.RequestAccessAsync())
            {
                _backgroundTaskService.Register(BG_TASK_ACTIONCENTER, "ActionNote.Tasks.ActionBarChangedBackgroundTask", new ToastNotificationHistoryChangedTrigger());
                _backgroundTaskService.Register(BG_TASK_TOAST_TRIGGERED, "ActionNote.Tasks.ActionTriggeredBackgroundTask", new ToastNotificationActionTrigger());
            }

            // start the user experience
            NavigationService.Navigate(pageType, parameter);
        }

        public async override Task OnSuspendingAsync(SuspendingEventArgs e)
        {
            await base.OnSuspendingAsync(e);

            // save data
            await _notesRepository.Save();
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
                    Symbol = GlyphIcons.Copy,
                    Label = "Notes",
                    DestinationPage = typeof(MainPage)
                },
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.BrowsePhotos,
                    Label = "Media",
                    DestinationPage = null // TODO: idea for own page?
                },
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.Archive,
                    Label = "Archive",
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
