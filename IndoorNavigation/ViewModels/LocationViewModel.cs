using System.Collections.Generic;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using System.Linq;
using Esri.ArcGISRuntime.Tasks.NetworkAnalysis;
using Esri.ArcGISRuntime.Geometry;

namespace IndoorNavigation
{
	/// <summary>
	/// Location view model handles all shared logic to do with locator and geocoding
	/// </summary>
	static class LocationViewModel
	{
		/// <summary>
		/// Static reference to the mmpk.
		/// </summary>
		internal static MobileMapPackage mmpk;

		/// <summary>
		/// Gets the location suggestions from the mmpk.
		/// </summary>
		/// <returns>List of location suggestions.</returns>
		/// <param name="userInput">User input.</param>
		internal static async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestions(string userInput)
		{
			// Load the locator from the mobile map package
			var locator = mmpk.LocatorTask;
			await locator.LoadAsync();
			var locatorInfo = locator.LocatorInfo;

			if (locatorInfo.SupportsSuggestions)
			{
				try
				{
					// restrict the search to return no more than 10 suggestions
					var suggestParams = new SuggestParameters { MaxResults = 10 };
					// get suggestions for the text provided by the user
					var suggestions = await locator.SuggestAsync(userInput, suggestParams);
					return suggestions;
				}
				catch
				{
					return null;
				}
			}
			return null;
		}

		/// <summary>
		/// Gets the searched location based on search terms user entered.
		/// </summary>
		/// <returns>The searched location.</returns>
		/// <param name="searchString">User input.</param>
		internal static async Task<GeocodeResult> GetSearchedLocation(string searchString)
		{
			// Load the locator from the mobile map package
			var locator = mmpk.LocatorTask;
			await locator.LoadAsync();
			var locatorInfo = locator.LocatorInfo;

			try
			{
				// Geocode location and return the best match from the list
				var matches = await locator.GeocodeAsync(searchString);
				var bestMatch = matches.FirstOrDefault();
				return bestMatch;
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Gets the requested route based on start and end location points.
		/// </summary>
		/// <returns>The requested route.</returns>
		/// <param name="fromLocation">From location.</param>
		/// <param name="toLocation">To location.</param>
		internal static async Task<RouteResult> GetRequestedRoute(MapPoint fromLocation, MapPoint toLocation)
		{
			if (mmpk.Maps[0].LoadStatus != Esri.ArcGISRuntime.LoadStatus.Loaded)
				await mmpk.Maps[0].LoadAsync();
			var routeTask = await RouteTask.CreateAsync(mmpk.Maps[0].TransportationNetworks[0]);

			// Get the default route parameters
			var routeParams = await routeTask.CreateDefaultParametersAsync();
			// Explicitly set values for some params
			routeParams.ReturnDirections = false; // Indoor networks do not support turn by turn navigation
			routeParams.ReturnRoutes = true;

			// Create stops
			var startPoint = new Stop(fromLocation);
			var endPoint = new Stop(toLocation);

			// assign the stops to the route parameters
			routeParams.SetStops(new List<Stop> {startPoint, endPoint});

			// Execute routing
			var routeResult = await routeTask.SolveRouteAsync(routeParams);

			return routeResult;
		}
	}
}
