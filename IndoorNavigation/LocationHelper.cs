using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Tasks.Geocoding;

namespace IndoorNavigation
{
	public static class LocationHelper
	{
		public static MobileMapPackage mmpk;


		public static async Task<IReadOnlyList<SuggestResult>> GetLocationSuggestions(string userInput)
		{
			LocatorTask locator = mmpk.LocatorTask;
			await locator.LoadAsync();
			var locatorInfo = locator.LocatorInfo;


			//var ResultFieldsList = new List<LocatorAttribute>();

			//foreach (var field in locatorInfo.ResultAttributes)
			//{
			//	// add the field's display name to a list box
			//	ResultFieldsList.Add(field);
			//}

			if (locatorInfo.SupportsSuggestions)
			{
				// restrict the search to the current map extent and return no more than 10 suggestions
				var suggestParams = new SuggestParameters { MaxResults = 10 };
				// get suggestions for the text provided by the user
				var suggestions = await locator.SuggestAsync(userInput, suggestParams);
				// show suggestions ...
				return suggestions;
			}
			else
				return null;
			//else
			//{
			//	var matches = await locator.GeocodeAsync(userInput);
			//}

		}
	}
}
