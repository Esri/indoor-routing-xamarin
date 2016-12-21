using Foundation;
using System;
using UIKit;

namespace IndoorNavigation.iOS
{
    public partial class SettingsController : UITableViewController
    {
        public SettingsController (IntPtr handle) : base (handle)
        {
        }

		public override void ViewWillAppear(bool animated)
		{
			this.NavigationController.NavigationBarHidden = false;
			HomeLocationLabel.Text = GlobalSettings.currentSettings.HomeLocation;

			base.ViewWillAppear(animated);
		} 
    }
}