using System;
using Foundation;
using UIKit;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Class handling the source data for the floors TableView 
	/// </summary>
	class FloorsTableSource : UITableViewSource
	{

		string[] TableItems;
		string CellIdentifier = "cell_id";

		MainViewController _owner;

		internal FloorsTableSource(string[] items, MainViewController owner)
		{
			TableItems = items;
			this._owner = owner;
		}

		/// <summary>
		/// Called by the TableView to determine how many cells to create for that particular section.
		/// </summary>
		public override nint RowsInSection(UITableView tableview, nint section)
		{
			return TableItems.Length;
		}

		/// <summary>
		/// Called by the TableView to get the actual UITableViewCell to render for the particular row
		/// </summary>
		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			var cell = tableView.DequeueReusableCell(CellIdentifier);
			var item = TableItems[indexPath.Row];

			//---- if there are no cells to reuse, create a new one
			var label = (UILabel)cell.ContentView.ViewWithTag(10);
			label.Text = item;



			return cell;
		}

		/// <summary>
		/// Handle user selecting a floor level
		/// </summary>
		/// <param name="tableView">Table view.</param>
		/// <param name="indexPath">Index path.</param>
		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			var selectedFloor = this.TableItems[indexPath.Row];
			_owner.HandleSelectedFloor(selectedFloor);
		}
	}
}
