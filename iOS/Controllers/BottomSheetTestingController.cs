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
    using System.Text;
    using System.Threading.Tasks;
    using Esri.ArcGISRuntime.Data;
    using Esri.ArcGISRuntime.Location;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.Symbology;
    using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
    using Esri.ArcGISRuntime.Toolkit.UI.Controls;
    using Esri.ArcGISRuntime.UI;
    using Esri.ArcGISRuntime.UI.Controls;
    using Foundation;
    using UIKit;

    /// <summary>
    /// Map view controller.
    /// </summary>
    public partial class BottomSheetTestingController : UIViewController
    {
        private NSLayoutConstraint[] _compactWidthConstraints;
        private NSLayoutConstraint[] _regularWidthConstraints;
        private NSLayoutConstraint[] _invariantConstraints;

        private MapView _mapView;

        private BottomSheetViewController _bottomSheet;

        /// <summary>
        /// Flag used to determine if the view was single or double tapped
        /// </summary>
        private bool _isViewDoubleTapped;

        /// <summary>
        /// The route.
        /// </summary>
        private RouteResult _route;

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

            // set up events
            this.ViewModel.PropertyChanged += this.ViewModelPropertyChanged;

            // Handle the user tapping on the map
            this._mapView.GeoViewTapped += this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this._mapView.GeoViewDoubleTapped += this.MapView_GeoViewDoubleTapped;

            // Handle the user holding tap on the map
            this._mapView.GeoViewHolding += this.MapView_GeoViewHolding;

            this._mapView.LocationDisplay.LocationChanged += this.MapView_LocationChanged;

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = true;

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
        }

        public override void ViewDidDisappear(bool animated)
        {
            base.ViewDidDisappear(animated);

            // set up events
            this.ViewModel.PropertyChanged -= this.ViewModelPropertyChanged;

            // Handle the user tapping on the map
            this._mapView.GeoViewTapped -= this.MapView_GeoViewTapped;

            // Handle the user double tapping on the map
            this._mapView.GeoViewDoubleTapped -= this.MapView_GeoViewDoubleTapped;

            // Handle the user holding tap on the map
            this._mapView.GeoViewHolding -= this.MapView_GeoViewHolding;

            this._mapView.LocationDisplay.LocationChanged -= this.MapView_LocationChanged;

            // Hide the navigation bar on the main screen 
            NavigationController.NavigationBarHidden = false;
        }

        public override void ViewDidAppear(bool animated)
        {
            base.ViewDidAppear(animated);
            

            _bottomSheet = new BottomSheetViewController(View);

            this.AddChildViewController(_bottomSheet);

            _bottomSheet.DidMoveToParentViewController(this);

            var searchBar = new UISearchBar { TranslatesAutoresizingMaskIntoConstraints = false };
            searchBar.BackgroundImage = new UIImage();
            searchBar.Translucent = true;
            searchBar.Placeholder = "Search for a place or address";
            _bottomSheet.DisplayedContentView.AddSubview(searchBar);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                searchBar.LeadingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.LeadingAnchor, 8),
                searchBar.TrailingAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TrailingAnchor, -8),
                searchBar.TopAnchor.ConstraintEqualTo(_bottomSheet.DisplayedContentView.TopAnchor)
            });
        }

        public override void LoadView()
        {
            base.LoadView();
            this.ViewModel = new MapViewModel();

            this.View = new UIView { BackgroundColor = UIColor.SystemBackgroundColor };

            _mapView = new MapView { TranslatesAutoresizingMaskIntoConstraints = false };

            View.AddSubviews(_mapView);

            _invariantConstraints = new NSLayoutConstraint[]
            {
                _mapView.LeadingAnchor.ConstraintEqualTo(View.LeadingAnchor),
                _mapView.TrailingAnchor.ConstraintEqualTo(View.TrailingAnchor),
                _mapView.TopAnchor.ConstraintEqualTo(View.TopAnchor),
                _mapView.BottomAnchor.ConstraintEqualTo(View.BottomAnchor),
            };

            _regularWidthConstraints = new NSLayoutConstraint[]
            {
            };

            _compactWidthConstraints = new NSLayoutConstraint[]
            {
            };

            NSLayoutConstraint.ActivateConstraints(_invariantConstraints);
            ApplyConstraintsForSizeClass();
        }

        private void ApplyConstraintsForSizeClass()
        {
            NSLayoutConstraint.DeactivateConstraints(_compactWidthConstraints);
            NSLayoutConstraint.DeactivateConstraints(_regularWidthConstraints);

            if (TraitCollection.HorizontalSizeClass == UIUserInterfaceSizeClass.Regular)
            {
                NSLayoutConstraint.ActivateConstraints(_regularWidthConstraints);
            }
            else
            {
                NSLayoutConstraint.ActivateConstraints(_compactWidthConstraints);
            }
        }

        public override void TraitCollectionDidChange(UITraitCollection previousTraitCollection)
        {
            base.TraitCollectionDidChange(previousTraitCollection);

            ApplyConstraintsForSizeClass();

            _bottomSheet?.TraitCollectionDidChange(previousTraitCollection);
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
                        this._mapView.Map = new Mapping.Map(Mapping.Basemap.CreateImageryWithLabelsVector());
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

                    // create a picture marker symbol
                    var uiImagePin = UIImage.FromBundle("MapPin");
                    var mapPin = uiImagePin.ToByteArray();
                    var roomMarker = new PictureMarkerSymbol(new RuntimeImage(mapPin));
                    roomMarker.OffsetY = uiImagePin.Size.Height * 0.65;

                    var identifiedResult = idResults.GeoElements.First();

                    // Create graphic
                    var mapPinGraphic = new Graphic(identifiedResult.Geometry.Extent.GetCenter(), roomMarker);

                    // Add pin to mapview
                    var graphicsOverlay = this._mapView.GraphicsOverlays["PinsGraphicsOverlay"];
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
                    }
                    else
                    {
                        this._mapView.GraphicsOverlays["PinsGraphicsOverlay"].Graphics.Clear();
                    }
                }
                catch
                {
                    this._mapView.GraphicsOverlays["PinsGraphicsOverlay"].Graphics.Clear();
                }
            }
        }

        /// <summary>
        /// Clears the route and hides route card.
        /// </summary>
        private void ClearRoute()
        {
            this._mapView.GraphicsOverlays["RouteGraphicsOverlay"].Graphics.Clear();
            this._mapView.GraphicsOverlays["PinsGraphicsOverlay"].IsVisible = true;
            //this._routeCard.Alpha = 0;
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
        /// When a floor is selected by the user, set global variable to the selected floor and set definition query on the feature layers
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Event args.</param>
        private void FloorsTableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<string> e)
        {
            this.ViewModel.SelectedFloorLevel = e.SelectedItem;
            this.ViewModel.SetFloorVisibility(true); 
        }
    }
}
