// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// https://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models
{
    /// <summary>
    /// Class handling the source data for the floors TableView 
    /// </summary>
    internal class FloorsTableSource : UITableViewSource
    {
        /// <summary>
        /// The cell identifier.
        /// </summary>
        private const string CellIdentifier = "floorTableCell";

        private readonly MapViewModel _viewModel;

        /// <summary>
        /// Creates a UITableViewSource for the floor picker
        /// </summary>
        /// <param name="viewmodel"></param>
        public FloorsTableSource(MapViewModel viewmodel) => _viewModel = viewmodel;

        /// <summary>
        /// Called by the TableView to determine how many cells to create for that particular section.
        /// </summary>
        /// <returns>The rows in section.</returns>
        /// <param name="tableview">Containing Tableview.</param>
        /// <param name="section">Specific Section.</param>
        public override nint RowsInSection(UITableView tableview, nint section) => _viewModel?.CurrentVisibleFloors?.Count() ?? 0;

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(CellIdentifier);

            // If there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier)
                {
                    BackgroundColor = UIColor.Clear,
                    SelectedBackgroundView = new UIView {BackgroundColor = ApplicationTheme.SelectionBackgroundColor}
                };
                cell.TextLabel.HighlightedTextColor = ApplicationTheme.SelectionForegroundColor;
                cell.TextLabel.TextAlignment = UITextAlignment.Center;

                // show separator full width
                cell.SeparatorInset = UIEdgeInsets.Zero;
                cell.LayoutMargins = UIEdgeInsets.Zero;
                cell.PreservesSuperviewLayoutMargins = false;
            }

            try
            {
                cell.TextLabel.Text = _viewModel.CurrentVisibleFloors.ElementAt(indexPath.Row);
                cell.AccessibilityLabel = "FloorPickerFloorAccessibilityPrefix".Localize() + cell.TextLabel.Text;
                cell.AccessibilityHint = "FloorPickerFloorAccessibilityHint".Localize();

                return cell;
            }
            catch(Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Forward row selection to the viewmodel
        /// </summary>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath) => _viewModel.SelectFloor(_viewModel?.CurrentVisibleFloors?.ElementAt(indexPath.Row));
    }
}
