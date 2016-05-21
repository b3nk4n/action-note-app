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
using Windows.ApplicationModel;
using UWPCore.Framework.Speech;
using UWPCore.Framework.Data;
using ActionNote.Common;
using ActionNote.Common.Helpers;
using Windows.UI;
using UWPCore.Framework.UI;
using System.Collections.Generic;
using UWPCore.Framework.Store;
using UWPCore.Framework.Notifications;

namespace ActionNote.App
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : UniversalApp
    {
        private static string BG_TASK_ACTIONCENTER = "ActionNote.ActionBarChangedBackgroundTask";
        private static string BG_TASK_TOAST_TRIGGERED = "ActionNote.ActionTriggeredBackgroundTask";
        private static string BG_TASK_AUTO_SYNC = "ActionNote.AutoSyncBackgroundTask";
        private static string BG_TASK_TIMED_REFRESH = "ActionNote.TimedRefreshBackgroundTask";

        private IBackgroundTaskService _backgroundTaskService;
        private IActionCenterService _actionCenterService;
        private IDataService _dataService;
        private ISpeechService _speechService;
        private ISerializationService _serializationService;
        private ILicenseService _licenseService;
        private ITilePinService _tilePinService;

        private Localizer _localizer;

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
            : base(typeof(MainPage), AppBackButtonBehaviour.KeepAlive, true, new DefaultModule(), new AppModule())
        {
            InitializeComponent();

            _backgroundTaskService = Injector.Get<IBackgroundTaskService>();
            _actionCenterService = Injector.Get<IActionCenterService>();
            _speechService = Injector.Get<ISpeechService>();
            _serializationService = Injector.Get<ISerializationService>();
            _dataService = Injector.Get<IDataService>();
            _licenseService = Injector.Get<ILicenseService>();
            _tilePinService = Injector.Get<ITilePinService>();

            ShareTargetPage = typeof(SharePage);

            // initialize Microsoft Application Insights
            Microsoft.ApplicationInsights.WindowsAppInitializer.InitializeAsync(
                Microsoft.ApplicationInsights.WindowsCollectors.Metadata |
                Microsoft.ApplicationInsights.WindowsCollectors.Session);
        }

        public async override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            await base.OnInitializeAsync(args);

            // create localizer here, because the Core Windows has to be initialized
            _localizer = new Localizer();

            // setup theme colors (mainly for title bar)
            ColorPropertiesDark = new AppColorProperties(AppConstants.COLOR_ACCENT, Colors.White, Colors.Black, Colors.White, Color.FromArgb(255, 31, 31, 31));
            ColorPropertiesLight = new AppColorProperties(AppConstants.COLOR_ACCENT, Colors.Black, Colors.White, Colors.Black, Color.FromArgb(255, 230, 230, 230));

            _speechService = Injector.Get<ISpeechService>();
            //await _speechService.InstallCommandSets("/Assets/Cortana/voicecommands.xml"); // TODO: caused app not starting on phone (after Cortana setup !?!)

#if DEBUG
            await _licenseService.RefeshSimulator();
#endif
        }

        public override void OnResuming(object args)
        {
            base.OnResuming(args);

            _actionCenterService.StartTemporaryRemoveBlocking(5);
            _actionCenterService.Clear();

            // indicate that the data has to be reloaded from disk, because it might have changed outside
            // be pessimistic here and reload all after each resume, because changing the setting is a background task is not (immediately) detected by the app.
            _dataService.FlagNotesNeedReload();
            _dataService.FlagArchiveNeedsReload();

            // unregister background tasks
            UnregisterBackgroundTasks();

            // refresh the page, so that the OnNavigatedTo event is fired on the current page,
            // bot NOT on EditPage (due to loss of selected photo) 
            if (NavigationService != null &&
                NavigationService.CurrentPageType != typeof(EditPage))
                NavigationService.Refresh();
        }

        public async override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            // reload data
            await _dataService.LoadNotesAsync();

            // unregister previous one, to ensure the latest version is running
            UnregisterBackgroundTasks();

            _actionCenterService.StartTemporaryRemoveBlocking(10);
            _actionCenterService.Clear();

            var pageType = DefaultPage;
            object parameter = null;
            if (args.Kind == ActivationKind.ToastNotification)
            {
                var toastArgs = args as ToastNotificationActivatedEventArgs;

                if (toastArgs.Argument == "quickNote")
                {
                    pageType = typeof(EditPage);
                }
                else
                {
                    pageType = typeof(EditPage);

                    // workaround (because of short key)
                    var noteIds = await _dataService.GetAllNoteIds();

                    var id = ActionCenterService.GetIdFromShortId(noteIds, toastArgs.Argument);
                    if (id != null)
                    {
                        parameter = AppConstants.PARAM_ID + id;
                    }
                    else
                    {
                        // note might have been deleted in the background
                        pageType = DefaultPage;
                    }
                }
            }
            else if (args.Kind == ActivationKind.Launch)
            {
                var cause = DetermineStartCause(args);

                if (cause == AdditionalKinds.SecondaryTile)
                {
                    var lauchArgs = args as LaunchActivatedEventArgs;

                    if (lauchArgs != null)
                    {
                        var noteId = lauchArgs.Arguments;
                        if (await _dataService.GetNote(noteId) != null)
                        {
                            pageType = typeof(EditPage);
                            parameter = AppConstants.PARAM_ID + noteId;
                        }
                        else
                        {
                            // note might have been deleted in the background
                            pageType = DefaultPage;
                        }
                    }
                }
            }
            else if (args.Kind == ActivationKind.VoiceCommand)
            {
                // check voice commands
                var command = _speechService.GetVoiceCommand(args);
                if (command != null)
                {
                    string title;
                    string content;
                    string color;
                    switch (command.CommandName)
                    {
                        case "newNote":
                            pageType = typeof(EditPage);
                            break;

                        case "newNoteWithTitle":
                            title = command.Interpretations["naturalLanguageTitle"];
                            pageType = typeof(EditPage);
                            parameter = _serializationService.SerializeJson(new NoteItem(title, null));
                            break;

                        case "newNoteWithContent":
                            content = command.Interpretations["naturalLanguageContent"];
                            pageType = typeof(EditPage);
                            parameter = _serializationService.SerializeJson(new NoteItem(null, content));
                            break;

                        case "newNoteWithTitleAndContent":
                            title = command.Interpretations["naturalLanguageTitle"];
                            content = command.Interpretations["naturalLanguageContent"];
                            pageType = typeof(EditPage);
                            parameter = _serializationService.SerializeJson(new NoteItem(title, content));
                            break;

                        case "newNoteWithContentAndColor":
                            content = command.Interpretations["naturalLanguageContent"];
                            color = command.Interpretations["colors"];
                            pageType = typeof(EditPage);
                            parameter = _serializationService.SerializeJson(new NoteItem(null, content) { Color = ColorCategoryConverter.FromAnyString(color) });
                            break;
                    }
                }
            }

            // start the user experience
            NavigationService.Navigate(pageType, parameter);
        }

        public async override Task OnSuspendingAsync(SuspendingEventArgs e)
        {
            await base.OnSuspendingAsync(e);

            // make sure to quit the scan-page
            if (NavigationService.CurrentPageType == typeof(ZXing.Mobile.ScanPage))
            {
                NavigationService.GoBack();
            }

            // (re)register background tasks
            await RegisterBackgroundTasks();

            var notes = await _dataService.GetAllNotes();
            if (notes != null)
                await _actionCenterService.Refresh(notes);

            _tilePinService.UpdateMainTile(notes);

            await _dataService.CleanUpAttachementFilesAsync();
        }

        /// <summary>
        /// Gets the navigation menu items.
        /// </summary>
        /// <returns>The navigation menu items.</returns>
        protected override IEnumerable<NavMenuItem> CreateNavigationMenuItems()
        {
            return new[]
            {
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.Copy,
                    Label = _localizer.Get("Nav.Notes"),
                    DestinationPage = typeof(MainPage)
                },
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.Archive,
                    Label = _localizer.Get("Nav.Archive"),
                    DestinationPage = typeof(ArchivPage)
                },
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.StarOutline,
                    Label = _localizer.Get("Nav.Upgrade"),
                    DestinationPage = typeof(UpgradePage)
                }
            };
        }

        /// <summary>
        /// Gets the navigation menu items that are docked at the bottom.
        /// </summary>
        /// <returns>The navigation menu items.</returns>
        protected override IEnumerable<NavMenuItem> CreateBottomDockedNavigationMenuItems()
        {
            return new[]
            {
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.Info,
                    Label = _localizer.Get("Nav.About"),
                    DestinationPage = typeof(AboutPage)
                },
                new NavMenuItem()
                {
                    Symbol = GlyphIcons.Setting,
                    Label = _localizer.Get("Nav.Settings"),
                    DestinationPage = typeof(SettingsPage)
                }
            };
        }

        /// <summary>
        /// Registers all background Tasks.
        /// </summary>
        private async Task RegisterBackgroundTasks()
        {
            if (await _backgroundTaskService.RequestAccessAsync())
            {
                if (AppSettings.ShowNotesInActionCenter.Value || AppSettings.QuickNotesEnabled.Value)
                {
                    _backgroundTaskService.Register(BG_TASK_ACTIONCENTER, "ActionNote.Tasks.ActionBarChangedBackgroundTask", new ToastNotificationHistoryChangedTrigger());
                    _backgroundTaskService.Register(BG_TASK_TOAST_TRIGGERED, "ActionNote.Tasks.ActionTriggeredBackgroundTask", new ToastNotificationActionTrigger());
                    _backgroundTaskService.Register(BG_TASK_TIMED_REFRESH, "ActionNote.Tasks.TimedRefreshBackgroundTask", new TimeTrigger(180, false), new SystemCondition(SystemConditionType.UserNotPresent));
                }

                if (_dataService.IsSynchronizationActive && AppSettings.SyncInBackground.Value &&
                    AppSettings.BackgroundTaskSyncInterval.Value != AppConstants.SYNC_INTERVAL_MANUAL)
                {
                    uint syncInterval = 60;
                    uint.TryParse(AppSettings.BackgroundTaskSyncInterval.Value, out syncInterval);

                    _backgroundTaskService.Register(BG_TASK_AUTO_SYNC, "ActionNote.Tasks.AutoSyncBackgroundTask", new TimeTrigger(syncInterval, false), new SystemCondition(SystemConditionType.InternetAvailable));
                }
            }
        }

        /// <summary>
        /// Unregisters all background Tasks.
        /// </summary>
        private void UnregisterBackgroundTasks()
        {
            if (_backgroundTaskService.RegistrationExists(BG_TASK_ACTIONCENTER))
                _backgroundTaskService.Unregister(BG_TASK_ACTIONCENTER, true); // <-- Cancel this task! Because there is a race condition, that all notes could be deleted by this task!!!
            if (_backgroundTaskService.RegistrationExists(BG_TASK_TOAST_TRIGGERED))
                _backgroundTaskService.Unregister(BG_TASK_TOAST_TRIGGERED);
            if (_backgroundTaskService.RegistrationExists(BG_TASK_AUTO_SYNC))
                _backgroundTaskService.Unregister(BG_TASK_AUTO_SYNC);
            if (_backgroundTaskService.RegistrationExists(BG_TASK_TIMED_REFRESH))
                _backgroundTaskService.Unregister(BG_TASK_TIMED_REFRESH);
        }
    }
}
