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
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models
{
    /// <summary>
    /// Route table source.
    /// </summary>
    public class RouteTableSource : UITableViewSource
    {
        /// <summary>
        /// The items in the table.
        /// </summary>
        private readonly IEnumerable<Feature> _items;

        /// <summary>
        /// The cell identifier for the start cell.
        /// </summary>
        private readonly string _startCellIdentifier;

        /// <summary>
        /// The end cell identifier for the end cell.
        /// </summary>
        private readonly string _endCellIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models.RouteTableSource"/> class.
        /// </summary>
        /// <param name="items">Table Items.</param>
        internal RouteTableSource(List<Feature> items)
        {
            if (items != null)
            {
                _items = items;
                _startCellIdentifier = "startCellID";
                _endCellIdentifier = "endCellID";
            }
        }

        /// <summary>
        /// Called by the TableView to determine how many cells to create for that particular section.
        /// </summary>
        /// <returns>The rows in section.</returns>
        /// <param name="tableview">Containing Tableview.</param>
        /// <param name="section">Specific Section.</param>
        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return _items?.Count() ?? 0;
        }

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            // Used to create the 2 route card cells
            // Zero base index, even cell is the start location odd cell is the end location
            var cellIdentifier = indexPath.Row % 2 == 1 ? _endCellIdentifier : _startCellIdentifier;
            var cell = tableView.DequeueReusableCell(cellIdentifier);

            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Subtitle, cellIdentifier);
                string imageName = indexPath.Row % 2 == 1 ? "EndCircle" : "StartCircle";
                cell.ImageView.Image = UIImage.FromBundle(imageName);
                cell.BackgroundColor = tableView.BackgroundColor;
            }

            try
            {
                if (_items.ElementAt(indexPath.Row) != null)
                {
                    var item = _items.ElementAt(indexPath.Row);
                    cell.TextLabel.Text = item.Attributes[AppSettings.CurrentSettings.LocatorFields[0]].ToString();
                    cell.DetailTextLabel.Text = $"{"FloorLabel".Localize()} {item.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName]}";

                    return cell;
                }
                else if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
                {
                    cell.TextLabel.Text = "CurrentLocationLabel".Localize();
                    return cell;
                }
                else
                {
                    cell.TextLabel.Text = "UnknownLocationLabel".Localize();
                    return cell;
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                throw;
            }
        }
    } 
}