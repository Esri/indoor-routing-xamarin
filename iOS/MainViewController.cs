using System;
using System.Linq;
using System.Threading.Tasks;
using CoreGraphics;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using UIKit;

namespace IndoorNavigation.iOS
{
    public partial class MainViewController : UIViewController
    {
        MainViewController(IntPtr handle) : base(handle)
		{
		}

		string _selectedFloor = "";

		/// <summary>
		/// Overrides the controller behavior before view is about to appear
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public override void ViewWillAppear(bool animated)
		{
			// Hide the navigation bar on the main screen 
			NavigationController.NavigationBarHidden = true;
			base.ViewWillAppear(animated);

			// Hide Home button if user doesn't have home location set
			if (AppSettings.currentSettings.HomeLocation != "Set home location")
				HomeButton.Enabled = true;

			// Hide Current Location button if location services is disabled
			if (!AppSettings.currentSettings.IsLocationServicesEnabled)
				CurrentLocationButton.Enabled = false;
		}

		/// <summary>
		/// Override default behavior when subviews are loaded
		/// </summary>
		public async override void ViewDidLayoutSubviews()
		{
			// If map is zoomed in past threshold, call to display the floors data
			if (MapView.Map != null && MapView.MapScale <= AppSettings.currentSettings.ZoomLevelToDisplayRoomLayers)
				await DisplayFloorLevels();
		}

		/// <summary>
		/// Overrides default behavior when view has loaded. 
		/// </summary>
		public async override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Get Mobile Map Package from the location on device
			var mmpk = await MobileMapPackage.OpenAsync(DownloadController.targetFilename);
			LocationViewModel.mmpk = mmpk;

			// Display map from the mmpk. Assumption is made that the mmpk has only one map
			var map = mmpk.Maps[0];
			await map.LoadAsync();

			// Set viewpoint of the map depending on user's settings
			MapViewModel.SetInitialViewPoint(map);

			// Add the map to the MapView to be displayed
			MapView.Map = map;

			// Remove the "Powered by Esri" logo at the bottom
			MapView.IsAttributionTextVisible = false;

			// Handle the user moving the map 
			MapView.NavigationCompleted += MapView_NavigationCompleted;

			// Show the floor picker if the map is zoomed to user's home location
			if (MapView.Map != null && AppSettings.currentSettings.HomeLocation != "Set home location")
				await DisplayFloorLevels();

			// Handle text changing in the search bar
			LocationSearchBar.TextChanged += async (sender, e) =>
			{
				// Call to populate autosuggestions 
				await RetrieveSuggestionsFromLocator();
			};
		}

		/// <summary>
		/// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
		/// </summary>
		async Task RetrieveSuggestionsFromLocator()
		{
			var suggestions = await LocationViewModel.GetLocationSuggestions(LocationSearchBar.Text);
			if (suggestions == null || suggestions.Count == 0)
			{
				AutosuggestionsTableView.Hidden = true;
			}
			if (suggestions.Count > 0)
			{
				// Show the tableview with autosuggestions and populate it
				AutosuggestionsTableView.Hidden = false;
				AutosuggestionsTableView.Source = new AutosuggestionsTableSource(suggestions);

				AutosuggestionsTableView.ReloadData();

				// Auto extend ot shrink the tableview based on the content inside
				var frame = AutosuggestionsTableView.Frame;
				frame.Height = AutosuggestionsTableView.ContentSize.Height;
				AutosuggestionsTableView.Frame = frame;
			}
		}

		/// <summary>
		/// Handle user navigating around the map
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		async void MapView_NavigationCompleted(object sender, EventArgs e)
		{
			// Display floors and level if user is zoomed in 
			if (MapView.MapScale <= AppSettings.currentSettings.ZoomLevelToDisplayRoomLayers)
				await DisplayFloorLevels();
			// If user is zoomed out, only show the base layer
			else
			{
				FloorsTableView.Hidden = true;
				MapViewModel.TurnLayersOnOff(false, MapView.Map, _selectedFloor);
			}
		}

		/// <summary>
		/// Display the floor levels based on which building the current viewpoint is over
		/// </summary>
		/// <returns>The floor levels.</returns>
		async Task DisplayFloorLevels()
		{
			if (MapView.Map.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
			{
				string[] tableItems = await MapViewModel.GetFloorsInVisibleArea(MapView);

				// Only show the floors tableview if the buildings in view have more than one floor
				if (tableItems.Count() > 1)
				{
					// Show the tableview and populate it
					FloorsTableView.Hidden = false;
					FloorsTableView.Source = new FloorsTableSource(tableItems, this);
					InvokeOnMainThread(() => FloorsTableView.ReloadData());

					// Auto extend ot shrink the tableview based on the content inside
					var frame = FloorsTableView.Frame;
					frame.Height = FloorsTableView.ContentSize.Height;
					FloorsTableView.Frame = frame;
				}
				else
				{
					FloorsTableView.Hidden = true;
					_selectedFloor = "";
				}
				// Turn layers on. If there is no floor selected, first floor will be displayed by default
				MapViewModel.TurnLayersOnOff(true, MapView.Map, _selectedFloor);
			}
		}

		/// <summary>
		/// When a floor is selected by the user, set global variable to the selected floor and set definition query on the feature layers
		/// </summary>
		/// <param name="_selectedFloor">Selected floor.</param>
		internal void HandleSelectedFloor(string selectedFloor)
		{
			_selectedFloor = selectedFloor;
			MapViewModel.TurnLayersOnOff(true, MapView.Map, _selectedFloor); ;
		}

		/// <summary>
		/// When user taps on the home button, zoom them to the home location
		/// </summary>
		/// <param name="sender">Home button</param>
		async partial void Home_TouchUpInside(UIButton sender)
		{
			var viewPoint = await MapViewModel.MoveToHomeLocation(MapView.Map);
			MapView.SetViewpoint(viewPoint);
		}
	}
}
