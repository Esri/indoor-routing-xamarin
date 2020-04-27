using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models
{
    internal class SettingsTableSource : UITableViewSource
    {
        // Cell reuse identifiers
        private const string HomeLocationCellIdentifier = nameof(HomeLocationCellIdentifier);
        private const string LocationServicesCellIdentifier = nameof(LocationServicesCellIdentifier);
        private const string RoutingCellIdentifier = nameof(RoutingCellIdentifier);
        private const string UseOnlineBasemapCellIdentifier = nameof(UseOnlineBasemapCellIdentifier);

        private readonly MapViewModel _viewModel;

        private UISwitch _locationSwitch;
        private UISwitch _routingSwitch;
        private UISwitch _useOnlineBasemapSwitch;

        public SettingsTableSource(MapViewModel viewModel) => _viewModel = viewModel;

        /// <summary>
        /// Returns the setting cell for a particular row
        /// </summary>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            switch (indexPath.Row)
            {
                // Cell for the home location setting
                case 0:
                    var homeCell = tableView.DequeueReusableCell(HomeLocationCellIdentifier) ?? new UITableViewCell(UITableViewCellStyle.Value1, HomeLocationCellIdentifier);
                    homeCell.TextLabel.Text = "HomeLocationSettingLabel".Localize();
                    homeCell.DetailTextLabel.Text = AppSettings.CurrentSettings.HomeLocation;
                    homeCell.Accessory = UITableViewCellAccessory.DisclosureIndicator;
                    homeCell.BackgroundColor = tableView.BackgroundColor;
                    return homeCell;
                // Cell for the location services setting
                case 1:
                    var locationCell = tableView.DequeueReusableCell(LocationServicesCellIdentifier) ?? new UITableViewCell(UITableViewCellStyle.Default, LocationServicesCellIdentifier);
                    locationCell.TextLabel.Text = "UseLocationServicesSettingLabel".Localize();
                    locationCell.BackgroundColor = tableView.BackgroundColor;

                    if (_locationSwitch == null)
                    {
                        _locationSwitch = new UISwitch();
                        _locationSwitch.ValueChanged += EnableLocationSwitch_Toggled;
                    }
                    locationCell.AccessoryView = _locationSwitch;

                    _locationSwitch.On = AppSettings.CurrentSettings.IsLocationServicesEnabled;
                    return locationCell;
                // Cell for the routing setting
                case 2:
                    var routingCell = tableView.DequeueReusableCell(RoutingCellIdentifier) ?? new UITableViewCell(UITableViewCellStyle.Default, RoutingCellIdentifier);
                    routingCell.TextLabel.Text = "EnableRoutingSettingLabel".Localize();
                    routingCell.BackgroundColor = tableView.BackgroundColor;

                    if (_routingSwitch == null)
                    {
                        _routingSwitch = new UISwitch();
                        _routingSwitch.ValueChanged += EnableRoutingSwitch_Toggled;
                    }
                    routingCell.AccessoryView = _routingSwitch;

                    _routingSwitch.Enabled = _viewModel?.Map?.TransportationNetworks?.Any() ?? false;
                    _routingSwitch.On = AppSettings.CurrentSettings.IsRoutingEnabled;
                    return routingCell;
                // Cell for the basemap setting
                case 3:
                    var useOnlineBasemapCell = tableView.DequeueReusableCell(UseOnlineBasemapCellIdentifier) ?? new UITableViewCell(UITableViewCellStyle.Default, UseOnlineBasemapCellIdentifier);
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

        /// <summary>
        /// The setting page only has 4 settings (rows).
        /// </summary>
        public override nint RowsInSection(UITableView tableview, nint section) => 4;

        /// <summary>
        /// Notifies observers that a row was selected
        /// </summary>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath) => TableRowSelected?.Invoke(this, indexPath);

        /// <summary>
        /// Saves the new online basemap usage setting
        /// </summary>
        private void UseOnlineBasemapSwitch_Toggled(object sender, EventArgs e)
        {
            // Update the setting then immediately save it in the background
            AppSettings.CurrentSettings.UseOnlineBasemap = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        /// <summary>
        /// Saves the new routing setting
        /// </summary>
        private void EnableRoutingSwitch_Toggled(object sender, EventArgs e)
        {
            // Update the setting then immediately save it in the background
            AppSettings.CurrentSettings.IsRoutingEnabled = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        /// <summary>
        /// Saves the new location services setting
        /// </summary>
        private void EnableLocationSwitch_Toggled(object sender, EventArgs e)
        {
            // Update the setting then immediately save it
            AppSettings.CurrentSettings.IsLocationServicesEnabled = ((UISwitch)sender).On;
            Task.Run(() => AppSettings.SaveSettings(Path.Combine(DownloadViewModel.GetDataFolder(), "AppSettings.xml")));
        }

        /// <summary>
        /// Occurs when table row selected.
        /// </summary>
        public event EventHandler<NSIndexPath> TableRowSelected;
    }
}