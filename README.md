# AddRem

A .NET library for enumerating installed programs on Windows — the same list
Settings > Apps & Features shows — without hand-polling the registry.

Getting the installed-program list on Windows means reading four different
registry hive/view combinations, reconciling per-machine against per-user
installs, applying a set of mostly-undocumented rules to work out which entries
a user would actually consider "installed", separately enumerating MSIX/Store
packages, and coping with values whose types vary between machines. The usual
shortcut, WMI's `Win32_Product`, is slow and triggers MSI self-repair.

AddRem does that once, behind one model.

```csharp
foreach (var program in InstalledPrograms.Enumerate())
{
    Console.WriteLine($"{program.DisplayName} {program.RawVersion} ({program.Scope})");
}
```

> **Status: early.** The public API described here is being built out
> milestone by milestone. Nothing is published to NuGet yet.

## Building

Requires the .NET SDK version pinned in `global.json` (10.0.1xx).

```
git clone https://github.com/NickSt/AddRem.git
cd AddRem
git config core.hooksPath .githooks     # enables the pre-push checks
dotnet tool restore
dotnet build AddRem.slnx -c Release
dotnet test AddRem.slnx -c Release
```

## Contributing

Work happens on feature branches merged into `main` by PR, with CI green.
Before pushing:

```
dotnet tool restore
dotnet csharpier check .                # `dotnet csharpier format .` to fix
dotnet build AddRem.slnx -c Release -warnaserror
dotnet test tests/AddRem.Tests/AddRem.Tests.csproj -c Release
```

The `.githooks/pre-push` hook runs exactly these. See [CLAUDE.md](CLAUDE.md) for
the full conventions.

## License

[MIT](LICENSE)
