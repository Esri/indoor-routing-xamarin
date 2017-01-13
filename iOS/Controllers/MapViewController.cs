using System;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using Foundation;
using UIKit;
using System.IO;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Tasks.Geocoding;

namespace IndoorNavigation.iOS
{
    public partial class MapViewController: UIViewController
    {

        MapViewController(IntPtr handle) : base(handle)
		{
			
		}

		string _selectedFloor = "";
		RouteResult _route;
		public RouteResult Route
		{
			get { return _route; }
			set
			{
				if (_route != value && value != null)
				{
					_route = value;
					OnRouteChangedAsync();
				}
			}
		}

		public async Task OnRouteChangedAsync()
		{
			if (Route != null)
			{
				// get the route from the results
				var route = Route.Routes.FirstOrDefault();

				// create a picture marker symbol for start pin
				var startPin = ImageToByteArray(UIImage.FromBundle("StartPin"));
				var startMarker = new PictureMarkerSymbol(new RuntimeImage(startPin));

				// create a picture marker symbol for end pin
				var endPin = ImageToByteArray(UIImage.FromBundle("EndPin"));
				var endMarker = new PictureMarkerSymbol(new RuntimeImage(endPin));

				if (route != null)
				{
					// Create graphics
					var startGraphic = new Graphic(route.RouteGeometry.Parts.First().Points.First(), startMarker);
					var endGraphic = new Graphic(route.RouteGeometry.Parts.Last().Points.Last(), endMarker);

					// create a graphic (with a dashed line symbol) to represent the routee
					var routeSymbol = new SimpleLineSymbol();
					routeSymbol.Width = 5;
					routeSymbol.Style = SimpleLineSymbolStyle.Dash;
					routeSymbol.Color = System.Drawing.Color.DarkRed;


					var routeGraphic = new Graphic(route.RouteGeometry, routeSymbol);

					MapView.GraphicsOverlays[0].Graphics.Add(routeGraphic);
					MapView.GraphicsOverlays[0].Graphics.Add(startGraphic);
					MapView.GraphicsOverlays[0].Graphics.Add(endGraphic);
					await MapView.SetViewpointGeometryAsync(route.RouteGeometry, 30);
				}
			}
		}

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
			if (AppSettings.CurrentSettings.HomeLocation != MapViewModel.defaultHomeLocationText)
				HomeButton.Enabled = true;

			// Hide Current Location button if location services is disabled
			if (!AppSettings.CurrentSettings.IsLocationServicesEnabled)
				CurrentLocationButton.Enabled = false;
		}

		/// <summary>
		/// Override default behavior when subviews are loaded
		/// </summary>
		public async override void ViewDidLayoutSubviews()
		{
			// If map is zoomed in past threshold, call to display the floors data
			if (MapView.Map != null && MapView.MapScale <= AppSettings.CurrentSettings.RoomsLayerMinimumZoomLevel)
				await DisplayFloorLevelsAsync();
		}

		/// <summary>
		/// Overrides default behavior when view has loaded. 
		/// </summary>
		public async override void ViewDidLoad()
		{
			base.ViewDidLoad();

			// Get Mobile Map Package from the location on device
			var mmpk = await MobileMapPackage.OpenAsync(Path.Combine(DownloadViewModel.GetDataFolder(), AppSettings.CurrentSettings.PortalItemName));
			LocationViewModel.mmpk = mmpk;

			// Display map from the mmpk. Assumption is made that the mmpk has only one map
			var map = mmpk.Maps[0];
			await map.LoadAsync();

			// Set viewpoint of the map depending on user's settings
			MapViewModel.SetInitialViewPointAsync(map);

			// Add the map to the MapView to be displayed
			MapView.Map = map;

			//// Add a graphics overlay to hold the pins and route graphics
			MapView.GraphicsOverlays.Add(new GraphicsOverlay());

			// Remove the "Powered by Esri" logo at the bottom
			MapView.IsAttributionTextVisible = false;

			// Handle the user moving the map 
			MapView.NavigationCompleted += MapView_NavigationCompleted;

			// Show the floor picker if the map is zoomed to user's home location
			if (MapView.Map != null && AppSettings.CurrentSettings.HomeLocation != MapViewModel.defaultHomeLocationText)
				await DisplayFloorLevelsAsync();

			// Handle text changing in the search bar
			LocationSearchBar.TextChanged += async (sender, e) =>
			{
				// Call to populate autosuggestions 
				await GetSuggestionsFromLocatorAsync();
			};

			LocationSearchBar.SearchButtonClicked += async (sender, e) =>
			{
				var searchText = ((UISearchBar)sender).Text;
				// Dismiss keyboard
				((UISearchBar)sender).EndEditing(true);

				// Dismiss autosuggestions table
				AutosuggestionsTableView.Hidden = true;
				await GetSearchedFeatureAsync(searchText);
			};


			// handle the tap event on the map view (or scene view)
			//TODO: Move this outside of the DidLoad event
			//TODO: Handle user tapping not on a room
			//TODO: Handle double taps
			MapView.GeoViewTapped += async (s, e) =>
			{
				if (LocationSearchBar.IsFirstResponder == true)
				{
					LocationSearchBar.ResignFirstResponder();
					ContactCardView.Hidden = true;
				}
				else
				{
					// get the tap location in screen units
					var tapScreenPoint = e.Position;

					var layer = MapView.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex];
					var pixelTolerance = 20;
					var returnPopupsOnly = false;
					var maxResults = 1;

					try
					{
						// identify a layer using MapView, passing in the layer, the tap point, tolerance, types to return, and max results
						IdentifyLayerResult idResults = await MapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResults);

						// get the floor number for the tapped feature
						var floorNumber = idResults.GeoElements.First().Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName];

						// create a picture marker symbol
						var mapPin = ImageToByteArray(UIImage.FromBundle("StartPin"));
						var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));

						// Create graphic
						var mapPinGraphic = new Graphic(GeometryEngine.LabelPoint(idResults.GeoElements.First().Geometry as Polygon), roomMarker);

						// Add pin to map
						var graphicsOverlay = MapView.GraphicsOverlays[0];
						graphicsOverlay.Graphics.Clear();
						graphicsOverlay.Graphics.Add(mapPinGraphic);


						await MapView.SetViewpointAsync(new Viewpoint(idResults.GeoElements.First().Geometry));

						// Get room attribute from the settings. First attribute should be set as the searcheable onee
						var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
						var roomNumber = idResults.GeoElements.First().Attributes[roomAttribute];
						NameLabel.Text = roomNumber.ToString();

						//TODO: Add additional fields to the contact card from the ContactCardDisplayFields
						ContactCardView.Hidden = false;
					}
					catch
					{
						MapView.GraphicsOverlays[0].Graphics.Clear();
						ContactCardView.Hidden = true;
					}
				}
			};
		}

		/// <summary>
		/// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
		/// </summary>
		async Task GetSuggestionsFromLocatorAsync()
		{
			var suggestions = await LocationViewModel.GetLocationSuggestionsAsync(LocationSearchBar.Text);
			if (suggestions == null || suggestions.Count == 0)
			{
				AutosuggestionsTableView.Hidden = true;
			}
			if (suggestions.Count > 0)
			{
				// Show the tableview with autosuggestions and populate it
				AutosuggestionsTableView.Hidden = false;
				var tableSource = new AutosuggestionsTableSource(suggestions);
				tableSource.TableRowSelected += AutosuggestionsTableSource_TableRowSelected;
				AutosuggestionsTableView.Source = tableSource;

				AutosuggestionsTableView.ReloadData();

				// Auto extend ot shrink the tableview based on the content inside
				var frame = AutosuggestionsTableView.Frame;
				frame.Height = AutosuggestionsTableView.ContentSize.Height;
				AutosuggestionsTableView.Frame = frame;
			}
		}

		/// <summary>
		/// Get the value selected in the Autosuggestions Table
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		async void AutosuggestionsTableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<SuggestResult> e)
		{
			var selectedItem = e.SelectedItem;
			LocationSearchBar.Text = selectedItem.Label;
			LocationSearchBar.ResignFirstResponder();
			AutosuggestionsTableView.Hidden = true;
			await GetSearchedFeatureAsync(selectedItem.Label);
		}

		/// <summary>
		/// Zooms to geocode result
		/// </summary>
		/// <param name="searchText">Search text entered by user.</param>
		async Task GetSearchedFeatureAsync(string searchText)
		{
			var geocodeResult = await LocationViewModel.GetSearchedLocationAsync(searchText);

			// create a picture marker symbol
			var mapPin = ImageToByteArray(UIImage.FromBundle("StartPin"));
			var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));

			// Create graphic
			var mapPinGraphic = new Graphic(geocodeResult.DisplayLocation, roomMarker);

			// Add pin to map
			var graphicsOverlay = MapView.GraphicsOverlays[0];
			graphicsOverlay.Graphics.Clear();
			graphicsOverlay.Graphics.Add(mapPinGraphic);


			await MapView.SetViewpointAsync(new Viewpoint(geocodeResult.DisplayLocation, 150));

			NameLabel.Text = searchText;
			ContactCardView.Hidden = false;


		}

		public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
		{
			base.PrepareForSegue(segue, sender);

			if (segue.Identifier == "RouteSegue")
			{
				var routeController = segue.DestinationViewController as RouteController;
				routeController.EndLocation = NameLabel.Text;
			}

		}

		byte[] ImageToByteArray(UIImage image)
		{
			using (NSData imageData = image.AsPNG())
			{
				var imageByteArray = new byte[imageData.Length];
				System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, imageByteArray, 0, Convert.ToInt32(imageData.Length));
				return imageByteArray;
			};
		}



		/// <summary>
		/// Handle user navigating around the map
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		async void MapView_NavigationCompleted(object sender, EventArgs e)
		{
			// Display floors and level if user is zoomed in 
			if (MapView.MapScale <= AppSettings.CurrentSettings.RoomsLayerMinimumZoomLevel)
				await DisplayFloorLevelsAsync();
			// If user is zoomed out, only show the base layer
			else
			{
				FloorsTableView.Hidden = true;
				MapViewModel.SetFloorVisibility(false, MapView.Map, _selectedFloor);
			}
		}

		/// <summary>
		/// Display the floor levels based on which building the current viewpoint is over
		/// </summary>
		/// <returns>The floor levels.</returns>
		async Task DisplayFloorLevelsAsync()
		{
			if (MapView.Map.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
			{
				try
				{
					string[] tableItems = await MapViewModel.GetFloorsInVisibleAreaAsync(MapView);

					// Only show the floors tableview if the buildings in view have more than one floor
					if (tableItems.Count() > 1)
					{
						// Show the tableview and populate it
						FloorsTableView.Hidden = false;
						var tableSource = new FloorsTableSource(tableItems);
						tableSource.TableRowSelected += FloorsTableSource_TableRowSelected;
						FloorsTableView.Source = tableSource;
						FloorsTableView.ReloadData();

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
				}
				catch
				{
					FloorsTableView.Hidden = true;
					_selectedFloor = "";
				}
				// Turn layers on. If there is no floor selected, first floor will be displayed by default
				MapViewModel.SetFloorVisibility(true, MapView.Map, _selectedFloor);
			}
		}

		///// <summary>
		///// When a floor is selected by the user, set global variable to the selected floor and set definition query on the feature layers
		///// </summary>
		void FloorsTableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<string> e)
		{
			_selectedFloor = e.SelectedItem;
			MapViewModel.SetFloorVisibility(true, MapView.Map, _selectedFloor); 
		}

		/// <summary>
		/// When user taps on the home button, zoom them to the home location
		/// </summary>
		/// <param name="sender">Home button</param>
		async partial void Home_TouchUpInside(UIButton sender)
		{
			var viewPoint = await MapViewModel.MoveToHomeLocationAsync(MapView.Map);
			await MapView.SetViewpointAsync(viewPoint);
		}


	}
}
