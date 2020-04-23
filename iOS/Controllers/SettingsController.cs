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
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{

    /// <summary>
    /// Controller handles the UI and logic for the Settings screen
    /// </summary>
    internal sealed class SettingsController : UITableViewController
    {
        private readonly MapViewModel _viewModel;

        private UITableView _settingsTableView;
        private UIBarButtonItem _closeButton;
        private SettingsTableSource _tableSource;

        public SettingsController(MapViewModel viewModel)
        {
            Title = "SettingsViewTitle".Localize();
            _viewModel = viewModel;
        }

        public override void LoadView()
        {
            View = new UIView { BackgroundColor = ApplicationTheme.BackgroundColor, TintColor = ApplicationTheme.ActionBackgroundColor };

            _tableSource = new SettingsTableSource(_viewModel);

            _settingsTableView = new UITableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundColor = UIColor.Clear,
                Source = _tableSource
            };

            View.AddSubview(_settingsTableView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _settingsTableView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _settingsTableView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _settingsTableView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _settingsTableView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });

            // Close button is shown directly in the navigation area, configured in ViewWillAppear
            _closeButton = new UIBarButtonItem("ModalCloseButtonText".Localize(), UIBarButtonItemStyle.Plain, null);
        }

        public override void ViewWillAppear(bool animated)
        {
            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;
            NavigationItem.SetRightBarButtonItem(_closeButton, false);

            // Subscribe to events
            _closeButton.Clicked += CloseButton_Clicked;
            _tableSource.TableRowSelected += TableSource_RowSelected;

            // reload any changed settings
            _settingsTableView.ReloadData();

            base.ViewWillAppear(animated);
        }

        public override void ViewDidDisappear(bool animated)
        {
            // Unsubscribe from events
            _closeButton.Clicked -= CloseButton_Clicked;
            _tableSource.TableRowSelected -= TableSource_RowSelected;

            base.ViewDidDisappear(animated);
        }

        private void TableSource_RowSelected(object sender, NSIndexPath e)
        {
            // Navigate to the page for selecting a home location
            if (e.Row == 0)
            {
                NavigationController.PushViewController(new HomeLocationController(_viewModel), true);
            }

            // Deselect the row (tableview isn't intended for selection)
            _settingsTableView.DeselectRow(e, true);
        }

        private void CloseButton_Clicked(object sender, EventArgs e) => NavigationController.DismissModalViewController(true);
    }
}
