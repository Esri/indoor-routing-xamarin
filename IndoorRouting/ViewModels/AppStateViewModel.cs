// // <copyright file="/Users/nathancastle/Documents/Dev/indoor-routing-xamarin/IndoorRouting/ViewModels/AppStateViewModel.cs" company="Esri, Inc">
// //     Copyright (c) Esri. All rights reserved.
// // </copyright>
// // <author>Mara Stoica</author>
using System;
using System.ComponentModel;
using Esri.ArcGISRuntime.Data;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.Models;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels
{
    /// <summary>
    /// ViewModel class tracks the current app UI state
    /// </summary>
    public class AppStateViewModel : INotifyPropertyChanged // TODO implement for all properties
    {
        public enum UIState
        {
            Downloading, //->AwaitingSearch
            ReadyWaiting, //-> SearchInProgress
            SearchingForDestination,
            SearchingForOrigin,
            SearchingForFeature,
            DestinationFound,
            OriginFound,
            FeatureSearchEntered,
            PlanningRoute, //-> RouteFound, RouteNotFound
            LocationFound, //-> PlanningRoute, AwaitingSearch
            RouteFound, //-> AwaitingSearch, LocationFound
            RouteNotFound, //-> AwaitingSearch
            LocationNotFound, //-> AwaitingSearch
        }

        public UIState CurrentState { get; private set; }

        private IdentifiedRoom _currentlyIdentifiedRoom;

        public IdentifiedRoom CurrentlyIdentifiedRoom
        {
            get => _currentlyIdentifiedRoom;
            set
            {
                if (value != _currentlyIdentifiedRoom)
                {
                    _currentlyIdentifiedRoom = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentlyIdentifiedRoom)));
                }
            }
        }

        private RouteResult _route;

        public RouteResult CurrentRoute
        {
            get => _route;
            set
            {
                if (value != _route)
                {
                    _route = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentRoute)));
                }
            }
        }

        private string _featureSearchText = string.Empty;
        private string _originSearchText = string.Empty;
        private string _destinationSearchText = string.Empty;

        public string FeatureSearchText
        {
            get => _featureSearchText;
            set
            {
                if (value != _featureSearchText)
                {
                    _featureSearchText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(FeatureSearchText)));
                }
            }
        }

        public string OriginSearchText
        {
            get => _originSearchText;
            set
            {
                if (value != _originSearchText)
                {
                    _originSearchText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(OriginSearchText)));
                }
            }
        }

        public string DestinationSearchText
        {
            get => _destinationSearchText;
            set
            {
                if (value != _destinationSearchText)
                {
                    _destinationSearchText = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(DestinationSearchText)));
                }
            }
        }

        //TODO restructure this further
        public async void PerformRouteSearchAndUpdateState()
        {
            // Geocode the locations selected by the user
            try
            {
                // TODO - move this behavior to the viewmodel
                if (OriginSearchText != "CurrentLocationLabel".AsLocalized())
                {
                    FromLocationFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(OriginSearchText);
                    ToLocationFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(DestinationSearchText);

                    var fromLocationPoint = FromLocationFeature.Geometry.Extent.GetCenter();
                    var toLocationPoint = ToLocationFeature.Geometry.Extent.GetCenter();

                    var route = await LocationViewModel.Instance.GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);

                    CurrentRoute = route;
                }
                else
                {
                    ToLocationFeature = await LocationViewModel.Instance.GetRoomFeatureAsync(DestinationSearchText);

                    var fromLocationPoint = LocationViewModel.Instance.CurrentLocation;
                    var toLocationPoint = ToLocationFeature.Geometry.Extent.GetCenter();

                    var route = await LocationViewModel.Instance.GetRequestedRouteAsync(fromLocationPoint, toLocationPoint);

                    CurrentRoute = route;
                }

                if (CurrentRoute != null)
                {
                    TransitionToState(UIState.RouteFound);
                }
                else
                {
                    TransitionToState(UIState.RouteNotFound);
                }
            }
            catch
            {
                CurrentRoute = null;
                TransitionToState(UIState.RouteNotFound);
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

        public void StartSearchFromFoundFeature()
        {
            if (CurrentlyIdentifiedRoom.IsHome)
            {
                OriginSearchText = string.Empty;
                DestinationSearchText = CurrentlyIdentifiedRoom.RoomNumber;
            }
            else
            {
                OriginSearchText = CurrentlyIdentifiedRoom.RoomNumber;
                DestinationSearchText = string.Empty;
            }

            TransitionToState(UIState.PlanningRoute);
        }

        public void CloseLocationInfo()
        {
            TransitionToState(AppStateViewModel.UIState.ReadyWaiting);
        }

        public static AppStateViewModel Instance { get; private set; }

        public static void Initialize(UIState initialState)
        {
            Instance = new AppStateViewModel();
            Instance.CurrentState = initialState;
        }

        public void TransitionToState(UIState newState)
        {
            CurrentState = newState;
            DidTransitionToState?.Invoke(this, newState);
        }

        public event EventHandler<UIState> DidTransitionToState;
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
