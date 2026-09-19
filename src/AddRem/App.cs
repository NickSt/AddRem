using Microsoft.Win32;

namespace AddRem;

/// <summary>
/// This object represents an installed application. 
/// Any field that directly correlates to a registry value is marked with [Registry Value] in its comment.
/// </summary>
public class App
{
    #region Registry Fields
    /// <summary>
    /// [Registry Value]
    /// This the name of the containing registry key 
    /// It is usually a GUID but it can be an application name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// [Registry Value]
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// [Registry Value]
    /// This is equivalent to "DisplayVersion" in the registry
    /// </summary
    public string? Version { get; set; }

    /// <summary>
    /// [Registry Value]
    /// </summary>
    public string? Publisher { get; set; }

    /// <summary>
    /// [Registry Value]
    /// This is the value that gets shown in Add or Remove Programs
    /// Installed Applications lacking a display name will not be shown in add or remove programs
    /// But they will still be listed in the registry/
    /// </summary>
    public string? DisplayName { get; set; }

    /// <summary>
    /// [Registry Value]
    /// Seldom used except in some microsoft products. Links to a manufacturers help page
    /// </summary>
    public string? HelpLink { get; set; }

    /// <summary>
    /// [Registry Value]
    /// Similar to above, it is seldom used
    /// </summary>
    public string? HelpTelephone {  get; set; }
    
    /// <summary>
    /// [Registry Value]
    /// This is the install date shown in add or remove programs
    /// Note it is stored as yyyymmdd as a string.
    /// </summary>
    public string? InstallDate { get; set; }

    /// <summary>
    /// [Registry Value]
    /// The Installed Location of the app. Note most developers leave it blank, but
    /// Microsoft products have it sometimes.
    /// </summary>
    public string? InstallLocation { get; set; }

    /// <summary>
    /// [Registry Value]
    /// Sometimes this is the location of the source installer files.  This is especially for true for
    /// applications that use packages like visual studio.  The original file used to do the install is here.
    /// Other times its a temporary location that goes away pretty fast.
    /// </summary>
    public string? InstallSource { get; set; }

    /// <summary>
    /// [Registry Value]
    /// A path used to run the modify install.
    /// </summary>
    public string? ModifyPath { get; set; }

    /// <summary>
    /// [Registry Value]
    /// </summary>
    public int? Size { get; set; }

    /// <summary>
    /// [Registry Value]
    /// </summary>
    public bool SystemComponent {  get; set; }

    /// <summary>
    /// [Registry Value]
    /// </summary>
    public string? UninstallString { get; set; }

    /// <summary>
    /// [Registry Value]
    /// </summary>
    public string? URLInfoAbout { get; set; }

    /// <summary>
    /// [Registry Value]
    /// </summary>
    public string? URLUpdateInfo { get; set; }
    #endregion

    public bool Viewable => string.IsNullOrWhiteSpace(DisplayName);

    public App(RegistryKey key)
    {
        Name = GetShortName(key.Name);

        DisplayName = key.GetValue(nameof(DisplayName))?.ToString() ?? string.Empty;
        Publisher = key.GetValue(nameof(Publisher))?.ToString() ?? string.Empty;
        Version = key.GetValue("DisplayVersion")?.ToString() ?? string.Empty;
        HelpLink = key.GetValue(nameof(HelpLink))?.ToString() ?? string.Empty;
        ModifyPath = key.GetValue(nameof(ModifyPath))?.ToString() ?? string.Empty;
        InstallSource = key.GetValue(nameof(InstallSource))?.ToString() ?? string.Empty;
        UninstallString = key.GetValue(nameof(UninstallString))?.ToString() ?? string.Empty;
        URLInfoAbout = key.GetValue(nameof(URLInfoAbout))?.ToString() ?? string.Empty;
        URLUpdateInfo = key.GetValue(nameof(URLUpdateInfo))?.ToString() ?? string.Empty;
        InstallDate = key.GetValue(nameof(InstallDate))?.ToString() ?? string.Empty;

        SystemComponent = DWordIntToBool(key.GetValue(nameof(SystemComponent)));
        Size = (int)(key.GetValue(nameof(Size)) ?? 0);
    }

    private static string GetShortName(string regPath) => regPath[(regPath.LastIndexOf('\\') + 1)..];
    private static bool DWordIntToBool(object? key)
    {
        if(key is null)
            return false;

        var tempVal = (int)key;
        return tempVal != 0;
    }
}
