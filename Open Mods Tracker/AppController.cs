using Microsoft.UI.Xaml;
using Windows.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace OpenModsTracker
{
    internal class AppController
    {
        private static AppController _instance;
        public static AppController Instance => _instance ??= new AppController();

        private const string ThemeSettingKey = "ApplicationTheme";
        private const string SettingsThemeToggleSwitchStateKey = "SettingsThemeToggleSwitchState";

        // Private constructor to enforce singleton pattern
        private AppController()
        {
        }


        // Save the theme preference
        public void SaveThemePreference(ApplicationTheme theme)
        {
            ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] = (int)theme;
        }

        // Load the theme preference from settings
        public ApplicationTheme? LoadThemePreference()
        {
            int? ivalue = ApplicationData.Current.LocalSettings.Values[ThemeSettingKey] as int?;
            if (ivalue.HasValue)
            {             
                return (ApplicationTheme)ivalue.Value;
            }
            return null;
        }
    }
}
