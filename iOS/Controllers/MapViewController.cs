// <copyright file="MapViewController.cs" company="Esri, Inc">
//      Copyright 2017 Esri.
//
//      Licensed under the Apache License, Version 2.0 (the "License");
//      you may not use this file except in compliance with the License.
//      You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
// </copyright>
// <author>Mara Stoica</author>
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Location;
    using Esri.ArcGISRuntime.Mapping;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
    using Esri.ArcGISRuntime.Symbology;
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
        private bool _isViewDoubleTapped;

        // Track previously-used search bar to know where to send selected search suggestions
        private UISearchBar _lastSelectedSearchBar;

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
            base.ViewWillAppear(animated);

            // set up events
            this.ViewModel.PropertyChanged += this.ViewModelPropertyChanged;

            // Handle the user moving the map 
            this._mapView.NavigationCompleted += this.MapView_NavigationCompleted;

            // Handle the user tapping on the map
            this._mapView.GeoViewTapped += this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this._mapView.GeoViewDoubleTapped += this.MapView_GeoViewDoubleTapped;

            this._mapView.LocationDisplay.LocationChanged += this.MapView_LocationChanged;

            //  the settings button
            _settingsButton.TouchUpInside += _settingsButton_TouchUpInside;

            // Home button
            _homeButton.TouchUpInside += Home_TouchUpInside;

            if (_attributionImageButton != null)
            {
                _attributionImageButton.TouchUpInside += Attribution_Tapped;
            }

            // location button
            _locationButton.TouchUpInside += CurrentLocationButton_TouchUpInside;

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = true;

            // Show home button if user has home location enabled (not set as default value)
            if (AppSettings.CurrentSettings.HomeLocation != MapViewModel.DefaultHomeLocationText)
            {
                _homeButton.Enabled = true;
            }

            // Show Current Location button if location services is enabled
            _locationButton.Enabled = AppSettings.CurrentSettings.IsLocationServicesEnabled;

            if (AppSettings.CurrentSettings.IsLocationServicesEnabled == true)
            {
                this._mapView.LocationDisplay.IsEnabled = true;
                this._mapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
                this._mapView.LocationDisplay.InitialZoomScale = 150;

                // TODO: Set floor when available in the API (Update 2?)
            }
            else
            {
                this._mapView.LocationDisplay.IsEnabled = false;
            }

            // Listen for app state changes
            AppStateViewModel.Instance.DidTransitionToState += AppState_Changed;
            AppStateViewModel.Instance.PropertyChanged += AppState_PropertyChanged;
        }

        private void AppState_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppStateViewModel.CurrentRoute))
            {
                _ = OnRouteChangedAsync();
            }
        }

        private async void AppState_Changed(object sender, AppStateViewModel.UIState newState)
        {
            _locationSearchCard.Hidden = true;
            _locationCard.Hidden = true;
            _routeSearchView.Hidden = true;
            _routeResultView.Hidden = true;

            switch (newState)
            {
                case AppStateViewModel.UIState.AwaitingSearch:
                    _locationSearchCard.Hidden = false;
                    return;
                case AppStateViewModel.UIState.LocationFound:
                    _locationCard.Hidden = false;
                    return;
                case AppStateViewModel.UIState.LocationNotFound:
                    ShowErrorAndContinuteWithAction("LocationNotFound", () => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.SearchInProgress));
                    return;
                case AppStateViewModel.UIState.PlanningRoute:
                    _routeSearchView.Hidden = false;
                    return;
                case AppStateViewModel.UIState.RouteFound:
                    _routeResultView.Hidden = false;
                    return;
                case AppStateViewModel.UIState.RouteNotFound:
                    ShowErrorAndContinuteWithAction("RouteNotFound", () => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.PlanningRoute));
                    return;
                case AppStateViewModel.UIState.SearchInProgress:
                    _locationSearchCard.Hidden = false;
                    return;
                case AppStateViewModel.UIState.SearchFinished:
                    if (AppStateViewModel.Instance.CurrentSearchTarget == AppStateViewModel.TargetSearchField.Feature)
                    {
                        await GetSearchedFeatureAsync(AppStateViewModel.Instance.FeatureSearchText);
                    }
                    else
                    {
                        _routeSearchView.Hidden = false;
                    }
                    return;
            }
            _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
        }

        private void _locationBar_CancelButtonClicked(object sender, EventArgs e)
        {
            (sender as UISearchBar).Text = string.Empty;
            (sender as UISearchBar).ShowsCancelButton = false;
            (sender as UISearchBar).ResignFirstResponder();
        }

        private async void _settingsButton_TouchUpInside(object sender, EventArgs e)
        {
            DismissableNavigationController navController = new DismissableNavigationController(new SettingsController());
            await PresentViewControllerAsync(navController, true);

            navController.DidDismiss += (o, x) =>
            {
                _homeButton.Enabled = !String.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation);
                _locationButton.Enabled = AppSettings.CurrentSettings.IsLocationServicesEnabled;
                _accessoryView.ReloadData();
            };
            
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // set up events
            this.ViewModel.PropertyChanged -= this.ViewModelPropertyChanged;

            // Handle the user moving the map 
            this._mapView.NavigationCompleted -= this.MapView_NavigationCompleted;

            // Handle the user tapping on the map
            this._mapView.GeoViewTapped -= this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this._mapView.GeoViewDoubleTapped -= this.MapView_GeoViewDoubleTapped;

            this._mapView.LocationDisplay.LocationChanged -= this.MapView_LocationChanged;

            if (_attributionImageButton != null)
            {
                _attributionImageButton.TouchUpInside -= Attribution_Tapped;
            }

            //  the settings button
            _settingsButton.TouchUpInside -= _settingsButton_TouchUpInside;

            // Home button
            _homeButton.TouchUpInside -= Home_TouchUpInside;

            // location button
            _locationButton.TouchUpInside -= CurrentLocationButton_TouchUpInside;

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = false;
        }

        /// <summary>
        /// Overrides default behavior when view has loaded. 
        /// </summary>
        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            try
            {
                await this.ViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                var genericError = "MapLoadError".AsLocalized();

                this.InvokeOnMainThread(() =>
                {
                    var detailsController = UIAlertController.Create("ErrorDetailAlertTitle".AsLocalized(), ex.Message, UIAlertControllerStyle.Alert);
                    detailsController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".AsLocalized(), UIAlertActionStyle.Default, null));

                    var alertController = UIAlertController.Create("ErrorDetailAlertTitle".AsLocalized(), genericError, UIAlertControllerStyle.Alert);
                    alertController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".AsLocalized(), UIAlertActionStyle.Default, null));
                    alertController.AddAction(
                        UIAlertAction.Create(
                            "ErrorAlertDetailsButtonText".AsLocalized(),
                            UIAlertActionStyle.Default,
                            (obj) => { this.PresentViewController(detailsController, true, null); }));
                    this.PresentViewController(alertController, true, null);
                });
            }

            // Remove mapview grid and set its background
            this._mapView.BackgroundGrid.GridLineWidth = 0;
            this._mapView.BackgroundGrid.Color = System.Drawing.Color.WhiteSmoke;

            // Add a graphics overlay to hold the pins and route graphics
            var pinsGraphicOverlay = new GraphicsOverlay();
            pinsGraphicOverlay.Id = "PinsGraphicsOverlay";
            this._mapView.GraphicsOverlays.Add(pinsGraphicOverlay);

            var labelsGraphicOverlay = new GraphicsOverlay();
            labelsGraphicOverlay.Id = "LabelsGraphicsOverlay";
            this._mapView.GraphicsOverlays.Add(labelsGraphicOverlay);

            var routeGraphicsOverlay = new GraphicsOverlay();
            routeGraphicsOverlay.Id = "RouteGraphicsOverlay";
            this._mapView.GraphicsOverlays.Add(routeGraphicsOverlay);
        }

        private void ShowErrorAndContinuteWithAction(string message, Action action)
        {
            var alertController = UIAlertController.Create("ErrorDetailAlertTitle".AsLocalized(), message, UIAlertControllerStyle.Alert);
            alertController.AddAction(UIAlertAction.Create("OkAlertActionButtonText".AsLocalized(), UIAlertActionStyle.Default, x => action()));
            this.PresentViewController(alertController, true, null);
        }

        /// <summary>
        /// Fires when a new route is generated
        /// </summary>
        /// <returns>The new route</returns>
        private async Task OnRouteChangedAsync()
        {
            var newRoute = AppStateViewModel.Instance.CurrentRoute.Routes.FirstOrDefault();
            if (newRoute == null)
            {
                this._mapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Clear();
            }
            else
            {
                // create a picture marker symbol for start pin
                var uiImageStartPin = UIImage.FromBundle("StartCircle");
                var startPin = await uiImageStartPin.ToRuntimeImageAsync();
                var startMarker = new PictureMarkerSymbol(startPin);

                // create a picture marker symbol for end pin
                var uiImageEndPin = UIImage.FromBundle("EndCircle");
                var endPin = await uiImageEndPin.ToRuntimeImageAsync();
                var endMarker = new PictureMarkerSymbol(endPin);

                // Create point graphics
                var startGraphic = new Graphic(newRoute.RouteGeometry.Parts.First().Points.First(), startMarker);
                var endGraphic = new Graphic(newRoute.RouteGeometry.Parts.Last().Points.Last(), endMarker);

                // create a graphic to represent the routee
                var routeSymbol = new SimpleLineSymbol();
                routeSymbol.Width = 5;
                routeSymbol.Style = SimpleLineSymbolStyle.Solid;
                routeSymbol.Color = System.Drawing.Color.FromArgb(127, 18, 121, 193);

                var routeGraphic = new Graphic(newRoute.RouteGeometry, routeSymbol);

                // Add graphics to overlay
                this._mapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Clear();
                this._mapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(routeGraphic);
                this._mapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(startGraphic);
                this._mapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(endGraphic);

                // Hide the pins graphics overlay
                this._mapView.GraphicsOverlays["PinsGraphicsOverlay"].IsVisible = false;

                try
                {
                    await this._mapView.SetViewpointGeometryAsync(newRoute.RouteGeometry, 30);
                }
                catch
                {
                    // If panning to the new route fails, just move on
                }
            }
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
                        this._mapView.Map = this.ViewModel.Map;

                        // Update attribution visibility in case it changed.
                        this.InvokeOnMainThread(SetAttributionForCurrentState);
                    }

                    break;
                case "Viewpoint":
                    if (this.ViewModel.Viewpoint != null)
                    {
                        await this._mapView.SetViewpointAsync(this.ViewModel.Viewpoint);
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
            // Update attribution visibility in case it changed
            this.InvokeOnMainThread(SetAttributionForCurrentState);

            // Display floors and level if user is zoomed in 
            // If user is zoomed out, only show the base layer
            if (this._mapView.MapScale <= AppSettings.CurrentSettings.RoomsLayerMinimumZoomLevel)
            {
                await this.DisplayFloorLevelsAsync();
            }
            else
            {
                this.DismissFloorsTableView();
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
            this._isViewDoubleTapped = true;
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

            // If view has been double tapped, set tapped to handled and flag back to false
            // If view has been tapped just once clear the map of selection, close keyboard and bottom sheet
            if (this._isViewDoubleTapped == true)
            {
                e.Handled = true;
                this._isViewDoubleTapped = false;
            }
            else
            {
                // If route card is visible, do not dismiss route
                if (AppStateViewModel.Instance.CurrentState == AppStateViewModel.UIState.RouteFound)
                {
                    // Create a new Alert Controller
                    UIAlertController actionSheetAlert = UIAlertController.Create(null, null, UIAlertControllerStyle.ActionSheet);

                    // Add Actions
                    actionSheetAlert.AddAction(UIAlertAction.Create("ClearExistingRouteButtonText".AsLocalized(), UIAlertActionStyle.Destructive,
                        (action) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.AwaitingSearch)));

                    actionSheetAlert.AddAction(UIAlertAction.Create("KeepExistingRouteButtonText".AsLocalized(), UIAlertActionStyle.Default, null));

                    // Required for iPad - You must specify a source for the Action Sheet since it is
                    // displayed as a popover
                    UIPopoverPresentationController presentationPopover = actionSheetAlert.PopoverPresentationController;
                    if (presentationPopover != null)
                    {
                        presentationPopover.SourceView = _bottomSheet.DisplayedContentView;
                        presentationPopover.PermittedArrowDirections = UIPopoverArrowDirection.Up;
                    }

                    // Display the alert
                    this.PresentViewController(actionSheetAlert, true, null);
                }
                else
                {
                    // get the tap location in screen unit
                    var tapScreenPoint = e.Position;

                    var layer = this._mapView.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex];
                    var pixelTolerance = 10;
                    var returnPopupsOnly = false;
                    var maxResults = 1;

                    try
                    {
                        // Identify a layer using MapView, passing in the layer, the tap point, tolerance, types to return, and max result
                        IdentifyLayerResult idResults = await this._mapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResults);

                        IdentifiedRoom room = IdentifiedRoom.ConstructFromIdentifyResult(idResults);

                        AppStateViewModel.Instance.CurrentlyIdentifiedRoom = room;

                        if (room != null)
                        {
                            
                            // create a picture marker symbol
                            // TODO - replace with renderer
                            var uiImagePin = UIImage.FromBundle("MapPin");
                            var mapPin = await uiImagePin.ToRuntimeImageAsync();
                            var roomMarker = new PictureMarkerSymbol(mapPin);
                            roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                            // Create graphic
                            var mapPinGraphic = new Graphic(room.FeatureLocation.Extent.GetCenter(), roomMarker);

                            // Add pin to mapview
                            var graphicsOverlay = this._mapView.GraphicsOverlays["PinsGraphicsOverlay"];
                            graphicsOverlay.Graphics.Clear();
                            graphicsOverlay.Graphics.Add(mapPinGraphic);

                            AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationFound);
                        }
                        else
                        {
                            AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationNotFound);
                        }
                    }
                    catch
                    {
                        AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationNotFound);
                    }
                }
            }
        }

        /// <summary>
        /// Event handler for user tapping the blue Current Location button
        /// </summary>
        /// <param name="sender">Sender control.</param>
        private void CurrentLocationButton_TouchUpInside(object sender, EventArgs e)
        {
            this._mapView.LocationDisplay.AutoPanMode = Esri.ArcGISRuntime.UI.LocationDisplayAutoPanMode.Off;
            this._mapView.LocationDisplay.AutoPanMode = Esri.ArcGISRuntime.UI.LocationDisplayAutoPanMode.Recenter;
            this._mapView.LocationDisplay.IsEnabled = true;

            this.ViewModel.Viewpoint = new Viewpoint(_mapView.LocationDisplay.Location.Position, 150);
        }

        /// <summary>
        /// Set the current location as user moves around
        /// </summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        private void MapView_LocationChanged(object sender, Location e)
        {
            if (LocationViewModel.Instance != null)
            {
                LocationViewModel.Instance.CurrentLocation = e.Position;
            }
            
        }

        /// <summary>
        /// Display the floor levels based on which building the current viewpoint is over
        /// </summary>
        /// <returns>The floor levels.</returns>
        private async Task DisplayFloorLevelsAsync()
        {
            if (this._mapView.Map.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                try
                {
                    var floorsViewModel = new FloorSelectorViewModel();
                    string[] tableItems = await floorsViewModel.GetFloorsInVisibleAreaAsync(this._mapView);

                    this.InvokeOnMainThread(() =>
                    {
                        // Only show the floors tableview if the buildings in view have more than one floor
                        if (tableItems.Count() > 1)
                        {
                            // Show the tableview and populate it
                            _innerFloorsTableView.Hidden = false;
                            _innerFloorsTableViewShadow.Hidden = false;
                            var tableSource = new FloorsTableSource(tableItems);
                            tableSource.TableRowSelected += this.FloorsTableSource_TableRowSelected;
                            _innerFloorsTableView.Source = tableSource;
                            _innerFloorsTableView.ReloadData();

                            if (string.IsNullOrEmpty(this.ViewModel.SelectedFloorLevel) || !tableItems.Contains(this.ViewModel.SelectedFloorLevel))
                            {
                                ViewModel.SelectedFloorLevel = MapViewModel.DefaultFloorLevel;
                            }

                            var selectedFloorNSIndex = GetTableViewRowIndex(ViewModel.SelectedFloorLevel, tableItems, 0);
                            _innerFloorsTableView.SelectRow(selectedFloorNSIndex, false, UITableViewScrollPosition.None);

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
        /// Zooms to geocode result of the searched feature
        /// </summary>
        /// <param name="searchText">Search text entered by user.</param>
        /// <returns>The searched feature</returns>
        private async Task GetSearchedFeatureAsync(string searchText)
        {
            var geocodeResult = await LocationViewModel.Instance.GetSearchedLocationAsync(searchText);
            this.ViewModel.SelectedFloorLevel = await LocationViewModel.Instance.GetFloorLevelFromQueryAsync(searchText);

            if (geocodeResult != null)
            {
                // create a picture marker symbol
                var uiImagePin = UIImage.FromBundle("MapPin");
                var mapPin = await uiImagePin.ToRuntimeImageAsync();
                var roomMarker = new PictureMarkerSymbol(mapPin);
                roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                // Create graphic
                var mapPinGraphic = new Graphic(geocodeResult.DisplayLocation, roomMarker);

                // Add pin to map
                var graphicsOverlay = this._mapView.GraphicsOverlays["PinsGraphicsOverlay"];
                graphicsOverlay.Graphics.Clear();
                graphicsOverlay.Graphics.Add(mapPinGraphic);
                graphicsOverlay.IsVisible = true;

                this.ViewModel.Viewpoint = new Viewpoint(geocodeResult.DisplayLocation, 150);

                // Get the feature to populate the Contact Card
                Feature roomFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(searchText);

                IdentifiedRoom room = IdentifiedRoom.ConstructFromFeature(roomFeature);

                AppStateViewModel.Instance.CurrentlyIdentifiedRoom = room;

                if (roomFeature != null)
                {
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationFound);
                }
                else
                {
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationNotFound);
                }
            }
            else
            {
                AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationNotFound);
            }
        }

        /// <summary>
        /// When called, clears all values and hide table view
        /// </summary>
        private void DismissFloorsTableView()
        {
            this.InvokeOnMainThread(() => { this._innerFloorsTableView.Hidden = true; this._innerFloorsTableViewShadow.Hidden = true; });
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
        private async void Home_TouchUpInside(object sender, EventArgs e)
        {
            var homeLocation = this.ViewModel.MoveToHomeLocation();

            if (homeLocation != null)
            {
                // create a picture marker symbol
                var uiImagePin = UIImage.FromBundle("HomePin");
                var mapPin = await uiImagePin.ToRuntimeImageAsync();
                var roomMarker = new PictureMarkerSymbol(mapPin);
                roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                // Create graphic
                var mapPinGraphic = new Graphic(homeLocation, roomMarker);

                // Add pin to map
                var graphicsOverlay = this._mapView.GraphicsOverlays["PinsGraphicsOverlay"];
                graphicsOverlay.Graphics.Clear();
                graphicsOverlay.Graphics.Add(mapPinGraphic);
                graphicsOverlay.IsVisible = true;

                AppStateViewModel.Instance.CurrentlyIdentifiedRoom = await IdentifiedRoom.ConstructHome();

                AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.LocationFound);
            }
            else
            {
                throw new Exception("This shouldn't happen; invalid home location");
            }
        }
    }
}
