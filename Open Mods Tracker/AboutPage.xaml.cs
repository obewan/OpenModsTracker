using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace OpenModsTracker
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class AboutPage : Page
    {
        public AboutPage()
        {
            this.InitializeComponent();
        }

        private void imageLogo_ActualThemeChanged(FrameworkElement sender, object args)
        {
            getImage();
        }

        private void imageLogo_Loaded(object sender, RoutedEventArgs e)
        {
            getImage();
        }

        private void getImage() {
            BitmapImage bitmapImage = new BitmapImage();
            if (App.Current.RequestedTheme == ApplicationTheme.Dark)
            {
                bitmapImage.UriSource = new Uri(imageLogo.BaseUri, "Assets/icon512.png");
            }
            else
            {
                bitmapImage.UriSource = new Uri(imageLogo.BaseUri, "Assets/icon512-black.png");
            }
            imageLogo.Source = bitmapImage;
        }
    }
}
