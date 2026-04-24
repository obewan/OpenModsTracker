using Microsoft.UI.Xaml;
using Windows.ApplicationModel;

namespace OpenModsTracker
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            ApplicationTheme? theme = AppController.Instance.LoadThemePreference();
            if (theme.HasValue)
            {
                RequestedTheme = theme.Value; //RequestedTheme can only be set once at app loading, or else there will be an exception.
            }
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            m_window = new MainWindow();
            m_window.AppWindow.SetIcon("icon.ico");
            m_window.Activate();
        }


        private Window? m_window;

        internal Window GetWindow() => m_window ?? throw new InvalidOperationException("Main window is not initialized.");

    }
}
