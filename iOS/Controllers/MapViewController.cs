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
namespace IndoorRouting.iOS
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Location;
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
        /// Initializes a new instance of the <see cref="T:IndoorRouting.iOS.MapViewController"/> class.
        /// </summary>
        /// <param name="handle">Controller Handle.</param>
        private MapViewController(IntPtr handle) : base(handle)
        {
            this.ViewModel = new MapViewModel();
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
                this.route = value;
                this.OnRouteChangedAsync();
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
            base.ViewWillAppear(animated);

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = true;

            // Show home button if user has home location enabled (not set as default value)
            if (AppSettings.CurrentSettings.HomeLocation != MapViewModel.DefaultHomeLocationText)
            {
                this.HomeButton.Enabled = true;
            }

            // Show Current Location button if location services is enabled
            this.CurrentLocationButton.Hidden = !AppSettings.CurrentSettings.IsLocationServicesEnabled;

            if (AppSettings.CurrentSettings.IsLocationServicesEnabled == true)
            {
                this.MapView.LocationDisplay.IsEnabled = true;
                this.MapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
                this.MapView.LocationDisplay.InitialZoomScale = 150;

                // TODO: Set floor when available in the API (Update 2?)
            }
            else
            {
                this.MapView.LocationDisplay.IsEnabled = false;
            }

            // If the routing is disabled, hide the directions button
            if (AppSettings.CurrentSettings.IsRoutingEnabled == false)
            {
                this.DirectionsButton.Enabled = false;
                this.DirectionsButton.TintColor = UIColor.White;

			}
            else
            {
				this.DirectionsButton.Enabled = true;
                this.DirectionsButton.TintColor = UIColor.Blue;
            }
        }

        // TODO: implement max size for floor picker 
        ////public override void ViewWillTransitionToSize(CoreGraphics.CGSize toSize, UIKit.IUIViewControllerTransitionCoordinator coordinator)
        ////{
        ////    base.ViewWillTransitionToSize(toSize, coordinator);
        ////        if (UIDevice.CurrentDevice.Orientation.IsLandscape())
        ////    {         
        ////    }
        ////}

        /// <summary>
        /// Overrides default behavior when view has loaded. 
        /// </summary>
        public async override void ViewDidLoad()
        {
            base.ViewDidLoad();

            this.ViewModel.PropertyChanged += this.ViewModelPropertyChanged;

            try
            {
                await this.ViewModel.InitializeAsync();
            }
            catch (Exception ex)
            {
                var genericError = "An error has occured and map was not loaded. Please restart the app";

                this.InvokeOnMainThread(() =>
                {
                    var detailsController = UIAlertController.Create("Error Details", ex.Message, UIAlertControllerStyle.Alert);
                    detailsController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));

                    var alertController = UIAlertController.Create("Error", genericError, UIAlertControllerStyle.Alert);
                    alertController.AddAction(UIAlertAction.Create("OK", UIAlertActionStyle.Default, null));
                    alertController.AddAction(
                        UIAlertAction.Create(
                            "Details",
                            UIAlertActionStyle.Default,
                            (obj) => { this.PresentViewController(detailsController, true, null); }));
                    this.PresentViewController(alertController, true, null);
                });
            }

            // Set borders and shadows on controls
            this.CurrentLocationButton.Layer.ShadowColor = UIColor.Gray.CGColor;
            this.CurrentLocationButton.Layer.ShadowOpacity = 1.0f;
            this.CurrentLocationButton.Layer.ShadowRadius = 6.0f;
            this.CurrentLocationButton.Layer.ShadowOffset = new System.Drawing.SizeF(0f, 3f);
            this.CurrentLocationButton.Layer.MasksToBounds = false;

            this.FloorsTableView.Layer.ShadowColor = UIColor.Gray.CGColor;
            this.FloorsTableView.Layer.ShadowOpacity = 1.0f;
            this.FloorsTableView.Layer.ShadowRadius = 6.0f;
            this.FloorsTableView.Layer.ShadowOffset = new System.Drawing.SizeF(0f, 3f);
            this.FloorsTableView.Layer.MasksToBounds = false;

            this.ContactCardView.Layer.ShadowColor = UIColor.Gray.CGColor;
            this.ContactCardView.Layer.ShadowOpacity = 1.0f;
            this.ContactCardView.Layer.ShadowRadius = 6.0f;
            this.ContactCardView.Layer.ShadowOffset = new System.Drawing.SizeF(0f, 3f);
            this.ContactCardView.Layer.MasksToBounds = false;

            this.SearchToolbar.Layer.ShadowColor = UIColor.Gray.CGColor;
            this.SearchToolbar.Layer.ShadowOpacity = 1.0f;
            this.SearchToolbar.Layer.ShadowRadius = 6.0f;
            this.SearchToolbar.Layer.ShadowOffset = new System.Drawing.SizeF(0f, 3f);
            this.SearchToolbar.Layer.MasksToBounds = false;

            // Remove mapview grid and set its background
            this.MapView.BackgroundGrid.GridLineWidth = 0;
            this.MapView.BackgroundGrid.Color = System.Drawing.Color.WhiteSmoke;

            // Add a graphics overlay to hold the pins and route graphics
            var pinsGraphicOverlay = new GraphicsOverlay();
            pinsGraphicOverlay.Id = "PinsGraphicsOverlay";
            this.MapView.GraphicsOverlays.Add(pinsGraphicOverlay);

            var labelsGraphicOverlay = new GraphicsOverlay();
            labelsGraphicOverlay.Id = "LabelsGraphicsOverlay";
            this.MapView.GraphicsOverlays.Add(labelsGraphicOverlay);

            var routeGraphicsOverlay = new GraphicsOverlay();
            routeGraphicsOverlay.Id = "RouteGraphicsOverlay";
            this.MapView.GraphicsOverlays.Add(routeGraphicsOverlay);

            // Handle the user moving the map 
            this.MapView.NavigationCompleted += this.MapView_NavigationCompleted;

            // Handle the user tapping on the map
            this.MapView.GeoViewTapped += this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this.MapView.GeoViewDoubleTapped += this.MapView_GeoViewDoubleTapped;

            // Handle the user holding tap on the map
            this.MapView.GeoViewHolding += this.MapView_GeoViewHolding;

            this.MapView.LocationDisplay.LocationChanged += this.MapView_LocationChanged;

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
        /// Unwinds to main view controller.
        /// </summary>
        /// <param name="segue">Unwind Segue name.</param>
        [Action("UnwindToMainViewController:")]
        public void UnwindToMainViewController(UIStoryboardSegue segue)
        {
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
                var uiImageStartPin = UIImage.FromBundle("StartCircle");
                var startPin = this.ImageToByteArray(uiImageStartPin);
                var startMarker = new PictureMarkerSymbol(new RuntimeImage(startPin));

                // create a picture marker symbol for end pin
                var uiImageEndPin = UIImage.FromBundle("EndCircle");
                var endPin = this.ImageToByteArray(uiImageEndPin);
                var endMarker = new PictureMarkerSymbol(new RuntimeImage(endPin));

                if (newRoute != null)
                {
                    StringBuilder walkTimeStringBuilder = new StringBuilder();

                    // Add walk time and distance label
                    if (newRoute.TotalTime.Hours > 0)
                    {
                        walkTimeStringBuilder.Append(string.Format("{0} h {1} m", newRoute.TotalTime.Hours, newRoute.TotalTime.Minutes));
                    }
                    else
                    {
                        walkTimeStringBuilder.Append(string.Format("{0} min", newRoute.TotalTime.Minutes + 1));
                    }

                    var tableSource = new List<Feature>() { this.FromLocationFeature, this.ToLocationFeature };
                    this.ShowRouteCard(tableSource, walkTimeStringBuilder.ToString());

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
                    this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Clear();
                    this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(routeGraphic);
                    this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(startGraphic);
                    this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Add(endGraphic);

                    // Hide the pins graphics overlay
                    this.MapView.GraphicsOverlays["PinsGraphicsOverlay"].IsVisible = false;

                    try
                    {
                        await this.MapView.SetViewpointGeometryAsync(newRoute.RouteGeometry, 30);
                    }
                    catch
                    {
                        // If panning to the new route fails, just move on
                    }
                }
                else
                {
                    this.ShowBottomCard("Routing Error", "Please retry route", true);
                }
            }
            else
            {
                this.ShowBottomCard("Routing Error", "Please retry route", true);
            }
        }

        /// <summary>
        /// Shows the route card.
        /// </summary>
        /// <param name="items">List of stops.</param>
        /// <param name="walkTime">Walk time.</param>
        private void ShowRouteCard(List<Feature> items, string walkTime)
        {
            this.InvokeOnMainThread(() =>
            {
                // Show the tableview and populate it
                RouteTableView.Source = new RouteTableSource(items);
                RouteTableView.ReloadData();

                WalkTimeLabel.Text = walkTime;

                UIView.Transition(
                    RouteCard,
                    0.2,
                    UIViewAnimationOptions.CurveLinear | UIViewAnimationOptions.LayoutSubviews,
                    () =>
                    {
                        RouteCard.Alpha = 1;
                    },
                    null);

                var buttonConstraint = 35 + RouteCard.Frame.Height;
                ButtonBottomConstraint.Constant = buttonConstraint;
                FloorPickerBottomConstraint.Constant = buttonConstraint;
            });
        }

        /// <summary>
        /// Shows the contact card and sets the fields on it depending of context.
        /// </summary>
        /// <param name="mainLabel">Main label.</param>
        /// <param name="secondaryLabel">Secondary label.</param>
        /// <param name="isRoute">If set to <c>true</c> is route.</param>
        private void ShowBottomCard(string mainLabel, string secondaryLabel, bool isRoute)
        {
            this.InvokeOnMainThread(() =>
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

                UIView.Transition(
                    ContactCardView, 
                    0.2, 
                    UIViewAnimationOptions.CurveLinear | UIViewAnimationOptions.LayoutSubviews, 
                    () =>
                    {
                        ContactCardView.Alpha = 1;
                    }, 
                    null);

                var buttonConstraint = 35 + ContactCardView.Frame.Height;
                ButtonBottomConstraint.Constant = buttonConstraint;
                FloorPickerBottomConstraint.Constant = buttonConstraint;
            });
        }

        /// <summary>
        /// Hides the contact card.
        /// </summary>
        private void HideContactCard()
        {
            this.InvokeOnMainThread(() =>
            {
                UIView.Animate(
                    0.2, 
                    0, 
                    UIViewAnimationOptions.CurveLinear | UIViewAnimationOptions.LayoutSubviews, 
                    () =>
                {
                    ContactCardView.Alpha = 0;
                    ButtonBottomConstraint.Constant = 35;
                    FloorPickerBottomConstraint.Constant = 35;
                }, 
                               null);
            });
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
                        this.MapView.Map = this.ViewModel.Map;
                    }

                    break;
                case "Viewpoint":
                    if (this.ViewModel.Viewpoint != null)
                    {
                        await this.MapView.SetViewpointAsync(this.ViewModel.Viewpoint);
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
            if (this.MapView.MapScale <= AppSettings.CurrentSettings.RoomsLayerMinimumZoomLevel)
            {
                await this.DisplayFloorLevelsAsync();
            }
            else
            {
                this.FloorsTableView.Hidden = true;
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

            // If view has been double tapped, set tapped to handled and flag back to false
            // If view has been tapped just once clear the map of selection, close keyboard and bottom sheet
            if (this.isViewDoubleTapped == true)
            {
                e.Handled = true;
                this.isViewDoubleTapped = false;
            }
            else
            {
                // If route card is visible, do not dismiss route
                if (this.RouteCard.Alpha == 1)
                {
                    // Create a new Alert Controller
                    UIAlertController actionSheetAlert = UIAlertController.Create(null, null, UIAlertControllerStyle.ActionSheet);

                    // Add Actions
                    actionSheetAlert.AddAction(UIAlertAction.Create("Clear Route", UIAlertActionStyle.Destructive, (action) => this.ClearRoute()));

                    actionSheetAlert.AddAction(UIAlertAction.Create("Keep Route", UIAlertActionStyle.Default, null));

                    // Required for iPad - You must specify a source for the Action Sheet since it is
                    // displayed as a popover
                    UIPopoverPresentationController presentationPopover = actionSheetAlert.PopoverPresentationController;
                    if (presentationPopover != null)
                    {
                        presentationPopover.SourceView = this.View;
                        presentationPopover.PermittedArrowDirections = UIPopoverArrowDirection.Up;
                    }

                    // Display the alert
                    this.PresentViewController(actionSheetAlert, true, null);
                }
                else
                {
                    // get the tap location in screen unit
                    var tapScreenPoint = e.Position;

                    var layer = this.MapView.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex];
                    var pixelTolerance = 10;
                    var returnPopupsOnly = false;
                    var maxResults = 1;

                    try
                    {
                        // Identify a layer using MapView, passing in the layer, the tap point, tolerance, types to return, and max result
                        IdentifyLayerResult idResults = await this.MapView.IdentifyLayerAsync(layer, tapScreenPoint, pixelTolerance, returnPopupsOnly, maxResults);

                        // create a picture marker symbol
                        var uiImagePin = UIImage.FromBundle("MapPin");
                        var mapPin = this.ImageToByteArray(uiImagePin);
                        var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));
                        roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                        var identifiedResult = idResults.GeoElements.First();

                        // Create graphic
                        var mapPinGraphic = new Graphic(identifiedResult.Geometry.Extent.GetCenter(), roomMarker);

                        // Add pin to mapview
                        var graphicsOverlay = this.MapView.GraphicsOverlays["PinsGraphicsOverlay"];
                        graphicsOverlay.Graphics.Clear();
                        graphicsOverlay.Graphics.Add(mapPinGraphic);

                        // Get room attribute from the settings. First attribute should be set as the searcheable one
                        var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
                        var roomNumber = identifiedResult.Attributes[roomAttribute];

                        if (roomNumber != null)
                        {
                            var employeeNameLabel = string.Empty;
                            if (AppSettings.CurrentSettings.ContactCardDisplayFields.Count > 1)
                            {

                                var employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                                var employeeName = identifiedResult.Attributes[employeeNameAttribute];
                                employeeNameLabel = employeeName as string ?? string.Empty;
                            }
                            
                            this.ShowBottomCard(roomNumber.ToString(), employeeNameLabel.ToString(), false);
                        }
                        else
                        {
                            this.MapView.GraphicsOverlays["PinsGraphicsOverlay"].Graphics.Clear();
                            this.HideContactCard();
                        }
                    }
                    catch
                    {
                        this.MapView.GraphicsOverlays["PinsGraphicsOverlay"].Graphics.Clear();
                        this.HideContactCard();
                    }

                    if (this.LocationSearchBar.IsFirstResponder == true)
                    {
                        this.LocationSearchBar.ResignFirstResponder();
                    }
                }
            }
        }

        /// <summary>
        /// Clears the route and hides route card.
        /// </summary>
        private void ClearRoute()
        {
            this.MapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Clear();
            this.MapView.GraphicsOverlays["PinsGraphicsOverlay"].IsVisible = true;
            this.RouteCard.Alpha = 0;
        }

        /// <summary>
        /// Event handler for user tapping the blue Current Location button
        /// </summary>
        /// <param name="sender">Sender control.</param>
        partial void CurrentLocationButton_TouchUpInside(UIButton sender)
        {
            this.MapView.LocationDisplay.AutoPanMode = Esri.ArcGISRuntime.UI.LocationDisplayAutoPanMode.Off;
            this.MapView.LocationDisplay.AutoPanMode = Esri.ArcGISRuntime.UI.LocationDisplayAutoPanMode.Recenter;
            this.MapView.LocationDisplay.IsEnabled = true;

            // this.ViewModel.Viewpoint = new Viewpoint(MapView.LocationDisplay.Location.Position, 150);
        }

        /// <summary>
        /// When user holds tap on a room, the information about the room is displayed
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private void MapView_GeoViewHolding(object sender, GeoViewInputEventArgs e)
        {
            // Override default behavior
            e.Handled = true;

            // TODO: Make map full screen
        }

        /// <summary>
        /// Set the current location as user moves around
        /// </summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        private void MapView_LocationChanged(object sender, Location e)
        {
            LocationViewModel.Instance.CurrentLocation = e.Position;
        }

        /// <summary>
        /// Display the floor levels based on which building the current viewpoint is over
        /// </summary>
        /// <returns>The floor levels.</returns>
        private async Task DisplayFloorLevelsAsync()
        {
            if (this.MapView.Map.LoadStatus == Esri.ArcGISRuntime.LoadStatus.Loaded)
            {
                try
                {
                    var floorsViewModel = new FloorSelectorViewModel();
                    string[] tableItems = await floorsViewModel.GetFloorsInVisibleAreaAsync(this.MapView);

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

                            // Set height constraint based on content inside table
                            HeightConstraint.Constant = FloorsTableView.ContentSize.Height;

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
            var suggestions = await LocationViewModel.Instance.GetLocationSuggestionsAsync(this.LocationSearchBar.Text);
            if (suggestions == null || suggestions.Count == 0)
            {
                this.AutosuggestionsTableView.Hidden = true;
            }
            else if (suggestions.Count > 0)
            {
                // Show the tableview with autosuggestions and populate it
                this.AutosuggestionsTableView.Hidden = false;
                var tableSource = new AutosuggestionsTableSource(suggestions);
                tableSource.TableRowSelected += this.AutosuggestionsTableSource_TableRowSelected;
                this.AutosuggestionsTableView.Source = tableSource;

                this.AutosuggestionsTableView.ReloadData();

                // Auto extend or shrink the tableview based on the content inside
                var frame = this.AutosuggestionsTableView.Frame;
                frame.Height = this.AutosuggestionsTableView.ContentSize.Height;
                this.AutosuggestionsTableView.Frame = frame;
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
            this.LocationSearchBar.Text = selectedItem.Label;
            this.LocationSearchBar.ResignFirstResponder();
            this.AutosuggestionsTableView.Hidden = true;
            await this.GetSearchedFeatureAsync(selectedItem.Label);
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
                var mapPin = this.ImageToByteArray(uiImagePin);
                var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));
                roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                // Create graphic
                var mapPinGraphic = new Graphic(geocodeResult.DisplayLocation, roomMarker);

                // Add pin to map
                var graphicsOverlay = this.MapView.GraphicsOverlays["PinsGraphicsOverlay"];
                graphicsOverlay.Graphics.Clear();
                graphicsOverlay.Graphics.Add(mapPinGraphic);
                graphicsOverlay.IsVisible = true;

                this.ViewModel.Viewpoint = new Viewpoint(geocodeResult.DisplayLocation, 150);

                // Get the feature to populate the Contact Card
                var roomFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(searchText);

                if (roomFeature != null)
                {
                    // Get room attribute from the settings. First attribute should be set as the searcheable one
                    var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
                    var roomNumber = roomFeature.Attributes[roomAttribute];
                    var roomNumberLabel = roomNumber ?? string.Empty;

                    var employeeNameLabel = string.Empty;
                    if (AppSettings.CurrentSettings.ContactCardDisplayFields.Count > 1)
                    {
                        var employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                        var employeeName = roomFeature.Attributes[employeeNameAttribute];
                        employeeNameLabel = employeeName as string ?? string.Empty;
                    }

                    this.ShowBottomCard(roomNumberLabel.ToString(), employeeNameLabel.ToString(), false);
                }
            }
            else
            {
                this.ShowBottomCard(searchText, "Location not found", true);
            }
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
            this.InvokeOnMainThread(() => this.FloorsTableView.Hidden = true);
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
            var homeLocation = this.ViewModel.MoveToHomeLocation();

            if (homeLocation != null)
            {
                // create a picture marker symbol
                var uiImagePin = UIImage.FromBundle("HomePin");
                var mapPin = this.ImageToByteArray(uiImagePin);
                var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));
                roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                // Create graphic
                var mapPinGraphic = new Graphic(homeLocation, roomMarker);

                // Add pin to map
                var graphicsOverlay = this.MapView.GraphicsOverlays["PinsGraphicsOverlay"];
                graphicsOverlay.Graphics.Clear();
                graphicsOverlay.Graphics.Add(mapPinGraphic);
                graphicsOverlay.IsVisible = true;
                this.HideContactCard();
            }

            // Get the feature to populate the Contact Card
            var roomFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(AppSettings.CurrentSettings.HomeLocation);

            if (roomFeature != null)
            {
                // Get room attribute from the settings. First attribute should be set as the searcheable one
                var roomAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[0];
                var roomNumber = roomFeature.Attributes[roomAttribute];
                var roomNumberLabel = roomNumber ?? string.Empty;

                var employeeNameLabel = string.Empty;
                if (AppSettings.CurrentSettings.ContactCardDisplayFields.Count > 1)
                {
                    var employeeNameAttribute = AppSettings.CurrentSettings.ContactCardDisplayFields[1];
                    var employeeName = roomFeature.Attributes[employeeNameAttribute];
                    employeeNameLabel = employeeName as string ?? string.Empty;
                }
                this.ShowBottomCard(roomNumberLabel.ToString(), employeeNameLabel.ToString(), false);
            }
        }
    }
}
