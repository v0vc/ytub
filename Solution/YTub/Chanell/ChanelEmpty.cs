﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YTub.Common;
using YTub.Controls;
using YTub.Video;

namespace YTub.Chanell
{
    public class ChanelEmpty: ChanelBase
    {
        public ChanelEmpty()
        {
            LastColumnHeader = "Download";
            ViewSeedColumnHeader = "Views";
            DurationColumnHeader = "Duration";
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

        public override void DownloadItem(IList list)
        {
            throw new NotImplementedException();
        }

        public override void SearchItems(string key, ObservableCollectionEx<VideoItemBase> listSearchVideoItems)
        {
            throw new NotImplementedException();
        }

        public override void GetPopularItems(string key, ObservableCollectionEx<VideoItemBase> listPopularVideoItems)
        {
            throw new NotImplementedException();
        }

        public override void DownloadVideoInternal(IList list)
        {
            throw new NotImplementedException();
        }
    }
}
