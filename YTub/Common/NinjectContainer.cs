using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;

namespace YTub.Common
{
    public class NinjectContainer
    {
        private static IKernel _vmKernel;

        public static IKernel VmKernel
        {
            get
            {
                if (_vmKernel == null)
                {
                    _vmKernel = new StandardKernel(new NinjectModuleViewModels());
                }
                return _vmKernel;
            }
        }
    }
}
