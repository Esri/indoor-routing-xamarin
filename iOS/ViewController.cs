using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Mapping.Popups;
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
			// Hide the navigation bar on the main screen 
			this.NavigationController.NavigationBarHidden = true;
			base.ViewWillAppear(animated);

			// Hide Home button if user doesn't have home location set
			if (GlobalSettings.currentSettings.HomeLocation != "Set home location")
				HomeButton.Enabled = true;
		}

		public async override void ViewDidLayoutSubviews()
		{
			if (MapView.Map != null && MapView.MapScale <= 500)
				await DisplayFloorLevels();
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

			// Set viewpoint of the map depending on user's settings
			MapHelper.SetInitialViewPoint(map);

			// Add the map to the MapView to be displayed
			MapView.Map = map;

			// Remove the "Powered by Esri" logo at the bottom
			MapView.IsAttributionTextVisible = false;

			// Handle the user moving the map 
			MapView.NavigationCompleted += MapView_NavigationCompleted;

			// Show the floor picker if the map is zoomed to user's home location
			if (MapView.Map != null && GlobalSettings.currentSettings.HomeLocation != "Set home location")
				await DisplayFloorLevels();
		}


		async void MapView_NavigationCompleted(object sender, EventArgs e)
		{
			// Display floors and level if user is zoomed in 
			if (MapView.MapScale <= 500)
				await DisplayFloorLevels();
			// If user is zoomed out, only show the base layer
			else
			{
				FloorsTableView.Hidden = true;
				MapHelper.TurnLayersOnOff(false, MapView.Map, selectedFloor);
			}
		}

		public async Task DisplayFloorLevels()
		{
			if (MapView.Map.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded )
			{
				string[] tableItems = await MapHelper.GetFloorsInVisibleArea(MapView);

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
				MapHelper.TurnLayersOnOff(true, MapView.Map, selectedFloor);
			}
		}

		// When a floor is selected by the user, set global variable to the selected floor and set definition query on the feature layers
		public void HandleSelectedFloor(string _selectedFloor)
		{
			this.selectedFloor = _selectedFloor;
			MapHelper.TurnLayersOnOff(true, MapView.Map, selectedFloor);;
		}

		/// <summary>
		/// When user taps on the home button, zoom them to the home location
		/// </summary>
		/// <param name="sender">Home button</param>
		async partial void Home_TouchUpInside(UIButton sender)
		{
			var viewPoint = await MapHelper.MoveToHomeLocation(MapView.Map);
			MapView.SetViewpoint(viewPoint);
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
