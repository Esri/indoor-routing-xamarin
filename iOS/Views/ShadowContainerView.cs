using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    /// <summary>
    /// Custom view container applies a standard shadow to the contained view
    /// </summary>
    public class ShadowContainerView : UIView
    {
        private UIView _innerContainer;

        private ShadowContainerView() { }

        public ShadowContainerView(UIView childView)
        {
            TranslatesAutoresizingMaskIntoConstraints = false; // TODO - is this a good idea?

            Layer.ShadowColor = UIColor.Black.CGColor;
            Layer.ShadowRadius = 1;
            //Layer.CornerRadius = 8;
            Layer.ShadowOpacity = 0.5f;
            Layer.ShadowOffset = new CoreGraphics.CGSize(0, 0);

            _innerContainer = new UIView();
            _innerContainer.ClipsToBounds = true;
            _innerContainer.Layer.CornerRadius = 8;
            _innerContainer.TranslatesAutoresizingMaskIntoConstraints = false;

            AddSubview(_innerContainer);

            _innerContainer.AddSubview(childView);
            childView.TranslatesAutoresizingMaskIntoConstraints = false;

            childView.Layer.CornerRadius = 8;
            childView.ClipsToBounds = true;

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                childView.LeadingAnchor.ConstraintEqualTo(_innerContainer.LeadingAnchor),
                childView.TrailingAnchor.ConstraintEqualTo(_innerContainer.TrailingAnchor),
                childView.TopAnchor.ConstraintEqualTo(_innerContainer.TopAnchor),
                childView.BottomAnchor.ConstraintEqualTo(_innerContainer.BottomAnchor),
                _innerContainer.LeadingAnchor.ConstraintEqualTo(LeadingAnchor),
                _innerContainer.TrailingAnchor.ConstraintEqualTo(TrailingAnchor),
                _innerContainer.BottomAnchor.ConstraintEqualTo(BottomAnchor),
                _innerContainer.TopAnchor.ConstraintEqualTo(TopAnchor)
            });

            
        }
    }
}
