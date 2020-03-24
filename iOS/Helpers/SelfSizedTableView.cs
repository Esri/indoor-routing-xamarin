using System;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    public class SelfSizedTableView : UITableView
    {
        public SelfSizedTableView() : base()
        {
            TableFooterView = new UIView() { Frame = new CGRect(0, 0, 44, 1) };
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
