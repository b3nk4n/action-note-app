using Ninject.Modules;
using ActionNote.Common.Services;
using ActionNote.Common.Models;
using UWPCore.Framework.Store;
using ActionNote.Common.Services.Store;

namespace ActionNote.Common.Modules
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            // services
            Bind<IActionCenterService>().To<ActionCenterService>().InSingletonScope();
            Bind<ITilePinService>().To<TilePinService>().InSingletonScope();
            Bind<IDataService>().To<DataService>().InSingletonScope();

            // repositories
            Bind<INotesRepository>().To<NotesRepository>();
            Bind<IUnsyncedRepository>().To<UnsyncedRepository>();

            Bind<ICachedLicenseService>().To<CachedLicenseService>().InSingletonScope();
        }
    }
}
