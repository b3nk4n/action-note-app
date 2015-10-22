using Ninject.Modules;
using ActionNote.Common.Services;

namespace ActionNote.Common.Modules
{
    public class AppModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IToastUpdateService>().To<ToastUpdateService>().InSingletonScope();
        }
    }
}
