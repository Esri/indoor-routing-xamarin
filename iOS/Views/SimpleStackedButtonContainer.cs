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

using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    /// <summary>
    /// Shows buttons in a stack, used for the accessory panel in the top right
    /// </summary>
    public sealed class SimpleStackedButtonContainer : SelfSizedTableView
    {
        /// <summary>
        /// Creates the stack with the provided buttons.
        /// Call ReloadData to update the view to not show buttons that are hidden or disabled
        /// </summary>
        /// <param name="buttons">Buttons to show, for best result use buttons with images, not text</param>
        public SimpleStackedButtonContainer(IEnumerable<UIButton> buttons)
        {
            Source = new AccessoryTableSource(buttons);
            BackgroundColor = UIColor.Clear;
            ScrollEnabled = false;
            AllowsSelection = false;
            SeparatorColor = ApplicationTheme.SeparatorColor;
            BackgroundView = new UIVisualEffectView(ApplicationTheme.PanelBackgroundMaterial);
        }
    }

    internal sealed class AccessoryViewCell : UITableViewCell
    {
        public AccessoryViewCell()
        {
            TintColor = ApplicationTheme.AccessoryButtonColor;
            BackgroundColor = UIColor.Clear;
        }

        /// <summary>
        /// Button is exposed to enable recycling cells
        /// </summary>
        public UIButton ContainedButton { get; private set; }

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
        /// The cell identifier.
        /// </summary>
        private const string CellIdentifier = "AccessoryButtonCell";

        /// <summary>
        /// The buttons in the table.
        /// </summary>
        private readonly IEnumerable<UIButton> _items;

        /// <summary>
        /// Defines the rules to apply for whether a button should be included in the view
        /// </summary>
        private readonly Func<UIButton, bool> _buttonInclusionPredicate = button => !button.Hidden && button.Enabled;

        /// <summary>
        /// Creates data source for accessory button view
        /// </summary>
        /// <param name="buttons">Buttons to show</param>
        internal AccessoryTableSource(IEnumerable<UIButton> buttons) => _items = buttons;

        /// <summary>
        /// Called by the TableView to determine how many cells to create for that particular section.
        /// </summary>
        /// <returns>The rows in section.</returns>
        /// <param name="tableview">Containing Tableview.</param>
        /// <param name="section">Specific Section.</param>
        public override nint RowsInSection(UITableView tableview, nint section)
        {
            return _items?.Where(_buttonInclusionPredicate).Count() ?? 0;
        }

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(CellIdentifier) as AccessoryViewCell ?? new AccessoryViewCell
            {
                // Remove default margin/padding
                LayoutMargins = UIEdgeInsets.Zero,
                PreservesSuperviewLayoutMargins = false,
                // show separator full width
                SeparatorInset = UIEdgeInsets.Zero
            };

            try
            {
                // Restrict items to only those that match the inclusion criteria
                var item = _items.Where(_buttonInclusionPredicate).ElementAt(indexPath.Row);

                if (cell.ContainedButton != item)
                {
                    cell.SetButton(item);
                }

                return cell;
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
                return null;
            }
        }

        /// <summary>
        /// Automatically deselect any selected row
        /// </summary>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath) => tableView.DeselectRow(indexPath, false);
    }
}
