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
            Bind<IActionCenterService>().To<ActionCenterService>().InSingletonScope();
            Bind<ITilePinService>().To<TilePinService>().InSingletonScope();
            Bind<INoteDataService>().To<NoteDataService>().InSingletonScope();

            // repositories
            Bind<INotesRepository>().To<NotesRepository>();
        }
    }
}
