﻿using System;
using System.Linq;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Foundation;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class FloorsTableView : SelfSizedTableView
    {
        private MapViewModel _viewModel;

        public FloorsTableView(MapViewModel viewModel) : base()
        {
            _viewModel = viewModel;

            Layer.CornerRadius = 8;
            SeparatorColor = UIColor.SystemGrayColor;
            BackgroundColor = UIColor.Clear;
            BackgroundView = new UIVisualEffectView(UIBlurEffect.FromStyle(UIBlurEffectStyle.SystemMaterial));

            Source = new FloorsTableSource(viewModel);

            _viewModel.PropertyChanged += _viewModel_PropertyChanged;
            _viewModel.CurrentVisibleFloors.CollectionChanged += CurrentVisibleFloors_CollectionChanged;
        }

        private void CurrentVisibleFloors_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            BeginInvokeOnMainThread(ReloadData);
        }

        private void _viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            BeginInvokeOnMainThread(() => {
                if (e.PropertyName == nameof(MapViewModel.SelectedFloorLevel) && _viewModel.CurrentVisibleFloors.Any())
                {

                    var selectedFloorNSIndex = GetTableViewRowIndex(_viewModel.SelectedFloorLevel, _viewModel.CurrentVisibleFloors.ToArray(), 0);
                    if (selectedFloorNSIndex != null)
                    {
                        SelectRow(selectedFloorNSIndex, false, UITableViewScrollPosition.None);
                    }
                }
            });
        }

        /// <summary>
        /// Gets the index of the table view row.
        /// </summary>
        /// <returns>The table view row index.</returns>
        /// <param name="rowValue">Row value.</param>
        /// <param name="tableSource">Table source.</param>
        /// <param name="section">TableView Section.</param>
        private NSIndexPath GetTableViewRowIndex(string rowValue, string[] tableSource, nint section)
        {
            var rowIndex = tableSource.Select((rowItem, index) => new { rowItem, index }).FirstOrDefault(i => i.rowItem == rowValue)?.index;
            if (rowIndex.HasValue)
            {
                return NSIndexPath.FromRowSection(rowIndex.Value, section);
            }
            else
            {
                return null;
            }
        }
    }
}
