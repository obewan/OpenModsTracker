using Microsoft.UI.Xaml;
using Windows.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace OpenModsTracker
{
    internal class AppController
    {
        private static AppController _instance;
        public static AppController Instance => _instance ??= new AppController();

        private const string ThemeSettingKey = "ApplicationTheme";

        // Private constructor to enforce singleton pattern
        private AppController()
        {
        }

        // Load the theme preference from settings (only on App Loading, not during runtime, or else there will be an exception)
        public void LoadThemePreference()
        {
            int? applicationThemei = ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] as int?;
            if (applicationThemei.HasValue)
            {
                App.Current.RequestedTheme = (ApplicationTheme)applicationThemei.Value;
            }
        }

        // Save the theme preference
        public void SaveThemePreference(ApplicationTheme theme)
        {
            ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] = (int)theme;
            
        }
    }
}
