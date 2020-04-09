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
                //UserInteractionEnabled = true, //TODO - needed?
                SearchBarStyle = UISearchBarStyle.Minimal,
                ShowsCancelButton = true
            };

            _headerLabel = new UILabel
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                TextColor = UIColor.LabelColor,
                Font = UIFont.BoldSystemFontOfSize(28)
            };

            AddSubviews(_headerLabel, _searchBar, _autoSuggestionsTableView);

            NSLayoutConstraint.ActivateConstraints(new[]
            {
                _headerLabel.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _headerLabel.TopAnchor.ConstraintEqualTo(this.TopAnchor, 8),
                _headerLabel.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -8),
                // search bar not given margins because it gives itself so much extra space
                _searchBar.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor),
                _searchBar.TopAnchor.ConstraintEqualTo(_headerLabel.BottomAnchor),
                _searchBar.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor),
                //
                _autoSuggestionsTableView.LeadingAnchor.ConstraintEqualTo(this.LeadingAnchor, 8),
                _autoSuggestionsTableView.TopAnchor.ConstraintEqualTo(_searchBar.BottomAnchor, 8),
                _autoSuggestionsTableView.TrailingAnchor.ConstraintEqualTo(this.TrailingAnchor, -8),
                //
                this.BottomAnchor.ConstraintEqualTo(_autoSuggestionsTableView.BottomAnchor, -8)
            });

            _searchBar.TextChanged += search_textChanged;
            _searchBar.SearchButtonClicked += search_buttonClicked;
            _searchBar.OnEditingStarted += search_editingStarted;
            _searchBar.CancelButtonClicked += search_CancelClicked;

            _suggestionSource.TableRowSelected += suggestion_Selected;

            AppStateViewModel.Instance.PropertyChanged += AppState_Changed;
        }

        private void AppState_Changed(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(AppStateViewModel.CurrentSearchTarget))
            {
                return;
            }

            _searchBar.Text = string.Empty;

            _headerLabel.Text = TitleForSelectedField();
        }

        private string TitleForSelectedField()
        {
            switch (AppStateViewModel.Instance.CurrentSearchTarget)
            {
                case AppStateViewModel.TargetSearchField.Feature:
                    return "Find a room or person";
                case AppStateViewModel.TargetSearchField.Origin:
                    return "Select origin";
                case AppStateViewModel.TargetSearchField.Destination:
                    return "Select destination";
            }
            throw new NotImplementedException();
        }

        private void suggestion_Selected(object sender, TableRowSelectedEventArgs<string> e)
        {
            _searchBar.Text = e.SelectedItem;

            // TODO - think about moving this
            UpdateTargetText(_searchBar.Text);

            AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.SearchFinished);
        }

        private void UpdateTargetText(string text)
        {
            switch (AppStateViewModel.Instance.CurrentSearchTarget)
            {
                case AppStateViewModel.TargetSearchField.Feature:
                    AppStateViewModel.Instance.FeatureSearchText = text;
                    break;
                case AppStateViewModel.TargetSearchField.Destination:
                    AppStateViewModel.Instance.DestinationSearchText = text;
                    break;
                case AppStateViewModel.TargetSearchField.Origin:
                    AppStateViewModel.Instance.OriginSearchText = text;
                    break;
            }
        }

        private void search_CancelClicked(object sender, EventArgs e) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.AwaitingSearch);

        private void search_editingStarted(object sender, EventArgs e) => AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.SearchInProgress);

        private void search_buttonClicked(object sender, EventArgs e)
        {
            UpdateTargetText(_searchBar.Text);
            AppStateViewModel.Instance.TransitionToState(AppStateViewModel.UIState.SearchFinished);
        }

        private async void search_textChanged(object sender, UISearchBarTextChangedEventArgs e)
        {
            var results = await LocationViewModel.Instance.GetLocationSuggestionsAsync(e.SearchText);
            _suggestionSource.UpdateSuggestions(results);
            _autoSuggestionsTableView.ReloadData();
        }
    }
}
