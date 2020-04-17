using System;
using CoreGraphics;
using UIKit;
using System.Linq;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class IntrinsicContentSizedStackView : UIStackView
    {
        public IntrinsicContentSizedStackView() : base()
        {
        }

        public override CGSize IntrinsicContentSize
        {
            get
            {
                nfloat height = 0;
                foreach(var subview in ArrangedSubviews.Where(view => !view.Hidden))
                {
                    if (subview.IntrinsicContentSize.Height > 0)
                    {
                        height += subview.IntrinsicContentSize.Height;
                    }
                }
                return new CGSize(-1, height);
            }
        }
    }
}
