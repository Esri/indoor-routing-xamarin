using System;
using System.Threading.Tasks;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    public class DismissableNavigationController : UINavigationController
    {
        public DismissableNavigationController(UIViewController controller) : base(controller)
        {
            NavigationBar.TintColor = ApplicationTheme.ActionBackgroundColor;
        }

        public override void DismissModalViewController(bool animated)
        {
            DidDismiss?.Invoke(this, EventArgs.Empty);
            base.DismissModalViewController(animated);
        }

        public override Task DismissViewControllerAsync(bool animated)
        {
            DidDismiss?.Invoke(this, EventArgs.Empty);
            return base.DismissViewControllerAsync(animated);
        }

        public event EventHandler DidDismiss;
    }
}
