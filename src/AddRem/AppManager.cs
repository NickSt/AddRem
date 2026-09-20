using Microsoft.Win32;

namespace AddRem;

public static class AppManager
{
    public static List<App> GetAllApps()
    {
        var result = ProcessAppsFromKey(RegKeys.UNINSTALL_KEY);

        if (Environment.Is64BitProcess)
            result.AddRange(ProcessAppsFromKey(RegKeys.UNINSTALL_KEY_6432));

        return result;
    }

    private static List<App> ProcessAppsFromKey(string key)
    {
        using var regkey = Registry.LocalMachine.OpenSubKey(key) ?? throw new Exception();

        var subkeys = regkey.GetSubKeyNames().Select(k => regkey.OpenSubKey(k)).ToList();

        List<App> InstalledApps = [];
        foreach (var subkey in subkeys)
        {
            ArgumentNullException.ThrowIfNull(subkey);
            var app = new App(subkey);

            InstalledApps.Add(app);
        }

        return InstalledApps;
    }
}
