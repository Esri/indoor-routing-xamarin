using Foundation;
using System;
using UIKit;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using System.Collections.Generic;
using System.Linq;
using CoreGraphics;
using System.Threading.Tasks;
using System.IO;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Controller handles the ui and logic of the user choosing a home location
	/// </summary>
    partial class HomeLocationController : UIViewController
    {

		HomeLocationController(IntPtr handle) : base(handle)
		{
		}

		/// <summary>
		/// Overrides the controller behavior before view is about to appear
		/// </summary>
		/// <param name="animated">If set to <c>true</c> animated.</param>
		public override void ViewWillAppear(bool animated)
		{
			// Show the navigation bar
			NavigationController.NavigationBarHidden = false;
			base.ViewWillAppear(animated);

		}

		/// <summary>
		/// Overrides the behavior of the controller once the view has loaded
		/// </summary>
		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			HomeLocationSearchBar.BecomeFirstResponder();

			// Set text changed event on the search bar
			HomeLocationSearchBar.TextChanged += async (sender, e) =>
			{
				//this is the method that is called when the user searchess
				await RetrieveSuggestionsFromLocator();
			};

			HomeLocationSearchBar.SearchButtonClicked += async (sender, e) =>
			{
				var locationText = ((UISearchBar)sender).Text;
				await SetHomeLocation(locationText);
				this.NavigationController.PopViewController(true);
			};
		}

		/// <summary>
		/// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
		/// </summary>
	    async Task RetrieveSuggestionsFromLocator()
		{
			var suggestions = await LocationViewModel.GetLocationSuggestions(HomeLocationSearchBar.Text);
			if (suggestions == null || suggestions.Count == 0)
			{
				AutosuggestionsTableView.Hidden = true;
			}
			// Only show the floors tableview if the buildings in view have more than one floor
			if (suggestions.Count > 0)
			{
				// Show the tableview with autosuggestions and populate it
				AutosuggestionsTableView.Hidden = false;
				AutosuggestionsTableView.Source = new AutosuggestionsTableSource(suggestions);

				AutosuggestionsTableView.ReloadData();

				// Auto extend or shrink the tableview based on the content inside
        		var frame = AutosuggestionsTableView.Frame;
				frame.Height = AutosuggestionsTableView.ContentSize.Height;
        		AutosuggestionsTableView.Frame = frame;
			}
		}

		/// <summary>
		/// Sets the home location for the user and saves it into settings.
		/// </summary>
		/// <param name="locationText">Location text.</param>
		async Task SetHomeLocation(string locationText)
		{
			AppSettings.currentSettings.HomeLocation = locationText;
			var homeLocation = await LocationViewModel.GetSearchedLocation(locationText);


			// Save extent of home location and floor level to Settings file
			CoordinatesKeyValuePair<string, double>[] homeCoordinates =
			{
				new CoordinatesKeyValuePair<string, double>("X", homeLocation.DisplayLocation.X),
				new CoordinatesKeyValuePair<string, double>("Y", homeLocation.DisplayLocation.Y),
				new CoordinatesKeyValuePair<string, double>("WKID", homeLocation.DisplayLocation.SpatialReference.Wkid),
				new CoordinatesKeyValuePair<string, double>("Floor", 1),
			};

			AppSettings.currentSettings.HomeCoordinates = homeCoordinates;

			// Save user settings
			var settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			await Task.Run(() => AppSettings.SaveSettings(Path.Combine(settingsPath, "AppSettings.xml")));
		}
	}
}