// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.UI.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Displays the attribution text for a map view.
    /// This is intended for use only in scenarios when the built-in attribution bar isn't visible.
    /// </summary>
    public class AttributionViewController : UIViewController
    {
        private readonly MapView _mapView;
        private UITextView _attributionTextView;
        private UIBarButtonItem _closeButton;

        public AttributionViewController(MapView mapView)
        {
            _mapView = mapView;
        }

        public override void LoadView()
        {
            base.LoadView();
            View = new UIView {BackgroundColor = ApplicationTheme.BackgroundColor};

            _attributionTextView = new UITextView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = ApplicationTheme.ForegroundColor
            };

            View.AddSubview(_attributionTextView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _attributionTextView.LeadingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.LeadingAnchor, ApplicationTheme.Margin),
                _attributionTextView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor, ApplicationTheme.Margin),
                _attributionTextView.TrailingAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TrailingAnchor, -ApplicationTheme.Margin),
                _attributionTextView.BottomAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.BottomAnchor, -ApplicationTheme.Margin)
            });

            _closeButton = new UIBarButtonItem("ModalCloseButtonText".Localize(), UIBarButtonItemStyle.Plain, null);
        }

        public override void ViewWillAppear(bool animated)
        {
            Title = "AttributionModalViewTitle".Localize();

            _attributionTextView.Text = _mapView.AttributionText;

            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;
            NavigationItem.SetRightBarButtonItem(_closeButton, false);

            _closeButton.Clicked += CloseButton_Clicked;

            base.ViewWillAppear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            _closeButton.Clicked -= CloseButton_Clicked;

            base.ViewDidDisappear(animated);
        }

        private void CloseButton_Clicked(object sender, EventArgs e)
        {
            NavigationController?.DismissModalViewController(true);
        }
    }
}