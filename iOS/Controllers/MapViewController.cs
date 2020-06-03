// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.UI;
using Esri.ArcGISRuntime.UI.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    /// <summary>
    /// Displays the map view and surrounding UI.
    /// Handles user interaction and updates the view in response to viewmodel changes.
    /// </summary>
    public partial class MapViewController
    {
        /// Flag used to determine if the view was single or double tapped
        private bool _isViewDoubleTapped;

        /// Gets or sets the map view model containing the common logic for dealing with the map
        private readonly MapViewModel _viewModel;

        // Graphics and symbols
        private GraphicsOverlay _homeOverlay;
        private GraphicsOverlay _identifiedFeatureOverlay;
        private GraphicsOverlay _routeOverlay;
        private PictureMarkerSymbol _routeStartSymbol;
        private PictureMarkerSymbol _routeEndSymbol;

        private async void ConfigureMapView()
        {
            try
            {
                // Configure location display
                _mapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
                _mapView.LocationDisplay.InitialZoomScale = 150;
                _mapView.LocationDisplay.IsEnabled = AppSettings.CurrentSettings.IsLocationServicesEnabled;
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                ShowError("UnableToEnableLocationDisplayErrorTitle".Localize(), "UnableToEnabledLocationDisplayErrorMessage".Localize());
            }

            try
            {
                // Configure identify overlay
                _identifiedFeatureOverlay = new GraphicsOverlay();

                var pinImage = UIImage.FromBundle("MapPin");
                var mapPin = await pinImage.ToRuntimeImageAsync();
                var roomMarker = new PictureMarkerSymbol(mapPin) {OffsetY = pinImage.Size.Height * 0.65};

                _identifiedFeatureOverlay.Renderer = new SimpleRenderer(roomMarker);

                // Configure home location overlay
                _homeOverlay = new GraphicsOverlay();
                var homeImage = UIImage.FromBundle("HomePin");
                var homePin = await homeImage.ToRuntimeImageAsync();
                var homeMarker = new PictureMarkerSymbol(homePin) {OffsetY = homeImage.Size.Height * 0.65};

                _homeOverlay.Renderer = new SimpleRenderer(homeMarker);

                // configure route overlay
                _routeOverlay = new GraphicsOverlay();

                var routeSymbol = new SimpleLineSymbol
                {
                    Width = 5,
                    Style = SimpleLineSymbolStyle.Solid,
                    Color = System.Drawing.Color.FromArgb(127, 18, 121, 193)
                };

                // line symbol renderer will be used for every graphic without its own symbol
                _routeOverlay.Renderer = new SimpleRenderer(routeSymbol);

                // Keep route graphics at the ready
                _routeStartSymbol = new PictureMarkerSymbol(await UIImage.FromBundle("StartCircle").ToRuntimeImageAsync());
                _routeEndSymbol = new PictureMarkerSymbol(await UIImage.FromBundle("EndCircle").ToRuntimeImageAsync());

                // Add graphics overlays to the map
                _mapView.GraphicsOverlays.Add(_identifiedFeatureOverlay);
                _mapView.GraphicsOverlays.Add(_homeOverlay);
                _mapView.GraphicsOverlays.Add(_routeOverlay);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);

                // Show error and crash app since this is an invalid state.
                ShowError("UnableToConfigureMapErrorTitle".Localize(), "ApplicationWillCloseDueToErrorMessage".Localize(), null, System.Threading.Thread.CurrentThread.Abort);
            }
        }

        /// <summary>
        /// Handle any map-related viewmodel property changes
        /// </summary>
        /// <param name="sender">The viewmodel</param>
        /// <param name="e">Information about the property change</param>
        private void ViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(_viewModel.Map):
                    if (_viewModel.Map != null)
                    {
                        // Add the map to the MapView to be displayed
                        _mapView.Map = _viewModel.Map;

                        // Update attribution visibility in case it changed.
                        SetAttributionForCurrentState();
                    }

                    break;
                case nameof(_viewModel.CurrentState):
                    // Hide or show cards as needed for the new UI state
                    UpdateUiForNewState();
                    // Ensure relevant layers are visible
                    if (_viewModel.CurrentState == UiState.LocationFound || _viewModel.CurrentState == UiState.PlanningRoute)
                    {
                        _identifiedFeatureOverlay.IsVisible = true;
                        _homeOverlay.IsVisible = true;
                    }
                    break;
                case nameof(_viewModel.CurrentRoom):
                    // Clear any existing graphics when the selected/identified room changes
                    _identifiedFeatureOverlay.Graphics.Clear();
                    _homeOverlay.Graphics.Clear();
                    // Turn off location display
                    _mapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Off;
                    // If the room is null, the view is now reset to a neutral state
                    if (_viewModel.CurrentRoom is Room room)
                    {
                        // If the room is home, show the home graphic and zoom to it
                        if (room.IsHome)
                        {
                            _homeOverlay.Graphics.Add(new Graphic(room.CenterPoint));
                            if (!GeometryEngine.Contains(_mapView.VisibleArea, room.CenterPoint))
                            {
                                TrySetViewpoint(room.CenterPoint, 150);
                            }
                        }
                        // If the room is standing in for the user's current location, re-enable location display automatic panning
                        else if (_viewModel.CurrentRoom.IsCurrentLocation)
                        {
                            TrySetViewpoint(_mapView.LocationDisplay.MapLocation, 150);
                            _mapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
                        }
                        // If the room is just a room, show the default graphic and zoom to the room geometry
                        else
                        {
                            _identifiedFeatureOverlay.Graphics.Add(new Graphic(room.CenterPoint));
                            if (!GeometryEngine.Contains(_mapView.VisibleArea, room.Geometry))
                            {
                                TrySetViewpoint(room.Geometry, 30);
                            }
                        }
                    }
                    // need to explicitly request re-layout because identified room can change without UI state changing
                    _bottomSheet.SetState(BottomSheetViewController.BottomSheetState.Partial);
                    break;
                case nameof(_viewModel.CurrentRoute):
                    // Clear any existing route
                    _routeOverlay.Graphics.Clear();

                    // Show the route and configure other layers if the route isn't null
                    if (_viewModel.CurrentRoute?.Routes?.FirstOrDefault() is Route route)
                    {
                        // Hide other graphics
                        _identifiedFeatureOverlay.IsVisible = false;
                        _homeOverlay.IsVisible = false;

                        // Add the route stops and route geometry
                        _routeOverlay.Graphics.Add(new Graphic(route.RouteGeometry.Parts.First().Points.First(), _routeStartSymbol));
                        _routeOverlay.Graphics.Add(new Graphic(route.RouteGeometry.Parts.Last().Points.Last(), _routeEndSymbol));
                        _routeOverlay.Graphics.Add(new Graphic(route.RouteGeometry));

                        // Zoom to the route
                        TrySetViewpoint(route.RouteGeometry, 30);
                    }
                    break;
            }
        }

        /// <summary>
        /// Attempt to set the viewpoint to the given center and scale
        /// </summary>
        /// <param name="centerPoint">Point to center the view on</param>
        /// <param name="scale">Scale to zoom to</param>
        private async void TrySetViewpoint(MapPoint centerPoint, double scale)
        {
            try
            {
                await _mapView.SetViewpointCenterAsync(centerPoint, scale);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Attempt to set the viewpoint to the given geometry and padding.
        /// </summary>
        /// <param name="geometry">Geometry to zoom to, must not be a point</param>
        /// <param name="padding">Padding around the target geometry</param>
        private async void TrySetViewpoint(Geometry.Geometry geometry, double padding)
        {
            try
            {
                await _mapView.SetViewpointGeometryAsync(geometry, padding);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Handle map visible area changes when navigation completes by updating the viewmodel.
        /// Hides or shows the attribution view as needed.
        /// </summary>
        private void MapView_NavigationCompleted(object sender, EventArgs e)
        {
            // Update the viewmodel
            _viewModel.CurrentViewArea = _mapView.VisibleArea.Extent;

            // Make sure attribution is shown properly for the current map state
            SetAttributionForCurrentState();
        }

        /// <summary>
        /// When view is double tapped, set flag to prevent accidental identify when double tapping to zoom
        /// </summary>
        private void MapView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e) => _isViewDoubleTapped = true;

        /// <summary>
        /// When view is tapped, identify the room, or if a route is in progress, give the user the option to ignore
        /// </summary>
        private async void MapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                // Wait for double tap to fire
                await Task.Delay(500);

                // If view has been double tapped, set tapped to handled and flag back to false
                if (_isViewDoubleTapped)
                {
                    e.Handled = true;
                    _isViewDoubleTapped = false;
                }
                else
                {
                    // If route card is visible, do not dismiss route
                    if (_viewModel.CurrentState == UiState.RouteFound)
                    {
                        // Create a new Alert Controller
                        UIAlertController actionSheetAlert = UIAlertController.Create(null, null, UIAlertControllerStyle.ActionSheet);

                        // Add Actions
                        actionSheetAlert.AddAction(UIAlertAction.Create("ClearExistingRouteButtonText".Localize(), UIAlertActionStyle.Destructive,
                            (action) => _viewModel.ReturnToWaitingState()));

                        actionSheetAlert.AddAction(UIAlertAction.Create("KeepExistingRouteButtonText".Localize(), UIAlertActionStyle.Default, null));

                        // Required for iPad - You must specify a source for the Action Sheet since it is
                        // displayed as a popover
                        UIPopoverPresentationController presentationPopover = actionSheetAlert.PopoverPresentationController;
                        if (presentationPopover != null)
                        {
                            presentationPopover.SourceView = _bottomSheet.DisplayedContentView;
                            presentationPopover.PermittedArrowDirections = UIPopoverArrowDirection.Up;
                        }

                        // Display the alert
                        PresentViewController(actionSheetAlert, true, null);
                    }
                    else
                    {
                        // Identify a layer using MapView, passing in the layer, the tap point, tolerance, types to return, and max result
                        IdentifyLayerResult idResults = await _mapView.IdentifyLayerAsync(
                            layer: _mapView.Map.OperationalLayers[AppSettings.CurrentSettings.RoomsLayerIndex],
                            screenPoint: e.Position,
                            tolerance: 10,
                            returnPopupsOnly: false,
                            maximumResults: 1);

                        // Call on the viewmodel to handle the identify result
                        _viewModel.IdentifyRoomFromLayerResult(idResults);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Shows the settings UI.
        /// </summary>
        private async void SettingsButton_Clicked(object sender, EventArgs e)
        {
            UINavigationController navController = new UINavigationController(new SettingsController(_viewModel));

            try
            {
                await PresentViewControllerAsync(navController, true);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                ShowError("UnableToShowSettingsErrorTitle".Localize(), null, _settingsButton);
            }
        }

        /// <summary>
        /// Displays the map's attribution when the user taps.
        /// </summary>
        /// <remarks>This is needed because the attribution bar isn't shown when in compact width.</remarks>
        private async void Attribution_Tapped(object sender, EventArgs e)
        {
            try
            {
                await PresentViewControllerAsync(new UINavigationController(new AttributionViewController(_mapView)), true);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Tells the viewmodel to move to the user's home location.
        /// </summary>
        private void Home_TouchUpInside(object sender, EventArgs e) => _viewModel.MoveToHomeLocation();

        /// <summary>
        /// Tells the viewmodel to navigate to the user's current location.
        /// </summary>
        private void CurrentLocationButton_TouchUpInside(object sender, EventArgs e) => _viewModel.MoveToCurrentLocation();

        /// <summary>
        /// Updates the viewmodel with the current user location when it changes.
        /// </summary>
        private void MapView_LocationChanged(object sender, Location.Location e)  => _viewModel.CurrentUserLocation = e.Position;

        /// <summary>
        /// Displays an error message
        /// </summary>
        /// <param name="title">Title, which is shown most prominently</param>
        /// <param name="message">Message, which is shown beneath the title</param>
        /// <param name="sourceView">If not null, the message is shown as a popover originating from this view</param>
        /// <param name="completion">Action to take after user acknowledges the error</param>
        private void ShowError(string title, string message, UIView sourceView = null, Action completion = null)
        {
            UIAlertControllerStyle preferredStyle = sourceView == null ? UIAlertControllerStyle.Alert : UIAlertControllerStyle.ActionSheet;

            // Create a new Alert Controller
            UIAlertController actionSheetAlert = UIAlertController.Create(title, message, preferredStyle);

            // Add Actions
            actionSheetAlert.AddAction(UIAlertAction.Create("OkAlertActionButtonText".Localize(), UIAlertActionStyle.Default, (value) => completion()));

            if (sourceView != null)
            {
                // Required for iPad - You must specify a source for the Action Sheet since it is displayed as a popover
                UIPopoverPresentationController presentationPopover = actionSheetAlert.PopoverPresentationController;
                if (presentationPopover != null)
                {
                    presentationPopover.SourceView = sourceView;
                    presentationPopover.PermittedArrowDirections = UIPopoverArrowDirection.Any;
                }
            }

            // Display the alert
            PresentViewController(actionSheetAlert, true, null);
        }
    }
}
