using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.Windows.Storage;
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
    public sealed partial class SettingsPage : Page
    {
        public SettingsPage()
        {
            this.InitializeComponent();         
        }

        private void OnThemeSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            ApplicationTheme applicationTheme = themeToggleSwitch.IsOn ? ApplicationTheme.Light : ApplicationTheme.Dark;
            AppController.Instance.SaveThemePreference(applicationTheme);
        }

        private void OnThemeSwitch_Loaded(object sender, RoutedEventArgs e)
        {
            //get current value
            ApplicationTheme? theme = AppController.Instance.LoadThemePreference();
            if (theme.HasValue)
            {
                themeToggleSwitch.IsOn = theme.Value == ApplicationTheme.Light; //when page navigate to
            }
            else
            {
                themeToggleSwitch.IsOn = App.Current.RequestedTheme == ApplicationTheme.Light;
            }
        }

        private void RevealModeCheckbox_Changed(object sender, RoutedEventArgs e)
        {
            if (revealModeCheckBox.IsChecked == true)
            {
                passwordBox_userKey.PasswordRevealMode = PasswordRevealMode.Visible;
            }
            else
            {
                passwordBox_userKey.PasswordRevealMode = PasswordRevealMode.Hidden;
            }

        }

        private void passwordBox_userKey_Loaded(object sender, RoutedEventArgs e)
        {
            string password = AppController.Instance.LoadUserKey();
            if (password != null)
            {
                passwordBox_userKey.Password = password;
                revealModeCheckBox.IsEnabled = password.Length == 0;
                buttonPasswordValidation.IsEnabled = password.Length > 0;
            }
        }

        private void passwordBox_userKey_PasswordChanged(object sender, RoutedEventArgs e)
        {
            AppController.Instance.SaveUserKey(passwordBox_userKey.Password);
            if (passwordBox_userKey.Password == string.Empty)
            {
                revealModeCheckBox.IsEnabled = true;
                buttonPasswordValidation.IsEnabled = false;
            }
            else
            {
                buttonPasswordValidation.IsEnabled = true;
            }
        }

        private void buttonPasswordValidation_Click(object sender, RoutedEventArgs e)
        {
            if (passwordBox_userKey.Password == null || passwordBox_userKey.Password == string.Empty)
            {
                return;
            }

        }

        private void buttonPasswordInfo_Click(object sender, RoutedEventArgs e)
        {
            buttonPasswordInfoTeachingTip1.IsOpen = true;

        }
    }
}
