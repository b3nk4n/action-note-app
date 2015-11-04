using Ninject.Modules;
using ActionNote.Common.Services;
using ActionNote.Common.Models;

namespace ActionNote.Common.Modules
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            // services
            Bind<IToastUpdateService>().To<ToastUpdateService>().InSingletonScope();
            Bind<ITilePinService>().To<TilePinService>().InSingletonScope();

            // repositories
            Bind<INotesRepository>().To<NotesRepository>().InSingletonScope();
        }
    }
}
