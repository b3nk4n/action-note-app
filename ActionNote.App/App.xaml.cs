using ActionNote.App.Views;
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
using UWPCore.Framework.Speech;
using System.Runtime.Serialization;
using UWPCore.Framework.Data;
using ActionNote.Common;

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
        private ISpeechService _speechService;
        private ISerializationService _serializationService;

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
            _speechService = Injector.Get<ISpeechService>();
            _serializationService = Injector.Get<ISerializationService>();

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

        public async override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            await base.OnInitializeAsync(args);

            // only add the app shell when the app was not already running
            if (args.PreviousExecutionState != ApplicationExecutionState.Running &&
                args.PreviousExecutionState != ApplicationExecutionState.Suspended)
            {
                Window.Current.Content = new AppShell(
                    RootFrame,
                    GetNavigationMenuItems(),
                    GetBottomDockedNavigationMenuItems());
            }

            _speechService = Injector.Get<ISpeechService>();
            await _speechService.InstallCommandSets("/Assets/Cortana/voicecommands.xml");
        }

        public async override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
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

                if (toastArgs.Argument == "quickNote")
                {
                    pageType = typeof(EditPage);
                }
                else if (toastArgs.Argument.StartsWith("edit"))
                {
                    var splitted = toastArgs.Argument.Split('-');
                    pageType = typeof(EditPage);
                    parameter = AppConstants.PARAM_ID + splitted[1];
                }
                else if (toastArgs.Argument.StartsWith("delete"))
                {
                    // TODO: foreground deleted launched
                }
                else
                {
                    pageType = typeof(EditPage);
                    parameter = AppConstants.PARAM_ID + toastArgs.Argument;
                }
            }
            else if (args.Kind == ActivationKind.Launch) // when launched from Action Center title
            {
                _toastUpdateService.Refresh(_notesRepository);
                _refreshActionCentertimer.Start();
            }
            else if (args.Kind == ActivationKind.VoiceCommand)
            {
                // check voice commands
                var command = _speechService.GetVoiceCommand(args);
                if (command != null)
                {
                    string content;
                    string color;
                    switch (command.CommandName)
                    {
                        case "newNote":
                            pageType = typeof(EditPage);
                            break;

                        case "newNoteWithContent":
                            content = command.Interpretations["naturalLanguage"];
                            pageType = typeof(EditPage);
                            parameter = _serializationService.SerializeJson(new NoteItem(null, content));
                            break;

                        case "newNoteWithContentAndColor":
                            content = command.Interpretations["naturalLanguage"];
                            color = command.Interpretations["colors"];
                            pageType = typeof(EditPage);
                            parameter = _serializationService.SerializeJson(new NoteItem(null, content) { Color = (ColorCategory)Enum.Parse(typeof(ColorCategory), color) });
                            break;
                    }
                }
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
