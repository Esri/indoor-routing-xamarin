using System;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    public class SelfSizedTableView : UITableView
    {
        public SelfSizedTableView() : base()
        {
            // Get more accurate height values from `ContentSize`
            EstimatedRowHeight = 0;
            EstimatedSectionFooterHeight = 0;
            EstimatedSectionHeaderHeight = 0;
        }

        public override void ReloadData()
        {
            base.ReloadData();
            InvalidateIntrinsicContentSize();
            LayoutIfNeeded();
        }

        public override CGSize IntrinsicContentSize
        {
            get
            {
                return ContentSize;
            }
        }
    }
}
