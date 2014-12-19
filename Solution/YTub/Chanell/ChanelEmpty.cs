using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YTub.Common;
using YTub.Video;

namespace YTub.Chanell
{
    public class ChanelEmpty: ChanelBase
    {
        public ChanelEmpty()
        {
        }

        public override CookieContainer GetSession()
        {
            throw new NotImplementedException();
        }

        public override void GetItemsFromNet()
        {
            throw new NotImplementedException();
        }

        public override void AutorizeChanel()
        {
            throw new NotImplementedException();
        }

        public override void DownloadItem()
        {
            throw new NotImplementedException();
        }

        public override void SearchItems(string key, TrulyObservableCollection<VideoItemBase> listSearchVideoItems)
        {
            throw new NotImplementedException();
        }

        public override void GetPopularItems(string key, TrulyObservableCollection<VideoItemBase> listPopularVideoItems)
        {
            throw new NotImplementedException();
        }
    }
}
