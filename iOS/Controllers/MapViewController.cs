// <copyright file="MapViewController.cs" company="Esri, Inc">
//     Copyright (c) Esri. All rights reserved.
// </copyright>
// <author>Mara Stoica</author>
namespace IndoorNavigation.iOS
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using CoreGraphics;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.Symbology;
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
    using Esri.ArcGISRuntime.UI;
    using Esri.ArcGISRuntime.UI.Controls;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Map view controller.
    /// </summary>
    public partial class MapViewController : UIViewController
    {
        /// <summary>
        /// Flag used to determine if the view was single or double tapped
        /// </summary>
        private bool isViewDoubleTapped;

        /// <summary>
        /// The route.
        /// </summary>
        private RouteResult route;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:IndoorNavigation.iOS.MapViewController"/> class.
        /// </summary>
        /// <param name="handle">Controller Handle.</param>
        private MapViewController(IntPtr handle) : base(handle)
        {
            this.ViewModel = new MapViewModel();
            this.ViewModel.PropertyChanged += this.ViewModelPropertyChanged;
        }

        /// <summary>
        /// Gets or sets the route.
        /// </summary>
        /// <value>The route.</value>
        public RouteResult Route
        {
            get 
            { 
                return this.route; 
            }

            set
            {
                if (this.route != value && value != null)
                {
                    this.route = value;
                    this.OnRouteChangedAsync();
                }
            }
        }

        /// <summary>
        /// Gets or sets from location feature.
        /// </summary>
        /// <value>From location feature.</value>
        public Feature FromLocationFeature { get; set; }

        /// <summary>
        /// Gets or sets to location feature.
        /// </summary>
        /// <value>To locationfeature.</value>
        public Feature ToLocationFeature { get; set; }

        /// <summary>
        /// Gets or sets the map view model containing the common logic for dealing with the map
        /// </summary>
        private MapViewModel ViewModel { get; set; }

        /// <summary>
        /// Overrides the controller behavior before view is about to appear
        /// </summary>
        /// <param name="animated">If set to <c>true</c> animated.</param>
        public override void ViewWillAppear(bool animated)
        {
            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = true;
            base.ViewWillAppear(animated);

            // Show home button if user has home location enabled (not set as default value)
            if (AppSettings.CurrentSettings.HomeLocation != MapViewModel.DefaultHomeLocationText)
            {
                this.HomeButton.Enabled = true;
            }

            // Show Current Location button if location services is enabled
            if (!AppSettings.CurrentSettings.IsLocationServicesEnabled)
            {
                this.CurrentLocationButton.Hidden = false;
            }
        }

        /// <summary>
        /// Overrides default behavior when view has loaded. 
        /// </summary>
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            CurrentLocationButton.Layer.ShadowColor = UIColor.Gray.CGColor;
            CurrentLocationButton.Layer.ShadowOpacity = 1.0f;
            CurrentLocationButton.Layer.ShadowRadius = 6.0f;
            CurrentLocationButton.Layer.ShadowOffset = new System.Drawing.SizeF(0f, 3f);
            CurrentLocationButton.Layer.MasksToBounds = false;
               
            FloorsTableView.Layer.ShadowColor = UIColor.Gray.CGColor;
            FloorsTableView.Layer.ShadowOpacity = 1.0f;
            FloorsTableView.Layer.ShadowRadius = 6.0f;
            FloorsTableView.Layer.ShadowOffset = new System.Drawing.SizeF(0f, 3f);
            FloorsTableView.Layer.MasksToBounds = false;

            ContactCardView.Layer.ShadowColor = UIColor.Gray.CGColor;
            ContactCardView.Layer.ShadowOpacity = 1.0f;
            ContactCardView.Layer.ShadowRadius = 6.0f;
            ContactCardView.Layer.ShadowOffset = new System.Drawing.SizeF(0f, 3f);
            ContactCardView.Layer.MasksToBounds = false;

            // Add a graphics overlay to hold the pins and route graphics
            this.MapView.GraphicsOverlays.Add(new GraphicsOverlay());

            // TODO: The comments below were added on January 24. Check to see if the last letter disappears. 
            // Handle the user moving the map 
            this.MapView.NavigationCompleted += this.MapView_NavigationCompleted;

            // Handle the user tapping on the map
            this.MapView.GeoViewTapped += this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this.MapView.GeoViewDoubleTapped += this.MapView_GeoViewDoubleTapped;

            // Handle the user holding tap on the mapp
            this.MapView.GeoViewHolding += this.MapView_GeoViewHolding;

            // Handle text changing in the search bar
            this.LocationSearchBar.TextChanged += async (sender, e) =>
            {
                // Call to populate autosuggestions 
                await GetSuggestionsFromLocatorAsync();
            };

            this.LocationSearchBar.SearchButtonClicked += async (sender, e) =>
            {
                var searchText = ((UISearchBar)sender).Text;

                // Dismiss keyboard
                ((UISearchBar)sender).EndEditing(true);

                // Dismiss autosuggestions table
                AutosuggestionsTableView.Hidden = true;
                await GetSearchedFeatureAsync(searchText);
            };
        }

        /// <summary>
        /// Prepares for segue.
        /// </summary>
        /// <param name="segue">Segue element.</param>
        /// <param name="sender">Sender element.</param>
        public override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
        {
            base.PrepareForSegue(segue, sender);

            if (segue.Identifier == "RouteSegue")
            {
                var routeController = segue.DestinationViewController as RouteController;
                routeController.EndLocation = this.MainLabel.Text;
            }
        }

        /// <summary>
        /// Fires when a new route is generated
        /// </summary>
        /// <returns>The new route</returns>
        private async Task OnRouteChangedAsync()
        {
            if (this.Route != null)
            {
                // get the route from the results
                var newRoute = this.Route.Routes.FirstOrDefault();

                // create a picture marker symbol for start pin
                var startPin = this.ImageToByteArray(UIImage.FromBundle("StartPin"));
                var startMarker = new PictureMarkerSymbol(new RuntimeImage(startPin));

                // create a picture marker symbol for end pin
                var endPin = this.ImageToByteArray(UIImage.FromBundle("EndPin"));
                var endMarker = new PictureMarkerSymbol(new RuntimeImage(endPin));

                if (newRoute != null)
                {
                    var labelStringBuilder = new StringBuilder("Walk time: ");

                    // Add walk time and distance label
                    if (newRoute.TotalTime.Hours > 0)
                    {
                        labelStringBuilder.Append(string.Format("{0} hr {1} min", newRoute.TotalTime.Hours, newRoute.TotalTime.Minutes));
                    }
                    else
                    {
                        labelStringBuilder.Append(string.Format("{0} min", newRoute.TotalTime.Minutes + 1));
                    }

                    var floorStringBuilder = new StringBuilder("Floor changes: ");

                    if (this.FromLocationFeature != null && this.ToLocationFeature != null)
                    {
                        floorStringBuilder.Append(string.Format(
                            "{0} to {1}",
                            this.FromLocationFeature.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName],
                            this.ToLocationFeature.Attributes[AppSettings.CurrentSettings.RoomsLayerFloorColumnName]));
                    }

                    this.ShowContactCard(labelStringBuilder.ToString(), floorStringBuilder.ToString(), true);

                    // Create graphics
                    var startGraphic = new Graphic(newRoute.RouteGeometry.Parts.First().Points.First(), startMarker);
                    var endGraphic = new Graphic(newRoute.RouteGeometry.Parts.Last().Points.Last(), endMarker);

                    // create a graphic (with a dashed line symbol) to represent the routee
                    var routeSymbol = new SimpleLineSymbol();
                    routeSymbol.Width = 5;
                    routeSymbol.Style = SimpleLineSymbolStyle.Solid;
                    routeSymbol.Color = System.Drawing.Color.FromArgb(127, 18, 121, 193);

                    var routeGraphic = new Graphic(newRoute.RouteGeometry, routeSymbol);

                    MapView.GraphicsOverlays[0].Graphics.Add(routeGraphic);
                    MapView.GraphicsOverlays[0].Graphics.Add(startGraphic);
                    MapView.GraphicsOverlays[0].Graphics.Add(endGraphic);
                    await MapView.SetViewpointGeometryAsync(newRoute.RouteGeometry, 30);
                }
            }
        }

        /// <summary>
        /// Shows the contact card and sets the fields on it depending of context.
        /// </summary>
        /// <param name="mainLabel">Main label.</param>
        /// <param name="secondaryLabel">Secondary label.</param>
        /// <param name="isRoute">If set to <c>true</c> is route.</param>
        private void ShowContactCard(string mainLabel, string secondaryLabel, bool isRoute)
        {
            // If the label is for the route, show the DetailedRoute button and fill in the labels with time and floor info
            // If the label is for the contact info, show the Directions button and fill the labels with the office info
            if (isRoute)
            {
                DirectionsButton.Hidden = true;
            }
            else
            {
                DirectionsButton.Hidden = false;
            }

            MainLabel.Text = mainLabel;
            SecondaryLabel.Text = secondaryLabel;

            UIView.Transition(ContactCardView, 0.2, UIViewAnimationOptions.CurveLinear | UIViewAnimationOptions.LayoutSubviews, () =>
                {
                    ContactCardView.Alpha = 1;

                }, null);

            var buttonConstraint = 15 + ContactCardView.Frame.Height;
            BottomConstraint.Constant = buttonConstraint;

        }

        /// <summary>
        /// Hides the contact card.
        /// </summary>
        private void HideContactCard()
        {
            UIView.Transition(ContactCardView, 0.2, UIViewAnimationOptions.CurveLinear | UIViewAnimationOptions.LayoutSubviews, () =>
            {
                ContactCardView.Alpha = 0;
                BottomConstraint.Constant = 15;

            }, null);
        }

        /// <summary>
        /// Fires when properties change in the MapViewModel
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private async void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "Map":
                    if (this.ViewModel.Map != null)
                    {
                        // Add the map to the MapView to be displayedd
                        MapView.Map = this.ViewModel.Map;
                    }

                    break;
                case "Viewpoint":
                    if (this.ViewModel.Viewpoint != null)
                    {
                        await MapView.SetViewpointAsync(this.ViewModel.Viewpoint);
                    }

                    break;
            }
        }

        /// <summary>
        /// Handle user navigating around the map
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private async void MapView_NavigationCompleted(object sender, EventArgs e)
        {
            // Display floors and level if user is zoomed in 
            // If user is zoomed out, only show the base layer
            if (MapView.MapScale <= AppSettings.CurrentSettings.RoomsLayerMinimumZoomLevel)
            {
                await this.DisplayFloorLevelsAsync();
            }
            else
            {
                FloorsTableView.Hidden = true;
                this.ViewModel.SetFloorVisibility(false);
            }
        }

        /// <summary>
        /// When view is double tapped, set flag so the tapped event doesn't fire
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private void MapView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e)
        {
            this.isViewDoubleTapped = true;
        }

        /// <summary>
        /// When view is tapped, clear the map of selection, close keyboard and bottom sheet
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private async void MapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            // Wait for double tap to fire
            await Task.Delay(500);

            // If view has been double tapped, set tapped to handled and flag back to fals
            // If view has been tapped just once clear the map of selection, close keyboard and bottom shee
            if (this.isViewDoubleTapped == true)
            {
                e.Handled = true;
                this.isViewDoubleTapped = false;
            }    
            else
            {
                this.MapView.GraphicsOverlays[0].Graphics.Clear();
                HideContactCard();
                if (this.LocationSearchBar.IsFirstResponder == true)
                {
                    this.LocationSearchBar.ResignFirstResponder();
                }
            }
        }

        /// <summary>
        /// When user holds tap on a room, the information about the room is displayed
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private async void MapView_GeoViewHolding(object sender, GeoViewInputEventArgs e)
        {
            // Override default behavior
            e.Handled = true;

            // get the tap hold location in screen unit
            var tapScreenPoint = e.Position;

            var layer = this.MapView.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex];
            var pixelTolerance = 20;
            var returnPopupsOnly = false;
            var maxResults = 1;

            try
            {
                // Identify a layer using MapView, passing in the layer, the tap point, tolerance, types to return, and max result
                IdentifyLayerResult idResults = await this.MapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResults);

                // create a picture marker symbol
                var mapPin = this.ImageToByteArray(UIImage.FromBundle("StartPin"));
                var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));

                // Create graphic
                var mapPinGraphic = new Graphic(GeometryEngine.LabelPoint(idResults.GeoElements.First().Geometry as Polygon), roomMarker);

                // Add pin to mapview
                var graphicsOverlay = this.MapView.GraphicsOverlays[0];
                graphicsOverlay.Graphics.Clear();
                graphicsOverlay.Graphics.Add(mapPinGraphic);

                // Get room attribute from the settings. First attribute should be set as the searcheable on
                var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
                var employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                var roomNumber = idResults.GeoElements.First().Attributes[roomAttribute];
                var employeeName = idResults.GeoElements.First().Attributes[employeeNameAttribute];

                var roomNumberLabel = roomNumber ?? string.Empty;
                var employeeNameLabel = employeeName ?? string.Empty;

                this.ShowContactCard(roomNumberLabel.ToString(), employeeNameLabel.ToString(), false);
            }
            catch
            {
                MapView.GraphicsOverlays[0].Graphics.Clear();
                HideContactCard();
            }
        }

        /// <summary>
        /// Display the floor levels based on which building the current viewpoint is over
        /// </summary>
        /// <returns>The floor levels.</returns>
        private async Task DisplayFloorLevelsAsync()
        {
            if (MapView.Map.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                try
                {
                    var floorsViewModel = new FloorSelectorViewModel();
                    string[] tableItems = await floorsViewModel.GetFloorsInVisibleAreaAsync(MapView);

                    this.InvokeOnMainThread(() =>
                    {
                        // Only show the floors tableview if the buildings in view have more than one floor
                        if (tableItems.Count() > 1)
                        {
                            // Show the tableview and populate it
                            FloorsTableView.Hidden = false;
                            var tableSource = new FloorsTableSource(tableItems);
                            tableSource.TableRowSelected += this.FloorsTableSource_TableRowSelected;
                            FloorsTableView.Source = tableSource;
                            FloorsTableView.ReloadData();

                            // Auto extend ot shrink the tableview based on the content inside
                            var frame = FloorsTableView.Frame;
                            frame.Height = FloorsTableView.ContentSize.Height;
                            FloorsTableView.Frame = frame;

                            if (string.IsNullOrEmpty(this.ViewModel.SelectedFloorLevel) || !tableItems.Contains(this.ViewModel.SelectedFloorLevel))
                            {
                                ViewModel.SelectedFloorLevel = MapViewModel.DefaultFloorLevel;
                            }

                            var selectedFloorNSIndex = GetTableViewRowIndex(ViewModel.SelectedFloorLevel, tableItems, 0);
                            FloorsTableView.SelectRow(selectedFloorNSIndex, false, UITableViewScrollPosition.None);

                            // Turn layers on. If there is no floor selected, first floor will be displayed by default
                            this.ViewModel.SetFloorVisibility(true);
                        }
                        else if (tableItems.Count() == 1)
                        {
                            this.DismissFloorsTableView();
                            ViewModel.SelectedFloorLevel = tableItems[0];

                            // Turn layers on. If there is no floor selected, first floor will be displayed by default
                            this.ViewModel.SetFloorVisibility(true);
                        }
                        else
                        {
                            this.DismissFloorsTableView();
                        }
                    });
                }
                catch
                {
                    this.DismissFloorsTableView();
                }
            }
        }

        /// <summary>
        /// Gets the index of the table view row.
        /// </summary>
        /// <returns>The table view row index.</returns>
        /// <param name="rowValue">Row value.</param>
        /// <param name="tableSource">Table source.</param>
        /// <param name="section">TableView Section.</param>
        private NSIndexPath GetTableViewRowIndex(string rowValue, string[] tableSource, nint section)
        {
            var rowIndex = tableSource.Select((rowItem, index) => new { rowItem, index }).First(i => i.rowItem == rowValue).index;
            return NSIndexPath.FromRowSection(rowIndex, section);
        }

        /// <summary>
        /// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
        /// </summary>
        /// <returns>The suggestions from locator</returns>
        private async Task GetSuggestionsFromLocatorAsync()
        {
            var suggestions = await LocationViewModel.LocationViewModelInstance.GetLocationSuggestionsAsync(LocationSearchBar.Text);
            if (suggestions == null || suggestions.Count == 0)
            {
                AutosuggestionsTableView.Hidden = true;
            }

            if (suggestions.Count > 0)
            {
                // Show the tableview with autosuggestions and populate it
                AutosuggestionsTableView.Hidden = false;
                var tableSource = new AutosuggestionsTableSource(suggestions);
                tableSource.TableRowSelected += this.AutosuggestionsTableSource_TableRowSelected;
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
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Event args.</param>
        private async void AutosuggestionsTableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<SuggestResult> e)
        {
            var selectedItem = e.SelectedItem;
            LocationSearchBar.Text = selectedItem.Label;
            LocationSearchBar.ResignFirstResponder();
            AutosuggestionsTableView.Hidden = true;
            await this.GetSearchedFeatureAsync(selectedItem.Label);
        }

        /// <summary>
        /// Zooms to geocode result of the searched feature
        /// </summary>
        /// <param name="searchText">Search text entered by user.</param>
        /// <returns>The searched feature</returns>
        private async Task GetSearchedFeatureAsync(string searchText)
        {
            var geocodeResult = await LocationViewModel.LocationViewModelInstance.GetSearchedLocationAsync(searchText);
            this.ViewModel.SelectedFloorLevel = await LocationViewModel.LocationViewModelInstance.GetFloorLevelFromQueryAsync(searchText);

            // create a picture marker symbol
            var mapPin = this.ImageToByteArray(UIImage.FromBundle("StartPin"));
            var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));

            // Create graphic
            var mapPinGraphic = new Graphic(geocodeResult.DisplayLocation, roomMarker);

            // Add pin to map
            var graphicsOverlay = MapView.GraphicsOverlays[0];
            graphicsOverlay.Graphics.Clear();
            graphicsOverlay.Graphics.Add(mapPinGraphic);

            this.ViewModel.Viewpoint = new Viewpoint(geocodeResult.DisplayLocation, 150);

            // Get the feature to populate the Contact Card
            var roomFeature = await LocationViewModel.LocationViewModelInstance.GetRoomFeatureAsync(searchText);

            // Get room attribute from the settings. First attribute should be set as the searcheable one
            var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
            var employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
            var roomNumber = roomFeature.Attributes[roomAttribute];
            var employeeName = roomFeature.Attributes[employeeNameAttribute];

            var roomNumberLabel = roomNumber ?? string.Empty;
            var employeeNameLabel = employeeName ?? string.Empty;

            this.ShowContactCard(roomNumberLabel.ToString(), employeeNameLabel.ToString(), false);
        }

        /// <summary>
        /// Convert images to byte array to be used as map pins.
        /// </summary>
        /// <returns>The to byte array.</returns>
        /// <param name="image">Input Image.</param>
        private byte[] ImageToByteArray(UIImage image)
        {
            using (NSData imageData = image.AsPNG())
            {
                var imageByteArray = new byte[imageData.Length];
                System.Runtime.InteropServices.Marshal.Copy(imageData.Bytes, imageByteArray, 0, Convert.ToInt32(imageData.Length));
                return imageByteArray;
            }
        }

        /// <summary>
        /// When called, clears all values and hide table view
        /// </summary>
        private void DismissFloorsTableView()
        {
            this.InvokeOnMainThread(() => FloorsTableView.Hidden = true);
        }

        /// <summary>
        /// When a floor is selected by the user, set global variable to the selected floor and set definition query on the feature layers
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Event args.</param>
        private void FloorsTableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<string> e)
        {
            this.ViewModel.SelectedFloorLevel = e.SelectedItem;
            this.ViewModel.SetFloorVisibility(true); 
        }

        /// <summary>
        /// When user taps on the home button, zoom them to the home location
        /// </summary>
        /// <param name="sender">Home button</param>
        async partial void Home_TouchUpInside(UIBarButtonItem sender)
        {
            var homeLocation = await this.ViewModel.MoveToHomeLocationAsync().ConfigureAwait(false);

            if (homeLocation != null)
            {
                // create a picture marker symbol
                var mapPin = this.ImageToByteArray(UIImage.FromBundle("HomePin"));
                var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));

                // Create graphic
                var mapPinGraphic = new Graphic(homeLocation, roomMarker);

                // Add pin to map
                var graphicsOverlay = MapView.GraphicsOverlays[0];
                graphicsOverlay.Graphics.Clear();
                graphicsOverlay.Graphics.Add(mapPinGraphic);
            }
        }
    }
}
