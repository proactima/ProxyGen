using Ninject.Modules;

namespace ProxyGen
{
    public class CodeGenModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ProxyGenerator>().ToSelf();
            Bind<Extractor>().ToSelf();
            Bind<Generator>().ToSelf();
        }
    }
}