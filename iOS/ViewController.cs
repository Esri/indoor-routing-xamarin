using System;
using System.IO;
using System.Linq;
using CoreGraphics;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using UIKit;

namespace IndoorNavigation.iOS
{
	public partial class ViewController : UIViewController
	{
		string selectedFloor = "";
		public ViewController(IntPtr handle) : base(handle)
		{
		}

		public override void ViewWillAppear(bool animated)
		{
			this.NavigationController.NavigationBarHidden = true;
			base.ViewWillAppear(animated);
			// Hide the navigation bar on the main screen 

		}

		public async override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Get Mobile Map Package from the location on device
			var mmpk = await MobileMapPackage.OpenAsync(DownloadController.targetFilename);

			LocationHelper.mmpk = mmpk;

			// Display map from the mmpk. Assumption is made that the mmpk has only one map
			Map map = mmpk.Maps[0];
			await map.LoadAsync();

			// Set initial viewpoint of the map depending on user's settings
			var mapHelper = new MapHelper();
			mapHelper.SetInitialViewPoint(map);

			// Add the map to the MapView to be displayed
			MapView.Map = map;

			// Remove the "Powered by Esri" logo at the bottom
			MapView.IsAttributionTextVisible = false;

			// Handle the user moving the map 
			MapView.NavigationCompleted += MapView_NavigationCompleted;
		}

		async void MapView_NavigationCompleted(object sender, EventArgs e)
		{
			MapHelper mapHelper = new MapHelper();

			if (MapView.MapScale <= 500)
			{
				string [] tableItems = await mapHelper.GetFloorsInVisibleArea(MapView);

				// Only show the floors tableview if the buildings in view have more than one floor
				if (tableItems.Count() > 1)
				{
					// Show the tableview and populate it
					FloorsTableView.Hidden = false;
					FloorsTableView.Source = new FloorsTableSource(tableItems, this);
					InvokeOnMainThread(() => FloorsTableView.ReloadData());

					// Auto extend ot shrink the tableview based on the content inside
					CGRect frame = FloorsTableView.Frame;
					frame.Height = FloorsTableView.ContentSize.Height;
					FloorsTableView.Frame = frame;

				}
				else
				{
					FloorsTableView.Hidden = true;
					selectedFloor = "";
				}
				// Turn layers on. If there is no floor selected, first floor will be displayed by default
				mapHelper.TurnLayersOnOff(true, MapView.Map, selectedFloor);
			}
			// If user is zoomed out, only show the base layer
			else
			{
				FloorsTableView.Hidden = true;
				mapHelper.TurnLayersOnOff(false, MapView.Map, selectedFloor);
			}


		}

		// When a floor is selected by the user, set global variable to the selected floor and set definition query on the feature layers
		public void HandleSelectedFloor(string _selectedFloor)
		{
			this.selectedFloor = _selectedFloor;
			MapHelper mapHelper = new MapHelper();
			mapHelper.TurnLayersOnOff(true, MapView.Map, selectedFloor);;
		}
	}

	
	/// <summary>
	/// Class handling the source data for the floors TableView 
	/// </summary>
	public class FloorsTableSource : UITableViewSource
	{

		string[] TableItems;
		string CellIdentifier = "cell_id";

		ViewController owner;

		public FloorsTableSource(string[] items, ViewController _owner)
		{
			TableItems = items;
			this.owner = _owner;
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
			UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
			string item = TableItems[indexPath.Row];

			//---- if there are no cells to reuse, create a new one
			//if (cell == null)
			//{ cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier); }
			UILabel label = (UILabel)cell.ContentView.ViewWithTag(10);
			label.Text = item;



			return cell;
		}

		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			var selectedFloor = this.TableItems[indexPath.Row];
			owner.HandleSelectedFloor(selectedFloor);
		}


	}
}
