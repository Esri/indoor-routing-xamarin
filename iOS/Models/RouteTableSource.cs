using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.Data;
using Foundation;
using UIKit;

namespace IndoorRouting.iOS
{
    public class RouteTableSource : UITableViewSource
    {
        /// <summary>
        /// The items in the table.
        /// </summary>
        private readonly IEnumerable<Feature> items;

        /// <summary>
        /// The cell identifier.
        /// </summary>
        private readonly string startCellIdentifier;
        private readonly string endCellIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.FloorsTableSource"/> class.
        /// </summary>
        /// <param name="items">Table Items.</param>
        internal RouteTableSource(List<Feature> items)
        {
            if (items != null)
            {
                this.items = items;
                this.startCellIdentifier = "startCellID";
                this.endCellIdentifier = "endCellID";
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
            UITableViewCell cell;
            if (indexPath.Row % 2 == 1)
            {
                cell = tableView.DequeueReusableCell(this.endCellIdentifier);
            }
            else
            {
                cell = tableView.DequeueReusableCell(this.startCellIdentifier);
            }
            try
            {
                if (this.items.ElementAt(indexPath.Row) != null)
                {
                    var item = this.items.ElementAt(indexPath.Row);
                    cell.TextLabel.Text = item.Attributes[AppSettings.CurrentSettings.LocatorFields[0]].ToString();
                    cell.DetailTextLabel.Text = string.Format("Floor {0}", item.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName]);

                    return cell;
                }
                else if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
                {
                    cell.TextLabel.Text = "Current Location";
                    return cell;
                }
                else
                {
                    cell.TextLabel.Text = "Unknown Location";
                    return cell;
                }
            }
            catch
            {
                cell.TextLabel.Text = "Unknown Location";
                return cell;
            }
        }
    } 
}