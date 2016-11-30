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
    public partial class HomeLocationController : UIViewController
    {

        public HomeLocationController (IntPtr handle) : base (handle)
        {
			
        }

		public override void ViewWillAppear(bool animated)
		{
			this.NavigationController.NavigationBarHidden = false;
			base.ViewWillAppear(animated);

		}

		public override void ViewDidLoad()
		{
			base.ViewDidLoad();
			HomeLocationTextField.BecomeFirstResponder();
			// Set observer when text changes to call the TextChangedEvent
			NSNotificationCenter.DefaultCenter.AddObserver(UITextField.TextFieldTextDidChangeNotification, TextChangedEvent);
		}

		/// <summary>
		/// Called when the text in the textbox changes. Triggers autosuggestions if available
		/// </summary>
		/// <param name="notification">Notification.</param>
		private void TextChangedEvent(NSNotification notification)
		{
			UITextField field = (UITextField)notification.Object;

			if (notification.Object == HomeLocationTextField)
			{
				RetrieveSuggestionsFromLocator();
			}
		}

		/// <summary>
		/// Retrieves the suggestions from locator and displays them in a tableview below the textbox.
		/// </summary>
	    public async void RetrieveSuggestionsFromLocator()
		{
			var suggestions = await LocationHelper.GetLocationSuggestions(HomeLocationTextField.Text);
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

				// Auto extend ot shrink the tableview based on the content inside
        		CGRect frame = AutosuggestionsTableView.Frame;
				frame.Height = AutosuggestionsTableView.ContentSize.Height;
        		AutosuggestionsTableView.Frame = frame;


			}
		}
	}

	/// <summary>>
	/// Class handling the source data for the autosuggestions TableView 
	/// </summary>
	public class AutosuggestionsTableSource : UITableViewSource
	{
		SuggestResult[] TableItems;
		string CellIdentifier = "cell_id";

		public AutosuggestionsTableSource(IReadOnlyList<SuggestResult> items)
		{
			TableItems = items.ToArray();
		}

		/// <summary>
		/// Called by the TableView to determine how many cells to create for that particular section.
		/// </summary>
		public override nint RowsInSection(UITableView tableview, nint section)
		{
			return TableItems.Length;
		}

		/// <summary>
		/// Called by the TableView to get the actual UITableViewCell to render for the particular row
		/// </summary>
		public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
		{
			UITableViewCell cell = tableView.DequeueReusableCell(CellIdentifier);
			var item = TableItems[indexPath.Row];

			//---- if there are no cells to reuse, create a new one
			if (cell == null)
			{ cell = new UITableViewCell(UITableViewCellStyle.Default, CellIdentifier); }


			cell.TextLabel.Text = item.Label;

			return cell;
		}

		public override async void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			var selectedLocation = this.TableItems[indexPath.Row];

			GlobalSettings.currentSettings.HomeLocation = selectedLocation.Label;
			var homeLocation = await LocationHelper.GetSearchedLocation(selectedLocation.Label);


			// Save extent of home location to Settings file
			CoordinatesKeyValuePair<string, double>[] homeCoordinates = 
			{ 
				new CoordinatesKeyValuePair<string, double>("X", homeLocation.DisplayLocation.X), 
				new CoordinatesKeyValuePair<string, double>("Y", homeLocation.DisplayLocation.Y),
				new CoordinatesKeyValuePair<string, double>("WKID", homeLocation.DisplayLocation.SpatialReference.Wkid)
			};

			GlobalSettings.currentSettings.HomeCoordinates = homeCoordinates;

			// Save user settings
			string settingsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
			await Task.Run(() => AppSettings.SaveSettings(Path.Combine(settingsPath, "AppSettings.xml")));

		}
	}
}