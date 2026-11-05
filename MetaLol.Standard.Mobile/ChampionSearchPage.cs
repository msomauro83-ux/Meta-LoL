using MetaLol.Desktop;

namespace MetaLol.Mobile;

public sealed class ChampionSearchPage : ContentPage
{
    private readonly IReadOnlyList<Champion> _catalog;
    private readonly CollectionView _list;
    private bool _closing;

    public ChampionSearchPage(string title, IReadOnlyList<Champion> catalog, Action<Champion> selected)
    {
        _catalog = catalog;
        SafeAreaEdges = Microsoft.Maui.SafeAreaEdges.All;
        BackgroundColor = Color.FromArgb("#07111F");
        var search = new SearchBar { Placeholder = "Buscar campeón…", TextColor = Colors.White, PlaceholderColor = Color.FromArgb("#8EA4B9"), BackgroundColor = Color.FromArgb("#101F33") };
        var close = new Button { Text = "Volver", BackgroundColor = Color.FromArgb("#183A55"), TextColor = Colors.White };
        close.Clicked += async (_, _) => { if (!_closing) { _closing = true; await Navigation.PopModalAsync(); } };
        _list = new CollectionView
        {
            ItemsSource = catalog, SelectionMode = SelectionMode.Single,
            ItemTemplate = new DataTemplate(() =>
            {
                var label = new Label { TextColor = Colors.White, FontSize = 17, Padding = new Thickness(14, 16), MinimumHeightRequest = 56, VerticalTextAlignment = TextAlignment.Center };
                label.SetBinding(Label.TextProperty, nameof(Champion.Name));
                return label;
            })
        };
        _list.SelectionChanged += async (_, args) =>
        {
            if (_closing || args.CurrentSelection.FirstOrDefault() is not Champion champion) return;
            _closing = true;
            selected(champion);
            await Navigation.PopModalAsync();
        };
        search.TextChanged += (_, args) => _list.ItemsSource = ManualDraft.Search(_catalog, args.NewTextValue);
        var grid = new Grid { Padding = new Thickness(16, 12), RowSpacing = 12, RowDefinitions = { new(GridLength.Auto), new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) } };
        grid.Add(new Label { Text = title, FontSize = 22, FontAttributes = FontAttributes.Bold, TextColor = Colors.White, LineBreakMode = LineBreakMode.WordWrap });
        grid.Add(search, 0, 1); grid.Add(_list, 0, 2); grid.Add(close, 0, 3);
        Content = grid;
    }
}
