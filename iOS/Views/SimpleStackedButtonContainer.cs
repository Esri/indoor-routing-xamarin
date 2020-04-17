using System;
using System.Collections.Generic;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class SimpleStackedButtonContainer : SelfSizedTableView
    {
        private UIVisualEffectView _visualEffectBackground;
        public SimpleStackedButtonContainer(IEnumerable<UIButton> buttons)
        {
            Source = new AccessoryTableSource(buttons);
            BackgroundColor = UIColor.Clear;
            ScrollEnabled = false;
            AllowsSelection = false;
            SeparatorColor = UIColor.OpaqueSeparatorColor;

            _visualEffectBackground = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial));
            BackgroundView = _visualEffectBackground;
        }
    }

    internal class AccessoryViewCell : UITableViewCell
    {
        internal UIButton _containedButton;
        public AccessoryViewCell()
        {
            // TODO - document `AccessoryButtonColor`
            TintColor = UIColor.FromName("AccessoryButtonColor");
            BackgroundColor = UIColor.Clear;
        }

        public void SetButton(UIButton button)
        {
            _containedButton = button;

            ContentView.AddSubview(_containedButton);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _containedButton.LeadingAnchor.ConstraintEqualTo(ContentView.LeadingAnchor),
                _containedButton.TrailingAnchor.ConstraintEqualTo(ContentView.TrailingAnchor),
                _containedButton.BottomAnchor.ConstraintEqualTo(ContentView.BottomAnchor),
                _containedButton.TopAnchor.ConstraintEqualTo(ContentView.TopAnchor)
            });
        }
    }

    internal class AccessoryTableSource : UITableViewSource
    {
        /// <summary>
        /// The items in the table.
        /// </summary>
        private readonly IEnumerable<UIButton> items;

        /// <summary>
        /// The cell identifier.
        /// </summary>
        private readonly string cellIdentifier;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.FloorsTableSource"/> class.
        /// </summary>
        /// <param name="items">Table Items.</param>
        internal AccessoryTableSource(IEnumerable<UIButton> items)
        {
            this.items = items;
            this.cellIdentifier = "cell_id";
        }

        /// <summary>
        /// Occurs when table row selected.
        /// </summary>
        public event EventHandler<NSIndexPath> TableRowSelected;

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
                return this.items.Where(_buttonInclusionPredicate).Count();
            }
            catch
            {
                return 0;
            }
        }

        private Func<UIButton, bool> _buttonInclusionPredicate = button => !button.Hidden && button.Enabled;

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(this.cellIdentifier) as AccessoryViewCell;

            // If there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new AccessoryViewCell();
                cell.LayoutMargins = UIEdgeInsets.Zero;
                cell.PreservesSuperviewLayoutMargins = false;
                cell.SeparatorInset = UIEdgeInsets.Zero;
            }

            try
            {
                var item = this.items.Where(_buttonInclusionPredicate).ElementAt(indexPath.Row);

                if (cell._containedButton != item)
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
            this.OnTableRowSelected(indexPath);
            tableView.DeselectRow(indexPath, false);
        }

        /// <summary>
        /// Get the tableview item the user selected and call event handler
        /// </summary>
        /// <param name="itemIndexPath">Item index path.</param>
        private void OnTableRowSelected(NSIndexPath itemIndexPath)
        {
            try
            {
                var item = this.items.ElementAt(itemIndexPath.Row);
                this.TableRowSelected?.Invoke(this, itemIndexPath);
            }
            catch
            {
            }
        }
    }
}
