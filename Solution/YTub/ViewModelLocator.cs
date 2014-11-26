using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ninject;

namespace YTub
{
    public class ViewModelLocator
    {
        public static ViewModels.MainWindowViewModel MvViewModel
        {
            get { return Common.NinjectContainer.VmKernel.Get<ViewModels.MainWindowViewModel>(); }
        }

        //public static Views.AddChanelView AddChanelView
        //{
        //    get { return Common.NinjectContainer.VmKernel.Get<Views.AddChanelView>(); }
        //}
    }
}
