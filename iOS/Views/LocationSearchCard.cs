using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Helpers;
using Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.ViewModels;
using Esri.ArcGISRuntime.Tasks.Geocoding;
using UIKit;

namespace Esri.ArcGISRuntime.OpenSourceApps.IndoorRouting.iOS.Views
{
    public class LocationSearchCard : UIView
    {
        private SelfSizedTableView _autoSuggestionsTableView;
        private AutosuggestionsTableSource _suggestionSource;
        private UISearchBar _searchBar;
        private UILabel _headerLabel;

        private UIStackView _containerStack;

        private const float searchBarMarginAdjustment = 4;

        public LocationSearchCard()
        {
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
                BottomAnchor.ConstraintEqualTo(_containerStack.BottomAnchor)
            });

            _searchBar.TextChanged += search_textChanged;
            _searchBar.SearchButtonClicked += search_buttonClicked;
            _searchBar.OnEditingStarted += search_editingStarted;
            _searchBar.OnEditingStopped += search_EditingStopped;
            _searchBar.CancelButtonClicked += search_CancelClicked;

            _suggestionSource.TableRowSelected += suggestion_Selected;

            AppStateViewModel.Instance.DidTransitionToState += AppState_Transitioned;
        }

        private async void AppState_Transitioned(object sender, AppStateViewModel.UIState e)
        {
            switch (e)
            {
                case AppStateViewModel.UIState.ReadyWaiting:
                    _headerLabel.Hidden = true;
                    _searchBar.ShowsCancelButton = false;
                    _autoSuggestionsTableView.Hidden = true;
                    _searchBar.Text = string.Empty;
                    return;
                case AppStateViewModel.UIState.SearchingForDestination:
                    _headerLabel.Text = "Select Destination";
                    _headerLabel.Hidden = false;
                    _searchBar.Text = AppStateViewModel.Instance.DestinationSearchText;
                    _searchBar.BecomeFirstResponder();
                    await UpdateTableView();
                    return;
                case AppStateViewModel.UIState.SearchingForFeature:
                    _headerLabel.Hidden = true;
                    _searchBar.ShowsCancelButton = true;
                    _searchBar.Text = AppStateViewModel.Instance.FeatureSearchText;
                    await UpdateTableView();
                    return;
                case AppStateViewModel.UIState.SearchingForOrigin:
                    _headerLabel.Text = "Select Origin";
                    _headerLabel.Hidden = false;
                    _searchBar.Text = AppStateViewModel.Instance.OriginSearchText;
                    _searchBar.BecomeFirstResponder();
                    await UpdateTableView();
                    return;
            }
        }

        private void suggestion_Selected(object sender, TableRowSelectedEventArgs<string> e)
        {
            _searchBar.Text = e.SelectedItem;
            CommitSearch(_searchBar.Text);
        }

        private void CommitSearch(string text)
        {
            switch (AppStateViewModel.Instance.CurrentState)
            {
                case AppStateViewModel.UIState.SearchingForFeature:
                    AppStateViewModel.Instance.FeatureSearchText = text;
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.FeatureSearchEntered);
                    break;
                case AppStateViewModel.UIState.SearchingForOrigin:
                    AppStateViewModel.Instance.OriginSearchText = text;
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.PlanningRoute);
                    break;
                case AppStateViewModel.UIState.SearchingForDestination:
                    AppStateViewModel.Instance.DestinationSearchText = text;
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.PlanningRoute);
                    break;
            }
            // has to be done after, otherwise editing will be canceled
            _searchBar.ResignFirstResponder();
        }

        private void search_CancelClicked(object sender, EventArgs e) => CancelEditing();

        private async void search_editingStarted(object sender, EventArgs e)
        {
            if (AppStateViewModel.Instance.CurrentState == AppStateViewModel.UIState.ReadyWaiting)
            {
                AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.SearchingForFeature);
            }

            await UpdateTableView();
        }

        private void CancelEditing()
        {
            _autoSuggestionsTableView.Hidden = true;
            switch (AppStateViewModel.Instance.CurrentState)
            {
                case AppStateViewModel.UIState.SearchingForDestination:
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.PlanningRoute);
                    break;
                case AppStateViewModel.UIState.SearchingForOrigin:
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.PlanningRoute);
                    break;
                case AppStateViewModel.UIState.SearchingForFeature:
                    AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.ReadyWaiting);
                    break;
            }
            _searchBar.ResignFirstResponder();
        }

        private async Task UpdateTableView()
        {
            var results = await LocationViewModel.Instance.GetLocationSuggestionsAsync(_searchBar.Text);
            _suggestionSource.UpdateSuggestions(results);
            _autoSuggestionsTableView.ReloadData();
            _autoSuggestionsTableView.Hidden = false;
        }

        private void search_EditingStopped(object sender, EventArgs e) => CancelEditing();

        private void search_buttonClicked(object sender, EventArgs e) => CommitSearch(_searchBar.Text);

        private async void search_textChanged(object sender, UISearchBarTextChangedEventArgs e)
        {
            await UpdateTableView();
        }
    }
}
