using Foundation;
using System;
using UIKit;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using System.Collections.Generic;
using System.Linq;

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
			NSNotificationCenter.DefaultCenter.AddObserver(UITextField.TextFieldTextDidChangeNotification, TextChangedEvent);
		}

		 private void TextChangedEvent(NSNotification notification)
		{
			UITextField field = (UITextField)notification.Object;

			if (notification.Object == HomeLocationTextField)
			{
				RetrieveSuggestionsFromLocator();

			}
		}

	    public async void RetrieveSuggestionsFromLocator()
		{
			var suggestions = await LocationHelper.GetLocationSuggestions(HomeLocationTextField.Text);
			// Only show the floors tableview if the buildings in view have more than one floor
			if (suggestions.Count > 1)
			{
				// Show the tableview with autosuggestions and populate it
				AutosuggestionsTableView.Hidden = false;
				AutosuggestionsTableView.Source = new AutosuggestionsTableSource(suggestions);
				InvokeOnMainThread(() => AutosuggestionsTableView.ReloadData());
				AutosuggestionsTableView.SizeToFit();

			}
		}
	}

	/// <summary>>
	/// Class handling the source data for the autosuggestions TableView 
	/// </summary>
	public class AutosuggestionsTableSource : UITableViewSource
	{
		SuggestResult [] TableItems;
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

		public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
		{
			var selectedLocation = this.TableItems[indexPath.Row];
		}
	}
}