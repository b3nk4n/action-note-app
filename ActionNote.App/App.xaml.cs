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
        private IActionCenterService _actionCenterService;
        private INoteDataService _dataService;
        private ISpeechService _speechService;
        private ISerializationService _serializationService;

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
            _dataService = Injector.Get<INoteDataService>();
            _speechService = Injector.Get<ISpeechService>();
            _serializationService = Injector.Get<ISerializationService>();

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
        }

        public override void OnResuming(object args)
        {
            base.OnResuming(args);

            // refresh the page, so that the OnNavigatedTo event is fired on the current page.
            NavigationService.Refresh();
        }

        public async override Task OnStartAsync(StartKind startKind, IActivatedEventArgs args)
        {
            // reload data
            await _dataService.LoadNotesAsync();

            // unregister previous one, to ensure the latest version is running
            if (_backgroundTaskService.RegistrationExists(BG_TASK_ACTIONCENTER))
                _backgroundTaskService.Unregister(BG_TASK_ACTIONCENTER);
            if (_backgroundTaskService.RegistrationExists(BG_TASK_TOAST_TRIGGERED))
                _backgroundTaskService.Unregister(BG_TASK_TOAST_TRIGGERED);

            _actionCenterService.StartTemporaryRemoveBlocking(10);
            _actionCenterService.Clear();

            var pageType = DefaultPage;
            string parameter = null;
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
                        pageType = typeof(EditPage);
                        parameter = AppConstants.PARAM_ID + lauchArgs.Arguments;
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

            if (AppSettings.SyncWithActionCenter.Value)
            {
                var notes = await _dataService.GetAllNotes();
                if (notes != null)
                    await _actionCenterService.RefreshAsync(notes);
            }

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
    }
}
