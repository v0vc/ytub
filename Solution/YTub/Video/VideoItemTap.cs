using System;
using System.Data.Common;
using YTub.Common;

namespace YTub.Video
{
    public class VideoItemTap :VideoItemBase
    {
        public VideoItemTap(DbDataRecord record) : base (record)
        {
        }

        public override void RunFile(object runtype)
        {
            return;
            throw new NotImplementedException();
        }

        public override bool IsFileExist()
        {
            return false;
            throw new NotImplementedException();
        }
    }
}
