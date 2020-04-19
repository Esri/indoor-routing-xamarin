// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// http://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Threading.Tasks;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Controllers
{
    public sealed class DismissableNavigationController : UINavigationController
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
