﻿using System;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using UIKit;
using static Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.MapViewModel;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class LocationSearchCard : UIView
    {
        private MapViewModel _viewModel;

        private SelfSizedTableView _autoSuggestionsTableView;
        private AutosuggestionsTableSource _suggestionSource;
        private UISearchBar _searchBar;
        private UILabel _headerLabel;

        private UIStackView _containerStack;

        internal LocationSearchCard(MapViewModel viewModel)
        {
            _viewModel = viewModel;

            _suggestionSource = new AutosuggestionsTableSource(null, true);
            _autoSuggestionsTableView = new SelfSizedTableView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Hidden = false,
                BackgroundColor = UIColor.Clear,
                SeparatorColor = UIColor.SystemGrayColor,
                Source = _suggestionSource
            };

            _searchBar = new UISearchBar
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                BackgroundImage = new UIImage(),
                Placeholder = "LocationSearchBarPlaceholder".AsLocalized(),
                SearchBarStyle = UISearchBarStyle.Minimal,
                ShowsCancelButton = false
            };

            _headerLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor,
                Font = UIFont.BoldSystemFontOfSize(28)
            };

            _containerStack = new UIStackView
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Axis = UILayoutConstraintAxis.Vertical,
            };

            _containerStack.AddArrangedSubview(_headerLabel);
            _containerStack.AddArrangedSubview(_searchBar);
            _containerStack.AddArrangedSubview(_autoSuggestionsTableView);

            AddSubviews(_containerStack);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _containerStack.LeadingAnchor.ConstraintEqualTo(LeadingAnchor, 8),
                _containerStack.TrailingAnchor.ConstraintEqualTo(TrailingAnchor, -8),
                _containerStack.TopAnchor.ConstraintEqualTo(TopAnchor, 8),
                BottomAnchor.ConstraintEqualTo(_containerStack.BottomAnchor, 8)
            });

            _searchBar.TextChanged += search_textChanged;
            _searchBar.SearchButtonClicked += search_buttonClicked;
            _searchBar.OnEditingStarted += search_editingStarted;
            _searchBar.OnEditingStopped += search_EditingStopped;
            _searchBar.CancelButtonClicked += search_CancelClicked;

            _suggestionSource.TableRowSelected += suggestion_Selected;

            _viewModel.PropertyChanged += viewModel_PropertyChanged;
        }

        private void viewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(_viewModel.CurrentState))
            {
                return;
            }

            switch (_viewModel.CurrentState)
            {
                case UIState.SearchingForDestination:
                    _headerLabel.Text = "Select Destination";
                    _headerLabel.Hidden = false;
                    _searchBar.Text = _viewModel.DestinationSearchText;
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                case UIState.SearchingForFeature:
                    _headerLabel.Hidden = true;
                    _searchBar.ShowsCancelButton = true;
                    _searchBar.Text = _viewModel.FeatureSearchText;
                    _searchBar.BecomeFirstResponder();
                    UpdateTableView();
                    return;
                case UIState.SearchingForOrigin:
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

        private async void suggestion_Selected(object sender, TableRowSelectedEventArgs<string> e)
        {
            _searchBar.Text = e.SelectedItem;
            _viewModel.FeatureSearchText = string.Empty;
            try
            {
                await _viewModel.CommitSearchAsync(_searchBar.Text);
            }
            catch (Exception)
            {
                // TODO - log exception
            }
            // has to be done after, otherwise editing will be canceled
            _searchBar.ResignFirstResponder();
        }

        private void search_CancelClicked(object sender, EventArgs e) => CancelEditing();

        private void search_editingStarted(object sender, EventArgs e) => _viewModel.StartEditingInLocationSearch();

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
                var results = await _viewModel.GetLocationSuggestionsAsync(_searchBar.Text);
                _suggestionSource.UpdateSuggestions(results);
                _autoSuggestionsTableView.ReloadData();
                _autoSuggestionsTableView.Hidden = false;
            }
            catch (Exception)
            {
                // TODO - log exceptions
            }
        }

        private void search_EditingStopped(object sender, EventArgs e) => CancelEditing();

        private void search_buttonClicked(object sender, EventArgs e) => _viewModel.CommitSearchAsync(_searchBar.Text);

        private void search_textChanged(object sender, UISearchBarTextChangedEventArgs e)
        {
            UpdateTableView();
        }
    }
}
