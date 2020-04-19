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
using System.IO;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
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
            Title = "Settings";
            _viewModel = viewModel;
        }

        public override void LoadView()
        {
            base.LoadView();

            View = new UIView { BackgroundColor = ApplicationTheme.BackgroundColor, TintColor = ApplicationTheme.ActionBackgroundColor };

            _tableSource = new SettingsTableSource();

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

            _closeButton = new UIBarButtonItem("ModalCloseButtonText".Localize(), UIBarButtonItemStyle.Plain, null);
        }

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            // Show the navigation bar
            NavigationController.NavigationBarHidden = false;
            NavigationItem.SetRightBarButtonItem(_closeButton, false);

            _closeButton.Clicked += CloseButton_Clicked;

            _tableSource.TableRowSelected += TableSource_RowSelected;

            // reload any changed settings
            _settingsTableView.ReloadData();

            base.ViewWillAppear(animated);
        }
        public override void ViewDidDisappear(bool animated)
        {
            _closeButton.Clicked -= CloseButton_Clicked;
            _tableSource.TableRowSelected -= TableSource_RowSelected;

            base.ViewDidDisappear(animated);
        }

        private void TableSource_RowSelected(object sender, NSIndexPath e)
        {
            if (e.Row == 0)
            {
                NavigationController.PushViewController(new HomeLocationController(_viewModel), true);
            }

            _settingsTableView.DeselectRow(e, true);
        }

        private void CloseButton_Clicked(object sender, EventArgs e) => NavigationController.DismissModalViewController(true);
    }

    internal class SettingsTableSource : UITableViewSource
    {
        private UISwitch _locationSwitch;
        private UISwitch _routingSwitch;
        private UISwitch _useOnlineBasemapSwitch;

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            switch (indexPath.Row)
            {
                case 0:
                    var homeCell = tableView.DequeueReusableCell("HomeLocationCell") ?? new UITableViewCell(UITableViewCellStyle.Value1, "HomeLocationCell");
                    homeCell.TextLabel.Text = "HomeLocationSettingLabel".Localize();
                    homeCell.DetailTextLabel.Text = AppSettings.CurrentSettings.HomeLocation;
                    homeCell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                    homeCell.BackgroundColor = tableView.BackgroundColor;
                    return homeCell;
                case 1:
                    var locationCell = tableView.DequeueReusableCell("LocationServicesCell") ?? new UITableViewCell(UITableViewCellStyle.Default, "LocationServicesCell");
                    locationCell.TextLabel.Text = "UseLocationServicesSettingLabel".Localize();
                    locationCell.BackgroundColor = tableView.BackgroundColor;

                    if (_locationSwitch == null)
                    {
                        _locationSwitch = new UISwitch();
                        _locationSwitch.ValueChanged += EnableLocationSwitch_Toggled;
                    }
                    locationCell.AccessoryView = _locationSwitch;

                    _locationSwitch.On  = AppSettings.CurrentSettings.IsLocationServicesEnabled;
                    return locationCell;
                case 2:
                    var routingCell = tableView.DequeueReusableCell("RoutingCell") ?? new UITableViewCell(UITableViewCellStyle.Default, "RoutingCell");
                    routingCell.TextLabel.Text = "EnableRoutingSettingLabel".Localize();
                    routingCell.BackgroundColor = tableView.BackgroundColor;

                    if (_routingSwitch == null)
                    {
                        _routingSwitch = new UISwitch();
                        _routingSwitch.ValueChanged += EnableRoutingSwitch_Toggled;
                    }
                    routingCell.AccessoryView = _routingSwitch;

                    //_routingSwitch.Enabled = MapViewModel.Instance.Map.TransportationNetworks.Any();
                    _routingSwitch.On = AppSettings.CurrentSettings.IsRoutingEnabled;
                    return routingCell;
                case 3:
                    var useOnlineBasemapCell = tableView.DequeueReusableCell("OnlineBasemapCell") ?? new UITableViewCell(UITableViewCellStyle.Default, "UseOnlineBasemapCell");
                    useOnlineBasemapCell.TextLabel.Text = "UseOnlineBasemapSettingLabel".Localize();
                    useOnlineBasemapCell.BackgroundColor = tableView.BackgroundColor;

                    if (_useOnlineBasemapSwitch == null)
                    {
                        _useOnlineBasemapSwitch = new UISwitch();
                        _useOnlineBasemapSwitch.ValueChanged += UseOnlineBasemapSwitch_Toggled;
                    }
                    useOnlineBasemapCell.AccessoryView = _useOnlineBasemapSwitch;

                    _useOnlineBasemapSwitch.On = AppSettings.CurrentSettings.UseOnlineBasemap;
                    return useOnlineBasemapCell;
            }
            throw new ArgumentOutOfRangeException(nameof(indexPath));
        }

        public override nint RowsInSection(UITableView tableview, nint section) => 4;

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath) => TableRowSelected?.Invoke(this, indexPath);

        private void UseOnlineBasemapSwitch_Toggled(object sender, EventArgs e)
        {
            AppSettings.CurrentSettings.UseOnlineBasemap = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        private void EnableRoutingSwitch_Toggled(object sender, EventArgs e)
        {
            AppSettings.CurrentSettings.IsRoutingEnabled = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        private void EnableLocationSwitch_Toggled(object sender, EventArgs e)
        {
            AppSettings.CurrentSettings.IsLocationServicesEnabled = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        /// <summary>
        /// Occurs when table row selected.
        /// </summary>
        public event EventHandler<NSIndexPath> TableRowSelected;
    }
}
