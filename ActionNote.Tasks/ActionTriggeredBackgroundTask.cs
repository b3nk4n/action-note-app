using ActionNote.Common.Models;
using ActionNote.Common.Modules;
using ActionNote.Common.Services;
using UWPCore.Framework.IoC;
using UWPCore.Framework.Logging;
using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ActionNote.Tasks
{
    public sealed class ActionTriggeredBackgroundTask : IBackgroundTask
    {
        private IToastUpdateService _toastUpdateService;
        private INoteDataService _dataService;

        public ActionTriggeredBackgroundTask()
        {
            IInjector injector = Injector.Instance;
            injector.Init(new DefaultModule(), new AppModule());
            _toastUpdateService = injector.Get<IToastUpdateService>();
            _dataService = injector.Get<INoteDataService>();
        }

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            var details = taskInstance.TriggerDetails as ToastNotificationActionTriggerDetail;
            if (details != null)
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
                            var noteItem = new NoteItem("Quick Note", content);
                            _dataService.Notes.Add(noteItem);
                            await _dataService.Notes.Save(noteItem);
                        }
                    }
                }

                // re-add the item again
                //var item = _notesRepository.Get(details.Argument);
                Logger.WriteLine("ActionTriggeredBackgroundTask - Note {0} was clicked. Refreshing history...", details.Argument);

                _toastUpdateService.Refresh(_dataService.Notes);
            }

            deferral.Complete();
        }
    }
}
