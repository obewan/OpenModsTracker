using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;

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
                "StatsPage" => typeof(StatsPage),
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

    private void HeaderLogoImage_ActualThemeChanged(FrameworkElement sender, object args)
    {
        UpdateHeaderLogo();
    }

    private void HeaderLogoImage_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateHeaderLogo();
    }

    private void UpdateHeaderLogo()
    {
        BitmapImage bitmapImage = new BitmapImage();
        if (App.Current.RequestedTheme == ApplicationTheme.Dark)
        {
            bitmapImage.UriSource = new Uri(HeaderLogoImage.BaseUri, "Assets/icon512.png");
        }
        else
        {
            bitmapImage.UriSource = new Uri(HeaderLogoImage.BaseUri, "Assets/icon512-black.png");
        }
        HeaderLogoImage.Source = bitmapImage;
    }
}
