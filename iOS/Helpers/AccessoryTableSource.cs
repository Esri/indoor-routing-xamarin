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
    using System.Collections.Generic;
    using System.Linq;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Class handling the source data for the floors TableView 
    /// </summary>
    internal class AccessoryTableSource : UITableViewSource
    {
        /// <summary>
        /// The items in the table.
        /// </summary>
        private readonly IEnumerable<UIImage> items;

        /// <summary>
        /// The cell identifier.
        /// </summary>
        private readonly string cellIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.FloorsTableSource"/> class.
        /// </summary>
        /// <param name="items">Table Items.</param>
        internal AccessoryTableSource(IEnumerable<UIImage> items)
        {
            if (items != null)
            {
                this.items = items;
                this.cellIdentifier = "cell_id";
            }
        }

        /// <summary>
        /// Occurs when table row selected.
        /// </summary>
        public event EventHandler<NSIndexPath> TableRowSelected;

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
                return this.items.Count();
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
            var cell = tableView.DequeueReusableCell(this.cellIdentifier) as AccessoryViewCell;

            // If there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new AccessoryViewCell();
            }

            try
            {
                var item = this.items.ElementAt(indexPath.Row);
                cell.SetImage(item);

                // show separator full width
                
                cell.LayoutMargins = UIEdgeInsets.Zero;
                cell.PreservesSuperviewLayoutMargins = false;

                cell.SeparatorInset = UIEdgeInsets.Zero;

                return cell;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Event for user selecting a floor level
        /// </summary>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            this.OnTableRowSelected(indexPath);
            tableView.DeselectRow(indexPath, false);
        }

        /// <summary>
        /// Get the tableview item the user selected and call event handler
        /// </summary>
        /// <param name="itemIndexPath">Item index path.</param>
        private void OnTableRowSelected(NSIndexPath itemIndexPath)
        {
            try
            {
                var item = this.items.ElementAt(itemIndexPath.Row);
                this.TableRowSelected?.Invoke(this, itemIndexPath);
            }
            catch 
            { 
            }
        }
    }
}
