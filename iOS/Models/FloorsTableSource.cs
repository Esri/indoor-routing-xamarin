using System;
using System.Collections.Generic;
using Foundation;
using UIKit;
using System.Linq;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Class handling the source data for the floors TableView 
	/// </summary>
	class FloorsTableSource : UITableViewSource
	{

		readonly IEnumerable<string> _items;
		readonly string _cellIdentifier;


		internal FloorsTableSource(IEnumerable<string> items)
		{
			_items = items;
			_cellIdentifier = "cell_id";
		}

		/// <summary>
		/// Called by the TableView to determine how many cells to create for that particular section.
		/// </summary>
		public override nint RowsInSection(UITableView tableview, nint section)
		{
			return _items.Count();
		}

		/// <summary>
		/// Called by the TableView to get the actual UITableViewCell to render for the particular row
		/// </summary>
		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			var cell = tableView.DequeueReusableCell(_cellIdentifier);
			//---- if there are no cells to reuse, create a new one
			if (cell == null)
				cell = new UITableViewCell(UITableViewCellStyle.Default, _cellIdentifier);

			var item = _items.ElementAt(indexPath.Row);

			var label = (UILabel)cell.ContentView.ViewWithTag(10);
			label.Text = item;


			return cell;
		}

		/// <summary>
		/// Event for user selecting a floor level
		/// </summary>
		/// <param name="tableView">Table view.</param>
		/// <param name="indexPath">Index path.</param>
		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			OnTableRowSelected(indexPath);
		}

		public event EventHandler<TableRowSelectedEventArgs<string>> TableRowSelected;

		/// <summary>
		/// Get the tableview item the user selected and call event handler
		/// </summary>
		/// <param name="itemIndexPath">Item index path.</param>
		void OnTableRowSelected(NSIndexPath itemIndexPath)
		{
			var item = _items.ElementAt(itemIndexPath.Row);
			TableRowSelected?.Invoke(this, new TableRowSelectedEventArgs<string>(item, itemIndexPath));
		}
	}
}
