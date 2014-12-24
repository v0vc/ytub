using Ninject;

namespace YTub
{
    public class ViewModelLocator
    {
        public static ViewModels.MainWindowViewModel MvViewModel
        {
            get { return Common.NinjectContainer.VmKernel.Get<ViewModels.MainWindowViewModel>(); }
        }
    }
}
