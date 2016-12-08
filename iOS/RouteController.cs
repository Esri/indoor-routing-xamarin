using Foundation;
using System;
using UIKit;
using System.Threading.Tasks;

namespace IndoorNavigation.iOS
{
    partial class RouteController : UIViewController
    {
		public string EndLocation { get; set; }

		public string StartLocation { get; set; }


        public RouteController (IntPtr handle) : base (handle)
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
			EndSearchBar.Text = EndLocation;
			if (AppSettings.currentSettings.HomeLocation != "Set home location")
			{
				StartLocation = AppSettings.currentSettings.HomeLocation;
				StartSearchBar.Text = StartLocation;
			}


			// Set text changed event on the start search barr
			StartSearchBar.TextChanged += async (sender, e) =>
			{
				//this is the method that is called when the user searchess
				await RetrieveSuggestionsFromLocator(((UISearchBar)sender).Text);
			};

			StartSearchBar.SearchButtonClicked += (sender, e) =>
			{
				StartLocation = ((UISearchBar)sender).Text;
				AutosuggestionsTableView.Hidden = true;
			};

			// Set text changed event on the end search bar
			EndSearchBar.TextChanged += async (sender, e) =>
			{
				//this is the method that is called when the user searches
				await RetrieveSuggestionsFromLocator(((UISearchBar)sender).Text);
			};

			EndSearchBar.SearchButtonClicked += (sender, e) =>
			{
				EndLocation = ((UISearchBar)sender).Text;
				AutosuggestionsTableView.Hidden = true;
			};
		}

		/// <summary>
		/// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
		/// </summary>
		async Task RetrieveSuggestionsFromLocator(string searchText)
		{
			var suggestions = await LocationViewModel.GetLocationSuggestions(searchText);
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

		//async partial void RouteButton_TouchUpInside(UIButton sender)
		//{

		//}

		public async override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
		{
			base.PrepareForSegue(segue, sender);

			if (segue.Identifier == "BackFromRouteSegue")
			{
				var mainViewController = segue.DestinationViewController as MainViewController;
				// Geocode the locations selected by the use
				try
				{
					var fromLocation = await LocationViewModel.GetSearchedLocation(StartLocation);
					var toLocation = await LocationViewModel.GetSearchedLocation(EndLocation);

					var route = await LocationViewModel.GetRequestedRoute(fromLocation.DisplayLocation, toLocation.DisplayLocation);
					mainViewController.Route = route;
				}
				catch { mainViewController.Route = null; }

			}

		}
	}
}