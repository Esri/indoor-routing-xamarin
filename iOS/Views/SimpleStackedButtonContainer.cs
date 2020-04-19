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
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public sealed class SimpleStackedButtonContainer : SelfSizedTableView
    {
        public SimpleStackedButtonContainer(IEnumerable<UIButton> buttons)
        {
            Source = new AccessoryTableSource(buttons);
            BackgroundColor = UIColor.Clear;
            ScrollEnabled = false;
            AllowsSelection = false;
            SeparatorColor = UIColor.OpaqueSeparatorColor;

            var visualEffectBackground = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial));
            BackgroundView = visualEffectBackground;
        }
    }

    internal sealed class AccessoryViewCell : UITableViewCell
    {
        internal UIButton ContainedButton;
        public AccessoryViewCell()
        {
            // TODO - document `AccessoryButtonColor`
            TintColor = UIColor.FromName("AccessoryButtonColor");
            BackgroundColor = UIColor.Clear;
        }

        public void SetButton(UIButton button)
        {
            ContainedButton = button;

            ContentView.AddSubview(ContainedButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                ContainedButton.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                ContainedButton.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                ContainedButton.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor),
                ContainedButton.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor)
            });
        }
    }

    internal class AccessoryTableSource : UITableViewSource
    {
        /// <summary>
        /// The items in the table.
        /// </summary>
        private readonly IEnumerable<UIButton> _items;

        /// <summary>
        /// The cell identifier.
        /// </summary>
        private readonly string _cellIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.FloorsTableSource"/> class.
        /// </summary>
        /// <param name="items">Table Items.</param>
        internal AccessoryTableSource(IEnumerable<UIButton> items)
        {
            _items = items;
            _cellIdentifier = "cell_id";
        }

        /// <summary>
        /// Called by the TableView to determine how many cells to create for that particular section.
        /// </summary>
        /// <returns>The rows in section.</returns>
        /// <param name="tableview">Containing Tableview.</param>
        /// <param name="section">Specific Section.</param>
        public override nint RowsInSection(UITableView tableview, nint section)
        {
            try
            {
                return _items.Where(_buttonInclusionPredicate).Count();
            }
            catch
            {
                return 0;
            }
        }

        private readonly Func<UIButton, bool> _buttonInclusionPredicate = button => !button.Hidden && button.Enabled;

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(_cellIdentifier) as AccessoryViewCell ?? new AccessoryViewCell
            {
                LayoutMargins = UIEdgeInsets.Zero,
                PreservesSuperviewLayoutMargins = false,
                SeparatorInset = UIEdgeInsets.Zero
            };

            try
            {
                var item = _items.Where(_buttonInclusionPredicate).ElementAt(indexPath.Row);

                if (cell.ContainedButton != item)
                {
                    cell.SetButton(item);
                }

                // show separator full width
                return cell;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Event for user selecting a floor level
        /// </summary>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            tableView.DeselectRow(indexPath, false);
        }
    }
}
