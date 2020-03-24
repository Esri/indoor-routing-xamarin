using System;
using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    public class AccessoryViewCell : UITableViewCell
    {
        private UIImageView _imageView;
        public AccessoryViewCell()
        {
            _imageView = new UIImageView { TranslatesAutoresizingMaskIntoConstraints = false };
            _imageView.TintColor = UIColor.FromName("AccessoryButtonColor");
            ContentView.AddSubview(_imageView);

            BackgroundColor = UIColor.Clear;

            //ContentView.LayoutMargins = new UIEdgeInsets(8, 8, 8, 8);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _imageView.CenterXAnchor.ConstraintEqualTo(ContentView.CenterXAnchor),
                _imageView.CenterYAnchor.ConstraintEqualTo(ContentView.CenterYAnchor),
                _imageView.WidthAnchor.ConstraintLessThanOrEqualTo(28),
                _imageView.HeightAnchor.ConstraintLessThanOrEqualTo(28)
            });
        }

        public void SetImage(UIImage image)
        {
            _imageView.Image = image;
        }
    }
}
