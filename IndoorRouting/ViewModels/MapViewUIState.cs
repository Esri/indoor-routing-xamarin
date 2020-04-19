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

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels
{
    public enum UiState
    {
        /// <summary>
        /// Map is shown and ready for user interaction
        /// </summary>
        ReadyWaiting,
        /// <summary>
        /// User is actively searching for a route destination
        /// </summary>
        SearchingForDestination,
        /// <summary>
        /// User is actively searching for a route origin
        /// </summary>
        SearchingForOrigin,
        /// <summary>
        /// User is actively search for a feature
        /// </summary>
        SearchingForFeature,
        
        DestinationFound,
        OriginFound,
        FeatureSearchEntered,
        PlanningRoute, //-> RouteFound, RouteNotFound
        /// <summary>
        /// A location has been found, either from user search, map identify, or UI selection like home, or current location
        /// </summary>
        LocationFound,
        /// <summary>
        /// A route search has completed with result and route is shown
        /// </summary>
        RouteFound,
        /// <summary>
        /// A route search found nothing (or an error occurred)
        /// </summary>
        RouteNotFound, //-> AwaitingSearch
        /// <summary>
        /// A location search found nothing (or an error occurred)
        /// </summary>
        LocationNotFound, //-> AwaitingSearch
    }
}
