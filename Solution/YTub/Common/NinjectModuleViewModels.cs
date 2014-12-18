using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject.Modules;

namespace YTub.Common
{
    public class NinjectModuleViewModels :NinjectModule
    {
        public override void Load()
        {
            Bind<ViewModels.MainWindowViewModel>().ToSelf().InSingletonScope();
        }
    }
}
