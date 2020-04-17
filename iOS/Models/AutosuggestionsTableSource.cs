// <copyright file="AutosuggestionsTableSource.cs" company="Esri, Inc">
//      Copyright 2017 Esri.
//
//      Licensed under the Apache License, Version 2.0 (the "License");
//      you may not use this file except in compliance with the License.
//      You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//      Unless required by applicable law or agreed to in writing, software
//      distributed under the License is distributed on an "AS IS" BASIS,
//      WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//      See the License for the specific language governing permissions and
//      limitations under the License.
// </copyright>
// <author>Mara Stoica</author>
namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;
    using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
    using Esri.ArcGISRuntime.Tasks.Geocoding;
    using Foundation;
    using UIKit;

    /// <summary>>
    /// Class handling the source data for the autosuggestions TableView 
    /// </summary>
    internal class AutosuggestionsTableSource : UITableViewSource
    {
        /// <summary>
        /// The items int he table.
        /// </summary>
        private IEnumerable<SuggestResult> items;

        private List<string> _specialSettings = new List<string>();

        /// <summary>
        /// The cell identifier.
        /// </summary>
        private readonly string cellIdentifier;

        public bool ShouldShowSpecialItems { get; set; } = true;

        /// <summary>
        /// Initializes a new instance of the <see cref="T:Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.AutosuggestionsTableSource"/> class.
        /// </summary>
        /// <param name="items">table items.</param>
        internal AutosuggestionsTableSource(IEnumerable<SuggestResult> items, bool showSpecialItems)
        {
            this.items = items ?? new List<SuggestResult>();
            cellIdentifier = "cell_id";
            ResetSpecialSettings();
            ShouldShowSpecialItems = showSpecialItems;
        }

        public void UpdateSuggestions(IEnumerable<SuggestResult> items)
        {
            this.items = items ?? new List<SuggestResult>();
        }

        /// <summary>
        /// Occurs when table row is selected.
        /// </summary>
        public event EventHandler<TableRowSelectedEventArgs<string>> TableRowSelected;

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
                if (_specialSettings.Count() > 0 && ShouldShowSpecialItems && !this.items.Any())
                {
                    return _specialSettings.Count;
                }
                else
                {
                    return this.items.Count();
                }
            }
            catch
            {
                return 0;
            }
        }

        public override nint NumberOfSections(UITableView tableView)
        {
            return 1;
        }

        private void ResetSpecialSettings()
        {
            _specialSettings.Clear();
            if (AppSettings.CurrentSettings.IsLocationServicesEnabled)
            {
                _specialSettings.Add("CurrentLocationLabel".AsLocalized());
            }

            if (!String.IsNullOrWhiteSpace(AppSettings.CurrentSettings.HomeLocation))
            {
                _specialSettings.Add(AppSettings.CurrentSettings.HomeLocation);
            }
        }

        /// <summary>
        /// Called by the TableView to get the actual UITableViewCell to render for the particular row
        /// </summary>
        /// <returns>The cell.</returns>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override UITableViewCell GetCell(UITableView tableView, NSIndexPath indexPath)
        {
            var cell = tableView.DequeueReusableCell(this.cellIdentifier);

            // If there are no cells to reuse, create a new one
            if (cell == null)
            {
                cell = new UITableViewCell(UITableViewCellStyle.Default, this.cellIdentifier);
                cell.BackgroundColor = tableView.BackgroundColor;
            }

            try
            {
                if (_specialSettings.Count() > 0 && indexPath.Section == 0 && ShouldShowSpecialItems && !this.items.Any())
                {
                    cell.TextLabel.Text = _specialSettings[indexPath.Row];
                }
                else
                {
                    var item = this.items.ElementAt(indexPath.Row);
                    cell.TextLabel.Text = item.Label;
                }

                return cell;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// When user selects location from list of autosuggestions, handle selection
        /// </summary>
        /// <param name="tableView">Table view.</param>
        /// <param name="indexPath">Index path.</param>
        public override void RowSelected(UITableView tableView, NSIndexPath indexPath)
        {
            this.OnTableRowSelected(indexPath);
            tableView.DeselectRow(indexPath, false);
        }

        /// <summary>
        /// Override ScrollView behavior to dismiss keyboard on scroll
        /// </summary>
        /// <param name="scrollView">Scroll view.</param>
        public override void Scrolled(UIScrollView scrollView)
        {
            scrollView.KeyboardDismissMode = UIScrollViewKeyboardDismissMode.OnDrag;
        }

        /// <summary>
        /// Handle the table row selection.
        /// </summary>
        /// <param name="itemIndexPath">Item index path.</param>
        private void OnTableRowSelected(NSIndexPath itemIndexPath)
        {
            try
            {
                if (_specialSettings.Count > 0 && itemIndexPath.Section == 0 && ShouldShowSpecialItems && !this.items.Any())
                {
                    var specialItem = _specialSettings[itemIndexPath.Row];
                    this.TableRowSelected?.Invoke(this, new TableRowSelectedEventArgs<string>(specialItem, itemIndexPath));
                }
                else
                {
                    var item = this.items.ElementAt(itemIndexPath.Row);
                    this.TableRowSelected?.Invoke(this, new TableRowSelectedEventArgs<string>(item.Label, itemIndexPath));
                }
            }
            catch 
            { 
            }
        }
    }
}
