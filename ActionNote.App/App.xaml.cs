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
            : base(typeof(MainPage), AppBackButtonBehaviour.KeepAlive, true, new DefaultModule(), new AppModule())
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

            // setup theme colors (mainly for title bar)
            ColorPropertiesDark = new AppColorProperties(AppConstants.COLOR_ACCENT, Colors.White, Colors.Black);
            ColorPropertiesLight = new AppColorProperties(AppConstants.COLOR_ACCENT, Colors.Black, Colors.White);

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

            var pageType = typeof(EditPage);
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
            else if (args.Kind == ActivationKind.Launch)
            {
                var cause = DetermineStartCause(args);

                if (cause == AdditionalKinds.Primary)
                {
                    // refresh needed when launched from action center (which is like launching from main tile)
                    _toastUpdateService.Refresh(_notesRepository); // TODO: refresh only when list is empty, because on primary-tile, the list is not deleted and a refresh is not necessary?
                    _refreshActionCentertimer.Start();
                }
                else if (cause == AdditionalKinds.SecondaryTile)
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

            // save data
            await _notesRepository.Save();
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
                    Label = "Notes",
                    DestinationPage = typeof(MainPage)
                },
                //new NavMenuItem()
                //{
                //    Symbol = GlyphIcons.BrowsePhotos,
                //    Label = "Media",
                //    DestinationPage = null // TODO: idea for own page?
                //},
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
        protected override IEnumerable<NavMenuItem> CreateBottomDockedNavigationMenuItems()
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
