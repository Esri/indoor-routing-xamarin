// Copyright 2020 Esri.

// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at

// https://www.apache.org/licenses/LICENSE-2.0

// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers
{
    /// <summary>
    /// Convenience extension methods
    /// </summary>
    public static class ViewExtensions
    {
        /// <summary>
        /// Returns the view wrapped in a shadow
        /// </summary>
        /// <param name="viewToEncapsulate">View to place a shadow around</param>
        /// <returns>ShadowContainerView containing the viewToEncapsulate</returns>
        public static UIView EncapsulateInShadowView(this UIView viewToEncapsulate)
        {
            return new ShadowContainerView(viewToEncapsulate);
        }
    }
}
