namespace AddRem.Registry;

/// <summary>
/// The registry implementations the library ships with.
/// </summary>
public static class RegistryRoots
{
    /// <summary>
    /// The real Windows registry. Pass this (or your own <see cref="IRegistryRoot"/>)
    /// to anything that reads the registry; tests pass an in-memory implementation
    /// instead.
    /// </summary>
    public static IRegistryRoot Windows { get; } = new WindowsRegistryRoot();
}
