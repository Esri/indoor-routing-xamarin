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

        public AttributionViewController(MapView mapView)
        {
            _mapView = mapView;
        }

        public override void LoadView()
        {
            base.LoadView();
            View = new UIView { BackgroundColor = ApplicationTheme.BackgroundColor };

            _textblock = new UITextView { TranslatesAutoresizingMaskIntoConstraints = false };
            _textblock.TextColor = ApplicationTheme.ForegroundColor;

            View.AddSubview(_textblock);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _textblock.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, ApplicationTheme.Margin),
                _textblock.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, ApplicationTheme.Margin),
                _textblock.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -ApplicationTheme.Margin),
                _textblock.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor, -ApplicationTheme.Margin)
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
