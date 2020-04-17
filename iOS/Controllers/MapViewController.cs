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
    using Esri.ArcGISRuntime.Geometry;
    using Esri.ArcGISRuntime.Location;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
    using Esri.ArcGISRuntime.Symbology;
    using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
    using Esri.ArcGISRuntime.UI;
    using Esri.ArcGISRuntime.UI.Controls;
    using UIKit;

    /// <summary>
    /// Map view controller.
    /// </summary>
    public partial class MapViewController : UIViewController
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
                ShowError("UnableToEnableLocationDisplayErrorTitle".AsLocalized(), "UnableToEnabledLocationDisplayErrorMessage".AsLocalized());
            }

            try
            {
                // Configure identify overlay
                _identifiedFeatureOverlay = new GraphicsOverlay();

                var pinImage = UIImage.FromBundle("MapPin");
                var mapPin = await pinImage.ToRuntimeImageAsync();
                var roomMarker = new PictureMarkerSymbol(mapPin);
                roomMarker.OffsetY = pinImage.Size.Height * 0.65;

                _identifiedFeatureOverlay.Renderer = new SimpleRenderer(roomMarker);

                // Configure home location overlay
                _homeOverlay = new GraphicsOverlay();
                var homeImage = UIImage.FromBundle("HomePin");
                var homePin = await homeImage.ToRuntimeImageAsync();
                var homeMarker = new PictureMarkerSymbol(homePin);
                homeMarker.OffsetY = homeImage.Size.Height * 0.65;

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
                ShowError("UnableToConfigureMapErrorTitle".AsLocalized(), "ApplicationWillCloseDueToErrorMessage".AsLocalized(), null, System.Threading.Thread.CurrentThread.Abort);
            }
        }

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
                    UpdateUIForNewState();
                    if (_viewModel.CurrentState == UIState.LocationFound || _viewModel.CurrentState == UIState.PlanningRoute)
                    {
                        _identifiedFeatureOverlay.IsVisible = true;
                        _homeOverlay.IsVisible = true;
                    }
                    break;
                case nameof(_viewModel.CurrentlyIdentifiedRoom):
                    _identifiedFeatureOverlay.Graphics.Clear();
                    _homeOverlay.Graphics.Clear();
                    // Turn off location display unless actively viewing current location
                    _mapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Off;
                    if (_viewModel.CurrentlyIdentifiedRoom is IdentifiedRoom room)
                    {
                        if (room.IsHome)
                        {
                            _homeOverlay.Graphics.Add(new Graphic(room.CenterPoint));
                            TrySetViewpoint(room.CenterPoint, 150);
                        }
                        else if (_viewModel.CurrentlyIdentifiedRoom.IsCurrentLocation)
                        {
                            _mapView.LocationDisplay.AutoPanMode = LocationDisplayAutoPanMode.Recenter;
                            TrySetViewpoint(_mapView.LocationDisplay.MapLocation, 150);
                        }
                        else
                        {
                            _identifiedFeatureOverlay.Graphics.Add(new Graphic(room.CenterPoint));
                            TrySetViewpoint(room.Geometry, 30);
                        }
                    }
                    // need to explicitly request re-layout because identified room can change without UI state changing
                    _bottomSheet.SetStateWithAnimation(BottomSheetViewController.BottomSheetState.partial);
                    break;
                case nameof(_viewModel.CurrentRoute):
                    _routeOverlay.Graphics.Clear();

                    if (_viewModel.CurrentRoute?.Routes?.FirstOrDefault() is Route route)
                    {
                        _identifiedFeatureOverlay.IsVisible = false;
                        _homeOverlay.IsVisible = false;

                        _routeOverlay.Graphics.Add(new Graphic(route.RouteGeometry.Parts.First().Points.First(), _routeStartSymbol));
                        _routeOverlay.Graphics.Add(new Graphic(route.RouteGeometry.Parts.Last().Points.Last(), _routeEndSymbol));
                        _routeOverlay.Graphics.Add(new Graphic(route.RouteGeometry));
                        TrySetViewpoint(route.RouteGeometry, 30);
                    }
                    break;
            }
        }

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

        private async void TrySetViewpoint(Geometry geometry, double padding)
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

        private void _mapView_NavigationCompleted(object sender, EventArgs e)
        {
            _viewModel.CurrentViewArea = _mapView.VisibleArea.Extent;
            SetAttributionForCurrentState();
        }

        /// <summary>
        /// When view is double tapped, set flag so the tapped event doesn't fire
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private void MapView_GeoViewDoubleTapped(object sender, GeoViewInputEventArgs e) => _isViewDoubleTapped = true;

        /// <summary>
        /// When view is tapped, clear the map of selection, close keyboard and bottom sheet
        /// </summary>
        /// <param name="sender">Sender element.</param>
        /// <param name="e">Eevent args.</param>
        private async void MapView_GeoViewTapped(object sender, GeoViewInputEventArgs e)
        {
            try
            {
                // Wait for double tap to fire
                await Task.Delay(500);

                // If view has been double tapped, set tapped to handled and flag back to false
                // If view has been tapped just once clear the map of selection, close keyboard and bottom sheet
                if (_isViewDoubleTapped == true)
                {
                    e.Handled = true;
                    _isViewDoubleTapped = false;
                }
                else
                {
                    // If route card is visible, do not dismiss route
                    if (_viewModel.CurrentState == UIState.RouteFound)
                    {
                        // Create a new Alert Controller
                        UIAlertController actionSheetAlert = UIAlertController.Create(null, null, UIAlertControllerStyle.ActionSheet);

                        // Add Actions
                        actionSheetAlert.AddAction(UIAlertAction.Create("ClearExistingRouteButtonText".AsLocalized(), UIAlertActionStyle.Destructive,
                            (action) => _viewModel.ReturnToWaitingState()));

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

                        _viewModel.IdentifyRoomFromLayerResult(idResults);
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        private async void _settingsButton_TouchUpInside(object sender, EventArgs e)
        {
            DismissableNavigationController navController = new DismissableNavigationController(new SettingsController(_viewModel));

            navController.DidDismiss += (o, x) =>
            {
                _accessoryView.ReloadData();
            };

            try
            {
                await PresentViewControllerAsync(navController, true);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                ShowError("UnableToShowSettingsErrorTitle".AsLocalized(), null, _settingsButton);
            }
        }

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
        /// When user taps on the home button, zoom them to the home location
        /// </summary>
        /// <param name="sender">Home button</param>
        private void Home_TouchUpInside(object sender, EventArgs e) => _viewModel.MoveToHomeLocation();

        /// <summary>
        /// Event handler for user tapping the blue Current Location button
        /// </summary>
        /// <param name="sender">Sender control.</param>
        private void CurrentLocationButton_TouchUpInside(object sender, EventArgs e) => _viewModel.MoveToCurrentLocation();

        /// <summary>
        /// Set the current location as user moves around
        /// </summary>
        /// <param name="sender">Sender control.</param>
        /// <param name="e">Event args.</param>
        private void MapView_LocationChanged(object sender, Location e)  => _viewModel.CurrentUserLocation = e.Position;

        private void ShowError(string title, string message, UIView sourceView = null, Action completion = null)
        {
            UIAlertControllerStyle preferredStyle = sourceView == null ? UIAlertControllerStyle.Alert : UIAlertControllerStyle.ActionSheet;

            // Create a new Alert Controller
            UIAlertController actionSheetAlert = UIAlertController.Create(title, message, preferredStyle);

            // Add Actions
            actionSheetAlert.AddAction(UIAlertAction.Create("ErrorMessageOK".AsLocalized(), UIAlertActionStyle.Default, (value) => completion()));

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
