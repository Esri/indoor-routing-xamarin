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

using CoreGraphics;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls
{
    /// <summary>
    /// Tableview that uses its content size as its IntrinsicContentSize
    /// </summary>
    /// <remarks>This allows the floors tableview to take up only the space it needs in the accessory area stack view</remarks>
    public class SelfSizedTableView : UITableView
    {
        public SelfSizedTableView()
        {
            // Get more accurate height values from `ContentSize`
            EstimatedRowHeight = 0;
            EstimatedSectionFooterHeight = 0;
            EstimatedSectionHeaderHeight = 0;
        }

        public override void ReloadData()
        {
            base.ReloadData();

            // Whenever the data changes, the intrinsic content size needs to be recalculated
            InvalidateIntrinsicContentSize();
            LayoutIfNeeded();
            InvalidateIntrinsicContentSize();
        }

        public override CGSize IntrinsicContentSize => ContentSize;
    }
}
