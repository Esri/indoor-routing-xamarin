using System.Collections.Generic;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using System.Linq;

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
	}
}
