using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using Foundation;
using UIKit;

namespace IndoorNavigation.iOS
{
	/// <summary>>
	/// Class handling the source data for the autosuggestions TableView 
	/// </summary>
	class AutosuggestionsTableSource : UITableViewSource
	{
		readonly IEnumerable<SuggestResult> _items;
		readonly string _cellIdentifier;

		internal AutosuggestionsTableSource(IEnumerable<SuggestResult> items)
		{
			if (items != null)
			{
				_items = items;
				_cellIdentifier = "cell_id";
			}
			else
			{
				//TODO: throw null exception
			}
		}

		/// <summary>
		/// Called by the TableView to determine how many cells to create for that particular section.
		/// </summary>
		public override nint RowsInSection(UITableView tableview, nint section)
		{
			try
			{
				return _items.Count();
			}
			catch
			{
				return 0;
			}
		}

		/// <summary>
		/// Called by the TableView to get the actual UITableViewCell to render for the particular row
		/// </summary>
		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			var cell = tableView.DequeueReusableCell(_cellIdentifier);

			//---- if there are no cells to reuse, create a new one
			if (cell == null)
				{ cell = new UITableViewCell(UITableViewCellStyle.Default, _cellIdentifier); }

			try
			{
				var item = _items.ElementAt(indexPath.Row);

				cell.TextLabel.Text = item.Label;

				return cell;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// When user selects location from list of autosuggestions, handle selection
		/// </summary>
		/// <param name="tableView">Table view.</param>
		/// <param name="indexPath">Index path.</param>
		public override async void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			OnTableRowSelected(indexPath);
		}

		public event EventHandler<TableRowSelectedEventArgs<SuggestResult>> TableRowSelected;


		void OnTableRowSelected(NSIndexPath itemIndexPath)
		{
			try
			{
				var item = _items.ElementAt(itemIndexPath.Row);
				TableRowSelected?.Invoke(this, new TableRowSelectedEventArgs<SuggestResult>(item, itemIndexPath));
			}
			catch
			{
				//TODO: figure out how to trigger this catch and what happens
			}
		}
	}
}
