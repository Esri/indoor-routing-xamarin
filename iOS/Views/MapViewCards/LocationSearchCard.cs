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

using CoreGraphics;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Models;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.Controls;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using System;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views.MapViewCards
{
    /// <summary>
    /// Shows a card with a search field with search-as-you-type.
    /// Used for finding a location, as well as origin or destination when planning a route.
    /// </summary>
    public sealed class LocationSearchCard : UIView
    {
        private readonly MapViewModel _viewModel;

        private readonly SelfSizedTableView _autoSuggestionsTableView;
        private readonly AutosuggestionsTableSource _suggestionSource;
        private readonly UISearchBar _searchBar;
        private readonly UILabel _headerLabel;

        private readonly NSLayoutConstraint _headerHeightConstraint;
        private readonly NSLayoutConstraint _tableviewBottomConstraint;

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

            AddSubviews(_headerLabel, _searchBar, _autoSuggestionsTableView);

            _headerHeightConstraint = _headerLabel.HeightAnchor.ConstraintGreaterThanOrEqualTo(40);
            _tableviewBottomConstraint = _autoSuggestionsTableView.BottomAnchor.ConstraintLessThanOrEqualTo(BottomAnchor, -ApplicationTheme.Margin * 2);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _headerLabel.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, ApplicationTheme.Margin),
                _headerLabel.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -ApplicationTheme.Margin),
                _headerLabel.TopAnchor.ConstraintEqualTo(TopAnchor, ApplicationTheme.Margin),
                _searchBar.LeadingAnchor.ConstraintEqualTo(_headerLabel.LeadingAnchor),
                _searchBar.TrailingAnchor.ConstraintEqualTo(_headerLabel.TrailingAnchor),
                _searchBar.TopAnchor.ConstraintEqualTo(_headerLabel.BottomAnchor),
                _searchBar.HeightAnchor.ConstraintEqualTo(56),
                _autoSuggestionsTableView.TopAnchor.ConstraintEqualTo(_searchBar.BottomAnchor),
                _autoSuggestionsTableView.LeadingAnchor.ConstraintEqualTo(_headerLabel.LeadingAnchor),
                _autoSuggestionsTableView.TrailingAnchor.ConstraintEqualTo(_headerLabel.TrailingAnchor),
                BottomAnchor.ConstraintGreaterThanOrEqualTo(_searchBar.BottomAnchor, ApplicationTheme.Margin),
            });

            _searchBar.TextChanged += Search_textChanged;
            _searchBar.SearchButtonClicked += Search_buttonClicked;
            _searchBar.OnEditingStarted += Search_EditingStarted;
            _searchBar.CancelButtonClicked += Search_CancelClicked;

            _suggestionSource.TableRowSelected += SuggestionSource_RowSelected;

            _viewModel.PropertyChanged += ViewModel_PropertyChanged;
        }

        /// <summary>
        /// Updates the UI in response to viewmodel property changes
        /// </summary>
        private void ViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentState))
            {
                return;
            }
            switch (_viewModel.CurrentState)
            {
                case UiState.SearchingForDestination:
                    _headerLabel.Text = "LocationSearchHeaderForDestinationSearch".Localize();
                    _headerLabel.Hidden = false;
                    _searchBar.Text = _viewModel.DestinationSearchText;
                    _searchBar.ShowsCancelButton = true;
                    _headerHeightConstraint.Active = true;
                    _tableviewBottomConstraint.Active = true;
                    // Focus the cursor on the search bar and show the keyboard
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                case UiState.SearchingForFeature:
                    _headerLabel.Hidden = true;
                    _headerLabel.Text = string.Empty;
                    _headerHeightConstraint.Active = false;
                    _searchBar.Text = _viewModel.FeatureSearchText;
                    _searchBar.ShowsCancelButton = true;
                    _tableviewBottomConstraint.Active = true;
                    // Focus the cursor on the search bar and show the keyboard
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                case UiState.SearchingForOrigin:
                    _headerLabel.Text = "LocationSearchHeaderForOriginSearch".Localize();
                    _headerLabel.Hidden = false;
                    _searchBar.Text = _viewModel.OriginSearchText;
                    _searchBar.ShowsCancelButton = true;
                    _headerHeightConstraint.Active = true;
                    _tableviewBottomConstraint.Active = true;
                    // Focus the cursor on the search bar and show the keyboard
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                default:
                    _headerLabel.Hidden = true;
                    _headerLabel.Text = string.Empty;
                    _autoSuggestionsTableView.Hidden = true;
                    _searchBar.Text = null;
                    _searchBar.ShowsCancelButton = false;
                    _headerHeightConstraint.Active = false;
                    _tableviewBottomConstraint.Active = false;
                    // Remove focus from the search bar and hide the keyboard whenever a search isn't in progress
                    _searchBar.ResignFirstResponder();
                    break;
            }
            InvalidateIntrinsicContentSize();
        }

        /// <summary>
        /// Resets the text in the search bar and hides the suggestion view
        /// </summary>
        private void CancelEditing()
        {
            _searchBar.Text = null;
            _autoSuggestionsTableView.Hidden = true;

            InvalidateIntrinsicContentSize();

            // Notify the viewmodel
            _viewModel.StopEditingInLocationSearch();
        }

        /// <summary>
        /// Gets new suggestions based on the search bar text
        /// </summary>
        private async void UpdateTableView()
        {
            try
            {
                _suggestionSource.UpdateSuggestions(await _viewModel.GetLocationSuggestionsAsync(_searchBar.Text));
                _autoSuggestionsTableView.ReloadData();
                _autoSuggestionsTableView.Hidden = false;
                InvalidateIntrinsicContentSize();
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }
        }

        /// <summary>
        /// Handle selection of a search suggestion
        /// </summary>
        private async void SuggestionSource_RowSelected(object sender, TableRowSelectedEventArgs<string> e)
        {
            // Update the search bar with the selected item
            _searchBar.Text = e.SelectedItem;

            try
            {
                // Commit the search with the viewmodel
                await _viewModel.CommitSearchAsync(_searchBar.Text);
            }
            catch (Exception ex)
            {
                ErrorLogger.Instance.LogException(ex);
            }

            // Remove focus from the search bar, which will hide the keyboard.
            // This has to be done last, otherwise search will be canceled.
            _searchBar.ResignFirstResponder();
        }

        /// <summary>
        /// Handle canceling edits in the search field
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Search_CancelClicked(object sender, EventArgs e) => CancelEditing();

        /// <summary>
        /// Notify the viewmodel that a search started
        /// </summary>
        private void Search_EditingStarted(object sender, EventArgs e) => _viewModel.StartEditingInLocationSearch();

        /// <summary>
        /// Commit the search to the viewmodel
        /// </summary>
        private void Search_buttonClicked(object sender, EventArgs e) => _ = _viewModel.CommitSearchAsync(_searchBar.Text);

        /// <summary>
        /// Update suggestions when text changes.
        /// </summary>
        private void Search_textChanged(object sender, UISearchBarTextChangedEventArgs e) => UpdateTableView();

        public override CGSize IntrinsicContentSize
        {
            get
            {
                var width = _searchBar.Frame.Width;
                nfloat height = 56f + ApplicationTheme.Margin; // search bar height

                if (_headerHeightConstraint.Active)
                {
                    height += _headerHeightConstraint.Constant;
                    height += ApplicationTheme.Margin;
                }

                if (!_autoSuggestionsTableView.Hidden)
                {
                    height += _autoSuggestionsTableView.IntrinsicContentSize.Height;
                }

                return new CGSize(width, height);
            }
        }
    }
}
