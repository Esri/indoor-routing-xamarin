using System;
using UIKit;

namespace IndoorNavigation.iOS
{
	/// <summary>
	/// Controller handles the UI and logic for the Settings screen
	/// </summary>
    partial class SettingsController : UITableViewController
    {
		SettingsController(IntPtr handle) : base(handle)
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
			// Set the label for the home location from settings
			HomeLocationLabel.Text = AppSettings.currentSettings.HomeLocation;

			base.ViewWillAppear(animated);
		} 
    }
}