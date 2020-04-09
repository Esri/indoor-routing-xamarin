using System;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.UI.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    public class AttributionViewController : UIViewController
    {
        MapView _mapView;
        private UITextView _textblock;
        private UIBarButtonItem _closeButton;

        public AttributionViewController(MapView mapView) : base()
        {
            _mapView = mapView;
        }

        public override void LoadView()
        {
            base.LoadView();

            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            _textblock = new UITextView { TranslatesAutoresizingMaskIntoConstraints = false };
            _textblock.TextColor = UIColor.LabelColor;

            View.AddSubview(_textblock);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _textblock.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, 8),
                _textblock.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, 8),
                _textblock.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -8),
                _textblock.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor, -8)
            });

            _closeButton = new UIBarButtonItem("ModalCloseButtonText".AsLocalized(), UIBarButtonItemStyle.Plain, null);
        }

        public override void ViewWillAppear(bool animated)
        {
            Title = "AttributionModalViewTitle".AsLocalized();

            _textblock.Text = _mapView.AttributionText;

            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;
            NavigationItem.SetRightBarButtonItem(_closeButton, false);

            _closeButton.Clicked += _closeButton_Clicked;

            base.ViewWillAppear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {

            _closeButton.Clicked -= _closeButton_Clicked;

            base.ViewDidDisappear(animated);
        }

        private void _closeButton_Clicked(object sender, EventArgs e)
        {
            NavigationController?.DismissModalViewController(true);
        }
    }
}
