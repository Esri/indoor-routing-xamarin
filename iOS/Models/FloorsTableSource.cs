// <copyright file="FloorsTableSource.cs" company="Esri, Inc">
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
    using System.Linq;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Class handling the source data for the floors TableView 
    /// </summary>
    internal class FloorsTableSource : UITableViewSource
    {
        /// <summary>
        /// The cell identifier.
        /// </summary>
        private readonly string cellIdentifier = "floortablecell";

        private MapViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.FloorsTableSource"/> class.
        /// </summary>
        /// <param name="items">Table Items.</param>
        internal FloorsTableSource(MapViewModel viewmodel) : base()
        {
            _viewModel = viewmodel;
        }

        /// <summary>
        /// Called by the TableView to determine how many cells to create for that particular section.
        /// </summary>
        /// <returns>The rows in section.</returns>
        /// <param name="tableview">Containing Tableview.</param>
        /// <param name="section">Specific Section.</param>
        public override nint RowsInSection(UITableView tableview, nint section)
        {
            try
            {
                return _viewModel?.CurrentVisibleFloors?.Count() ?? 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(this.cellIdentifier);

            // If there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Default, this.cellIdentifier);
                cell.BackgroundColor = UIColor.Clear;
                cell.SelectedBackgroundView = new UIView();
                cell.SelectedBackgroundView.BackgroundColor = ApplicationTheme.SelectionBackgroundColor;
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
                cell.AccessibilityLabel = "Floor: ".AsLocalized() + cell.TextLabel.Text;
                cell.AccessibilityHint = "Select to show this floor".AsLocalized();

                return cell;
            }
            catch(Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                throw;
            }
        }

        /// <summary>
        /// Event for user selecting a floor level
        /// </summary>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            _viewModel.SelectFloor(_viewModel?.CurrentVisibleFloors?.ElementAt(indexPath.Row));
        }
    }
}
