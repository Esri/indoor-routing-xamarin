// // <copyright file="/Users/nathancastle/Documents/Dev/indoor-routing-xamarin/IndoorRouting/ViewModels/MapViewUIState.cs" company="Esri, Inc">
// //     Copyright (c) Esri. All rights reserved.
// // </copyright>
// // <author>Mara Stoica</author>
using System;
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels
{
    public enum UIState
    {
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
}
