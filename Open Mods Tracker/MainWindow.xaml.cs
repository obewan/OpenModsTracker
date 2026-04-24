using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace OpenModsTracker;

public sealed partial class MainWindow : Window
{
    private Type _pageType = typeof(HomePage);

    public MainWindow()
    {
        InitializeComponent();
        AppController.Instance.TrySetDesktopAcrylicBackdrop(this);
    }

    private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            _pageType = typeof(SettingsPage);
        }
        else if (args.SelectedItemContainer is not null)
        {
            _pageType = args.SelectedItemContainer.Tag?.ToString() switch
            {
                "HomePage" => typeof(HomePage),
                "ModsPage" => typeof(ModsPage),
                "AboutPage" => typeof(AboutPage),
                _ => typeof(HomePage)
            };
        }

        contentFrame.Navigate(_pageType);
    }

    private void NavigationView_Loaded(object sender, RoutedEventArgs e)
    {
        contentFrame.Navigate(_pageType);
    }
}
