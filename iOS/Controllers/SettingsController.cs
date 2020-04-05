// <copyright file="SettingsController.cs" company="Esri, Inc">
//      Copyright 2017 Esri.
//
//      Licensed under the Apache License, Version 2.0 (the "License");
//      you may not use this file except in compliance with the License.
//      You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
// </copyright>
// <author>Mara Stoica</author>
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Controller handles the UI and logic for the Settings screen
    /// </summary>
    
    internal class SettingsController : UITableViewController
    {
        UITableView _SettingsTableView;
        UIBarButtonItem _closeButton;
        SettingsTableSource _tableSource;

        public SettingsController()
        {
            Title = "Settings";
        }

        public override void LoadView()
        {
            base.LoadView();

            View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            _SettingsTableView = new UITableView { TranslatesAutoresizingMaskIntoConstraints = false };
            _SettingsTableView.BackgroundColor = UIColor.Clear;
            _tableSource = new SettingsTableSource();
            _SettingsTableView.Source = _tableSource;

            View.AddSubview(_SettingsTableView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _SettingsTableView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _SettingsTableView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _SettingsTableView.TopAnchor.ConstraintEqualTo(View.SafeAreaLayoutGuide.TopAnchor),
                _SettingsTableView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor)
            });

            _closeButton = new UIBarButtonItem("Close", UIBarButtonItemStyle.Plain, null);
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

            _closeButton.Clicked += _closeButton_Clicked;

            _tableSource.TableRowSelected += _tableSource_TableRowSelected;

            base.ViewWillAppear(animated);
        }

        private void _tableSource_TableRowSelected(object sender, NSIndexPath e)
        {
            if (e.Row == 0)
            {
                NavigationController.PushViewController(new HomeLocationController(), true);
            }

            _SettingsTableView.DeselectRow(e, true);
        }

        public override void ViewDidDisappear(bool animated)
        {
            _closeButton.Clicked -= _closeButton_Clicked;
            _tableSource.TableRowSelected -= _tableSource_TableRowSelected;

            base.ViewDidDisappear(animated);
        }

        private void _closeButton_Clicked(object sender, EventArgs e)
        {
            this.DismissModalViewController(true);
        }
    }

    class SettingsTableSource : UITableViewSource
    {
        private UISwitch _locationSwitch;
        private UISwitch _routingSwitch;

        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            switch (indexPath.Row)
            {
                case 0:
                    var homeCell = tableView.DequeueReusableCell("HomeLocationCell") ?? new UITableViewCell(UITableViewCellStyle.Value1, "HomeLocationCell");
                    homeCell.TextLabel.Text = "Home Location";
                    homeCell.DetailTextLabel.Text = AppSettings.CurrentSettings.HomeLocation;
                    homeCell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                    homeCell.BackgroundColor = tableView.BackgroundColor;
                    return homeCell;
                case 1:
                    var locationCell = tableView.DequeueReusableCell("LocationServicesCell") ?? new UITableViewCell(UITableViewCellStyle.Default, "LocationServicesCell");
                    locationCell.TextLabel.Text = "Use Location Services";
                    locationCell.BackgroundColor = tableView.BackgroundColor;

                    if (_locationSwitch == null)
                    {
                        _locationSwitch = new UISwitch();
                        _locationSwitch.ValueChanged += _locationSwitch_ValueChanged;
                    }
                    locationCell.AccessoryView = _locationSwitch;

                    _locationSwitch.On  = AppSettings.CurrentSettings.IsLocationServicesEnabled;
                    return locationCell;
                case 2:
                    var routingCell = tableView.DequeueReusableCell("RoutingCell") ?? new UITableViewCell(UITableViewCellStyle.Default, "RoutingCell");
                    routingCell.TextLabel.Text = "Enable Routing";
                    routingCell.BackgroundColor = tableView.BackgroundColor;

                    if (_routingSwitch == null)
                    {
                        _routingSwitch = new UISwitch();
                        _routingSwitch.ValueChanged += _routingSwitch_ValueChanged;
                    }
                    routingCell.AccessoryView = _routingSwitch;

                    _routingSwitch.Enabled = LocationViewModel.Instance.Map.TransportationNetworks.Any();
                        _routingSwitch.On = AppSettings.CurrentSettings.IsRoutingEnabled;
                    return routingCell;
            }
            throw new ArgumentOutOfRangeException(nameof(indexPath));
        }

        private void _routingSwitch_ValueChanged(object sender, EventArgs e)
        {
            AppSettings.CurrentSettings.IsRoutingEnabled = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        private void _locationSwitch_ValueChanged(object sender, EventArgs e)
        {
            AppSettings.CurrentSettings.IsLocationServicesEnabled = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return 3;
        }

        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            TableRowSelected?.Invoke(this, indexPath);
        }

        /// <summary>
        /// Occurs when table row selected.
        /// </summary>
        public event EventHandler<NSIndexPath> TableRowSelected;
    }
}
