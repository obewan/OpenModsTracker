using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage;
using OpenModsTracker;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.AccessControl;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenModsTracker
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {

        private DesktopAcrylicBackdrop _desktopAcrylicBackdrop;
        private Type _pageType = typeof(HomePage);

        public MainWindow()
        {
            this.InitializeComponent();
            TrySetDesktopAcrylicBackdrop();
        }


        bool TrySetDesktopAcrylicBackdrop()
        {
            if (DesktopAcrylicController.IsSupported())
            {
                _desktopAcrylicBackdrop = new DesktopAcrylicBackdrop();

                this.SystemBackdrop = _desktopAcrylicBackdrop;

                return true; // Succeeded.
            }

            return false; // DesktopAcrylic is not supported on this system.

        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {


            if (args.IsSettingsSelected)
            {
                _pageType = typeof(SettingsPage);
            }
            else if (args.SelectedItemContainer != null)
            {
                var selectedTag = args.SelectedItemContainer.Tag.ToString();
                _pageType = selectedTag switch
                {
                    "HomePage" => typeof(HomePage),
                    "ModsPage" => typeof(ModsPage),
                    "AboutPage" => typeof(AboutPage),
                    _ => null
                };
            }

            contentFrame.Navigate(_pageType);
        }

        private void NavigationView_Loaded(object sender, RoutedEventArgs e)
        {
            contentFrame.Navigate(_pageType);
        }
    }
}
