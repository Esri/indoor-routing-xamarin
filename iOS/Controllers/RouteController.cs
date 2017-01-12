using Foundation;
using System;
using UIKit;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.Tasks.Geocoding;

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

			// Set start location as home location if available
			if (AppSettings.CurrentSettings.HomeLocation != "Set home location")
			{
				StartLocation = AppSettings.CurrentSettings.HomeLocation;
				StartSearchBar.Text = StartLocation;
			}


			// Set text changed event on the start search bar
			StartSearchBar.TextChanged += async (sender, e) =>
			{
				//this is the method that is called when the user searchess
				await RetrieveSuggestionsFromLocator(((UISearchBar)sender).Text, true);
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
				await RetrieveSuggestionsFromLocator(((UISearchBar)sender).Text, false);
			};

			EndSearchBar.SearchButtonClicked += (sender, e) =>
			{
				EndLocation = ((UISearchBar)sender).Text;
				AutosuggestionsTableView.Hidden = true;
			};
		}

		bool _startSearchBarFlag;
		/// <summary>
		/// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
		/// </summary>
		async Task RetrieveSuggestionsFromLocator(string searchText, bool startSearchBarFlag)
		{
			_startSearchBarFlag = startSearchBarFlag;
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
				var tableSource = new AutosuggestionsTableSource(suggestions);
				tableSource.TableRowSelected += TableSource_TableRowSelected;
				AutosuggestionsTableView.Source = tableSource;

				AutosuggestionsTableView.ReloadData();

				// Auto extend or shrink the tableview based on the content inside
				var frame = AutosuggestionsTableView.Frame;
				frame.Height = AutosuggestionsTableView.ContentSize.Height;
				AutosuggestionsTableView.Frame = frame;
			}
		}

		/// <summary>
		/// Get the value selected in the Autosuggestions Table
		/// </summary>
		/// <param name="sender">Sender.</param>
		/// <param name="e">E.</param>
		async void TableSource_TableRowSelected(object sender, TableRowSelectedEventArgs<SuggestResult> e)
		{
			var selectedItem = e.SelectedItem;

			// Test which search box the initial request came from
			if (_startSearchBarFlag == true)
			{
				StartSearchBar.Text = selectedItem.Label;
				StartSearchBar.ResignFirstResponder();
			}
			else
			{
				EndSearchBar.Text = selectedItem.Label;
				EndSearchBar.ResignFirstResponder();
			}

			// Dismiss autosuggest table and keyboard
			AutosuggestionsTableView.Hidden = true;

		}

		/// <summary>
		/// Prepares for segueto go back to the Main View Controller. This segue is initiated by the Route button
		/// </summary>
		/// <param name="segue">Segue.</param>
		/// <param name="sender">Sender.</param>
		public async override void PrepareForSegue(UIStoryboardSegue segue, NSObject sender)
		{
			base.PrepareForSegue(segue, sender);

			if (segue.Identifier == "BackFromRouteSegue")
			{
				var mapViewController = segue.DestinationViewController as MapViewController;
				// Geocode the locations selected by the use
				try
				{
					var fromLocation = await LocationViewModel.GetSearchedLocation(StartLocation);
					var toLocation = await LocationViewModel.GetSearchedLocation(EndLocation);

					var route = await LocationViewModel.GetRequestedRoute(fromLocation.DisplayLocation, toLocation.DisplayLocation);
					mapViewController.Route = route;
				}
				catch { mapViewController.Route = null; }

			}

		}
	}
}