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
		SuggestResult[] TableItems;
		string CellIdentifier = "cell_id";

		internal AutosuggestionsTableSource(IReadOnlyList<SuggestResult> items)
		{
			TableItems = items.ToArray();
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
			if (cell == null)
			{ cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier); }


			cell.TextLabel.Text = item.Label;

			return cell;
		}

		/// <summary>
		/// When user selects location from list of autosuggestions, handle selection
		/// </summary>
		/// <param name="tableView">Table view.</param>
		/// <param name="indexPath">Index path.</param>
		public override async void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			var selectedLocation = this.TableItems[indexPath.Row];

			// Set the value of the textbox to the selected autosuggestion and dismiss keyboard
			var parentView = tableView.Superview;
			// Access the textbox by setting it's Tag value to "2" in the Main.Storyboard
			foreach (var subView in parentView.Subviews)
			{
				// Handle the autosuggest for the search bar
				if (subView.Tag == 2)
				{
					var searchBar = subView as UISearchBar;

					if (searchBar != null)
					{
						searchBar.Text = selectedLocation.Label;

					}
				}

			}


			// TODO: if the search was for a home location, assign home location
			AppSettings.currentSettings.HomeLocation = selectedLocation.Label;
			var homeLocation = await LocationViewModel.GetSearchedLocation(selectedLocation.Label);


			// Save extent of home location and floor level to Settings file
			CoordinatesKeyValuePair<string, double>[] homeCoordinates =
			{
				new CoordinatesKeyValuePair<string, double>("X", homeLocation.DisplayLocation.X),
				new CoordinatesKeyValuePair<string, double>("Y", homeLocation.DisplayLocation.Y),
				new CoordinatesKeyValuePair<string, double>("WKID", homeLocation.DisplayLocation.SpatialReference.Wkid),
				new CoordinatesKeyValuePair<string, double>("Floor", homeLocation.DisplayLocation.X),
			};

			AppSettings.currentSettings.HomeCoordinates = homeCoordinates;

			// Save user settings
			var settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			await Task.Run(() => AppSettings.SaveSettings(Path.Combine(settingsPath, "AppSettings.xml")));

		}
	}
}
