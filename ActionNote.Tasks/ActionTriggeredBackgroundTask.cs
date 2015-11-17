using ActionNote.Common;
using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using UWPCore.Framework.Common;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Logging;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionTriggeredBackgroundTask : IBackgroundTask
    {
        private IActionCenterService _actionCenterService;
        private INoteDataService _dataService;

        private Localizer _localizer = new Localizer("ActionNote.Common");

        public ActionTriggeredBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _actionCenterService = injector.Get<IActionCenterService>();
            _dataService = injector.Get<INoteDataService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
            if (details != null &&
                AppSettings.SyncWithActionCenter.Value)
            {
                // load data
                await _dataService.Notes.Load();

                if (details.Argument == "quickNote")
                {
                    if (details.UserInput.ContainsKey("content"))
                    {
                        var content = (string)details.UserInput["content"];

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            var noteItem = new NoteItem(_localizer.Get("QuickNote"), content);
                            _dataService.Notes.Add(noteItem);
                            await _dataService.Notes.Save(noteItem);

                            // add it into the action center at the beginning
                            _actionCenterService.AddToTop(noteItem);
                        }
                    }
                }
            }

            deferral.Complete();
        }
    }
}
