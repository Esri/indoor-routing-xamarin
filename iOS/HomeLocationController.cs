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
			//TODO: Decide if the search field should be first responder or not. Remove this line if not needed
			//HomeLocationTextField.BecomeFirstResponder();

			// Set text changed event on the search bar
			HomeLocationSearchBar.TextChanged += async (sender, e) =>
			{
				//this is the method that is called when the user searchess
				await RetrieveSuggestionsFromLocator();
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
	}
}