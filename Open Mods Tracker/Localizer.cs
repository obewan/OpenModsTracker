using Microsoft.Windows.ApplicationModel.Resources;

namespace OpenModsTracker;

public static class Localizer
{
    private static ResourceLoader? _resourceLoader;

    public static string GetString(string key)
    {
        try
        {
            _resourceLoader ??= new ResourceLoader();
            return _resourceLoader.GetString(key);
        }
        catch
        {
            return key; // Fallback
        }
    }
}
