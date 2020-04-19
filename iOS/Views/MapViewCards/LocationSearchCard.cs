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

using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.MapViewCards
{
    public sealed class LocationSearchCard : UIView
    {
        private readonly MapViewModel _viewModel;

        private readonly SelfSizedTableView _autoSuggestionsTableView;
        private readonly AutosuggestionsTableSource _suggestionSource;
        private readonly UISearchBar _searchBar;
        private readonly UILabel _headerLabel;

        internal LocationSearchCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _suggestionSource = new AutosuggestionsTableSource(null, true);
            _autoSuggestionsTableView = new SelfSizedTableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = true, // must be hidden by default for voiceover
                BackgroundColor = UIColor.Clear,
                SeparatorColor = UIColor.SystemGrayColor,
                Source = _suggestionSource,
            };

            _searchBar = new UISearchBar
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundImage = new UIImage(),
                Placeholder = "LocationSearchBarPlaceholder".Localize(),
                SearchBarStyle = UISearchBarStyle.Minimal,
                ShowsCancelButton = false,
                TintColor = ApplicationTheme.ActionBackgroundColor
            };

            _headerLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = ApplicationTheme.ForegroundColor,
                Font = ApplicationTheme.HeaderFont
            };

            var containerStack = new UIStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical,
            };

            containerStack.AddArrangedSubview(_headerLabel);
            containerStack.AddArrangedSubview(_searchBar);
            containerStack.AddArrangedSubview(_autoSuggestionsTableView);

            AddSubviews(containerStack);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                containerStack.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                containerStack.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                containerStack.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                BottomAnchor.ConstraintEqualTo(containerStack.BottomAnchor, ApplicationTheme.Margin)
            });

            _searchBar.TextChanged += Search_textChanged;
            _searchBar.SearchButtonClicked += Search_buttonClicked;
            _searchBar.OnEditingStarted += Search_editingStarted;
            _searchBar.OnEditingStopped += Search_EditingStopped;
            _searchBar.CancelButtonClicked += Search_CancelClicked;

            _suggestionSource.TableRowSelected += SuggestionSource_RowSelected;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentState))
            {
                return;
            }

            switch (_viewModel.CurrentState)
            {
                case UiState.SearchingForDestination:
                    _headerLabel.Text = "Select Destination";
                    _headerLabel.Hidden = false;
                    _searchBar.Text = _viewModel.DestinationSearchText;
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                case UiState.SearchingForFeature:
                    _headerLabel.Hidden = true;
                    _searchBar.ShowsCancelButton = true;
                    _searchBar.Text = _viewModel.FeatureSearchText;
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                case UiState.SearchingForOrigin:
                    _headerLabel.Text = "Select Origin";
                    _headerLabel.Hidden = false;
                    _searchBar.Text = _viewModel.OriginSearchText;
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                default:
                    _headerLabel.Hidden = true;
                    _searchBar.ShowsCancelButton = false;
                    _autoSuggestionsTableView.Hidden = true;
                    _searchBar.Text = string.Empty;
                    _searchBar.ResignFirstResponder();
                    return;

            }
        }

        private void CancelEditing()
        {
            _searchBar.Text = string.Empty;
            _autoSuggestionsTableView.Hidden = true;
            _viewModel.StopEditingInLocationSearch();
        }

        private async void UpdateTableView()
        {
            try
            {
                _suggestionSource.UpdateSuggestions(await _viewModel.GetLocationSuggestionsAsync(_searchBar.Text));
                _autoSuggestionsTableView.ReloadData();
                _autoSuggestionsTableView.Hidden = false;
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        private async void SuggestionSource_RowSelected(object sender, TableRowSelectedEventArgs<string> e)
        {
            _searchBar.Text = e.SelectedItem;
            _viewModel.FeatureSearchText = string.Empty;
            try
            {
                await _viewModel.CommitSearchAsync(_searchBar.Text);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
            // has to be done after, otherwise editing will be canceled
            _searchBar.ResignFirstResponder();
        }

        private void Search_CancelClicked(object sender, EventArgs e) => CancelEditing();

        private void Search_editingStarted(object sender, EventArgs e) => _viewModel.StartEditingInLocationSearch();

        private void Search_EditingStopped(object sender, EventArgs e) => CancelEditing();

        private void Search_buttonClicked(object sender, EventArgs e) => _ = _viewModel.CommitSearchAsync(_searchBar.Text);

        private void Search_textChanged(object sender, UISearchBarTextChangedEventArgs e) => UpdateTableView();
    }
}
